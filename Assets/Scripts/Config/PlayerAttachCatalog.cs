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

        public static int GetCoverageCircleCount(string attachId, int sameTypeCount)
        {
            var info = GetOrDefault(attachId);
            if (info == null)
            {
                return 0;
            }

            if (sameTypeCount <= 1)
            {
                return info.CoverageCircleCount1;
            }

            if (sameTypeCount == 2)
            {
                return info.CoverageCircleCount2;
            }

            return info.CoverageCircleCount3;
        }

        public static float GetCoverageRadius(string attachId)
        {
            var radius = GetOrDefault(attachId)?.CoverageRadius ?? 0f;
            return radius > 0f ? radius : 0.35f;
        }

        public static string GetCoverageColor(string attachId)
        {
            var color = GetOrDefault(attachId)?.CoverageColor;
            return string.IsNullOrEmpty(color) ? "#202020" : color;
        }
    }
}
