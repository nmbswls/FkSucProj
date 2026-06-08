using Cinemachine;
using My.Dungeon;
using My.Map;
using My.MapExport;
using My.Map.Logic;
using My.Map.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace My
{
    public partial class MainGameManager
    {
        public const float DefaultCameraOverrideVisualRadius = 28f;
        public const float DefaultCameraOverrideDuration = 3f;
        public const int DefaultOverrideVcamPriority = 20;
        const float CameraOverrideReadyTimeoutSec = 2f;

        [FormerlySerializedAs("MapCameraBoundsExtension")]
        [SerializeField] MapCameraBoundsExtension mapBoundsExtension;

        [Header("Camera Override")]
        [SerializeField] CinemachineVirtualCamera OverrideVCam;
        [SerializeField] Vector3 overrideVcamWorldOffset = Vector3.zero;
        [SerializeField] int overrideVcamPriority = DefaultOverrideVcamPriority;

        MapCameraBoundsController _mapCameraBounds;
        Coroutine _cameraOverrideRoutine;
        bool _cameraOverrideInputLocked;

        public bool IsCameraOverrideActive { get; private set; }

        public struct CameraOverrideIntent
        {
            public float StartTime;
            public float Duration;

            public bool IsFixed;
            public Vector2 FixPoint;

            public bool IsFollow;
            public long FollowEntityId;

            public long PinEntityId;
            public float VisualRadius;
            public int Priority;
            public bool BlockPlayerInput;
        }

        public List<CameraOverrideIntent> CameraShowOverrideIntens = new();

        public void ClearMapCameraBounds()
        {
            _mapCameraBounds?.Clear();
        }

        public void ApplyMapCameraBounds()
        {
            if (_mapCameraBounds == null || !_mapCameraBounds.IsReady)
            {
                return;
            }

            var overlay = gameLogicManager?.AreaManager?.cacheMapOverlayCfg;
            if (overlay == null || overlay.IsSecretBase)
            {
                _mapCameraBounds.Clear();
                return;
            }

            if (TryResolveMapCameraBounds(out var rect))
            {
                _mapCameraBounds.Apply(rect);
            }
            else
            {
                _mapCameraBounds.Clear();
            }
        }

        void ResolveMapCameraBoundsExtension()
        {
            if (mapBoundsExtension != null)
            {
                return;
            }

            if (MainMapVCam != null)
            {
                mapBoundsExtension = MainMapVCam.GetComponent<MapCameraBoundsExtension>()
                    ?? MainMapVCam.GetComponentInChildren<MapCameraBoundsExtension>(true);
            }

            if (mapBoundsExtension == null)
            {
                mapBoundsExtension = FindObjectOfType<MapCameraBoundsExtension>(true);
            }

            if (mapBoundsExtension == null)
            {
                if (MainMapVCam == null)
                {
                    Debug.LogError("[MainGameManager] MainMapVCam is not assigned on GameMain.");
                }
                else
                {
                    Debug.LogError(
                        "[MainGameManager] MapCameraBoundsExtension must be attached to MainMapVCam in Main_Root.");
                }

                return;
            }

            if (MainMapVCam != null && mapBoundsExtension.gameObject != MainMapVCam.gameObject)
            {
                Debug.LogWarning(
                    "[MainGameManager] MapCameraBoundsExtension should be on the same GameObject as MainMapVCam.");
            }

            var legacyConfiner = mapBoundsExtension.GetComponent<CinemachineConfiner2D>();
            if (legacyConfiner != null)
            {
                legacyConfiner.enabled = false;
            }
        }

        bool TryResolveMapCameraBounds(out Rect rect)
        {
            rect = default;

            var worldRoot = WorldAreaManager != null ? WorldAreaManager.currentRoot : null;
            var chunkDb = gameLogicManager?.AreaManager?.cacheChunkDatabase;
            var overlay = gameLogicManager?.AreaManager?.cacheMapOverlayCfg;

            if (worldRoot != null && worldRoot.HasLogicWorldRectOverride)
            {
                rect = worldRoot.LogicWorldRectOverride;
                return true;
            }

            if (chunkDb != null)
            {
                rect = chunkDb.ResolveLogicWorldRect();
                if (rect.width > 0f && rect.height > 0f)
                {
                    return true;
                }
            }

            if (overlay != null && DungeonPresentation.IsProceduralOverlay(overlay))
            {
                return TryResolveDungeonCameraBounds(overlay.Id, worldRoot, out rect);
            }

            return false;
        }

        static bool TryResolveDungeonCameraBounds(string overlayId, WorldAreaRoot worldRoot, out Rect rect)
        {
            rect = default;

            var result = DungeonSession.GetLastResult(overlayId);
            var grid = worldRoot != null ? worldRoot.Grid : null;
            if (result?.WalkableCells == null || result.WalkableCells.Count == 0 || grid == null)
            {
                return false;
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;
            foreach (var cell in result.WalkableCells)
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }

            if (minX == int.MaxValue)
            {
                return false;
            }

            var cellSize = grid.cellSize;
            var halfCell = new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
            var sw = grid.GetCellCenterWorld(new Vector3Int(minX, minY, 0)) - halfCell;
            var ne = grid.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0)) + halfCell;
            rect = Rect.MinMaxRect(sw.x, sw.y, ne.x, ne.y);
            return rect.width > 0f && rect.height > 0f;
        }

        public void ClearMapVcamBinding()
        {
            if (MainMapVCam == null)
            {
                return;
            }

            MainMapVCam.Follow = null;
            MainMapVCam.LookAt = null;
            MainMapVCam.PreviousStateIsValid = false;
        }

        public void EnsureOpenWorldVcamFollow()
        {
            var overlay = gameLogicManager?.AreaManager?.cacheMapOverlayCfg;
            if (overlay != null && overlay.IsSecretBase)
            {
                return;
            }

            if (playerScenePresenter == null || MainMapVCam == null)
            {
                return;
            }

            var follow = playerScenePresenter.ViewPoint != null
                ? playerScenePresenter.ViewPoint
                : playerScenePresenter.transform;

            if (MainMapVCam.Follow != follow)
            {
                MainMapVCam.Follow = follow;
                MainMapVCam.LookAt = null;
                MainMapVCam.PreviousStateIsValid = false;
            }
        }

        void InitCameraSystems()
        {
            ResolveMapCameraBoundsExtension();
            _mapCameraBounds = new MapCameraBoundsController(mapBoundsExtension);
            InitOverrideVCamState();
        }

        void InitOverrideVCamState()
        {
            if (OverrideVCam == null)
            {
                Debug.LogError("[MainGameManager] OverrideVCam is not assigned on GameMain in Main_Root.");
                return;
            }

            OverrideVCam.Priority = 0;
            OverrideVCam.Follow = null;
            OverrideVCam.LookAt = null;
        }

        public void ShowCameraOverrideFix(
            Vector2 logicPos,
            float duration = DefaultCameraOverrideDuration,
            long pinEntityId = 0,
            float visualRadius = DefaultCameraOverrideVisualRadius,
            bool blockInput = true)
        {
            if (_cameraOverrideRoutine != null)
            {
                StopCoroutine(_cameraOverrideRoutine);
                EndCameraOverrideImmediate();
            }

            _cameraOverrideRoutine = StartCoroutine(ShowCameraOverrideRoutine(
                logicPos, duration, pinEntityId, visualRadius, blockInput));
        }

        public void PushCameraOverrideIntent(CameraOverrideIntent intent)
        {
            if (intent.Duration <= 0f)
            {
                return;
            }

            intent.StartTime = LogicTime.time;
            if (intent.VisualRadius <= 0f)
            {
                intent.VisualRadius = DefaultCameraOverrideVisualRadius;
            }

            if (intent.Priority <= 0)
            {
                intent.Priority = overrideVcamPriority;
            }

            CameraShowOverrideIntens.Add(intent);

            if (intent.IsFixed)
            {
                ShowCameraOverrideFix(
                    intent.FixPoint,
                    intent.Duration,
                    intent.PinEntityId,
                    intent.VisualRadius,
                    intent.BlockPlayerInput);
            }
        }

        IEnumerator ShowCameraOverrideRoutine(
            Vector2 logicPos,
            float duration,
            long pinEntityId,
            float visualRadius,
            bool blockInput)
        {
            var aoi = AOIManager != null ? AOIManager : SceneAOIManager.Instance;
            if (aoi == null || OverrideVCam == null)
            {
                Debug.LogError("[MainGameManager] Camera override failed: AOIManager or OverrideVCam missing.");
                yield break;
            }

            float until = LogicTime.time + Mathf.Max(0.1f, duration);
            float focusUntil = until + 0.5f;
            aoi.AddVisualFocus(logicPos, visualRadius, focusUntil);
            if (pinEntityId != 0)
            {
                aoi.PinPresentation(pinEntityId);
                gameLogicManager?.GetLogicEntity(pinEntityId, ensureExist: true);
            }

            aoi.PrewarmTickAtFocusOnce(logicPos, 0f);

            float deadline = Time.realtimeSinceStartup + CameraOverrideReadyTimeoutSec;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (aoi.IsFocusAreaReady(logicPos, pinEntityId))
                {
                    break;
                }

                aoi.PrewarmTickAtFocusOnce(logicPos, LogicTime.deltaTime);
                yield return null;
            }

            if (!aoi.IsFocusAreaReady(logicPos, pinEntityId))
            {
                Debug.LogWarning(
                    $"[MainGameManager] Camera override focus not ready at {logicPos}, pin={pinEntityId}");
            }

            ActivateOverrideVcam(logicPos);
            SetCameraOverrideInputLock(blockInput);

            while (LogicTime.time < until)
            {
                if (pinEntityId != 0)
                {
                    aoi.TickPinnedPresentation(pinEntityId, LogicTime.deltaTime);
                }

                yield return null;
            }

            EndCameraOverrideSession(logicPos, pinEntityId, blockInput);
            _cameraOverrideRoutine = null;
        }

        void ActivateOverrideVcam(Vector2 logicPos)
        {
            if (OverrideVCam == null)
            {
                Debug.LogError("[MainGameManager] ActivateOverrideVcam failed: OverrideVCam not assigned.");
                return;
            }

            var worldPos = GetWorldPosFromLogicPos(logicPos) + overrideVcamWorldOffset;
            OverrideVCam.Follow = null;
            OverrideVCam.LookAt = null;
            OverrideVCam.transform.position = worldPos;
            OverrideVCam.Priority = overrideVcamPriority;
            OverrideVCam.PreviousStateIsValid = false;
            IsCameraOverrideActive = true;
        }

        void DeactivateOverrideVcam()
        {
            if (OverrideVCam == null)
            {
                IsCameraOverrideActive = false;
                return;
            }

            OverrideVCam.Priority = 0;
            OverrideVCam.PreviousStateIsValid = false;
            IsCameraOverrideActive = false;
        }

        void SetCameraOverrideInputLock(bool blockInput)
        {
            if (inputBinder == null)
            {
                return;
            }

            if (blockInput)
            {
                inputBinder.GlobalLock = true;
                _cameraOverrideInputLocked = true;
            }
        }

        void ReleaseCameraOverrideInputLock(bool blockInput)
        {
            if (!blockInput || inputBinder == null || !_cameraOverrideInputLocked)
            {
                return;
            }

            inputBinder.GlobalLock = false;
            _cameraOverrideInputLocked = false;
        }

        void EndCameraOverrideSession(Vector2 logicPos, long pinEntityId, bool blockInput)
        {
            DeactivateOverrideVcam();
            ReleaseCameraOverrideInputLock(blockInput);

            var aoi = AOIManager != null ? AOIManager : SceneAOIManager.Instance;
            if (aoi != null)
            {
                aoi.RemoveVisualFocus(logicPos);
                if (pinEntityId != 0)
                {
                    aoi.UnpinPresentation(pinEntityId);
                }
            }
        }

        void EndCameraOverrideImmediate()
        {
            DeactivateOverrideVcam();
            if (_cameraOverrideInputLocked && inputBinder != null)
            {
                inputBinder.GlobalLock = false;
                _cameraOverrideInputLocked = false;
            }

            AOIManager?.ClearAllVisualFocusAndPins();
            IsCameraOverrideActive = false;
        }

        void TryRefreshCameraShowStatus()
        {
            if (CameraShowOverrideIntens.Count == 0)
            {
                return;
            }

            float now = LogicTime.time;
            for (int i = CameraShowOverrideIntens.Count - 1; i >= 0; i--)
            {
                var intent = CameraShowOverrideIntens[i];
                if (now >= intent.StartTime + intent.Duration)
                {
                    CameraShowOverrideIntens.RemoveAt(i);
                }
            }

            AOIManager?.RemoveExpiredVisualFocuses(now);
        }
    }
}
