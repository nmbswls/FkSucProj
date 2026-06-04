using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.CliffDepth
{
    [CreateAssetMenu(fileName = "CliffDepthRuleTile", menuName = "Map/Tile/Cliff Depth Rule Tile", order = 10)]
    public class CliffDepthRuleTile : RuleTile<CliffDepthRuleTile.Neighbor>
    {
        [Range(0, 15)]
        public int Depth;

        // 同 terrain 的 CliffDepthRuleTile（含子类）在 8 邻格中视为 This
        public string Terrain = string.Empty;

        // 仅在有邻接 cliff 时比较 depth；无 cliff 由 8 邻格 NotThis 表达
        public enum DepthCheck
        {
            DontCare = 0,
            SameDepth = 1,
            GreaterDepth = 2,
            LessDepth = 3,
        }

        public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
        {
        }

        [Serializable]
        public new class TilingRule : RuleTile.TilingRule
        {
            public DepthCheck m_LeftDepthCheck = DepthCheck.DontCare;
            public DepthCheck m_RightDepthCheck = DepthCheck.DontCare;
        }

        public override bool RuleMatch(int neighbor, TileBase other)
        {
            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This:
                    return IsMatchingCliff(other);
                case TilingRuleOutput.Neighbor.NotThis:
                    return !IsMatchingCliff(other);
            }

            return true;
        }

        bool IsMatchingCliff(TileBase other)
        {
            return TryResolveCliff(other, out CliffDepthRuleTile cliff) && HasSameTerrain(cliff);
        }

        protected virtual bool HasSameTerrain(CliffDepthRuleTile other)
        {
            return other != null && Terrain == other.Terrain;
        }

        static bool TryResolveCliff(TileBase tile, out CliffDepthRuleTile cliff)
        {
            cliff = null;
            if (tile is RuleOverrideTile overrideTile)
            {
                tile = overrideTile.m_InstanceTile != null ? overrideTile.m_InstanceTile : overrideTile.m_Tile;
            }

            cliff = tile as CliffDepthRuleTile;
            return cliff != null;
        }

        public override bool RuleMatches(RuleTile.TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
        {
            if (!base.RuleMatches(rule, position, tilemap, ref transform))
            {
                return false;
            }

            if (rule is TilingRule cliffRule)
            {
                return MatchDepthChecks(cliffRule, position, tilemap);
            }

            return true;
        }

        bool MatchDepthChecks(TilingRule rule, Vector3Int position, ITilemap tilemap)
        {
            if (!MatchDepthCheck(rule.m_LeftDepthCheck, Depth, Terrain, position.x - 1, position.y, tilemap))
            {
                return false;
            }

            if (!MatchDepthCheck(rule.m_RightDepthCheck, Depth, Terrain, position.x + 1, position.y, tilemap))
            {
                return false;
            }

            return true;
        }

        static bool MatchDepthCheck(DepthCheck check, int currentDepth, string terrain, int x, int y, ITilemap tilemap)
        {
            if (check == DepthCheck.DontCare)
            {
                return true;
            }

            if (!TryGetCellDepth(tilemap, x, y, terrain, out int neighborDepth))
            {
                return false;
            }

            switch (check)
            {
                case DepthCheck.SameDepth:
                    return neighborDepth == currentDepth;
                case DepthCheck.GreaterDepth:
                    return neighborDepth > currentDepth;
                case DepthCheck.LessDepth:
                    return neighborDepth < currentDepth;
                default:
                    return true;
            }
        }

        static bool TryGetCellDepth(ITilemap tilemap, int x, int y, string terrain, out int depth)
        {
            depth = 0;
            if (!TryResolveCliff(tilemap.GetTile(new Vector3Int(x, y, 0)), out CliffDepthRuleTile cliffTile))
            {
                return false;
            }

            if (cliffTile.Terrain != terrain)
            {
                return false;
            }

            depth = cliffTile.Depth;
            return true;
        }
    }
}
