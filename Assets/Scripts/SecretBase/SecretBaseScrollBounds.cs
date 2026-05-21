using My.Config;
using UnityEngine;

namespace My.SecretBase
{
    // 据点建设等级 -> 卷轴左右边界（TbSecretBaseBuildLevel）
    public static class SecretBaseScrollBounds
    {
        public static (float minX, float maxX) Get(int level)
        {
            level = Mathf.Max(1, level);
            var table = CfgMgr.Cfgs?.TbSecretBaseBuildLevel;
            if (table == null || table.DataList == null || table.DataList.Count == 0)
            {
                return (0f, 32f);
            }

            if (table.DataMap.TryGetValue(level, out var row) && row != null)
            {
                return (row.ScrollMinX, row.ScrollMaxX);
            }

            var list = table.DataList;
            cfg.demo.SecretBaseBuildLevel best = null;
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                if (r == null || r.Level > level)
                {
                    continue;
                }

                if (best == null || r.Level > best.Level)
                {
                    best = r;
                }
            }

            if (best != null)
            {
                return (best.ScrollMinX, best.ScrollMaxX);
            }

            var fallback = list[0];
            return (fallback.ScrollMinX, fallback.ScrollMaxX);
        }
    }
}
