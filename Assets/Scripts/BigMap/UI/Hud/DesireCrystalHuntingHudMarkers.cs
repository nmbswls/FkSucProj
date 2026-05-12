using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Map.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    /// <summary>
    /// 猎杀模式下，为附着欲望结晶的 NPC 在 HUD（Screen Space - Camera）上显示屏幕跟随标记，避免进入世界后处理导致的失色。
    /// 与 SceneNpcPresenter 解耦：世界锚点来自 AOI + IScenePresentation。
    /// </summary>
    public class DesireCrystalHuntingHudMarkers : MonoBehaviour
    {
        public static readonly Vector3 WorldAnchorOffset = new Vector3(0f, 0.25f, 0f);

        /// <summary>
        /// UI 层使用的狩猎结晶标记（粒子）；与场景旧路径共用 Resources 预制。
        /// </summary>
        public const string DesireCrystalMarkerResourcePath = "Prefab/SceneEffect/desire_crystal_marker";

        [SerializeField]
        RectTransform markersParent;

        [Tooltip("非空时优先于 Resources 路径")]
        [SerializeField]
        GameObject markerPrefab;

        [Tooltip("instantiate 后粒子根 localScale（根为 Transform 型预制）")]
        [SerializeField]
        float markerVisualScale = 1f;

        [SerializeField]
        Vector2 markerSize = new Vector2(48f, 48f);

        GameObject _markerPrefabResolved;

        readonly Dictionary<long, RectTransform> _byEntity = new();
        readonly Queue<RectTransform> _pool = new();
        readonly List<long> _removeIds = new();

        Camera _worldCam;

        void Awake()
        {
            EnsureMarkersParent();
            if (markerPrefab != null)
            {
                _markerPrefabResolved = markerPrefab;
            }
            else
            {
                _markerPrefabResolved = Resources.Load<GameObject>(DesireCrystalMarkerResourcePath);
                if (_markerPrefabResolved == null)
                {
                    Debug.LogWarning($"DesireCrystalHuntingHudMarkers: Resources.Load failed '{DesireCrystalMarkerResourcePath}', using Image fallback.");
                }
            }
        }

        void EnsureMarkersParent()
        {
            if (markersParent != null)
            {
                return;
            }

            var existing = transform.Find("DesireCrystalHuntingMarkers");
            if (existing != null)
            {
                markersParent = existing as RectTransform;
                return;
            }

            var go = new GameObject("DesireCrystalHuntingMarkers", typeof(RectTransform));
            go.layer = gameObject.layer;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            markersParent = rt;
        }

        void OnEnable()
        {
            OverworldHUDPanel.HunterModeChanged += OnHunterModeChanged;
        }

        void OnDisable()
        {
            OverworldHUDPanel.HunterModeChanged -= OnHunterModeChanged;
            HideAllVisuals();
        }

        void OnHunterModeChanged(bool _)
        {
            if (OverworldHUDPanel.Instance == null || !OverworldHUDPanel.Instance.IsHunterMode)
            {
                HideAllVisuals();
            }
        }

        void LateUpdate()
        {
            if (OverworldHUDPanel.Instance == null || !OverworldHUDPanel.Instance.IsHunterMode)
            {
                HideAllVisuals();
                return;
            }

            var mgm = MainGameManager.Instance;
            var glm = mgm?.gameLogicManager;
            var aoi = mgm?.AOIManager;
            var area = glm?.AreaManager;
            if (area == null || aoi == null)
            {
                HideAllVisuals();
                return;
            }

            TryCacheWorldCamera();
            var ui = UIManager.Instance;
            if (ui == null || markersParent == null)
            {
                return;
            }

            var canvas = markersParent.GetComponentInParent<Canvas>();
            if (!TryResolveHudProjectionCameras(canvas, ui, out Camera worldToScreenCam, out Camera screenToLocalCam))
            {
                return;
            }

            var want = new HashSet<long>();
            foreach (var ent in area.Repo.Loaded.Values)
            {
                if (ent is NpcUnitLogicEntity npc && npc.HasAttachedDesireCrystal)
                {
                    want.Add(npc.Id);
                }
            }

            _removeIds.Clear();
            foreach (var kv in _byEntity)
            {
                if (!want.Contains(kv.Key))
                {
                    _removeIds.Add(kv.Key);
                }
            }

            foreach (var id in _removeIds)
            {
                RecycleMarker(id);
            }

            foreach (var id in want)
            {
                var pres = aoi.GetActivePresentation(id);
                if (pres == null)
                {
                    RecycleMarker(id);
                    continue;
                }

                Vector3 anchor = pres.GetWorldPosition() + WorldAnchorOffset;
                Vector3 screen = worldToScreenCam.WorldToScreenPoint(anchor);
                if (screen.z <= 0f)
                {
                    RecycleMarker(id);
                    continue;
                }

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        markersParent,
                        new Vector2(screen.x, screen.y),
                        screenToLocalCam,
                        out Vector2 local))
                {
                    continue;
                }

                RectTransform markerRt = RentMarker(id);
                markerRt.gameObject.SetActive(true);
                markerRt.anchoredPosition = local;
            }
        }

        void TryCacheWorldCamera()
        {
            if (_worldCam != null && _worldCam.gameObject.activeInHierarchy)
            {
                return;
            }

            if (MainGameManager.Instance?.CameraCtrl != null)
            {
                _worldCam = MainGameManager.Instance.CameraCtrl.GetComponent<Camera>();
            }

            if (_worldCam == null)
            {
                _worldCam = Camera.main;
            }
        }

        RectTransform RentMarker(long entityId)
        {
            if (_byEntity.TryGetValue(entityId, out var rt))
            {
                return rt;
            }

            var fromPool = _pool.Count > 0;
            if (fromPool)
            {
                rt = _pool.Dequeue();
            }
            else
            {
                rt = CreateMarkerInstance();
            }

            _byEntity[entityId] = rt;
            rt.SetParent(markersParent, false);
            SetupMarkerTransform(rt);
            if (fromPool)
            {
                RestartMarkerParticles(rt);
            }

            return rt;
        }

        static void SetupMarkerTransform(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }

        RectTransform CreateMarkerInstance()
        {
            // 插槽必须为 RectTransform，便于 anchoredPosition；粒子预制根多为 Transform，挂到插槽下。
            var slotGo = new GameObject("DesireCrystalMarkerSlot", typeof(RectTransform));
            slotGo.layer = markersParent.gameObject.layer;
            var slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.SetParent(markersParent, false);
            SetupMarkerTransform(slotRt);

            if (_markerPrefabResolved != null)
            {
                var inst = Instantiate(_markerPrefabResolved, slotRt);
                var t = inst.transform;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one * markerVisualScale;

                foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var rend = ps.GetComponent<ParticleSystemRenderer>();
                    if (rend != null)
                    {
                        rend.maskInteraction = SpriteMaskInteraction.None;
                        var canvas = GetComponentInParent<Canvas>();
                        if (canvas != null)
                        {
                            rend.sortingLayerID = canvas.sortingLayerID;
                            rend.sortingOrder = canvas.sortingOrder + 1;
                        }
                    }
                }
            }
            else
            {
                var imgGo = new GameObject("Fallback", typeof(RectTransform));
                imgGo.layer = slotGo.layer;
                var rt0 = imgGo.GetComponent<RectTransform>();
                rt0.SetParent(slotRt, false);
                rt0.anchorMin = rt0.anchorMax = new Vector2(0.5f, 0.5f);
                rt0.pivot = new Vector2(0.5f, 0.5f);
                rt0.sizeDelta = markerSize;
                rt0.anchoredPosition = Vector2.zero;
                var img = imgGo.AddComponent<Image>();
                img.color = new Color(1f, 0.35f, 0.85f, 0.92f);
                img.raycastTarget = false;
            }

            slotRt.gameObject.SetActive(false);
            return slotRt;
        }

        void RecycleMarker(long entityId)
        {
            if (!_byEntity.TryGetValue(entityId, out var rt))
            {
                return;
            }

            _byEntity.Remove(entityId);
            StopMarkerParticles(rt);
            rt.gameObject.SetActive(false);
            _pool.Enqueue(rt);
        }

        void HideAllVisuals()
        {
            foreach (var kv in _byEntity)
            {
                StopMarkerParticles(kv.Value);
                kv.Value.gameObject.SetActive(false);
                _pool.Enqueue(kv.Value);
            }

            _byEntity.Clear();
        }

        static bool TryResolveHudProjectionCameras(Canvas canvas, UIManager ui, out Camera worldToScreenCam, out Camera screenToLocalCam)
        {
            worldToScreenCam = null;
            screenToLocalCam = null;

            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
                {
                    worldToScreenCam = canvas.worldCamera;
                    screenToLocalCam = canvas.worldCamera;
                    return true;
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    if (MainGameManager.Instance?.CameraCtrl != null)
                    {
                        worldToScreenCam = MainGameManager.Instance.CameraCtrl.GetComponent<Camera>();
                    }

                    if (worldToScreenCam == null)
                    {
                        worldToScreenCam = Camera.main;
                    }

                    screenToLocalCam = null;
                    return worldToScreenCam != null;
                }
            }

            worldToScreenCam = ui.UICamera;
            screenToLocalCam = ui.UICamera;
            if (worldToScreenCam == null && MainGameManager.Instance?.CameraCtrl != null)
            {
                worldToScreenCam = MainGameManager.Instance.CameraCtrl.GetComponent<Camera>();
            }

            if (worldToScreenCam == null)
            {
                worldToScreenCam = Camera.main;
            }

            return worldToScreenCam != null;
        }

        static void RestartMarkerParticles(RectTransform slotRt)
        {
            foreach (var ps in slotRt.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }

        static void StopMarkerParticles(RectTransform slotRt)
        {
            foreach (var ps in slotRt.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
