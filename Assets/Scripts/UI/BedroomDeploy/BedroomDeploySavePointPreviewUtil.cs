using System.Collections.Generic;
using cfg.demo;
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
    }

    public static class BedroomDeploySavePointPreviewUtil
    {
        public static void CollectTeleportMarkers(
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

            var points = SavePointUnlockHelper.GetUnlockedForMap(glm, mapCfg.VarId);
            if (points == null || points.Count == 0)
            {
                return;
            }

            points.Sort((a, b) =>
            {
                var order = a.SortOrder.CompareTo(b.SortOrder);
                return order != 0
                    ? order
                    : string.CompareOrdinal(a?.SavePointId, b?.SavePointId);
            });

            foreach (var sp in points)
            {
                if (sp == null)
                {
                    continue;
                }

                outMarkers.Add(new SavePointMarkerVm
                {
                    Config = sp,
                    NormPos01 = new Vector2(
                        Mathf.Clamp01(sp.SnapShowX),
                        Mathf.Clamp01(sp.SnapShowY)),
                    HasMapPosition = true,
                });
            }
        }
    }
}
