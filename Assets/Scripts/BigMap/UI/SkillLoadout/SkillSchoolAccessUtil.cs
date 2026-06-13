using My.Config;

namespace My.UI.SkillLoadout
{
    public static class SkillSchoolAccessUtil
    {
        public static bool IsSchoolUnlocked(int schoolId)
        {
            var school = SkillLearnCatalog.TryGetSchool(schoolId);
            if (school == null)
            {
                return false;
            }

            if (school.UnlockConds == null || school.UnlockConds.Count == 0)
            {
                return true;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            return glm != null && glm.CheckCommonCondsAll(school.UnlockConds);
        }

        public static string ResolveLockedHint(int schoolId)
        {
            if (schoolId <= 0)
            {
                return "尚未开放";
            }

            var school = SkillLearnCatalog.TryGetSchool(schoolId);
            if (school == null)
            {
                return "尚未开放";
            }

            string condHint = SkillLearnEntryTextUtil.BuildFirstLearnCondHint(school.UnlockConds);
            return string.IsNullOrEmpty(condHint)
                ? $"{school.DisplayName}尚未解锁"
                : $"{school.DisplayName}尚未解锁：{condHint}";
        }
    }
}
