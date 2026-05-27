using System.Collections.Generic;
using cfg.demo;
using My.Map.Logic;
using My.Player;
using UnityEngine;

namespace My.Config
{
    public readonly struct BodyPartLevelProgress
    {
        public readonly bool IsMaxLevel;
        public readonly float Fill01;
        public readonly long ExpInLevel;
        public readonly long ExpSpan;

        public BodyPartLevelProgress(bool isMaxLevel, float fill01, long expInLevel, long expSpan)
        {
            IsMaxLevel = isMaxLevel;
            Fill01 = fill01;
            ExpInLevel = expInLevel;
            ExpSpan = expSpan;
        }
    }

    public static class BodyPartCatalog
    {
        public static BodyPartDef GetPartDef(EBodyPart partId)
        {
            if (partId == EBodyPart.None || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbBodyPartDef.GetOrDefault(partId);
        }

        public static IReadOnlyList<BodyPartDef> GetAllPartsSorted()
        {
            if (CfgMgr.Cfgs == null)
            {
                return System.Array.Empty<BodyPartDef>();
            }

            var list = new List<BodyPartDef>(CfgMgr.Cfgs.TbBodyPartDef.DataList);
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return list;
        }

        public static BodyPartLevel GetLevelRow(EBodyPart partId, int level)
        {
            if (CfgMgr.Cfgs == null || partId == EBodyPart.None || level <= 0)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbBodyPartLevel.Get(partId, level);
        }

        public static int ResolveLevelByExp(EBodyPart partId, long exp)
        {
            var def = GetPartDef(partId);
            if (def == null || CfgMgr.Cfgs == null)
            {
                return 0;
            }

            int resolved = 0;
            for (int lv = 1; lv <= def.MaxLevel; lv++)
            {
                var row = CfgMgr.Cfgs.TbBodyPartLevel.Get(partId, lv);
                if (row == null)
                {
                    break;
                }

                if (exp >= row.NeedExp)
                {
                    resolved = lv;
                }
                else
                {
                    break;
                }
            }

            return resolved;
        }

        public static long GetNeedExpForLevel(EBodyPart partId, int level)
        {
            var row = GetLevelRow(partId, level);
            return row?.NeedExp ?? 0;
        }

        public static bool IsPartUnlocked(EBodyPart partId, GameLogicManager glm)
        {
            var def = GetPartDef(partId);
            if (def == null)
            {
                return false;
            }

            if (def.UnlockConds == null || def.UnlockConds.Count == 0)
            {
                return true;
            }

            return glm != null && glm.CheckCommonCondsAll(def.UnlockConds);
        }

        public static bool TryGetLevelProgress(
            EBodyPart partId,
            int level,
            long exp,
            out BodyPartLevelProgress progress)
        {
            progress = default;
            var def = GetPartDef(partId);
            if (def == null || level <= 0)
            {
                return false;
            }

            if (level >= def.MaxLevel)
            {
                progress = new BodyPartLevelProgress(true, 1f, 0, 0);
                return true;
            }

            long curNeed = GetNeedExpForLevel(partId, level);
            long nextNeed = GetNeedExpForLevel(partId, level + 1);
            long span = nextNeed - curNeed;
            if (span <= 0)
            {
                progress = new BodyPartLevelProgress(false, 0f, 0, 0);
                return true;
            }

            long expInLevel = System.Math.Max(0, exp - curNeed);
            float fill = Mathf.Clamp01(expInLevel / (float)span);
            progress = new BodyPartLevelProgress(false, fill, expInLevel, span);
            return true;
        }

        public static void AccumulateGlobalBonuses(EBodyPart partId, int level, StatMap targetMap)
        {
            if (targetMap == null || partId == EBodyPart.None || level <= 0 || CfgMgr.Cfgs == null)
            {
                return;
            }

            for (int lv = 1; lv <= level; lv++)
            {
                var row = CfgMgr.Cfgs.TbBodyPartLevel.Get(partId, lv);
                if (row?.GlobalBonuses == null)
                {
                    continue;
                }

                for (int i = 0; i < row.GlobalBonuses.Count; i++)
                {
                    var b = row.GlobalBonuses[i];
                    targetMap.Add(b.AttrId, b.Val);
                }
            }
        }

        public static StatMap BuildLocalStats(EBodyPart partId, int level)
        {
            var map = new StatMap();
            if (partId == EBodyPart.None || level <= 0 || CfgMgr.Cfgs == null)
            {
                return map;
            }

            for (int lv = 1; lv <= level; lv++)
            {
                var row = CfgMgr.Cfgs.TbBodyPartLevel.Get(partId, lv);
                if (row?.LocalBonuses == null)
                {
                    continue;
                }

                for (int i = 0; i < row.LocalBonuses.Count; i++)
                {
                    var b = row.LocalBonuses[i];
                    map.Add(b.AttrId, b.Val);
                }
            }

            return map;
        }

        public static EYCAttribute MapPartToGearPointYc(EBodyPart partId)
        {
            return partId switch
            {
                EBodyPart.Mouth => EYCAttribute.PartGearPoint_Mouth,
                EBodyPart.Breast => EYCAttribute.PartGearPoint_Breast,
                EBodyPart.Womb => EYCAttribute.PartGearPoint_Womb,
                EBodyPart.Tail => EYCAttribute.PartGearPoint_Tail,
                EBodyPart.Wing => EYCAttribute.PartGearPoint_Wing,
                EBodyPart.Skin => EYCAttribute.PartGearPoint_Skin,
                _ => EYCAttribute.None,
            };
        }

        public static string GetLocalAttrDisplayName(int attrId)
        {
            if (CfgMgr.Cfgs == null)
            {
                return attrId.ToString();
            }

            var row = CfgMgr.Cfgs.TbPartLocalAttrDef.GetOrDefault(attrId);
            return row != null && !string.IsNullOrEmpty(row.DisplayName) ? row.DisplayName : attrId.ToString();
        }
    }
}
