using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.Home
{
    public sealed class FacilityEffect
    {
        public EFacilityEffectType EffectType;
        public string TargetFacilityId;
        public EBuildingAttribute BuildingAttr;
        public EHumanCivilizationAttribute HumanAttr;
        public int Value;
    }

    public sealed class FacilityOutputBundle
    {
        public int OutputInterval = 1;
        public List<TalentUnlockCost> OutputItems = new();
        public List<FacilityEffect> Effects = new();
    }

    // 汇总设施等级/改造上的 effects，并提供建筑属性查询
    public static class TownFacilityEffectCatalog
    {
        public static long GetBuildingAttribute(
            GameLogicManager glm,
            string logicAreaId,
            string targetFacilityId,
            EBuildingAttribute attribute)
        {
            if (glm == null
                || string.IsNullOrEmpty(logicAreaId)
                || string.IsNullOrEmpty(targetFacilityId)
                || attribute == EBuildingAttribute.None)
            {
                return 0;
            }

            var dev = glm.townFacilityDevelopmentSystem;
            if (dev == null || !dev.IsAreaUnderPlayerControl(logicAreaId))
            {
                return 0;
            }

            long total = 0;
            foreach (var sourceFacilityId in GetActiveFacilityIds(glm, logicAreaId))
            {
                var level = dev.GetFacilityDevelopmentLevel(logicAreaId, sourceFacilityId);
                if (level <= 0)
                {
                    continue;
                }

                var renovationId = GetRenovationId(glm, logicAreaId, sourceFacilityId);
                AccumulateBuildingAttribute(
                    logicAreaId,
                    sourceFacilityId,
                    targetFacilityId,
                    attribute,
                    FacilityDevelopmentCatalog.GetLevel(sourceFacilityId, level)?.Effects,
                    ref total);
                if (!string.IsNullOrEmpty(renovationId))
                {
                    AccumulateBuildingAttribute(
                        logicAreaId,
                        sourceFacilityId,
                        targetFacilityId,
                        attribute,
                        FacilityRenovationCatalog.Get(sourceFacilityId, renovationId)?.Effects,
                        ref total);
                }
            }

            return total;
        }

        public static long GetHumanCivilizationBonus(
            GameLogicManager glm,
            string logicAreaId,
            EHumanCivilizationAttribute attribute)
        {
            if (glm == null
                || string.IsNullOrEmpty(logicAreaId)
                || attribute == EHumanCivilizationAttribute.None)
            {
                return 0;
            }

            var dev = glm.townFacilityDevelopmentSystem;
            if (dev == null || !dev.IsAreaUnderPlayerControl(logicAreaId))
            {
                return 0;
            }

            long total = 0;
            foreach (var facilityId in GetActiveFacilityIds(glm, logicAreaId))
            {
                var level = dev.GetFacilityDevelopmentLevel(logicAreaId, facilityId);
                if (level <= 0)
                {
                    continue;
                }

                var renovationId = GetRenovationId(glm, logicAreaId, facilityId);
                AccumulateHumanCivilizationBonus(
                    FacilityDevelopmentCatalog.GetLevel(facilityId, level)?.Effects,
                    attribute,
                    ref total);
                if (!string.IsNullOrEmpty(renovationId))
                {
                    AccumulateHumanCivilizationBonus(
                        FacilityRenovationCatalog.Get(facilityId, renovationId)?.Effects,
                        attribute,
                        ref total);
                }
            }

            return total;
        }

        public static FacilityOutputBundle GetLevelOutputBundle(string facilityId, int level)
        {
            var row = FacilityDevelopmentCatalog.GetLevel(facilityId, level);
            return row == null
                ? new FacilityOutputBundle()
                : new FacilityOutputBundle
                {
                    OutputInterval = row.OutputInterval,
                    OutputItems = row.OutputItems,
                    Effects = row.Effects,
                };
        }

        public static FacilityOutputBundle GetRenovationOutputBundle(string facilityId, string renovationId)
        {
            var row = FacilityRenovationCatalog.Get(facilityId, renovationId);
            return row == null
                ? new FacilityOutputBundle()
                : new FacilityOutputBundle
                {
                    OutputInterval = row.OutputInterval,
                    OutputItems = row.OutputItems,
                    Effects = row.Effects,
                };
        }

        static IEnumerable<string> GetActiveFacilityIds(GameLogicManager glm, string logicAreaId)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var dev = glm?.townFacilityDevelopmentSystem;
            if (dev == null)
            {
                return result;
            }

            foreach (var site in TownFacilitySiteCatalog.GetSitesForMap(logicAreaId))
            {
                if (site == null || string.IsNullOrEmpty(site.FacilityCfgId))
                {
                    continue;
                }

                if (dev.GetFacilityDevelopmentLevel(logicAreaId, site.Id) > 0)
                {
                    result.Add(site.FacilityCfgId);
                }
            }

            var facilities = glm.worldPersistState?.GetTownFacilities(logicAreaId);
            if (facilities != null)
            {
                foreach (var facility in facilities)
                {
                    if (facility == null || string.IsNullOrEmpty(facility.FacilityId))
                    {
                        continue;
                    }

                    if (facility.SiteId > 0)
                    {
                        continue;
                    }

                    if (Mathf.Max(0, facility.DevelopmentLevel) > 0)
                    {
                        result.Add(facility.FacilityId);
                    }
                }
            }

            return result;
        }

        static string GetRenovationId(GameLogicManager glm, string logicAreaId, string facilityId)
        {
            var site = TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, facilityId);
            if (site != null)
            {
                return glm.worldPersistState?.GetTownFacilityBySite(logicAreaId, site.Id, false)?.RenovationId;
            }

            return glm.worldPersistState?.GetTownFacility(logicAreaId, 0, facilityId, false)?.RenovationId;
        }

        static void AccumulateBuildingAttribute(
            string logicAreaId,
            string sourceFacilityId,
            string targetFacilityId,
            EBuildingAttribute attribute,
            IReadOnlyList<FacilityEffect> effects,
            ref long total)
        {
            if (effects == null)
            {
                return;
            }

            foreach (var effect in effects)
            {
                if (effect == null
                    || effect.EffectType != EFacilityEffectType.AddBuildingAttribute
                    || effect.BuildingAttr != attribute
                    || effect.Value <= 0)
                {
                    continue;
                }

                string resolvedTarget = string.IsNullOrEmpty(effect.TargetFacilityId)
                    ? sourceFacilityId
                    : effect.TargetFacilityId;
                if (!string.Equals(resolvedTarget, targetFacilityId, StringComparison.Ordinal))
                {
                    continue;
                }

                total += effect.Value;
            }
        }

        static void AccumulateHumanCivilizationBonus(
            IReadOnlyList<FacilityEffect> effects,
            EHumanCivilizationAttribute attribute,
            ref long total)
        {
            if (effects == null)
            {
                return;
            }

            foreach (var effect in effects)
            {
                if (effect == null
                    || effect.EffectType != EFacilityEffectType.AddHumanCivilizationAttribute
                    || effect.HumanAttr != attribute
                    || effect.Value <= 0)
                {
                    continue;
                }

                total += effect.Value;
            }
        }

        internal static FacilityEffect Map(FacilityEffectConfig row)
        {
            if (row == null)
            {
                return null;
            }

            return new FacilityEffect
            {
                EffectType = row.EffectType,
                TargetFacilityId = row.TargetFacilityId,
                BuildingAttr = row.BuildingAttr,
                HumanAttr = row.HumanAttr,
                Value = row.Value,
            };
        }

        internal static List<FacilityEffect> MapList(List<FacilityEffectConfig> rows)
        {
            var result = new List<FacilityEffect>();
            if (rows == null)
            {
                return result;
            }

            foreach (var row in rows)
            {
                var mapped = Map(row);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }

            return result;
        }
    }
}
