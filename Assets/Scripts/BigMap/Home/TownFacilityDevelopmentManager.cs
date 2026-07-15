using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
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

        public int GetFacilityDevelopmentLevel(string logicAreaId, int siteId)
        {
            return _logic?.worldPersistState?.GetSiteDevelopmentLevel(logicAreaId, siteId) ?? 0;
        }

        public int GetFacilityDevelopmentLevel(string logicAreaId, string facilityId)
        {
            return _logic?.worldPersistState?.GetFacilityDevelopmentLevel(logicAreaId, facilityId) ?? 0;
        }

        public int GetFacilityDevelopmentLevel(string logicAreaId, long instanceId, string facilityId)
        {
            return _logic?.worldPersistState?.GetInstanceFacilityDevelopmentLevel(logicAreaId, instanceId, facilityId) ?? 0;
        }

        public IReadOnlyList<FacilityDevelopmentDefinition> GetFacilityDefinitions(string logicAreaId)
        {
            return FacilityDevelopmentCatalog.GetDefinitions(logicAreaId);
        }

        public FacilityDevelopmentLevel GetNextFacilityLevel(string logicAreaId, string facilityId)
        {
            var current = GetFacilityDevelopmentLevel(logicAreaId, facilityId);
            return FacilityDevelopmentCatalog.GetLevel(facilityId, current + 1);
        }

        public bool CanUpgradeFacility(string logicAreaId, int siteId, out string failReason)
        {
            failReason = null;
            var site = TownFacilitySiteCatalog.Get(siteId);
            if (site == null)
            {
                failReason = "invalid_site";
                return false;
            }

            if (!string.Equals(site.MapId, logicAreaId, StringComparison.Ordinal))
            {
                failReason = "site_area_mismatch";
                return false;
            }

            return CanUpgradeFacility(logicAreaId, 0, site.FacilityCfgId, out failReason);
        }

        public bool CanUpgradeFacility(string logicAreaId, long instanceId, string facilityId, out string failReason)
        {
            failReason = null;
            if (!IsAreaUnderPlayerControl(logicAreaId))
            {
                failReason = "area_not_controlled";
                return false;
            }

            var facilityDef = FacilityDevelopmentCatalog.GetDefinition(facilityId);
            if (facilityDef == null)
            {
                failReason = "no_facility_development_cfg";
                return false;
            }

            var current = GetFacilityDevelopmentLevel(logicAreaId, instanceId, facilityId);
            if (current >= facilityDef.MaxLevel)
            {
                failReason = "max_level";
                return false;
            }

            var upgradeDef = FacilityDevelopmentCatalog.GetLevel(facilityId, current + 1);
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

        public bool CanUpgradeFacility(string logicAreaId, string facilityId, out string failReason)
        {
            return CanUpgradeFacility(logicAreaId, 0, facilityId, out failReason);
        }

        public bool TryUpgradeFacility(string logicAreaId, int siteId, out string failReason)
        {
            var site = TownFacilitySiteCatalog.Get(siteId);
            if (site == null)
            {
                failReason = "invalid_site";
                return false;
            }

            return TryUpgradeFacility(logicAreaId, 0, site.FacilityCfgId, out failReason);
        }

        public bool TryUpgradeFacility(string logicAreaId, long instanceId, string facilityId, out string failReason)
        {
            if (!CanUpgradeFacility(logicAreaId, instanceId, facilityId, out failReason))
            {
                return false;
            }

            var current = GetFacilityDevelopmentLevel(logicAreaId, instanceId, facilityId);
            var upgradeDef = FacilityDevelopmentCatalog.GetLevel(facilityId, current + 1);
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
            _logic.worldPersistState.SetInstanceFacilityDevelopmentLevel(logicAreaId, instanceId, facilityId, nextLevel);
            EvOnFacilityDevelopmentLevelChanged?.Invoke(logicAreaId, facilityId, nextLevel);
            _logic.AreaManager?.ReevaluateTownFacilityVisibility(logicAreaId, facilityId);
            SceneAOIManager.Instance?.RequestVisibleChunkRefresh();
            return true;
        }

        public bool TryUpgradeFacility(string logicAreaId, string facilityId, out string failReason)
        {
            return TryUpgradeFacility(logicAreaId, 0, facilityId, out failReason);
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
            var handledSites = new HashSet<int>();
            foreach (var site in TownFacilitySiteCatalog.GetSitesForMap(logicAreaId))
            {
                if (site == null || !handledSites.Add(site.Id))
                {
                    continue;
                }

                var level = GetFacilityDevelopmentLevel(logicAreaId, site.Id);
                if (level <= 0)
                {
                    continue;
                }

                var facilityRow = _logic.worldPersistState?.GetTownFacilityBySite(logicAreaId, site.Id, false);
                CollectSingleFacilityOutputs(
                    logicAreaId,
                    site.FacilityCfgId,
                    level,
                    facilityRow?.RenovationId,
                    mergedOutputs);
            }

            var facilities = _logic.worldPersistState?.GetTownFacilities(logicAreaId);
            if (facilities == null || facilities.Count == 0)
            {
                return;
            }

            var handledLegacy = new HashSet<string>(StringComparer.Ordinal);
            foreach (var facilityRow in facilities)
            {
                if (facilityRow == null || string.IsNullOrEmpty(facilityRow.FacilityId))
                {
                    continue;
                }

                if (facilityRow.SiteId > 0)
                {
                    continue;
                }

                if (facilityRow.InstanceId == 0
                    && TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, facilityRow.FacilityId) != null)
                {
                    continue;
                }

                var level = Mathf.Max(0, facilityRow.DevelopmentLevel);
                if (level <= 0)
                {
                    continue;
                }

                var key = $"{facilityRow.InstanceId}:{facilityRow.FacilityId}";
                if (!handledLegacy.Add(key))
                {
                    continue;
                }

                CollectSingleFacilityOutputs(
                    logicAreaId,
                    facilityRow.FacilityId,
                    level,
                    facilityRow.RenovationId,
                    mergedOutputs);
            }
        }

        void CollectFacilityOutputsLegacy(string logicAreaId, Dictionary<string, long> mergedOutputs)
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

                CollectSingleFacilityOutputs(logicAreaId, facilityDef.FacilityId, level, null, mergedOutputs);
            }
        }

        void CollectSingleFacilityOutputs(
            string logicAreaId,
            string facilityId,
            int level,
            string renovationId,
            Dictionary<string, long> mergedOutputs)
        {
            var upgradeDef = FacilityDevelopmentCatalog.GetLevel(facilityId, level);
            if (upgradeDef?.DailyOutputs != null && upgradeDef.DailyOutputs.Count > 0)
            {
                var tech = _logic.playerDataManager?.ProgressionSystem?.HumanCivilization;
                MergeOutputs(upgradeDef.DailyOutputs, tech, facilityId, mergedOutputs);
            }

            if (string.IsNullOrEmpty(renovationId))
            {
                return;
            }

            var renovation = FacilityRenovationCatalog.Get(facilityId, renovationId);
            if (renovation?.DailyOutputs == null || renovation.DailyOutputs.Count == 0)
            {
                return;
            }

            var civilization = _logic.playerDataManager?.ProgressionSystem?.HumanCivilization;
            MergeOutputs(renovation.DailyOutputs, civilization, facilityId, mergedOutputs);
        }

        static double ResolveOutputMultiplier(Player.HumanCivilizationSystem tech, string facilityId, string itemId)
        {
            var outputMultiplier = 1d;
            if (string.Equals(itemId, "gold", StringComparison.Ordinal))
            {
                outputMultiplier += Math.Max(0, tech?.GetTechEffectValue(EHumanCivilizationAttribute.TownFacilityGoldOutput) ?? 0) * 0.1d;
            }

            if (string.Equals(facilityId, "tavern", StringComparison.Ordinal))
            {
                outputMultiplier += Math.Max(0, tech?.GetTechEffectValue(EHumanCivilizationAttribute.TavernHumanKnowledgeOutput) ?? 0) * 0.1d;
            }

            if (string.Equals(facilityId, "workshop", StringComparison.Ordinal))
            {
                outputMultiplier += Math.Max(0, tech?.GetTechEffectValue(EHumanCivilizationAttribute.WorkshopOperationEfficiency) ?? 0) * 0.1d;
            }

            return outputMultiplier;
        }

        static void MergeOutputs(
            System.Collections.Generic.List<TalentUnlockCost> outputs,
            Player.HumanCivilizationSystem tech,
            string facilityId,
            Dictionary<string, long> mergedOutputs)
        {
            if (outputs == null)
            {
                return;
            }

            foreach (var output in outputs)
            {
                if (output == null || string.IsNullOrEmpty(output.ItemId) || output.Count <= 0)
                {
                    continue;
                }

                var multiplier = ResolveOutputMultiplier(tech, facilityId, output.ItemId);
                var amount = (long)Math.Floor(output.Count * multiplier);
                if (mergedOutputs.TryGetValue(output.ItemId, out var existing))
                {
                    mergedOutputs[output.ItemId] = existing + amount;
                }
                else
                {
                    mergedOutputs[output.ItemId] = amount;
                }
            }
        }
    }
}
