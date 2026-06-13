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

        public static bool TryGetEntitySkillData(string skillId, out EntitySkillData data)
        {
            data = My.Map.Entity.SkillLibrary.GetSkillConfig(skillId);
            return data != null;
        }
    }
}
