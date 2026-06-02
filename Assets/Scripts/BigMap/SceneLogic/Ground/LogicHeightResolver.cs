using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Ground
{
    public struct LogicHeightProbeInput
    {
        public Vector2 Pos;
        public float CurrentLogicY;
        public float MaxDownSearch;
        // 上一帧支撑面高度，用于同高重叠消歧（M2）
        public float PreferredSupportLogicY;
        public bool IsFlying;
    }

    public struct LogicHeightProbeResult
    {
        public bool Found;
        public float LogicY;
        public bool IsSlope;
        public float SlopeT;
    }

    public interface ILogicHeightResolver
    {
        LogicHeightProbeResult Probe(in LogicHeightProbeInput input, MapLogicHeightConfig config, Tilemap[] groundLayers);
    }

    // 北高南低 Slope：SouthLogicY → NorthLogicY 格内 lerp
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
                    if (!TryEvaluateNorthHighSlope(layer, cell, input.Pos, slopeDef, out float slopeY, out float t))
                    {
                        continue;
                    }

                    // Slope：格内连续高度，按 Pos 直接取支撑面，不做「高于 CurrentLogicY 则拒绝」
                    if (!found || slopeY > bestLogicY)
                    {
                        found = true;
                        bestLogicY = slopeY;
                        bestIsSlope = true;
                        bestSlopeT = t;
                    }

                    continue;
                }

                if (!groundLookup.TryGetValue(tile, out float groundY))
                {
                    continue;
                }

                // 平地：不可无跳跃踩上更高平台（M2 向下探测语义）
                if (groundY > input.CurrentLogicY + SupportEpsilon)
                {
                    continue;
                }

                if (!found || groundY > bestLogicY)
                {
                    found = true;
                    bestLogicY = groundY;
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

        static bool TryEvaluateNorthHighSlope(
            Tilemap layer,
            Vector3Int cell,
            Vector2 pos,
            MapLogicHeightConfig.SlopeTileEntry slope,
            out float logicY,
            out float t)
        {
            logicY = 0f;
            t = 0f;

            float southLogicY = slope.SouthLogicY;
            float northLogicY = slope.NorthLogicY;
            if (northLogicY <= southLogicY + 1e-5f)
            {
                return false;
            }

            // 格内南缘(minY) → 北缘(maxY)，按 Pos.y 在 world 范围内插值 LogicY
            float cellMinY = layer.CellToWorld(cell).y;
            float cellMaxY = layer.CellToWorld(cell + new Vector3Int(0, 1, 0)).y;
            float cellSpanY = cellMaxY - cellMinY;
            if (Mathf.Abs(cellSpanY) <= 1e-5f)
            {
                logicY = southLogicY;
                return true;
            }

            // 北高南低：南缘 t=0 → SouthLogicY，北缘 t=1 → NorthLogicY
            t = Mathf.Clamp01((pos.y - cellMinY) / cellSpanY);
            logicY = Mathf.Lerp(southLogicY, northLogicY, t);
            return true;
        }

        /*
         * M2: ProbeDownwardFromLogicY
         * 过滤 candidateLogicY <= CurrentLogicY + epsilon，取最大支撑面；
         * 同高时 PreferredSupportLogicY 优先。
         */
    }
}
