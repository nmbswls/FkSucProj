using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;

namespace My.Home
{
    public sealed class FacilitySupervisorCandidate
    {
        public string CharacterKey;
        public string DisplayName;
        public string DisplayTitle;
        public int SortOrder;
    }

    public static class TownFacilitySupervisorCatalog
    {
        public static CharacterFacilitySupervisor Get(string characterKey)
        {
            return string.IsNullOrEmpty(characterKey)
                ? null
                : CfgMgr.Cfgs?.TbCharacterFacilitySupervisor?.GetOrDefault(characterKey);
        }

        public static bool CanAssign(string characterKey)
        {
            var row = Get(characterKey);
            return row != null && row.CanAssignSupervisor;
        }
    }

    public static class TownFacilityTownCharacterCatalog
    {
        public static IReadOnlyList<string> GetCharacterKeysInTown(GameLogicManager glm, string logicAreaId)
        {
            var result = new HashSet<string>(System.StringComparer.Ordinal);
            if (glm == null || string.IsNullOrEmpty(logicAreaId))
            {
                return new List<string>();
            }

            var currentAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(glm.AreaManager);
            if (string.Equals(currentAreaId, logicAreaId, System.StringComparison.Ordinal))
            {
                var records = glm.AreaManager?.Repo?.Records;
                if (records != null)
                {
                    foreach (var pair in records)
                    {
                        if (pair.Value is not LogicEntityRecord4Npc npc || string.IsNullOrEmpty(npc.CharacterKey))
                        {
                            continue;
                        }

                        result.Add(npc.CharacterKey);
                    }
                }
            }

            var bindings = CfgMgr.Cfgs?.TbNpcRoutineBinding?.DataList;
            if (bindings != null)
            {
                foreach (var binding in bindings)
                {
                    if (binding != null
                        && binding.OverlayId == logicAreaId
                        && !string.IsNullOrEmpty(binding.CharacterKey))
                    {
                        result.Add(binding.CharacterKey);
                    }
                }
            }

            return new List<string>(result);
        }

        public static List<FacilitySupervisorCandidate> GetAssignableSupervisors(GameLogicManager glm, string logicAreaId)
        {
            var result = new List<FacilitySupervisorCandidate>();
            var inTown = new HashSet<string>(GetCharacterKeysInTown(glm, logicAreaId), System.StringComparer.Ordinal);
            var table = CfgMgr.Cfgs?.TbCharacterFacilitySupervisor?.DataList;
            if (table == null)
            {
                return result;
            }

            foreach (var row in table)
            {
                if (row == null || !row.CanAssignSupervisor || string.IsNullOrEmpty(row.CharacterKey))
                {
                    continue;
                }

                if (!inTown.Contains(row.CharacterKey))
                {
                    continue;
                }

                var info = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(row.CharacterKey);
                result.Add(new FacilitySupervisorCandidate
                {
                    CharacterKey = row.CharacterKey,
                    DisplayName = info?.Name ?? row.CharacterKey,
                    DisplayTitle = row.DisplayTitle,
                    SortOrder = row.SortOrder,
                });
            }

            result.Sort((left, right) => left.SortOrder.CompareTo(right.SortOrder));
            return result;
        }
    }
}
