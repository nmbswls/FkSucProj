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
        [SerializeField] float scrollMinX;
        [SerializeField] float scrollMaxX = 32f;
        [SerializeField] SecretBaseParallaxLayer[] parallaxLayers;

        float _scrollX;
        Transform _savedFollow;
        Transform _savedLookAt;
        bool _cameraBound;

        SecretBaseInteractable[] _interactables;
        SecretBaseInteractable _hovered;

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
            _scrollX = scrollMinX;
            _interactables = GetComponentsInChildren<SecretBaseInteractable>(true);
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
            BindCamera();
        }

        public void ExitMode()
        {
            ClearHover();
            UnbindCamera();
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

            if (_cameraBound && Mathf.Abs(axis) > 0.01f)
            {
                _scrollX = Mathf.Clamp(_scrollX + axis * ScrollSpeed * dt, scrollMinX, scrollMaxX);
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

        void BindCamera()
        {
            var vcam = MainGameManager.Instance?.MainMapVCam;
            if (vcam == null)
            {
                Debug.LogError("SecretBaseSceneRoot: MainMapVCam missing.");
                return;
            }

            if (_cameraBound)
            {
                return;
            }

            _savedFollow = vcam.Follow;
            _savedLookAt = vcam.LookAt;
            vcam.Follow = null;
            vcam.LookAt = null;
            vcam.PreviousStateIsValid = false;
            _cameraBound = true;
            ApplyCameraAndParallax();
        }

        void UnbindCamera()
        {
            if (!_cameraBound)
            {
                return;
            }

            var vcam = MainGameManager.Instance?.MainMapVCam;
            if (vcam != null)
            {
                vcam.Follow = _savedFollow;
                vcam.LookAt = _savedLookAt;
                vcam.PreviousStateIsValid = false;
            }

            _savedFollow = null;
            _savedLookAt = null;
            _cameraBound = false;
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
            FindTopHit(worldPos)?.OpenPanel();
        }

        void ClearHover()
        {
            _hovered?.SetHighlight(false);
            _hovered = null;
        }

        SecretBaseInteractable FindTopHit(Vector2 worldPos)
        {
            SecretBaseInteractable best = null;
            var bestOrder = int.MinValue;

            var cols = Physics2D.OverlapPointAll(worldPos);
            for (int i = 0; i < cols.Length; i++)
            {
                var item = cols[i].GetComponent<SecretBaseInteractable>()
                    ?? cols[i].GetComponentInParent<SecretBaseInteractable>();
                if (item == null || !item.isActiveAndEnabled)
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

            if (_interactables == null)
            {
                return best;
            }

            for (int i = 0; i < _interactables.Length; i++)
            {
                var item = _interactables[i];
                if (item == null || !item.isActiveAndEnabled || !item.ContainsPoint(worldPos))
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

        static bool IsPointerOverHudButton(Vector2 screenPos)
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
