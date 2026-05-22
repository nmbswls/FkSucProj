using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map;
using UnityEngine;

namespace My.UI
{
    public sealed class SavePointMarkerVm
    {
        public SavePoint Config;
        public Vector2 NormPos01;
        public bool HasMapPosition;
        public bool CanSelect;
    }

    public static class BedroomDeploySavePointPreviewUtil
    {
        public static void CollectSavePointMarkers(
            string mapOverlayId,
            GameLogicManager glm,
            List<SavePointMarkerVm> outMarkers)
        {
            outMarkers.Clear();
            if (string.IsNullOrEmpty(mapOverlayId) || glm == null)
            {
                return;
            }

            var mapCfg = CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.GetOrDefault(mapOverlayId);
            if (mapCfg == null || string.IsNullOrEmpty(mapCfg.VarId))
            {
                return;
            }

            var pointStateList = new List<(SavePoint, bool)>();

            foreach (var cfg in CfgMgr.Cfgs?.TbSavePoint.DataList)
            {
                if (cfg == null || cfg.AreaVarId != mapCfg.VarId)
                {
                    continue;
                }

                if (SavePointUnlockHelper.IsActivated(glm, cfg.SavePointId))
                {
                    pointStateList.Add((cfg, true));
                    continue;
                }

                if (!glm.CheckCommonCondsAll(cfg.ShowUnlockConds))
                {
                    continue;
                }

                pointStateList.Add((cfg, false));
            }

            pointStateList.Sort((a, b) =>
            {
                var order = a.Item1.SortOrder.CompareTo(b.Item1.SortOrder);
                return order != 0
                    ? order
                    : string.CompareOrdinal(a.Item1?.SavePointId, b.Item1?.SavePointId);
            });

            foreach (var sp in pointStateList)
            {
                if (sp.Item1 == null)
                {
                    continue;
                }

                outMarkers.Add(new SavePointMarkerVm
                {
                    Config = sp.Item1,
                    NormPos01 = new Vector2(
                        Mathf.Clamp01(sp.Item1.SnapShowX),
                        Mathf.Clamp01(sp.Item1.SnapShowY)),
                    HasMapPosition = true,
                    CanSelect = sp.Item2,
                });
            }
        }
    }
}
