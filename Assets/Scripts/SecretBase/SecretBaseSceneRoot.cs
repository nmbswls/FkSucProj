using System.Collections.Generic;
using Cinemachine;
using My;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace My.SecretBase
{
    // 据点场景唯一入口：卷轴相机、视差、点击交互。
    public class SecretBaseSceneRoot : MonoBehaviour
    {
        public static SecretBaseSceneRoot Instance { get; private set; }

        const float ScrollSpeed = 8f;

        [SerializeField] Transform scrollAnchor;
        [SerializeField] SecretBaseParallaxLayer[] parallaxLayers;
        [SerializeField] Transform facilitySpawnRoot;

        float _scrollX;
        float _scrollMinX;
        float _scrollMaxX = 32f;
        bool _sessionActive;

        readonly SecretBaseWorldSpawnRuntime _worldSpawn = new();
        ISecretBaseClickTarget _hovered;

        static readonly List<RaycastResult> UiRaycastBuffer = new(8);

        public static SecretBaseSceneRoot FindLoaded()
        {
            if (Instance != null)
            {
                return Instance;
            }

            return Object.FindObjectOfType<SecretBaseSceneRoot>(true);
        }

        void Awake()
        {
            Instance = this;
            RefreshScrollBounds();
            _scrollX = Mathf.Clamp(_scrollX, _scrollMinX, _scrollMaxX);
        }

        void OnDestroy()
        {
            ClearHover();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnterMode()
        {
            EnterScrollCameraMode();
            RefreshScrollBounds();
            var glm = MainGameManager.Instance?.gameLogicManager;
            var root = facilitySpawnRoot != null ? facilitySpawnRoot : transform;
            _worldSpawn.Refresh(glm, root);
        }

        public void ExitMode()
        {
            ClearHover();
            _worldSpawn.ClearSpawned();
            _sessionActive = false;
        }

        public void RefreshScrollBounds()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            int level = glm != null ? glm.GetSecretBaseBuildLevel() : 1;
            var bounds = SecretBaseScrollBounds.Get(level);
            _scrollMinX = bounds.minX;
            _scrollMaxX = bounds.maxX;
            _scrollX = Mathf.Clamp(_scrollX, _scrollMinX, _scrollMaxX);
            if (_sessionActive)
            {
                ApplyCameraAndParallax();
            }
        }

        public void Tick(float dt)
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
            {
                axis -= 1f;
            }

            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
            {
                axis += 1f;
            }

            if (_sessionActive && Mathf.Abs(axis) > 0.01f)
            {
                _scrollX = Mathf.Clamp(_scrollX + axis * ScrollSpeed * dt, _scrollMinX, _scrollMaxX);
                ApplyCameraAndParallax();
            }

            var screen = GetPointerScreenPosition();
            if (!ScreenToWorld(screen, out var world))
            {
                return;
            }

            TickHover(world);
            if (UnityEngine.Input.GetMouseButtonDown(0) && !IsPointerOverHudButton(screen))
            {
                TryClick(world);
            }
        }

        public void HandleScreenPointer(Vector2 screenPos, bool click)
        {
            if (click && IsPointerOverHudButton(screenPos))
            {
                return;
            }

            if (!ScreenToWorld(screenPos, out var world))
            {
                return;
            }

            TickHover(world);
            if (click)
            {
                TryClick(world);
            }
        }

        public void ApplyPanScreenDelta(Vector2 screenDelta)
        {
            if (!_sessionActive || Mathf.Abs(screenDelta.x) < 0.01f)
            {
                return;
            }

            var cam = ResolveViewCamera();
            if (cam == null)
            {
                return;
            }

            var anchor = scrollAnchor != null ? scrollAnchor : transform;
            float z = Mathf.Abs(cam.transform.position.z - anchor.position.z);
            var w0 = cam.ScreenToWorldPoint(new Vector3(0f, Screen.height * 0.5f, z));
            var w1 = cam.ScreenToWorldPoint(new Vector3(screenDelta.x, Screen.height * 0.5f, z));
            float worldDx = w1.x - w0.x;
            _scrollX = Mathf.Clamp(_scrollX - worldDx, _scrollMinX, _scrollMaxX);
            ApplyCameraAndParallax();
        }

        void EnterScrollCameraMode()
        {
            var vcam = MainGameManager.Instance?.MainMapVCam;
            if (vcam == null)
            {
                Debug.LogError("SecretBaseSceneRoot: MainMapVCam missing.");
                return;
            }

            if (_sessionActive)
            {
                return;
            }

            vcam.Follow = null;
            vcam.LookAt = null;
            vcam.PreviousStateIsValid = false;
            _sessionActive = true;
            ApplyCameraAndParallax();
        }

        void ApplyCameraAndParallax()
        {
            var vcam = MainGameManager.Instance?.MainMapVCam;
            if (vcam == null)
            {
                return;
            }

            var anchor = scrollAnchor != null ? scrollAnchor : transform;
            var p = vcam.transform.position;
            p.x = _scrollX;
            p.y = anchor.position.y;
            p.z = anchor.position.z;
            vcam.transform.position = p;

            if (parallaxLayers == null)
            {
                return;
            }

            for (int i = 0; i < parallaxLayers.Length; i++)
            {
                parallaxLayers[i]?.ApplyOffset(_scrollX);
            }
        }

        void TickHover(Vector2 worldPos)
        {
            var hit = FindTopHit(worldPos);
            if (_hovered == hit)
            {
                return;
            }

            _hovered?.SetHighlight(false);
            _hovered = hit;
            _hovered?.SetHighlight(true);
        }

        void TryClick(Vector2 worldPos)
        {
            FindTopHit(worldPos)?.OnClick();
        }

        void ClearHover()
        {
            _hovered?.SetHighlight(false);
            _hovered = null;
        }

        ISecretBaseClickTarget FindTopHit(Vector2 worldPos)
        {
            ISecretBaseClickTarget best = null;
            var bestOrder = int.MinValue;
            var list = _worldSpawn.Spawned;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var mb = item as MonoBehaviour;
                if (item == null || mb == null || !mb.isActiveAndEnabled || !item.ContainsPoint(worldPos))
                {
                    continue;
                }

                var order = item.SortOrder;
                if (best == null || order > bestOrder)
                {
                    best = item;
                    bestOrder = order;
                }
            }

            return best;
        }

        public static Vector2 GetPointerScreenPosition()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            return UnityEngine.Input.mousePosition;
        }

        public static bool ScreenToWorld(Vector2 screenPos, out Vector2 world)
        {
            var cam = ResolveViewCamera();
            if (cam == null)
            {
                world = default;
                return false;
            }

            world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            return true;
        }

        static Camera ResolveViewCamera()
        {
            var mgr = MainGameManager.Instance;
            if (mgr?.CineBrain != null && mgr.CineBrain.OutputCamera != null)
            {
                return mgr.CineBrain.OutputCamera;
            }

            return Camera.main;
        }

        public static bool IsPointerOverHudButton(Vector2 screenPos)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var ped = new PointerEventData(EventSystem.current) { position = screenPos };
            UiRaycastBuffer.Clear();
            EventSystem.current.RaycastAll(ped, UiRaycastBuffer);

            for (int i = 0; i < UiRaycastBuffer.Count; i++)
            {
                var go = UiRaycastBuffer[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                var graphic = go.GetComponent<Graphic>();
                if (graphic == null || !graphic.raycastTarget)
                {
                    continue;
                }

                if (go.GetComponentInParent<Button>() != null)
                {
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        void Update()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                glm.EnterSecretBase("default");
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.F10))
            {
                glm.ExitSecretBase();
            }
        }
#endif
    }
}
