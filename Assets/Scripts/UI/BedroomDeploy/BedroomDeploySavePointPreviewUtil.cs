using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.MapExport;
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
            string mapId,
            GameLogicManager glm,
            List<SavePointMarkerVm> outMarkers)
        {
            outMarkers.Clear();
            if (string.IsNullOrEmpty(mapId) || glm == null)
            {
                return;
            }

            var mapCfg = CfgMgr.Cfgs?.TbMapAreaInfo?.GetOrDefault(mapId);
            if (mapCfg == null || string.IsNullOrEmpty(mapCfg.MapDataName))
            {
                return;
            }

            var points = SavePointUnlockHelper.GetUnlockedForMap(glm, mapId);
            if (points == null || points.Count == 0)
            {
                return;
            }

            var db = Resources.Load<MapExportDatabase>($"MapExport/{mapCfg.MapDataName}");
            if (db == null)
            {
                Debug.LogWarning(
                    $"[BedroomDeploy] MapExport not found for preview map '{mapId}' data '{mapCfg.MapDataName}'.");
                foreach (var sp in points)
                {
                    outMarkers.Add(new SavePointMarkerVm { Config = sp, HasMapPosition = false });
                }

                return;
            }

            if (!TryComputeNamedPointBounds(db, out var minX, out var maxX, out var minY, out var maxY))
            {
                foreach (var sp in points)
                {
                    outMarkers.Add(new SavePointMarkerVm { Config = sp, HasMapPosition = false });
                }

                return;
            }

            var spanX = Mathf.Max(0.001f, maxX - minX);
            var spanY = Mathf.Max(0.001f, maxY - minY);

            foreach (var sp in points)
            {
                var vm = new SavePointMarkerVm { Config = sp, HasMapPosition = false };
                //if (sp == null || string.IsNullOrEmpty(sp.TeleportNamedPoint))
                //{
                //    outMarkers.Add(vm);
                //    continue;
                //}

                //var np = db.FindNamedPointByName(sp.TeleportNamedPoint);
                //if (!np.HasValue)
                //{
                //    Debug.LogWarning(
                //        $"[BedroomDeploy] Named point '{sp.TeleportNamedPoint}' not in MapExport/{mapCfg.MapDataName} for save point '{sp.SavePointId}'.");
                //    outMarkers.Add(vm);
                //    continue;
                //}

                //var pos = np.Value.Position;
                //vm.NormPos01 = new Vector2(
                //    Mathf.Clamp01((pos.x - minX) / spanX),
                //    Mathf.Clamp01((pos.y - minY) / spanY));
                //vm.HasMapPosition = true;
                outMarkers.Add(vm);
            }
        }

        static bool TryComputeNamedPointBounds(
            MapExportDatabase db,
            out float minX,
            out float maxX,
            out float minY,
            out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (db?.NamedPoints == null || db.NamedPoints.Count == 0)
            {
                return false;
            }

            var first = true;
            foreach (var p in db.NamedPoints)
            {
                var x = p.Position.x;
                var y = p.Position.y;
                if (first)
                {
                    minX = maxX = x;
                    minY = maxY = y;
                    first = false;
                }
                else
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return !first;
        }
    }
}
