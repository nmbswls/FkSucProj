using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Ground
{
    public struct LogicHeightProbeInput
    {
        public Vector2 Pos;
        public float CurrentLogicY;
        public float MaxDownSearch;
        public int PreferredGroundLevel;
        public bool IsFlying;
    }

    public struct LogicHeightProbeResult
    {
        public bool Found;
        public float LogicY;
        public int GroundLevel;
        public bool IsSlope;
        public float SlopeT;
    }

    public interface ILogicHeightResolver
    {
        LogicHeightProbeResult Probe(in LogicHeightProbeInput input, MapLogicHeightConfig config, Tilemap[] groundLayers);
    }

    // v1：北高南低 Slope 格内 lerp；向下探测完整版见 ProbeDownwardFromLogicY 注释（M2）。
    public class LogicHeightResolver : ILogicHeightResolver
    {
        public static readonly LogicHeightResolver Instance = new();

        const float SupportEpsilon = 0.05f;

        public LogicHeightProbeResult Probe(in LogicHeightProbeInput input, MapLogicHeightConfig config, Tilemap[] groundLayers)
        {
            var result = new LogicHeightProbeResult();
            if (config == null || groundLayers == null || groundLayers.Length == 0 || input.IsFlying)
            {
                return result;
            }

            config.BuildRuntimeLookup(out var groundLookup, out var slopeLookup);

            float bestLogicY = float.MinValue;
            int bestLevel = -1;
            bool bestIsSlope = false;
            float bestSlopeT = 0f;
            bool found = false;

            var worldProbe = new Vector3(input.Pos.x, input.Pos.y, 0f);

            foreach (var layer in groundLayers)
            {
                if (layer == null)
                {
                    continue;
                }

                if (!TrySampleCell(layer, worldProbe, out var cell, out var tile) || tile == null)
                {
                    continue;
                }

                if (slopeLookup.TryGetValue(tile, out var slopeDef))
                {
                    if (!TryEvaluateNorthHighSlope(layer, cell, input.Pos, slopeDef, config, out float slopeY, out float t))
                    {
                        continue;
                    }

                    if (slopeY > input.CurrentLogicY + SupportEpsilon)
                    {
                        continue;
                    }

                    if (!found || slopeY > bestLogicY)
                    {
                        found = true;
                        bestLogicY = slopeY;
                        bestLevel = slopeDef.FromLevel;
                        bestIsSlope = true;
                        bestSlopeT = t;
                    }

                    continue;
                }

                if (!groundLookup.TryGetValue(tile, out int groundLevel))
                {
                    continue;
                }

                float groundY = config.GetLevelHeight(groundLevel);
                if (groundY > input.CurrentLogicY + SupportEpsilon)
                {
                    continue;
                }

                if (!found || groundY > bestLogicY)
                {
                    found = true;
                    bestLogicY = groundY;
                    bestLevel = groundLevel;
                    bestIsSlope = false;
                    bestSlopeT = 0f;
                }
            }

            if (!found)
            {
                return result;
            }

            result.Found = true;
            result.LogicY = bestLogicY;
            result.GroundLevel = bestLevel;
            result.IsSlope = bestIsSlope;
            result.SlopeT = bestSlopeT;
            return result;
        }

        static bool TrySampleCell(Tilemap layer, Vector3 worldProbe, out Vector3Int cell, out TileBase tile)
        {
            cell = layer.WorldToCell(worldProbe);
            if (!layer.cellBounds.Contains(cell))
            {
                tile = null;
                return false;
            }

            tile = layer.GetTile(cell);
            return tile != null;
        }

        // 北高南低：南缘 Hn，北缘 Hn+1
        static bool TryEvaluateNorthHighSlope(
            Tilemap layer,
            Vector3Int cell,
            Vector2 pos,
            MapLogicHeightConfig.SlopeTileEntry slope,
            MapLogicHeightConfig config,
            out float logicY,
            out float t)
        {
            logicY = 0f;
            t = 0f;

            if (slope.ToLevel != slope.FromLevel + 1)
            {
                return false;
            }

            float fromY = config.GetLevelHeight(slope.FromLevel);
            float toY = config.GetLevelHeight(slope.ToLevel);

            var grid = layer.layoutGrid;
            var cellSize = grid != null ? grid.cellSize : Vector3.one;
            var cellMinWorld = layer.CellToWorld(cell);
            float cellMinY = cellMinWorld.y;
            float cellHeight = Mathf.Abs(cellSize.y * layer.transform.lossyScale.y);
            if (cellHeight <= 1e-5f)
            {
                logicY = fromY;
                return true;
            }

            t = Mathf.Clamp01((pos.y - cellMinY) / cellHeight);
            logicY = Mathf.Lerp(fromY, toY, t);
            return true;
        }

        /*
         * M2: ProbeDownwardFromLogicY 完整算法
         * 1. 在 Pos 处遍历 GroundLayers 采样 Ground / Slope tile
         * 2. Ground -> candidateLogicY = GetLevelHeight(level)
         * 3. Slope（北高南低）-> EvaluateNorthHighSlope
         * 4. 过滤 candidateLogicY <= CurrentLogicY + epsilon
         * 5. 取 candidateLogicY 最大者；同高 PreferredGroundLevel 优先
         * 6. 非 Teleport/JumpDown：MoveTowards 限速
         */
    }
}
