using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class WorldMapPanel : PanelWithInput
    {
        public sealed class OpenArgs
        {
            public string SelectMapId;
        }

        [Header("Layout")]
        [SerializeField] Image dimBackground;
        [SerializeField] RectTransform mapViewport;
        [SerializeField] TextMeshProUGUI closeHintText;

        [Header("Map list")]
        [SerializeField] RectTransform mapListContent;
        [SerializeField] BedroomDeployMapRowView mapRowTemplate;

        [Header("Map viewport")]
        [SerializeField] Image mapImage;
        [SerializeField] TextMeshProUGUI mapEmptyHintText;
        [SerializeField] RectTransform markersRoot;
        [SerializeField] WorldMapLiveMarkerView liveMarkerTemplate;
        [SerializeField] RectTransform savePointMarkerRoot;
        [SerializeField] BedroomDeploySavePointMarkerView savePointMarkerTemplate;

        [Header("Save point list")]
        [SerializeField] GameObject savePointListOverlay;
        [SerializeField] RectTransform savePointListContent;
        [SerializeField] BedroomDeploySavePointListRowView savePointListRowTemplate;

        [Header("Actions")]
        [SerializeField] Button btnTeleport;
        [SerializeField] Button btnClose;
        [SerializeField] TextMeshProUGUI mapDescText;

        [SerializeField] Button btnBackSecret;

        readonly List<AreaOverlayStateInfo> _browsableMaps = new();
        readonly List<BedroomDeployMapRowView> _spawnedMapRows = new();
        readonly List<SavePointMarkerVm> _savePointMarkers = new();
        readonly List<BedroomDeploySavePointMarkerView> _spawnedSavePointMarkers = new();
        readonly List<BedroomDeploySavePointListRowView> _spawnedSavePointRows = new();
        readonly List<WorldMapLiveMarkerView> _spawnedLiveMarkers = new();

        TextMeshProUGUI _btnTeleportLabel;
        WorldMapViewContext _boundContext;
        AreaOverlayStateInfo _selectedMap;
        SavePoint _selectedSavePoint;
        string _currentMapId;
        bool _trackLiveMarkers;
        int _zoomLevel;

        public override bool CanFocus => true;
        public override int FocusPriority => 850;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = WorldMapRuntime.PanelId;
            }

            layer = UILayer.Overlay;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (dimBackground == null)
            {
                dimBackground = transform.Find("Dim")?.GetComponent<Image>();
            }

            if (mapViewport == null)
            {
                mapViewport = transform.Find("MapViewport") as RectTransform;
            }

            if (mapImage == null)
            {
                mapImage = transform.Find("MapViewport/MapImage")?.GetComponent<Image>();
            }

            if (markersRoot == null)
            {
                markersRoot = transform.Find("MapViewport/MarkersRoot") as RectTransform;
            }

            if (closeHintText == null)
            {
                closeHintText = transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
            }

            if (mapImage != null)
            {
                mapImage.preserveAspect = true;
                mapImage.type = Image.Type.Simple;
            }

            if (liveMarkerTemplate != null)
            {
                liveMarkerTemplate.gameObject.SetActive(false);
            }

            if (mapEmptyHintText != null)
            {
                mapEmptyHintText.gameObject.SetActive(false);
            }

            if (btnTeleport != null)
            {
                _btnTeleportLabel = btnTeleport.GetComponentInChildren<TextMeshProUGUI>(true);
                btnTeleport.onClick.RemoveAllListeners();
                btnTeleport.onClick.AddListener(OnClickTeleport);
            }

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(CloseSelf);
            }

            if(btnBackSecret != null)
            {
                btnBackSecret.onClick.RemoveAllListeners();
                btnBackSecret.onClick.AddListener(BackToSecret);
            }

            if (mapRowTemplate != null)
            {
                mapRowTemplate.gameObject.SetActive(false);
            }

            if (savePointMarkerTemplate != null)
            {
                savePointMarkerTemplate.gameObject.SetActive(false);
            }

            if (savePointListRowTemplate != null)
            {
                savePointListRowTemplate.gameObject.SetActive(false);
            }
        }

        public override void Setup(object data = null)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            _currentMapId = glm?.AreaManager?.AreaOverlayId ?? string.Empty;

            var openArgs = data as OpenArgs;
            var preferMapId = openArgs?.SelectMapId ?? _currentMapId;

            WorldMapRuntime.CollectBrowsableMaps(glm, _browsableMaps);
            RebuildMapList();

            SelectMap(ResolveInitialMap(glm, preferMapId));

            if (closeHintText != null)
            {
                closeHintText.text = "M / Esc 关闭";
            }

            if (dimBackground != null)
            {
                dimBackground.color = new Color(0f, 0f, 0f, 0.55f);
            }

            if (MainGameManager.Instance.gameLogicManager.AreaManager.AreaOverlayId == "secret_base_hub")
            {
                btnBackSecret.gameObject.SetActive(false);
            }
            else
            {
                btnBackSecret.gameObject.SetActive(true);
            }
        }

        public override void Show()
        {
            base.Show();
            StopAllCoroutines();
            StartCoroutine(RebuildLiveMarkersWhenLayoutReady());
        }

        IEnumerator RebuildLiveMarkersWhenLayoutReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            RebuildLiveMarkers();
        }

        void RebuildMapList()
        {
            foreach (var row in _spawnedMapRows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedMapRows.Clear();
            if (mapListContent == null || mapRowTemplate == null)
            {
                return;
            }

            foreach (var map in _browsableMaps)
            {
                var row = Instantiate(mapRowTemplate, mapListContent);
                row.gameObject.SetActive(true);
                var captured = map;
                row.Clicked -= OnMapRowClicked;
                row.Clicked += OnMapRowClicked;
                row.Bind(captured, _selectedMap != null && ReferenceEquals(_selectedMap, captured));
                _spawnedMapRows.Add(row);
            }
        }

        AreaOverlayStateInfo ResolveInitialMap(GameLogicManager glm, string preferMapId)
        {
            AreaOverlayStateInfo FindById(string id)
            {
                if (string.IsNullOrEmpty(id))
                {
                    return null;
                }

                foreach (var m in _browsableMaps)
                {
                    if (m.Id == id)
                    {
                        return m;
                    }
                }

                return null;
            }

            AreaOverlayStateInfo PickFirstWithLayer()
            {
                foreach (var m in _browsableMaps)
                {
                    if (WorldMapRuntime.HasBigMapLayer(m.Id))
                    {
                        return m;
                    }
                }

                return _browsableMaps.Count > 0 ? _browsableMaps[0] : null;
            }

            if (glm != null && glm.IsInSecretBase)
            {
                var bookmarkId = glm.LastOpenWorldBeforeSecretBase?.MapId;
                var fromBookmark = FindById(bookmarkId);
                if (fromBookmark != null)
                {
                    return fromBookmark;
                }
            }

            var prefer = FindById(preferMapId);
            if (prefer != null)
            {
                return WorldMapRuntime.HasBigMapLayer(prefer.Id) ? prefer : PickFirstWithLayer();
            }

            return PickFirstWithLayer();
        }

        void OnMapRowClicked(AreaOverlayStateInfo map)
        {
            SelectMap(map);
        }

        void SelectMap(AreaOverlayStateInfo map)
        {
            _selectedMap = map;
            _selectedSavePoint = null;

            if (mapDescText != null)
            {
                mapDescText.text = map != null ? map.Desc : "暂无地图配置。";
            }

            RefreshMapRowSelection();
            RefreshMapViewport();
            RebuildSavePointDisplay();
        }

        void RefreshMapRowSelection()
        {
            for (var i = 0; i < _spawnedMapRows.Count && i < _browsableMaps.Count; i++)
            {
                var on = _selectedMap != null && ReferenceEquals(_selectedMap, _browsableMaps[i]);
                _spawnedMapRows[i].SetSelected(on);
            }
        }

        void RefreshMapViewport()
        {
            ClearLiveMarkers();
            _boundContext = null;
            _trackLiveMarkers = false;

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (_selectedMap == null)
            {
                ApplyMapViewportEmpty(null);
                return;
            }

            var isCurrentMap = glm != null
                && !string.IsNullOrEmpty(_currentMapId)
                && _selectedMap.Id == _currentMapId;
            if (!WorldMapRuntime.TryBuildViewContext(glm, _selectedMap.Id, isCurrentMap, out _boundContext))
            {
                ApplyMapViewportEmpty(null);
                return;
            }

            if (!_boundContext.HasMapLayer)
            {
                ApplyMapViewportEmpty("暂无地图");
                return;
            }

            SetMapEmptyHintVisible(false);
            _trackLiveMarkers = isCurrentMap && glm?.playerLogicEntity != null;

            if (mapImage != null)
            {
                mapImage.type = Image.Type.Simple;
                mapImage.gameObject.SetActive(true);
                mapImage.sprite = _boundContext.MapSprite;
                mapImage.color = _boundContext.MapSprite != null ? Color.white : new Color(0.2f, 0.22f, 0.28f, 1f);
            }

            if (markersRoot != null)
            {
                markersRoot.gameObject.SetActive(true);
            }

            if (_trackLiveMarkers)
            {
                RebuildLiveMarkers();
            }
        }

        void ApplyMapViewportEmpty(string hint)
        {
            if (mapImage != null)
            {
                mapImage.sprite = null;
                mapImage.color = new Color(0.2f, 0.22f, 0.28f, 1f);
                mapImage.gameObject.SetActive(string.IsNullOrEmpty(hint));
            }

            if (markersRoot != null)
            {
                markersRoot.gameObject.SetActive(false);
            }

            SetMapEmptyHintVisible(!string.IsNullOrEmpty(hint), hint);
        }

        void SetMapEmptyHintVisible(bool visible, string text = null)
        {
            if (mapEmptyHintText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                mapEmptyHintText.text = text;
            }

            mapEmptyHintText.gameObject.SetActive(visible);
        }

        void RebuildLiveMarkers()
        {
            ClearLiveMarkers();
            if (!_trackLiveMarkers || _boundContext == null || markersRoot == null || liveMarkerTemplate == null)
            {
                return;
            }

            var rect = markersRoot.rect;
            foreach (var m in _boundContext.Markers)
            {
                var marker = Instantiate(liveMarkerTemplate, markersRoot);
                marker.gameObject.SetActive(true);
                marker.Bind(m, WorldToAnchored(m.worldPos, rect));
                _spawnedLiveMarkers.Add(marker);
            }
        }

        void RebuildSavePointDisplay()
        {
            ClearSavePointSpawned();
            _selectedSavePoint = null;

            var hasMap = _selectedMap != null;
            if (savePointListOverlay != null)
            {
                savePointListOverlay.SetActive(hasMap);
            }

            var showMapMarkers = hasMap && _boundContext != null && _boundContext.HasMapLayer;
            if (savePointMarkerRoot != null)
            {
                savePointMarkerRoot.gameObject.SetActive(showMapMarkers);
            }

            if (!hasMap)
            {
                RefreshTeleportButton();
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            BedroomDeploySavePointPreviewUtil.CollectSavePointMarkers(_selectedMap.Id, glm, _savePointMarkers);

            var parentMarkers = savePointMarkerRoot != null ? savePointMarkerRoot : mapViewport;
            if (showMapMarkers && parentMarkers != null && savePointMarkerTemplate != null)
            {
                foreach (var vm in _savePointMarkers)
                {
                    if (!vm.HasMapPosition || vm.Config == null)
                    {
                        continue;
                    }

                    var marker = Instantiate(savePointMarkerTemplate, parentMarkers);
                    marker.gameObject.SetActive(true);
                    var captured = vm.Config;
                    marker.Clicked -= OnSavePointClicked;
                    marker.Clicked += OnSavePointClicked;
                    marker.Bind(captured, vm.NormPos01, false, vm.CanSelect);
                    _spawnedSavePointMarkers.Add(marker);
                }
            }

            if (savePointListContent != null && savePointListRowTemplate != null)
            {
                foreach (var vm in _savePointMarkers)
                {
                    if (vm.Config == null)
                    {
                        continue;
                    }

                    var row = Instantiate(savePointListRowTemplate, savePointListContent);
                    row.gameObject.SetActive(true);
                    var captured = vm.Config;
                    row.Clicked -= OnSavePointClicked;
                    row.Clicked += OnSavePointClicked;
                    row.Bind(captured, false);
                    _spawnedSavePointRows.Add(row);
                }
            }

            SavePoint defaultPoint = null;
            foreach (var vm in _savePointMarkers)
            {
                if (vm.CanSelect && vm.Config != null)
                {
                    defaultPoint = vm.Config;
                    break;
                }
            }

            if (defaultPoint != null)
            {
                SelectSavePoint(defaultPoint);
            }
            else
            {
                RefreshTeleportButton();
            }
        }

        void ClearSavePointSpawned()
        {
            foreach (var m in _spawnedSavePointMarkers)
            {
                if (m != null)
                {
                    Destroy(m.gameObject);
                }
            }

            _spawnedSavePointMarkers.Clear();

            foreach (var r in _spawnedSavePointRows)
            {
                if (r != null)
                {
                    Destroy(r.gameObject);
                }
            }

            _spawnedSavePointRows.Clear();
            _savePointMarkers.Clear();
        }

        void OnSavePointClicked(SavePoint sp)
        {
            SelectSavePoint(sp);
        }

        void SelectSavePoint(SavePoint sp)
        {
            _selectedSavePoint = sp;
            var id = sp?.SavePointId;

            foreach (var m in _spawnedSavePointMarkers)
            {
                if (m == null)
                {
                    continue;
                }

                var bound = m.BoundSavePoint;
                m.SetSelected(bound != null && bound.SavePointId == id);
            }

            foreach (var row in _spawnedSavePointRows)
            {
                if (row == null)
                {
                    continue;
                }

                var bound = row.BoundSavePoint;
                row.SetSelected(bound != null && bound.SavePointId == id);
            }

            RefreshTeleportButton();
        }

        void RefreshTeleportButton()
        {
            if (btnTeleport == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var canTeleport = glm != null
                && _selectedMap != null
                && _selectedSavePoint != null
                && WorldMapRuntime.CanTeleportToSavePoint(glm, _selectedMap, _selectedSavePoint, out _);

            btnTeleport.interactable = canTeleport;
            if (_btnTeleportLabel != null)
            {
                _btnTeleportLabel.text = "传送";
            }
        }

        void OnClickTeleport()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || _selectedMap == null || _selectedSavePoint == null)
            {
                return;
            }

            if (!WorldMapRuntime.CanTeleportToSavePoint(glm, _selectedMap, _selectedSavePoint, out var reason))
            {
                Debug.LogWarning("[WorldMapPanel] Teleport denied: " + reason);
                return;
            }

            CloseSelf();
            glm.BeginFreeBigMapSession();
            glm.PreparePlayerSwitchArea(_selectedMap.Id, true, targetPoint: _selectedSavePoint.TargetNamedPoint);
        }

        void LateUpdate()
        {
            if (!IsVisible || !_trackLiveMarkers || _boundContext == null || markersRoot == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            var rect = markersRoot.rect;
            foreach (var marker in _spawnedLiveMarkers)
            {
                if (marker == null)
                {
                    continue;
                }

                var e = glm.GetLogicEntity(marker.SourceEntityId, false);
                if (e == null)
                {
                    marker.SetVisible(false);
                    continue;
                }

                marker.SetVisible(true);
                marker.SetAnchoredPosition(WorldToAnchored(e.Pos, rect));
            }
        }

        Vector2 WorldToAnchored(Vector2 worldPos, Rect containerRect)
        {
            if (_boundContext == null)
            {
                return Vector2.zero;
            }

            if (!_boundContext.UsesWorldCoords)
            {
                return new Vector2(
                    (Mathf.Clamp01(worldPos.x) - 0.5f) * containerRect.width,
                    (Mathf.Clamp01(worldPos.y) - 0.5f) * containerRect.height);
            }

            var wMin = _boundContext.WorldMin;
            var wMax = _boundContext.WorldMax;
            if (Mathf.Approximately(wMax.x, wMin.x))
            {
                wMax.x = wMin.x + 0.01f;
            }

            if (Mathf.Approximately(wMax.y, wMin.y))
            {
                wMax.y = wMin.y + 0.01f;
            }

            var nx = Mathf.Clamp01(Mathf.InverseLerp(wMin.x, wMax.x, worldPos.x));
            var ny = Mathf.Clamp01(Mathf.InverseLerp(wMin.y, wMax.y, worldPos.y));

            return new Vector2(
                (nx - 0.5f) * containerRect.width,
                (ny - 0.5f) * containerRect.height);
        }

        void ClearLiveMarkers()
        {
            foreach (var marker in _spawnedLiveMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
            }

            _spawnedLiveMarkers.Clear();
        }

        void CloseSelf()
        {
            UIManager.Instance?.HidePanel(WorldMapRuntime.PanelId);
        }

        /// <summary>
        /// 回家
        /// </summary>
        void BackToSecret()
        {
            if(MainGameManager.Instance.gameLogicManager.AreaManager.AreaOverlayId == "secret_base_hub")
            {
                return;
            }
            MainGameManager.Instance.gameLogicManager.BeginFreeBigMapSession();
            MainGameManager.Instance.gameLogicManager.PreparePlayerSwitchArea("secret_base_hub", true);
        }

        public override void Teardown()
        {
            ClearLiveMarkers();
            ClearSavePointSpawned();
            SetMapEmptyHintVisible(false);
            _boundContext = null;
            _trackLiveMarkers = false;
        }

        public override bool OnCancel()
        {
            CloseSelf();
            return true;
        }

        public override bool OnHotkey(string keyName)
        {
            if (keyName == "M")
            {
                CloseSelf();
                return true;
            }

            return false;
        }

        // TODO: 下版滚轮缩放 — 世界/区域多级地图衔接
        public override bool OnScroll(float deltaY)
        {
            _ = _zoomLevel;
            return false;
        }
    }
}
