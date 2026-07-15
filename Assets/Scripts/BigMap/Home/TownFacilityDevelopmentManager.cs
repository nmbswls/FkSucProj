using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using UnityEngine;

namespace My.Home
{
    public sealed class HomesteadDailySettlementResult
    {
        public readonly Dictionary<string, long> MergedOutputs = new(StringComparer.Ordinal);
    }

    // 以 logic_area map_id 为键，管理各家园区域的建筑升级运行时
    public class TownFacilityDevelopmentManager
    {
        readonly GameLogicManager _logic;

        public event Action<string, string, int> EvOnFacilityDevelopmentLevelChanged;

        public TownFacilityDevelopmentManager(GameLogicManager logic)
        {
            _logic = logic;
        }

        public bool IsAreaUnderPlayerControl(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || _logic?.worldPersistState == null)
            {
                return false;
            }

            if (_logic.worldPersistState.IsLogicAreaAnnexed(logicAreaId))
            {
                return true;
            }

            return _logic.worldPersistState.IsLogicAreaControlRequirementMet(logicAreaId);
        }

        public bool CanOpenManagementForCurrentArea()
        {
            var logicAreaId = ResolveCurrentLogicAreaId();
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return false;
            }

            return IsAreaUnderPlayerControl(logicAreaId)
                && TownFacilityUtil.HasDevelopableFacilities(logicAreaId);
        }

        public string ResolveCurrentLogicAreaId()
        {
            return TownFacilityUtil.ResolveCurrentLogicAreaId(_logic?.AreaManager);
        }

        public int GetFacilityDevelopmentLevel(string logicAreaId, string facilityId)
        {
            return _logic?.worldPersistState?.GetFacilityDevelopmentLevel(logicAreaId, facilityId) ?? 0;
        }

        public IReadOnlyList<FacilityDevelopmentDefinition> GetFacilityDefinitions(string logicAreaId)
        {
            return FacilityDevelopmentCatalog.GetDefinitions(logicAreaId);
        }

        public FacilityDevelopmentLevel GetNextFacilityLevel(string logicAreaId, string facilityId)
        {
            var current = GetFacilityDevelopmentLevel(logicAreaId, facilityId);
            return FacilityDevelopmentCatalog.GetLevel(logicAreaId, facilityId, current + 1);
        }

        public bool CanUpgradeFacility(string logicAreaId, string facilityId, out string failReason)
        {
            failReason = null;
            if (!IsAreaUnderPlayerControl(logicAreaId))
            {
                failReason = "area_not_controlled";
                return false;
            }

            var facilityDef = FacilityDevelopmentCatalog.GetDefinition(logicAreaId, facilityId);
            if (facilityDef == null)
            {
                failReason = "no_facility_development_cfg";
                return false;
            }

            var current = GetFacilityDevelopmentLevel(logicAreaId, facilityId);
            if (current >= facilityDef.MaxLevel)
            {
                failReason = "max_level";
                return false;
            }

            var upgradeDef = FacilityDevelopmentCatalog.GetLevel(logicAreaId, facilityId, current + 1);
            if (upgradeDef == null)
            {
                failReason = "no_upgrade_cfg";
                return false;
            }

            if (_logic == null)
            {
                failReason = "no_logic";
                return false;
            }

            if (upgradeDef.UnlockConds != null && upgradeDef.UnlockConds.Count > 0
                && !_logic.CheckCommonCondsAll(upgradeDef.UnlockConds))
            {
                failReason = "unlock_cond_fail";
                return false;
            }

            if (!HasUpgradeCosts(upgradeDef, out failReason))
            {
                return false;
            }

            return true;
        }

        public bool TryUpgradeFacility(string logicAreaId, string facilityId, out string failReason)
        {
            if (!CanUpgradeFacility(logicAreaId, facilityId, out failReason))
            {
                return false;
            }

            var current = GetFacilityDevelopmentLevel(logicAreaId, facilityId);
            var upgradeDef = FacilityDevelopmentCatalog.GetLevel(logicAreaId, facilityId, current + 1);
            if (upgradeDef == null)
            {
                failReason = "no_upgrade_cfg";
                return false;
            }

            if (!PayUpgradeCosts(upgradeDef, out failReason))
            {
                return false;
            }

            var nextLevel = current + 1;
            _logic.worldPersistState.SetFacilityDevelopmentLevel(logicAreaId, facilityId, nextLevel);
            EvOnFacilityDevelopmentLevelChanged?.Invoke(logicAreaId, facilityId, nextLevel);
            return true;
        }

        bool HasUpgradeCosts(FacilityDevelopmentLevel upgradeDef, out string failReason)
        {
            failReason = null;
            var pdm = _logic?.playerDataManager;
            if (pdm == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            if (upgradeDef?.UnlockCosts == null || upgradeDef.UnlockCosts.Count == 0)
            {
                return true;
            }

            foreach (var cost in upgradeDef.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (!pdm.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    failReason = "not_enough_item";
                    return false;
                }
            }

            return true;
        }

        bool PayUpgradeCosts(FacilityDevelopmentLevel upgradeDef, out string failReason)
        {
            failReason = null;
            var pdm = _logic?.playerDataManager;
            if (pdm == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            if (upgradeDef?.UnlockCosts == null || upgradeDef.UnlockCosts.Count == 0)
            {
                return true;
            }

            foreach (var cost in upgradeDef.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (pdm.CostItem(cost.ItemId, cost.Count) < cost.Count)
                {
                    failReason = "not_enough_item";
                    return false;
                }
            }

            return true;
        }

        // 对已控制城镇内已升级建筑，按当前等级配置汇总每日产出并发放
        public HomesteadDailySettlementResult ApplyDailySettlement(PlayerSystemManager pdm)
        {
            var result = new HomesteadDailySettlementResult();
            if (pdm == null)
            {
                return result;
            }

            foreach (var logicAreaId in TownFacilityUtil.GetDistinctLogicAreaIdsWithDevelopableFacilities())
            {
                if (!IsAreaUnderPlayerControl(logicAreaId))
                {
                    continue;
                }

                CollectFacilityOutputs(logicAreaId, result.MergedOutputs);
            }

            foreach (var kv in result.MergedOutputs)
            {
                pdm.GiveItemToPlayer(kv.Key, kv.Value);
            }

            return result;
        }

        void CollectFacilityOutputs(string logicAreaId, Dictionary<string, long> mergedOutputs)
        {
            foreach (var facilityDef in GetFacilityDefinitions(logicAreaId))
            {
                if (facilityDef == null || string.IsNullOrEmpty(facilityDef.FacilityId))
                {
                    continue;
                }

                var level = GetFacilityDevelopmentLevel(logicAreaId, facilityDef.FacilityId);
                if (level <= 0)
                {
                    continue;
                }

                var upgradeDef = FacilityDevelopmentCatalog.GetLevel(
                    logicAreaId, facilityDef.FacilityId, level);
                if (upgradeDef?.DailyOutputs == null || upgradeDef.DailyOutputs.Count == 0)
                {
                    continue;
                }

                var tech = _logic.playerDataManager?.ProgressionSystem?.HumanCivilization;
                var outputMultiplier = 1d;
                if (string.Equals(facilityDef.FacilityId, "tavern", StringComparison.Ordinal))
                {
                    outputMultiplier += Math.Max(0, tech?.GetTechEffectValue(EHumanCivilizationAttribute.TavernHumanKnowledgeOutput) ?? 0) * 0.1d;
                }
                if (string.Equals(facilityDef.FacilityId, "workshop", StringComparison.Ordinal))
                {
                    outputMultiplier += Math.Max(0, tech?.GetTechEffectValue(EHumanCivilizationAttribute.WorkshopOperationEfficiency) ?? 0) * 0.1d;
                }

                foreach (var output in upgradeDef.DailyOutputs)
                {
                    if (output == null || string.IsNullOrEmpty(output.ItemId) || output.Count <= 0)
                    {
                        continue;
                    }

                    if (mergedOutputs.TryGetValue(output.ItemId, out var existing))
                    {
                        mergedOutputs[output.ItemId] = existing + (long)Math.Floor(output.Count * outputMultiplier);
                    }
                    else
                    {
                        mergedOutputs[output.ItemId] = (long)Math.Floor(output.Count * outputMultiplier);
                    }
                }
            }
        }
    }
}
