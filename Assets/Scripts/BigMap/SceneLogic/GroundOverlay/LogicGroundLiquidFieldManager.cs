using System;
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class LogicGroundLiquidFieldManager
    {
        readonly Dictionary<Vector2Int, LiquidFieldChunkData> _chunks = new();
        readonly HashSet<Vector2Int> _dirtyChunks = new();
        readonly List<Vector2Int> _emptyChunks = new();
        readonly List<Vector2Int> _scratchChunks = new();

        public event Action<Vector2Int> OnChunkDirty;

        public LogicGroundLiquidFieldManager(GameLogicManager logicManager)
        {
        }

        public void Tick()
        {
            ProcessFade();
            FlushDirtyChunks();
        }

        public void AddElementCircle(Vector2 worldCenter, float radius, EGroundLiquidType type, float duration)
        {
            if (type == EGroundLiquidType.None || radius <= 0f)
            {
                return;
            }

            float expireAt = duration < 0f ? -1f : Time.time + duration;
            float sqrRadius = radius * radius;

            CollectIntersectingChunks(worldCenter, radius, _scratchChunks);

            for (int c = 0; c < _scratchChunks.Count; c++)
            {
                Vector2Int chunkCoord = _scratchChunks[c];
                var chunk = GetOrCreateChunk(chunkCoord);
                bool changed = false;
                bool touchesEdge = false;

                Vector2 chunkMin = ChunkWorldMin(chunkCoord);

                for (int ty = 0; ty < LiquidFieldChunkData.CoreSize; ty++)
                {
                    float worldY = chunkMin.y + (ty + 0.5f) * LiquidFieldConstants.SubCellWorldSize;
                    float dy = worldY - worldCenter.y;

                    for (int tx = 0; tx < LiquidFieldChunkData.CoreSize; tx++)
                    {
                        float worldX = chunkMin.x + (tx + 0.5f) * LiquidFieldConstants.SubCellWorldSize;
                        float dx = worldX - worldCenter.x;
                        float sqrDist = dx * dx + dy * dy;
                        if (sqrDist > sqrRadius)
                        {
                            continue;
                        }

                        float dist = Mathf.Sqrt(sqrDist);
                        byte weight = ComputeWeight(dist, radius);
                        if (weight == 0)
                        {
                            continue;
                        }

                        WriteSubCell(chunk, tx, ty, type, weight, expireAt);
                        changed = true;
                        if (tx == 0 || ty == 0 ||
                            tx == LiquidFieldChunkData.CoreSize - 1 ||
                            ty == LiquidFieldChunkData.CoreSize - 1)
                        {
                            touchesEdge = true;
                        }
                    }
                }

                if (changed)
                {
                    MarkChunkDirty(chunkCoord, touchesEdge);
                }
                else if (!chunk.HasVisibleContent())
                {
                    _emptyChunks.Add(chunkCoord);
                }
            }

            RemoveEmptyChunks();
            FlushDirtyChunks();
        }

        public HashSet<EGroundLiquidType> CheckAllLiquidsUnderUnit(Vector3 unitPos, float unitRadius)
        {
            var touchedTypes = new HashSet<EGroundLiquidType>();
            float sqrRadius = unitRadius * unitRadius;
            byte threshold = LiquidFieldConstants.LiquidIntensityThreshold;

            Vector2Int minGlobal = WorldToGlobalSubCell(new Vector2(unitPos.x - unitRadius, unitPos.y - unitRadius));
            Vector2Int maxGlobal = WorldToGlobalSubCell(new Vector2(unitPos.x + unitRadius, unitPos.y + unitRadius));

            for (int gy = minGlobal.y; gy <= maxGlobal.y; gy++)
            {
                float worldY = (gy + 0.5f) * LiquidFieldConstants.SubCellWorldSize;
                float dy = worldY - unitPos.y;

                for (int gx = minGlobal.x; gx <= maxGlobal.x; gx++)
                {
                    float worldX = (gx + 0.5f) * LiquidFieldConstants.SubCellWorldSize;
                    float dx = worldX - unitPos.x;
                    if (dx * dx + dy * dy > sqrRadius)
                    {
                        continue;
                    }

                    if (!TrySampleGlobalSubCell(gx, gy, out byte intensity, out EGroundLiquidType type))
                    {
                        continue;
                    }

                    if (intensity > threshold && type != EGroundLiquidType.None)
                    {
                        touchedTypes.Add(type);
                    }
                }
            }

            return touchedTypes;
        }

        public bool TryGetCoreTexel(Vector2Int chunkCoord, int tx, int ty, out byte intensity, out EGroundLiquidType type)
        {
            intensity = 0;
            type = EGroundLiquidType.None;

            if (tx < 0 || ty < 0 || tx >= LiquidFieldChunkData.CoreSize || ty >= LiquidFieldChunkData.CoreSize)
            {
                return false;
            }

            if (!_chunks.TryGetValue(chunkCoord, out var chunk))
            {
                return false;
            }

            int idx = LiquidFieldChunkData.ToIndex(tx, ty);
            intensity = chunk.Intensities[idx];
            type = (EGroundLiquidType)chunk.Types[idx];
            return intensity > 0 && type != EGroundLiquidType.None;
        }

        public bool TryGetChunk(Vector2Int chunkCoord, out LiquidFieldChunkData chunk)
        {
            return _chunks.TryGetValue(chunkCoord, out chunk);
        }

        public void ClearAll()
        {
            _chunks.Clear();
            _dirtyChunks.Clear();
            _emptyChunks.Clear();
        }

        public static Vector2 ChunkWorldMin(Vector2Int chunkCoord)
        {
            return new Vector2(
                chunkCoord.x * LiquidFieldConstants.ChunkWorldSize,
                chunkCoord.y * LiquidFieldConstants.ChunkWorldSize);
        }

        public static Vector2Int WorldToGlobalSubCell(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / LiquidFieldConstants.SubCellWorldSize),
                Mathf.FloorToInt(worldPos.y / LiquidFieldConstants.SubCellWorldSize));
        }

        public static Vector2Int GlobalSubCellToChunk(Vector2Int globalSubCell)
        {
            return new Vector2Int(
                LiquidFieldConstants.FloorDiv(globalSubCell.x, LiquidFieldConstants.ChunkTexSize),
                LiquidFieldConstants.FloorDiv(globalSubCell.y, LiquidFieldConstants.ChunkTexSize));
        }

        public static int GlobalSubCellToLocal(int global, int chunkAxis)
        {
            return global - chunkAxis * LiquidFieldConstants.ChunkTexSize;
        }

        static byte ComputeWeight(float dist, float radius)
        {
            if (radius <= 0f)
            {
                return 0;
            }

            float t = 1f - dist / radius;
            if (t <= 0f)
            {
                return 0;
            }

            int weight = Mathf.RoundToInt(LiquidFieldConstants.PeakIntensity * t);
            return (byte)Mathf.Clamp(weight, 0, LiquidFieldConstants.PeakIntensity);
        }

        LiquidFieldChunkData GetOrCreateChunk(Vector2Int chunkCoord)
        {
            if (!_chunks.TryGetValue(chunkCoord, out var chunk))
            {
                chunk = new LiquidFieldChunkData();
                _chunks[chunkCoord] = chunk;
            }

            return chunk;
        }

        static void WriteSubCell(LiquidFieldChunkData chunk, int tx, int ty, EGroundLiquidType type, byte weight, float expireAt)
        {
            int idx = LiquidFieldChunkData.ToIndex(tx, ty);
            byte existingType = chunk.Types[idx];

            if (existingType == (byte)type)
            {
                int sum = chunk.Intensities[idx] + weight;
                chunk.Intensities[idx] = (byte)Mathf.Min(sum, LiquidFieldConstants.PeakIntensity);
            }
            else
            {
                chunk.Types[idx] = (byte)type;
                chunk.Intensities[idx] = weight;
            }

            if (expireAt < 0f)
            {
                chunk.ExpireTimes[idx] = -1f;
            }
            else
            {
                chunk.ExpireTimes[idx] = Mathf.Max(chunk.ExpireTimes[idx], expireAt);
            }
        }

        void ProcessFade()
        {
            float now = Time.time;

            foreach (var kvp in _chunks)
            {
                var chunk = kvp.Value;
                bool chunkChanged = false;
                bool touchesEdge = false;

                for (int ty = 0; ty < LiquidFieldChunkData.CoreSize; ty++)
                {
                    for (int tx = 0; tx < LiquidFieldChunkData.CoreSize; tx++)
                    {
                        int idx = LiquidFieldChunkData.ToIndex(tx, ty);
                        if (chunk.Intensities[idx] == 0)
                        {
                            continue;
                        }

                        float expireAt = chunk.ExpireTimes[idx];
                        if (expireAt < 0f || now < expireAt)
                        {
                            continue;
                        }

                        float fadeEnd = expireAt + LiquidFieldConstants.SourceFadeDelaySeconds;
                        if (now >= fadeEnd)
                        {
                            chunk.Intensities[idx] = 0;
                            chunk.Types[idx] = 0;
                            chunk.ExpireTimes[idx] = 0f;
                        }
                        else
                        {
                            int next = chunk.Intensities[idx] -
                                       Mathf.Max(1, Mathf.RoundToInt(LiquidFieldConstants.FadeSpeedPerSecond * Time.deltaTime));
                            if (next <= 0)
                            {
                                chunk.Intensities[idx] = 0;
                                chunk.Types[idx] = 0;
                                chunk.ExpireTimes[idx] = 0f;
                            }
                            else
                            {
                                chunk.Intensities[idx] = (byte)next;
                            }
                        }

                        chunkChanged = true;
                        if (tx == 0 || ty == 0 ||
                            tx == LiquidFieldChunkData.CoreSize - 1 ||
                            ty == LiquidFieldChunkData.CoreSize - 1)
                        {
                            touchesEdge = true;
                        }
                    }
                }

                if (chunkChanged)
                {
                    MarkChunkDirty(kvp.Key, touchesEdge);

                    if (!chunk.HasVisibleContent())
                    {
                        _emptyChunks.Add(kvp.Key);
                    }
                }
            }

            RemoveEmptyChunks();
        }

        void CollectIntersectingChunks(Vector2 worldCenter, float radius, List<Vector2Int> result)
        {
            result.Clear();

            Vector2 min = worldCenter - Vector2.one * radius;
            Vector2 max = worldCenter + Vector2.one * radius;

            Vector2Int minChunk = GlobalSubCellToChunk(WorldToGlobalSubCell(min));
            Vector2Int maxChunk = GlobalSubCellToChunk(WorldToGlobalSubCell(max));

            for (int cy = minChunk.y; cy <= maxChunk.y; cy++)
            {
                for (int cx = minChunk.x; cx <= maxChunk.x; cx++)
                {
                    result.Add(new Vector2Int(cx, cy));
                }
            }
        }

        bool TrySampleGlobalSubCell(int gx, int gy, out byte intensity, out EGroundLiquidType type)
        {
            var chunkCoord = GlobalSubCellToChunk(new Vector2Int(gx, gy));
            int tx = GlobalSubCellToLocal(gx, chunkCoord.x);
            int ty = GlobalSubCellToLocal(gy, chunkCoord.y);
            return TryGetCoreTexel(chunkCoord, tx, ty, out intensity, out type);
        }

        void MarkChunkDirty(Vector2Int chunkCoord, bool touchesEdge)
        {
            _dirtyChunks.Add(chunkCoord);

            if (!touchesEdge)
            {
                return;
            }

            _dirtyChunks.Add(chunkCoord + Vector2Int.left);
            _dirtyChunks.Add(chunkCoord + Vector2Int.right);
            _dirtyChunks.Add(chunkCoord + Vector2Int.up);
            _dirtyChunks.Add(chunkCoord + Vector2Int.down);
            _dirtyChunks.Add(chunkCoord + new Vector2Int(-1, -1));
            _dirtyChunks.Add(chunkCoord + new Vector2Int(1, -1));
            _dirtyChunks.Add(chunkCoord + new Vector2Int(-1, 1));
            _dirtyChunks.Add(chunkCoord + new Vector2Int(1, 1));
        }

        void FlushDirtyChunks()
        {
            if (_dirtyChunks.Count == 0)
            {
                return;
            }

            foreach (var chunkCoord in _dirtyChunks)
            {
                OnChunkDirty?.Invoke(chunkCoord);
            }

            _dirtyChunks.Clear();
        }

        void RemoveEmptyChunks()
        {
            for (int i = 0; i < _emptyChunks.Count; i++)
            {
                _chunks.Remove(_emptyChunks[i]);
                OnChunkDirty?.Invoke(_emptyChunks[i]);
            }

            _emptyChunks.Clear();
        }
    }
}
