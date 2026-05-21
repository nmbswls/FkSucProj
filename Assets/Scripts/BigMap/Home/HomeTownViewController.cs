using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;

namespace My.Home
{
    // 城镇管理 UI：打开时隐藏非建筑单位表现，仅保留玩家与 HomeFacility
    public static class HomeTownViewController
    {
        public static bool ManagementPanelOpen { get; private set; }

        public static void SetManagementPanelOpen(bool open)
        {
            ManagementPanelOpen = open;
            RefreshAllPresenters();
        }

        // 由 UI（如 MapTownManagementPanel）订阅 SceneAOIManager.AfterPresentationShown 后调用
        public static void ApplyPresentationVisibilityForTownManagement(IScenePresentation pres, ILogicEntity entity)
        {
            if (!ManagementPanelOpen || pres == null || entity == null)
            {
                return;
            }

            if (!IsHomeMap())
            {
                return;
            }

            if (entity.Type == EEntityType.HomeFacility || entity.Type == EEntityType.Player)
            {
                return;
            }

            if (entity is BaseUnitLogicEntity)
            {
                pres.SetVisible(false);
            }
        }

        public static void RefreshAllPresenters()
        {
            if (SceneAOIManager.Instance == null)
            {
                return;
            }

            foreach (var pres in SceneAOIManager.Instance.GetAllActivePresentation())
            {
                var e = pres?.GetLogicEntity();
                if (e == null)
                {
                    continue;
                }

                if (!IsHomeMap())
                {
                    pres.SetVisible(true);
                    continue;
                }

                if (ManagementPanelOpen && e is BaseUnitLogicEntity && e.Type != EEntityType.Player && e.Type != EEntityType.HomeFacility)
                {
                    pres.SetVisible(false);
                }
                else
                {
                    pres.SetVisible(true);
                }
            }
        }

        private static bool IsHomeMap()
        {
            /*
            var areaOverlayId = MainGameManager.Instance?.gameLogicManager?.AreaManager?.AreaOverlayId;
            if (string.IsNullOrEmpty(areaOverlayId))
            {
                return false;
            }

            var cfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(areaOverlayId);
            return cfg != null && cfg.IsHome;
            */
            return false;
        }
    }
}
