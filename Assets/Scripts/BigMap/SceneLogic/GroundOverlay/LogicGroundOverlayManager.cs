

using System.Collections.Generic;
using My.Map.Logic;
using UnityEngine;

namespace My
{

    // 液体/雾气种类（最大支持4种以匹配 RGBA 通道）
    public enum EGroundElementType 
    { 
        None, 
        GcLiquid,
        Milk,
    }

    // 单个网格的生命周期数据
    public class EGroundElementData
    {
        public EGroundElementType CurrentType = EGroundElementType.None;
        public int RefCount = 0;      // 引用计数（多少个技能/实体在这个格子上放置了该元素）
        public float ExpireTime = 0f; // 倒计时：记录该格子元素将于何时完全消散

        public void Clear()
        {
            CurrentType = EGroundElementType.None;
            RefCount = 0;
            ExpireTime = 0f;
        }
    }

    public class LogicGroundOverlayManager
    {

        protected GameLogicManager logicManager { get;private set; }

        public LogicGroundOverlayManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }

        // 稀疏矩阵：只记录有数据的格子（gridSize精度，所以坐标可以按 /gridSize 放大存为 int）
        private Dictionary<Vector2Int, EGroundElementData> _gridData = new Dictionary<Vector2Int, EGroundElementData>();

        // 精度：1单位等于4格
        private const float GridSize = 0.2f;

        // 渲染层事件回调
        public System.Action<Vector2Int, EGroundElementType> OnCellAdded;
        public System.Action<Vector2Int> OnCellRemoved;

        public void Tick()
        {
            ProcessExpirations();
        }

        // 每一帧检查是否有格子过期
        private void ProcessExpirations()
        {
            float currentTime = Time.time;
            List<Vector2Int> toRemove = new List<Vector2Int>();

            foreach (var kvp in _gridData)
            {
                // 只有引用计数为0（即没有持续施法源），且时间超过过期时间时才消除
                if (kvp.Value.RefCount <= 0 && currentTime >= kvp.Value.ExpireTime)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                RemoveCellImmediate(key);
            }
        }

        // 辅助方法：将世界坐标转换为网格坐标
        public Vector2Int WorldToGrid(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / GridSize),
                Mathf.FloorToInt(worldPos.y / GridSize) // 如果是3D俯视角，这里可能是 worldPos.z
            );
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * GridSize + (GridSize / 2f), gridPos.y * GridSize + (GridSize / 2f), 0);
        }

        /// <summary>
        /// 在指定位置添加地形效果
        /// </summary>
        /// <param name="duration">如果是持续技能填负数(需手动调Remove)，如果是泼水填持续时间</param>
        public void AddElement(Vector2Int gridPos, EGroundElementType type, float duration)
        {
            if (!_gridData.TryGetValue(gridPos, out EGroundElementData cell))
            {
                cell = new EGroundElementData();
                _gridData[gridPos] = cell;
            }

            // 如果是新类型，直接覆盖（互斥原则）
            if (cell.CurrentType != type)
            {
                cell.CurrentType = type;
                cell.RefCount = 0;
                OnCellAdded?.Invoke(gridPos, type); // 通知渲染层生成印花
            }

            // 处理生命周期
            if (duration < 0)
            {
                // 持续性源（比如站在毒圈里），增加引用计数
                cell.RefCount++;
            }
            else
            {
                // 一次性泼洒，刷新过期时间
                cell.ExpireTime = Mathf.Max(cell.ExpireTime, Time.time + duration);
            }
        }

        /// <summary>
        /// 在指定圆形区域内生成地形效果（例如：毒液炸弹、水气球爆炸）
        /// </summary>
        /// <param name="worldCenter">圆心（世界坐标）</param>
        /// <param name="radius">半径（世界坐标单位）</param>
        /// <param name="type">元素类型</param>
        /// <param name="duration">持续时间</param>
        public void AddElementCircle(Vector2 worldCenter, float radius, EGroundElementType type, float duration)
        {
            // 1. 性能优化：预先计算半径的平方，避免在循环内部使用消耗性能的 Mathf.Sqrt
            float sqrRadius = radius * radius;

            // 2. 计算圆形的 AABB 包围盒（确定需要遍历的网格最小/最大索引）
            Vector2Int minGrid = WorldToGrid(new Vector3(worldCenter.x - radius, worldCenter.y - radius, 0));
            Vector2Int maxGrid = WorldToGrid(new Vector3(worldCenter.x + radius, worldCenter.y + radius, 0));

            // 3. 遍历包围盒内的每一个网格
            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    Vector2Int currentGridPos = new Vector2Int(x, y);

                    // 获取当前网格的真实世界中心点
                    Vector3 cellWorldPos = GridToWorld(currentGridPos);

                    // 4. 距离检测：判断网格中心点是否在圆内
                    // 注意：由于是2D平面逻辑，忽略 Z 轴
                    float dx = cellWorldPos.x - worldCenter.x;
                    float dy = cellWorldPos.y - worldCenter.y;
                    float sqrDistance = dx * dx + dy * dy;

                    if (sqrDistance <= sqrRadius)
                    {
                        // 复用之前的单格添加逻辑，自动处理生命周期和通知渲染层
                        AddElement(currentGridPos, type, duration);
                    }
                }
            }
        }

        public HashSet<EGroundElementType> CheckAllLiquidsUnderUnit(Vector3 unitPos, float unitRadius)
        {
            HashSet<EGroundElementType> touchedTypes = new HashSet<EGroundElementType>();

            Vector2Int minGrid = WorldToGrid(new Vector3(unitPos.x - unitRadius, unitPos.y - unitRadius, 0));
            Vector2Int maxGrid = WorldToGrid(new Vector3(unitPos.x + unitRadius, unitPos.y + unitRadius, 0));

            float sqrRadius = unitRadius * unitRadius;
            float halfGrid = GridSize / 2f;

            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    Vector2Int currentPos = new Vector2Int(x, y);

                    if (_gridData.TryGetValue(currentPos, out EGroundElementData cell) && cell.CurrentType != EGroundElementType.None)
                    {
                        // 如果这种液体已经记录过了，为了省性能，直接跳过精确计算
                        if (touchedTypes.Contains(cell.CurrentType)) continue;

                        Vector3 cellCenter = GridToWorld(currentPos);
                        float closestX = Mathf.Clamp(unitPos.x, cellCenter.x - halfGrid, cellCenter.x + halfGrid);
                        float closestY = Mathf.Clamp(unitPos.y, cellCenter.y - halfGrid, cellCenter.y + halfGrid);

                        float dx = unitPos.x - closestX;
                        float dy = unitPos.y - closestY;

                        if ((dx * dx + dy * dy) <= sqrRadius)
                        {
                            touchedTypes.Add(cell.CurrentType);
                        }
                    }
                }
            }
            return touchedTypes;
        }

        /// <summary>
        /// 移除持续性施法源
        /// </summary>
        public void RemoveElementSource(Vector2Int gridPos)
        {
            if (_gridData.TryGetValue(gridPos, out EGroundElementData cell))
            {
                cell.RefCount--;
                if (cell.RefCount <= 0)
                {
                    // 持续源离开，设定几秒后自然消散（例如2秒后消散）
                    cell.ExpireTime = Time.time + 2.0f;
                }
            }
        }

        private void RemoveCellImmediate(Vector2Int gridPos)
        {
            _gridData.Remove(gridPos);
            OnCellRemoved?.Invoke(gridPos); // 通知渲染层销毁印花
        }
    }
}



