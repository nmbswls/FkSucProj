using cfg.demo;

namespace My.Config
{
    public static class PlayerAttachCatalog
    {
        public static PlayerAttachInfo GetOrDefault(string attachId)
        {
            if (string.IsNullOrEmpty(attachId))
            {
                return null;
            }

            return CfgMgr.Cfgs?.TbPlayerAttachInfo?.GetOrDefault(attachId);
        }

        public static string GetAttachType(string attachId)
        {
            var info = GetOrDefault(attachId);
            if (info == null || string.IsNullOrEmpty(info.AttachType))
            {
                return attachId;
            }

            return info.AttachType;
        }

        public static string GetDisplayGroupId(string attachId)
        {
            var info = GetOrDefault(attachId);
            if (info != null && !string.IsNullOrEmpty(info.DisplayGroupId))
            {
                return info.DisplayGroupId;
            }

            var attachType = GetAttachType(attachId);
            return string.IsNullOrEmpty(attachType) ? attachId : attachType;
        }

        public static int GetDisplayWeight(string attachId)
        {
            return GetOrDefault(attachId)?.DisplayWeight ?? 0;
        }

        public static int GetDisplayCountWeight(string attachId)
        {
            return GetOrDefault(attachId)?.DisplayCountWeight ?? 0;
        }

        public static int GetMaxVisibleCount(string attachId)
        {
            return GetOrDefault(attachId)?.MaxVisibleCount ?? 0;
        }

        public static string GetAttachMainBuff(string attachId)
        {
            return GetOrDefault(attachId)?.AttachMainBuff;
        }

        public static string GetAttachViewPrefabPath(string attachId)
        {
            var path = GetOrDefault(attachId)?.AttachViewPrefab;
            return string.IsNullOrEmpty(path) ? $"Prefab/Attach/{attachId}" : path;
        }

        public static float GetAutoDropTime(string attachId)
        {
            return GetOrDefault(attachId)?.AutoDropTime ?? 0f;
        }

        public static float GetHitCount(string attachId)
        {
            return GetOrDefault(attachId)?.HitCount ?? 3f;
        }
    }
}
