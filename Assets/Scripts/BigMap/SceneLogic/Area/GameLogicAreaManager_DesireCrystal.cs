using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    public sealed class DesireCrystalDynamicSegmentRuntime
    {
        public float UnlockAtLogicTime;
        public int Remaining;
        public int AttachChancePermille;
    }

    public partial class GameLogicAreaManager
    {
        internal HashSet<int> DesireCrystalFixedRefreshStaticIds;

        readonly List<DesireCrystalDynamicSegmentRuntime> _desireCrystalDynamicSegments = new();

        float _desireCrystalSessionEnterLogicTime;

        internal void ClearDesireCrystalSession()
        {
            DesireCrystalFixedRefreshStaticIds?.Clear();
            DesireCrystalFixedRefreshStaticIds = null;
            _desireCrystalDynamicSegments.Clear();
            _desireCrystalSessionEnterLogicTime = 0f;
        }

        internal void SetupDesireCrystalSession(string mapName)
        {
            ClearDesireCrystalSession();
            if (cacheMapOverlayCfg == null)
            {
                return;
            }

            _desireCrystalSessionEnterLogicTime = LogicTime.time;
            DesireCrystalFixedRefreshStaticIds = DesireCrystalFixedAssignments.BuildFixedStaticIds(mapName, this);

            foreach (var q in CfgMgr.Cfgs.TbMapWalkerDesireCrystalQuota.DataList
                         .Where(x => x.MapId == mapName)
                         .OrderBy(x => x.SegmentOrder))
            {
                _desireCrystalDynamicSegments.Add(new DesireCrystalDynamicSegmentRuntime
                {
                    UnlockAtLogicTime = _desireCrystalSessionEnterLogicTime + q.UnlockAfterMinutes * 60f,
                    Remaining = q.Quota,
                    AttachChancePermille = q.AttachChancePermille,
                });
            }
        }

        internal bool TryConsumeWalkerDesireCrystalRoll(out string crystalTypeId)
        {
            crystalTypeId = null;
            if (_desireCrystalDynamicSegments.Count == 0)
            {
                return false;
            }

            var now = LogicTime.time;
            foreach (var seg in _desireCrystalDynamicSegments)
            {
                if (now < seg.UnlockAtLogicTime || seg.Remaining <= 0)
                {
                    continue;
                }

                if (Random.Range(0, 1000) >= seg.AttachChancePermille)
                {
                    continue;
                }

                seg.Remaining--;
                crystalTypeId = DesireCrystalRoller.RollWeightedCrystalTypeId();
                return !string.IsNullOrEmpty(crystalTypeId);
            }

            return false;
        }
    }

    internal static class DesireCrystalFixedAssignments
    {
        internal static HashSet<int> BuildFixedStaticIds(string mapName, GameLogicAreaManager area)
        {
            var result = new HashSet<int>();
            int budget = DesireCrystalRoller.RollAttachBudget(mapName);
            if (budget <= 0)
            {
                return result;
            }

            var candidates = new List<int>();
            foreach (var ri in area.EntityRefreshInfo)
            {
                if (ri?.InitInfo is not EntityInitInfo4Npc npcInit)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(npcInit.CharacterKey))
                {
                    continue;
                }

                var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(npcInit.CfgId);
                if (npcCfg == null || !npcCfg.DesireCrystalRandomAttachable)
                {
                    continue;
                }

                candidates.Add(ri.StaticId);
            }

            if (candidates.Count == 0)
            {
                return result;
            }

            Shuffle(candidates);
            int assign = Mathf.Min(budget, candidates.Count);
            for (int i = 0; i < assign; i++)
            {
                result.Add(candidates[i]);
            }

            return result;
        }

        static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    internal static class DesireCrystalRoller
    {
        internal static int RollAttachBudget(string mapId)
        {
            var rows = CfgMgr.Cfgs.TbMapDesireCrystalBudget.DataList.Where(r => r.MapId == mapId).ToList();
            if (rows.Count == 0)
            {
                return 0;
            }

            int sum = 0;
            foreach (var r in rows)
            {
                sum += Mathf.Max(0, r.Weight);
            }

            if (sum <= 0)
            {
                return 0;
            }

            int roll = Random.Range(0, sum);
            foreach (var r in rows)
            {
                int w = Mathf.Max(0, r.Weight);
                if (roll < w)
                {
                    return Mathf.Max(0, r.AttachCount);
                }

                roll -= w;
            }

            return Mathf.Max(0, rows[^1].AttachCount);
        }

        internal static string RollWeightedCrystalTypeId()
        {
            var list = CfgMgr.Cfgs.TbDesireCrystalDef.DataList;
            if (list == null || list.Count == 0)
            {
                return null;
            }

            int sum = 0;
            foreach (var d in list)
            {
                sum += Mathf.Max(0, d.RandomPickWeight);
            }

            if (sum <= 0)
            {
                return list[0].Id;
            }

            int roll = Random.Range(0, sum);
            foreach (var d in list)
            {
                int w = Mathf.Max(0, d.RandomPickWeight);
                if (roll < w)
                {
                    return d.Id;
                }

                roll -= w;
            }

            return list[^1].Id;
        }
    }
}
