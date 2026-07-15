using Cinemachine;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using My.Player;
using My.UI.Home;
using UnityEngine;

namespace My.Home
{
    // home 地图设施管理视图：拉远镜头、隐藏无关单位，保留建筑地基/已建成设施与玩家。
    public static class HomeTownViewController
    {
        public const float DefaultOverviewOrthoSize = 11f;
        public const float OverviewOrthoBlendSpeed = 6f;

        static int _activeRequestCount;
        static bool _aoiSubscribed;
        static float? _savedOrthoSize;
        static Transform _savedFollow;
        static float _targetOrthoSize;
        static bool _cameraOverviewActive;
        static int _mapTargetMask = -1;

        public static bool IsFacilityManagementViewActive => _activeRequestCount > 0;

        public static bool TryHandleManagementClick(Vector2 screenPos)
        {
            if (!IsFacilityManagementViewActive || !IsHomeMap())
            {
                return false;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return false;
            }

            if (_mapTargetMask < 0)
            {
                _mapTargetMask = 1 << LayerMask.NameToLayer("MapTarget");
            }

            var world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            var col = Physics2D.OverlapPoint(world, _mapTargetMask);
            if (col == null)
            {
                return false;
            }

            var site = col.GetComponentInParent<TownFacilitySiteInteract>();
            if (site == null || !site.CanInteractEnable())
            {
                return false;
            }

            site.TriggerInteract(TownFacilityInteractUtil.SelectManageFacility, GamePlayerIds.Local);
            return true;
        }

        public static void EnterFacilityManagementView()
        {
            if (!CanUseOnCurrentMap())
            {
                return;
            }

            _activeRequestCount++;
            if (_activeRequestCount == 1)
            {
                EnsureAoiSubscription(true);
                ApplyCameraOverview(true);
                RefreshAllPresenters();
            }
        }

        public static void LeaveFacilityManagementView()
        {
            if (_activeRequestCount <= 0)
            {
                return;
            }

            _activeRequestCount--;
            if (_activeRequestCount == 0)
            {
                EnsureAoiSubscription(false);
                ApplyCameraOverview(false);
                RefreshAllPresenters();
            }
        }

        public static void ApplyPresentationVisibility(IScenePresentation pres, ILogicEntity entity)
        {
            if (!IsFacilityManagementViewActive || pres == null || entity == null || !IsHomeMap())
            {
                return;
            }

            pres.SetVisible(ShouldKeepPresentationVisible(entity));
        }

        public static void RefreshAllPresenters()
        {
            var aoi = SceneAOIManager.Instance;
            if (aoi == null)
            {
                return;
            }

            bool filter = IsFacilityManagementViewActive && IsHomeMap();
            foreach (var pres in aoi.GetAllActivePresentation())
            {
                var entity = pres?.GetLogicEntity();
                if (entity == null)
                {
                    continue;
                }

                if (!filter)
                {
                    pres.SetVisible(true);
                    continue;
                }

                pres.SetVisible(ShouldKeepPresentationVisible(entity));
            }
        }

        public static void TickCamera(float dt)
        {
            if (!_cameraOverviewActive || dt <= 0f)
            {
                return;
            }

            var mgr = MainGameManager.Instance;
            var vcam = mgr?.MainMapVCam;
            if (vcam == null)
            {
                return;
            }

            var lens = vcam.m_Lens;
            lens.OrthographicSize = Mathf.Lerp(
                lens.OrthographicSize,
                _targetOrthoSize,
                1f - Mathf.Exp(-OverviewOrthoBlendSpeed * dt));
            vcam.m_Lens = lens;
        }

        static void EnsureAoiSubscription(bool subscribe)
        {
            var aoi = SceneAOIManager.Instance;
            if (aoi == null)
            {
                return;
            }

            if (subscribe)
            {
                if (_aoiSubscribed)
                {
                    return;
                }

                aoi.AfterPresentationShown += OnAfterPresentationShown;
                _aoiSubscribed = true;
                return;
            }

            if (!_aoiSubscribed)
            {
                return;
            }

            aoi.AfterPresentationShown -= OnAfterPresentationShown;
            _aoiSubscribed = false;
        }

