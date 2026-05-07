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
            list.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
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

        public static SkillLearnEntry TryGetLearnEntry(int entryId) =>
            CfgMgr.Cfgs?.TbSkillLearnEntry.GetOrDefault(entryId);

        public static bool TryGetEntitySkillData(string skillId, out EntitySkillData data)
        {
            data = My.Map.Entity.SkillLibrary.GetSkillConfig(skillId);
            return data != null;
        }
    }
}
