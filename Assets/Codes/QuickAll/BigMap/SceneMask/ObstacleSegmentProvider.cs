using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Map.Logic.Chunk;
using Unity.VisualScripting;
using UnityEngine;
using static SceneAOIManager;
using static UnityEngine.Rendering.HableCurve;

namespace Map.Scene.Fov
{
    [Serializable]
    public struct Segment2D
    {
        public int segmentId;
        public Vector2 a;
        public Vector2 b;
        //public object source; // 可选：来源对象，比如 BoxCollider2D
        public Bounds bounds; // AABB，用于网格覆盖

        
        public Segment2D(int segmentId, Vector2 a, Vector2 b)
        {
            this.segmentId = segmentId;
            this.a = a;
            this.b = b;
            //this.source = source;
            var min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            var max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            // Z 维保持 0
            this.bounds = new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0f));
        }

        // 修改端点后需调用
        public void RecomputeBounds()
        {
            var min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            var max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            bounds = new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0f));
        }

        // 最近距离到点（平方距离），用于圆形精筛
        public float SqrDistanceToPoint(Vector2 p)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 1e-8f ? Vector2.Dot(p - a, ab) / ab.sqrMagnitude : 0f;
            t = Mathf.Clamp01(t);
            Vector2 proj = a + t * ab;
            return (proj - p).sqrMagnitude;
        }
    }

    public class SegmentGridIndex
    {
        private readonly float cellSize;
        private readonly Dictionary<Vector2Int, List<int>> buckets = new Dictionary<Vector2Int, List<int>>();
        private readonly Dictionary<int, List<Vector2Int>> segmentCoveredCells = new Dictionary<int, List<Vector2Int>>();

        // 内部段存储与活跃标记
        private readonly List<Segment2D> segments;
        private readonly List<bool> active;

        // 复用容器
        private readonly List<Vector2Int> tmpCells = new List<Vector2Int>();

        public SegmentGridIndex(float cellSize)
        {
            this.cellSize = Mathf.Max(1e-4f, cellSize);
            segments = new List<Segment2D>();
            active = new List<bool>();
        }

        // 如需用已有段列表初始化，也提供这个构造
        public SegmentGridIndex(IList<Segment2D> initialSegments, float cellSize) : this(cellSize)
        {
            if (initialSegments != null && initialSegments.Count > 0)
                AddSegments(initialSegments, out _);
        }

        public int Count => segments.Count;
        public Segment2D GetSegment(int idx) => segments[idx];
        public bool IsActive(int idx) => idx >= 0 && idx < active.Count && active[idx];

        // 动态添加单个段：返回其索引
        public int AddSegment(Segment2D s)
        {
            // 保证 bounds 有效
            if (s.bounds.size.x == 0f && s.bounds.size.y == 0f)
            {
                var min = new Vector2(Mathf.Min(s.a.x, s.b.x), Mathf.Min(s.a.y, s.b.y));
                var max = new Vector2(Mathf.Max(s.a.x, s.b.x), Mathf.Max(s.a.y, s.b.y));
                s.bounds = new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0f));
            }

            segments.Add(s);
            active.Add(true);
            int idx = segments.Count - 1;

            tmpCells.Clear();
            ComputeCoveredCells(s.bounds, tmpCells);
            for (int k = 0; k < tmpCells.Count; k++)
                AddToBucket(tmpCells[k], idx);

            segmentCoveredCells[idx] = new List<Vector2Int>(tmpCells);
            return idx;
        }

        // 动态批量添加：返回每个段的索引
        public void AddSegments(IList<Segment2D> input, out List<int> outIndices)
        {
            outIndices = new List<int>(input?.Count ?? 0);
            if (input == null || input.Count == 0) return;

            for (int i = 0; i < input.Count; i++)
            {
                int idx = AddSegment(input[i]);
                outIndices.Add(idx);
            }
        }

        // 更新既有段的几何（按索引覆盖），并增量更新其网格覆盖
        public void UpdateSegment(int idx, Segment2D s)
        {
            if (idx < 0 || idx >= segments.Count || !active[idx]) return;

            // 写入新几何
            segments[idx] = s;

            // 旧覆盖移除
            if (segmentCoveredCells.TryGetValue(idx, out var oldCells))
            {
                for (int k = 0; k < oldCells.Count; k++)
                    RemoveFromBucket(oldCells[k], idx);
            }

            // 新覆盖加入
            tmpCells.Clear();
            ComputeCoveredCells(s.bounds, tmpCells);
            for (int k = 0; k < tmpCells.Count; k++)
                AddToBucket(tmpCells[k], idx);

            segmentCoveredCells[idx] = new List<Vector2Int>(tmpCells);
        }

        // 批量更新
        public void UpdateSegments(IList<int> indices, IList<Segment2D> newValues)
        {
            if (indices == null || newValues == null) return;
            int n = Mathf.Min(indices.Count, newValues.Count);
            for (int i = 0; i < n; i++)
                UpdateSegment(indices[i], newValues[i]);
        }

        // 动态移除（逻辑失活 + 清桶）
        public void RemoveSegment(int idx)
        {
            if (idx < 0 || idx >= segments.Count || !active[idx]) return;

            if (segmentCoveredCells.TryGetValue(idx, out var cells))
            {
                for (int k = 0; k < cells.Count; k++)
                    RemoveFromBucket(cells[k], idx);
                segmentCoveredCells.Remove(idx);
            }
            active[idx] = false;
        }

        // 清空并重置
        public void Clear()
        {
            buckets.Clear();
            segmentCoveredCells.Clear();
            segments.Clear();
            active.Clear();
        }

        // 查询接口示例（你已有的实现可复用）
        public void QueryCircle(Vector2 center, float radius, HashSet<int> results)
        {
            results.Clear();
            // 根据圆的 AABB 找到覆盖的格子，然后合并 bucket
            Bounds bb = new Bounds(center, new Vector3(radius * 2f, radius * 2f, 0f));
            tmpCells.Clear();
            ComputeCoveredCells(bb, tmpCells);
            for (int i = 0; i < tmpCells.Count; i++)
            {
                if (!buckets.TryGetValue(tmpCells[i], out var list)) continue;
                for (int k = 0; k < list.Count; k++)
                {
                    int idx = list[k];
                    if (active[idx]) results.Add(idx);
                }
            }
        }

        public void QueryRay(Vector2 origin, Vector2 dir, float maxDist, HashSet<int> results)
        {
            results.Clear();
            // 生成射线的 AABB（保守包围），查询覆盖格子
            var end = origin + dir.normalized * maxDist;
            var min = new Vector2(Mathf.Min(origin.x, end.x), Mathf.Min(origin.y, end.y));
            var max = new Vector2(Mathf.Max(origin.x, end.x), Mathf.Max(origin.y, end.y));
            Bounds bb = new Bounds((min + max) * 0.5f, new Vector3(max.x - min.x, max.y - min.y, 0f));

            tmpCells.Clear();
            ComputeCoveredCells(bb, tmpCells);
            for (int i = 0; i < tmpCells.Count; i++)
            {
                if (!buckets.TryGetValue(tmpCells[i], out var list)) continue;
                for (int k = 0; k < list.Count; k++)
                {
                    int idx = list[k];
                    if (active[idx]) results.Add(idx);
                }
            }
        }

        // 下面是你已有的工具：覆盖格子计算与桶操作
        private void ComputeCoveredCells(Bounds bb, List<Vector2Int> outCells)
        {
            outCells.Clear();
            Vector2 min = bb.min;
            Vector2 max = bb.max;
            int x0 = Mathf.FloorToInt(min.x / cellSize);
            int y0 = Mathf.FloorToInt(min.y / cellSize);
            int x1 = Mathf.FloorToInt(max.x / cellSize);
            int y1 = Mathf.FloorToInt(max.y / cellSize);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    outCells.Add(new Vector2Int(x, y));
                }
            }
        }

        private void AddToBucket(Vector2Int cell, int idx)
        {
            if (!buckets.TryGetValue(cell, out var list))
            {
                list = new List<int>(8);
                buckets[cell] = list;
            }
            list.Add(idx);
        }

        private void RemoveFromBucket(Vector2Int cell, int idx)
        {
            if (!buckets.TryGetValue(cell, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == idx)
                {
                    list.RemoveAt(i);
                    break;
                }
            }
            if (list.Count == 0) buckets.Remove(cell);
        }
    }


    public class ObstacleSegmentProvider : MonoBehaviour
    {
        private SegmentGridIndex segentGridIndex;    // 只读索引

        // 复用容器
        private readonly HashSet<int> tmpIdxSet = new HashSet<int>();
        private readonly List<int> tmpList = new List<int>();

        // 源管理：sourceId -> indices
        private readonly Dictionary<string, List<int>> sourceMap = new Dictionary<string, List<int>>();

        void Awake()
        {
            // 初始化动态索引
            segentGridIndex = new SegmentGridIndex(5f);
        }

        public void OnAreaEnter()
        {
            segentGridIndex.Clear();
            sourceMap.Clear();

        }

        public Segment2D GetSegment(int idx)
        {
            return segentGridIndex.GetSegment(idx);
        }

        // 外部查询：圆形范围候选（合并静态+动态）
        public void QueryCircle(Vector2 center, float radius, HashSet<int> results)
        {
            results.Clear();

            segentGridIndex.QueryCircle(center, radius, results);

        }

        // 外部查询：光线沿途候选（合并静态+动态）
        public void QueryRay(Vector2 origin, Vector2 dir, float maxDist, HashSet<int> results)
        {
            results.Clear();
            segentGridIndex.QueryRay(origin, dir, maxDist, results);
        }

        // 添加一批段，返回其全局索引列表（供 AOI 记录）
        public void AddSegments(string sourceId, IEnumerable<Segment2D> inputs)
        {
            if (!sourceMap.TryGetValue(sourceId, out var indices))
            {
                indices = new List<int>();
                sourceMap[sourceId] = indices;
            }

            foreach (var seg in inputs)
            {
                // 确保 bounds 有效
                if (seg.bounds.size.x == 0f && seg.bounds.size.y == 0f)
                {
                    seg.RecomputeBounds();
                }
                segentGridIndex.AddSegment(seg);
                indices.Add(seg.segmentId);
            }

            Debug.Log("AddSegments now seg count:" + segentGridIndex.Count);
        }

        // 按来源整批移除
        public void RemoveSource(string sourceId)
        {
            if (!sourceMap.TryGetValue(sourceId, out var indices)) return;
            RemoveSegments(indices);
            sourceMap.Remove(sourceId);

            Debug.Log("RemoveSource now seg count:" + segentGridIndex.Count);
        }


        // 移除一批段（逻辑失活）
        public void RemoveSegments(IList<int> segmentIndices)
        {
            if (segmentIndices == null || segmentIndices.Count == 0) return;
            for (int i = 0; i < segmentIndices.Count; i++)
            {
                int idx = segmentIndices[i];
                segentGridIndex.RemoveSegment(idx);
            }
        }
    }
}



