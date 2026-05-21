using System.Collections.Generic;
using cfg.demo;
using My.Config;

namespace My.UI
{
    public static class BedroomDeployMapUtil
    {
        //public static void CollectHuntMaps(List<MapAreaInfo> outMaps)
        //{
        //    outMaps.Clear();
        //    var tb = CfgMgr.Cfgs?.TbMapAreaInfo;
        //    if (tb?.DataList == null)
        //    {
        //        return;
        //    }

        //    var glm = MainGameManager.Instance?.gameLogicManager;
        //    foreach (var m in tb.DataList)
        //    {
        //        if (m == null || string.IsNullOrEmpty(m.Id) || !m.HuntingTarget)
        //        {
        //            continue;
        //        }

        //        if (m.DayPeriodLimit == 1)
        //        {
        //            continue;
        //        }

        //        var conds = m.HuntingUnlockConds;
        //        bool passed = true;
        //        if (conds != null && glm != null)
        //        {
        //            foreach (var cond in conds)
        //            {
        //                if (!glm.CheckCommonCond(cond))
        //                {
        //                    passed = false;
        //                    break;
        //                }
        //            }
        //        }

        //        if (passed)
        //        {
        //            outMaps.Add(m);
        //        }
        //    }

        //    outMaps.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        //}
    }
}