        static void OnAfterPresentationShown(IScenePresentation pres, ILogicEntity entity)
        {
            ApplyPresentationVisibility(pres, entity);
        }

        static void ApplyCameraOverview(bool enable)
        {
            var mgr = MainGameManager.Instance;
            var vcam = mgr?.MainMapVCam;
            if (vcam == null)
            {
                return;
            }

            if (enable)
            {
                if (!_cameraOverviewActive)
                {
                    _savedOrthoSize = vcam.m_Lens.OrthographicSize;
                    _savedFollow = vcam.Follow;
                }

                _targetOrthoSize = ResolveOverviewOrthoSize();
                _cameraOverviewActive = true;

                vcam.Follow = null;
                vcam.LookAt = null;
                vcam.PreviousStateIsValid = false;

                var focus = ResolveOverviewFocusWorldPos();
                var camPos = vcam.transform.position;
                camPos.x = focus.x;
                camPos.y = focus.y;
                vcam.transform.position = camPos;
                return;
            }

            if (!_cameraOverviewActive)
            {
                return;
            }

            _cameraOverviewActive = false;
            if (_savedOrthoSize.HasValue)
            {
                var lens = vcam.m_Lens;
                lens.OrthographicSize = _savedOrthoSize.Value;
                vcam.m_Lens = lens;
            }

            _savedOrthoSize = null;
            if (_savedFollow != null)
            {
                vcam.Follow = _savedFollow;
            }
            else
            {
                mgr.EnsureOpenWorldVcamFollow();
            }

            vcam.LookAt = null;
            vcam.PreviousStateIsValid = false;
            _savedFollow = null;
        }

        static float ResolveOverviewOrthoSize()
        {
            var mgr = MainGameManager.Instance;
            var chunkDb = mgr?.gameLogicManager?.AreaManager?.cacheChunkDatabase;
            if (chunkDb != null)
            {
                var rect = chunkDb.ResolveLogicWorldRect();
                if (rect.width > 0f && rect.height > 0f)
                {
                    return Mathf.Clamp(Mathf.Max(rect.width, rect.height) * 0.34f, 8f, 16f);
                }
            }

            return DefaultOverviewOrthoSize;
        }

        static Vector3 ResolveOverviewFocusWorldPos()
        {
            var mgr = MainGameManager.Instance;
            var glm = mgr?.gameLogicManager;
            var hm = glm?.homeDataManager;
            hm?.RefreshFixedFacilities();

            Vector2 logicSum = Vector2.zero;
            int count = 0;
            if (hm != null)
            {
                foreach (var facility in hm.FixedFacilities)
                {
                    if (facility == null || facility.Removed || facility.Entity == null)
                    {
                        continue;
                    }

                    logicSum += facility.Entity.Pos;
                    count++;
                }
            }

            Vector2 logicPos;
            if (count > 0)
            {
                logicPos = logicSum / count;
            }
            else if (glm?.playerLogicEntity != null)
            {
                logicPos = glm.playerLogicEntity.Pos;
            }
            else
            {
                logicPos = Vector2.zero;
            }

            return mgr != null ? mgr.GetWorldPosFromLogicPos(logicPos) : (Vector3)logicPos;
        }

        static bool ShouldKeepPresentationVisible(ILogicEntity entity)
        {
            if (entity == null)
            {
                return true;
            }

            switch (entity.Type)
            {
                case EEntityType.Player:
                case EEntityType.HomeFacility:
                case EEntityType.FacilityRuin:
                    return true;
                default:
                    return entity is not BaseUnitLogicEntity;
            }
        }

        static bool CanUseOnCurrentMap()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            return glm?.townFacilityDevelopmentSystem?.CanOpenManagementForCurrentArea() == true
                   || IsHomeMap();
        }

        static bool IsHomeMap()
        {
            var areaOverlayId = MainGameManager.Instance?.gameLogicManager?.AreaManager?.AreaOverlayId;
            if (string.IsNullOrEmpty(areaOverlayId))
            {
                return false;
            }

            var cfg = CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.GetOrDefault(areaOverlayId);
            return cfg != null && cfg.IsHome;
        }
    }
}
