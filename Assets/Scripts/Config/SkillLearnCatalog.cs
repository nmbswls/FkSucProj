using System.Collections.Generic;
using cfg.demo;

namespace My.Config
{
    // 技能学派与学习条目：数据来自 Config/Datas/skill.xlsx（Luban）
    public static class SkillLearnCatalog
    {
        public static SkillSchool TryGetSchool(int schoolId) =>
            CfgMgr.Cfgs?.TbSkillSchool.GetOrDefault(schoolId);

        public static List<SkillSchool> GetSchoolsSorted()
        {
            if (CfgMgr.Cfgs == null)
            {
                return new List<SkillSchool>();
            }

            var list = new List<SkillSchool>(CfgMgr.Cfgs.TbSkillSchool.DataList);
            return list;
        }

        public static List<SkillLearnEntry> GetLearnEntriesBySchool(int schoolId)
        {
            var r = new List<SkillLearnEntry>();
            if (CfgMgr.Cfgs == null)
            {
                return r;
            }

            foreach (var e in CfgMgr.Cfgs.TbSkillLearnEntry.DataList)
            {
                if (e.SchoolId == schoolId)
                {
                    r.Add(e);
                }
            }

            return r;
        }

        public static bool SkillDefinedInSchool(int schoolId, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            foreach (var e in GetLearnEntriesBySchool(schoolId))
            {
                if (e.SkillId == skillId)
                {
                    return true;
                }
            }

            return false;
        }

        public static SkillLearnEntry TryGetLearnEntry(int entryId) =>
            CfgMgr.Cfgs?.TbSkillLearnEntry.GetOrDefault(entryId);

        public static SkillLearnEntry TryFindLearnEntryBySkillId(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            foreach (var entry in CfgMgr.Cfgs.TbSkillLearnEntry.DataList)
            {
                if (entry != null && entry.SkillId == skillId)
                {
                    return entry;
                }
            }

            return null;
        }

        public static bool TryGetEntitySkillData(string skillId, out EntitySkillData data)
        {
            data = My.Map.Entity.SkillLibrary.GetSkillConfig(skillId);
            return data != null;
        }

        // 查找指定技能所属学派ID，用于定位功能，未找到返回0
        public static int TryFindSchoolIdForSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || CfgMgr.Cfgs == null)
            {
                return 0;
            }

            foreach (var e in CfgMgr.Cfgs.TbSkillLearnEntry.DataList)
            {
                if (e != null && e.SkillId == skillId)
                {
                    return e.SchoolId;
                }
            }

            return 0;
        }

        // 查找指定技能的下一级升级条目，当前等级之上的最小SkillLevel条目，无则返回null
        public static SkillLearnEntry TryFindNextLevelEntry(string skillId, int currentLevel)
        {
            if (string.IsNullOrEmpty(skillId) || CfgMgr.Cfgs == null)
            {
                return null;
            }

            SkillLearnEntry best = null;
            foreach (var e in CfgMgr.Cfgs.TbSkillLearnEntry.DataList)
            {
                if (e == null || e.SkillId != skillId || e.SkillLevel <= currentLevel)
                {
                    continue;
                }

                if (best == null || e.SkillLevel < best.SkillLevel)
                {
                    best = e;
                }
            }

            return best;
        }
    }
}
