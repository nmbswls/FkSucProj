using System;
using System.Collections.Generic;
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

        // RuleTile.m_TilingRules 为基类列表，派生 TilingRule 的字段无法持久化；按 m_Id 绑定 depth
        [Serializable]
        public struct RuleDepthEntry
        {
            public int m_RuleId;
            public DepthCheck m_Left;
            public DepthCheck m_Right;
        }

        [SerializeField, HideInInspector]
        List<RuleDepthEntry> m_RuleDepths = new List<RuleDepthEntry>();

        // 旧版按索引存储，仅用于一次性迁移
        [SerializeField, HideInInspector]
        List<DepthCheck> m_LeftDepthChecks = new List<DepthCheck>();

        [SerializeField, HideInInspector]
        List<DepthCheck> m_RightDepthChecks = new List<DepthCheck>();

        [Serializable]
        public new class TilingRule : RuleTile.TilingRule
        {
            public DepthCheck m_LeftDepthCheck = DepthCheck.DontCare;
            public DepthCheck m_RightDepthCheck = DepthCheck.DontCare;

            public new TilingRule Clone()
            {
                var rule = new TilingRule
                {
                    m_Id = m_Id,
                    m_Neighbors = new List<int>(m_Neighbors),
                    m_NeighborPositions = new List<Vector3Int>(m_NeighborPositions),
                    m_RuleTransform = m_RuleTransform,
                    m_Sprites = new Sprite[m_Sprites.Length],
                    m_GameObject = m_GameObject,
                    m_MinAnimationSpeed = m_MinAnimationSpeed,
                    m_MaxAnimationSpeed = m_MaxAnimationSpeed,
                    m_PerlinScale = m_PerlinScale,
                    m_Output = m_Output,
                    m_ColliderType = m_ColliderType,
                    m_RandomTransform = m_RandomTransform,
                    m_LeftDepthCheck = m_LeftDepthCheck,
                    m_RightDepthCheck = m_RightDepthCheck,
                };
                Array.Copy(m_Sprites, rule.m_Sprites, m_Sprites.Length);
                return rule;
            }
        }

        public DepthCheck GetLeftDepth(int ruleId)
        {
            int index = FindDepthEntryIndex(ruleId);
            return index >= 0 ? m_RuleDepths[index].m_Left : DepthCheck.DontCare;
        }

        public DepthCheck GetRightDepth(int ruleId)
        {
            int index = FindDepthEntryIndex(ruleId);
            return index >= 0 ? m_RuleDepths[index].m_Right : DepthCheck.DontCare;
        }

        public void SetLeftDepth(int ruleId, DepthCheck value)
        {
            UpsertDepthEntry(ruleId, value, GetRightDepth(ruleId));
        }

        public void SetRightDepth(int ruleId, DepthCheck value)
        {
            UpsertDepthEntry(ruleId, GetLeftDepth(ruleId), value);
        }

        public void SetRuleDepth(int ruleId, DepthCheck left, DepthCheck right)
        {
            UpsertDepthEntry(ruleId, left, right);
        }

        public void EnsureDepthEntriesForAllRules()
        {
            if (m_TilingRules == null)
            {
                return;
            }

            for (int i = 0; i < m_TilingRules.Count; i++)
            {
                int ruleId = m_TilingRules[i].m_Id;
                if (FindDepthEntryIndex(ruleId) < 0)
                {
                    m_RuleDepths.Add(new RuleDepthEntry
                    {
                        m_RuleId = ruleId,
                        m_Left = DepthCheck.DontCare,
                        m_Right = DepthCheck.DontCare,
                    });
                }
            }

            PruneOrphanDepthEntries();
        }

        void UpsertDepthEntry(int ruleId, DepthCheck left, DepthCheck right)
        {
            int index = FindDepthEntryIndex(ruleId);
            if (index >= 0)
            {
                var entry = m_RuleDepths[index];
                entry.m_Left = left;
                entry.m_Right = right;
                m_RuleDepths[index] = entry;
                return;
            }

            m_RuleDepths.Add(new RuleDepthEntry
            {
                m_RuleId = ruleId,
                m_Left = left,
                m_Right = right,
            });
        }

        int FindDepthEntryIndex(int ruleId)
        {
            for (int i = 0; i < m_RuleDepths.Count; i++)
            {
                if (m_RuleDepths[i].m_RuleId == ruleId)
                {
                    return i;
                }
            }

            return -1;
        }

        void PruneOrphanDepthEntries()
        {
            if (m_TilingRules == null || m_TilingRules.Count == 0)
            {
                m_RuleDepths.Clear();
                return;
            }

            var validIds = new HashSet<int>();
            foreach (var rule in m_TilingRules)
            {
                validIds.Add(rule.m_Id);
            }

            for (int i = m_RuleDepths.Count - 1; i >= 0; i--)
            {
                if (!validIds.Contains(m_RuleDepths[i].m_RuleId))
                {
                    m_RuleDepths.RemoveAt(i);
                }
            }
        }

        void MigrateLegacyIndexDepthIfNeeded()
        {
            if (m_LeftDepthChecks == null || m_LeftDepthChecks.Count == 0)
            {
                return;
            }

            if (m_TilingRules == null)
            {
                m_LeftDepthChecks.Clear();
                m_RightDepthChecks.Clear();
                return;
            }

            int count = Mathf.Min(m_TilingRules.Count, m_LeftDepthChecks.Count);
            for (int i = 0; i < count; i++)
            {
                var right = i < m_RightDepthChecks.Count ? m_RightDepthChecks[i] : DepthCheck.DontCare;
                SetRuleDepth(m_TilingRules[i].m_Id, m_LeftDepthChecks[i], right);
            }

            m_LeftDepthChecks.Clear();
            m_RightDepthChecks.Clear();
        }

        void OnValidate()
        {
            MigrateLegacyIndexDepthIfNeeded();
            EnsureDepthEntriesForAllRules();
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
            return TryResolve(other, out CliffDepthRuleTile cliff) && HasSameTerrain(cliff);
        }

        protected virtual bool HasSameTerrain(CliffDepthRuleTile other)
        {
            return other != null && Terrain == other.Terrain;
        }

        public static bool TryResolve(TileBase tile, out CliffDepthRuleTile cliff)
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

            return MatchDepthChecks(rule.m_Id, position, tilemap);
        }

        bool MatchDepthChecks(int ruleId, Vector3Int position, ITilemap tilemap)
        {
            if (!MatchDepthCheck(GetLeftDepth(ruleId), Depth, Terrain, position.x - 1, position.y, tilemap))
            {
                return false;
            }

            if (!MatchDepthCheck(GetRightDepth(ruleId), Depth, Terrain, position.x + 1, position.y, tilemap))
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
            if (!TryResolve(tilemap.GetTile(new Vector3Int(x, y, 0)), out CliffDepthRuleTile cliffTile))
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

