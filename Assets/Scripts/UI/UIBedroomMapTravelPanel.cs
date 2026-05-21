using System.Collections.Generic;
using System.Xml.Linq;
using cfg.demo;
using My.Config;
using My.Map;
using My.MiniGame.Dream;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 基地地图传送：选地图 + 缩略图标点 + 右下存档点列表
    public class UIBedroomMapTravelPanel : PanelWithInput
    {
        public const string PanelIdConst = "UIBedroomMapTravel";

        [Header("地图列表")]
        [SerializeField] RectTransform mapListContent;
        [SerializeField] BedroomDeployMapRowView mapRowTemplate;

        [Header("右侧地图预览")]
        [SerializeField] RectTransform mapThumbRoot;
        [SerializeField] Image detailThumb;
        [SerializeField] TextMeshProUGUI detailDesc;
        [SerializeField] RectTransform markerRoot;
        [SerializeField] BedroomDeploySavePointMarkerView savePointMarkerTemplate;

        [Header("右下角存档点列表")]
        [SerializeField] GameObject savePointListOverlay;
        [SerializeField] RectTransform savePointListContent;
        [SerializeField] BedroomDeploySavePointListRowView savePointListRowTemplate;

        [Header("操作")]
        [SerializeField] Button btnPrimary;
        [SerializeField] Button btnClose;

        readonly List<AreaOverlayStateInfo> _safeMaps = new();

        readonly List<BedroomDeployMapRowView> _spawnedMapRows = new();
        readonly List<SavePointMarkerVm> _markers = new();
        readonly List<BedroomDeploySavePointMarkerView> _spawnedMarkers = new();
        readonly List<BedroomDeploySavePointListRowView> _spawnedListRows = new();

        TextMeshProUGUI _btnPrimaryLabel;
        AreaOverlayStateInfo _selectedMap;
        SavePoint _selectedSavePoint;

        public override int FocusPriority => 805;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
                panelId = PanelIdConst;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            layer = UILayer.Popup;

            if (detailThumb != null)
                detailThumb.preserveAspect = true;

            if (btnPrimary != null)
                _btnPrimaryLabel = btnPrimary.GetComponentInChildren<TextMeshProUGUI>(true);

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(TryCloseSelf);
            }

            if (btnPrimary != null)
            {
                btnPrimary.onClick.RemoveAllListeners();
                btnPrimary.onClick.AddListener(OnClickTeleport);
            }


            if (mapRowTemplate != null)
                mapRowTemplate.gameObject.SetActive(false);
            if (savePointMarkerTemplate != null)
                savePointMarkerTemplate.gameObject.SetActive(false);
            if (savePointListRowTemplate != null)
                savePointListRowTemplate.gameObject.SetActive(false);

            ResolveMapThumbRefs();
        }

        void ResolveMapThumbRefs()
        {
            if (mapThumbRoot == null)
            {
                foreach (var rt in GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "MapThumbRoot")
                    {
                        mapThumbRoot = rt;
                        break;
                    }
                }
            }

            if (markerRoot == null && mapThumbRoot != null)
            {
                var found = mapThumbRoot.Find("MarkerRoot");
                if (found != null)
                    markerRoot = found as RectTransform;
            }

            if (savePointMarkerTemplate == null && markerRoot != null)
                savePointMarkerTemplate = markerRoot.GetComponentInChildren<BedroomDeploySavePointMarkerView>(true);

            if (savePointListOverlay == null && mapThumbRoot != null)
            {
                var found = mapThumbRoot.Find("SavePointListOverlay");
                if (found != null)
                    savePointListOverlay = found.gameObject;
            }

            if (savePointListContent == null && savePointListOverlay != null)
            {
                var found = savePointListOverlay.transform.Find("OverlayListContent");
                if (found != null)
                    savePointListContent = found as RectTransform;
            }

            if (savePointListRowTemplate == null && savePointListContent != null)
                savePointListRowTemplate = savePointListContent.GetComponentInChildren<BedroomDeploySavePointListRowView>(true);
        }

        public override void Setup(object data = null)
        {
            CollectHumanTravelMap(_safeMaps);
            RebuildMapList();
            SelectMap(_safeMaps.Count > 0 ? _safeMaps[0] : null);
        }

        private void CollectHumanTravelMap(List<AreaOverlayStateInfo> outMaps)
        {
            outMaps.Clear();
            var tb = CfgMgr.Cfgs?.TbAreaOverlayStateInfo;
            if (tb?.DataList == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var dayPeriod = glm.DayPeriod;
            foreach (var m in tb.DataList)
            {
                if (m == null || string.IsNullOrEmpty(m.Id))
                {
                    continue;
                }

                if (!m.CanTeleport)
                {
                    continue;
                }

                if (m.DayPeriodLimit != 0)
                {
                    if(m.DayPeriodLimit == 1 && dayPeriod != GameLogicManager.EDayPeriod.Day)
                    {
                        continue;
                    }
                    if (m.DayPeriodLimit == 2 && dayPeriod != GameLogicManager.EDayPeriod.Night)
                    {
                        continue;
                    }
                }

                // 危险区域不可传送
                if(m.IsDangerArea)
                {
                    continue;
                }

                var conds = m.HuntingUnlockConds;
                bool passed = true;
                if (conds != null && glm != null)
                {
                    foreach (var cond in conds)
                    {
                        if (!glm.CheckCommonCond(cond))
                        {
                            passed = false;
                            break;
                        }
                    }
                }

                if (passed)
                {
                    outMaps.Add(m);
                }
            }

            outMaps.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }


        public override bool OnCancel()
        {
            TryCloseSelf();
            return true;
        }

        void TryCloseSelf()
        {
            UIManager.Instance?.HidePanel(panelId);
        }

        void RebuildMapList()
        {
            foreach (var row in _spawnedMapRows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            _spawnedMapRows.Clear();
            if (mapListContent == null || mapRowTemplate == null)
            {
                Debug.LogError("[UIBedroomMapTravel] mapListContent or mapRowTemplate not assigned.");
                return;
            }

            foreach (var map in _safeMaps)
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

        void OnMapRowClicked(AreaOverlayStateInfo map)
        {
            SelectMap(map);
        }

        void SelectMap(AreaOverlayStateInfo map)
        {
            _selectedMap = map;
            if (detailDesc != null)
                detailDesc.text = map != null ? map.Desc : "暂无地图配置。";

            if (detailThumb != null)
            {
                var thumbName = map?.BelongVariantInfo?.ThumbMap ?? string.Empty;
                Sprite sp = null;
                if (!string.IsNullOrEmpty(thumbName))
                    sp = Resources.Load<Sprite>($"MiniMap/{thumbName}");
                detailThumb.sprite = sp;
                detailThumb.color = sp != null ? Color.white : new Color(0.15f, 0.16f, 0.2f, 1f);
                DreamUISpriteUtil.EnsureWhiteSprite(detailThumb);
            }

            RefreshMapRowSelection();
            RebuildSavePointDisplay();
        }

        void RefreshMapRowSelection()
        {
            for (var i = 0; i < _spawnedMapRows.Count && i < _safeMaps.Count; i++)
            {
                var on = _selectedMap != null && ReferenceEquals(_selectedMap, _safeMaps[i]);
                _spawnedMapRows[i].SetSelected(on);
            }
        }

        public void CollectSavePointMarkers(
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


        void RebuildSavePointDisplay()
        {
            ResolveMapThumbRefs();
            ClearSavePointSpawned();
            _selectedSavePoint = null;

            var hasMap = _selectedMap != null;
            if (savePointListOverlay != null)
                savePointListOverlay.SetActive(hasMap);
            if (markerRoot != null)
                markerRoot.gameObject.SetActive(hasMap);

            if (!hasMap)
            {
                RefreshPrimaryButton();
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            CollectSavePointMarkers(_selectedMap.Id, glm, _markers);

            var parentMarkers = markerRoot != null ? markerRoot : mapThumbRoot;
            if (parentMarkers != null && savePointMarkerTemplate != null)
            {
                foreach (var vm in _markers)
                {
                    if (!vm.HasMapPosition || vm.Config == null)
                        continue;

                    var marker = Instantiate(savePointMarkerTemplate, parentMarkers);
                    marker.gameObject.SetActive(true);
                    var captured = vm.Config;
                    marker.Clicked -= OnSavePointClicked;
                    marker.Clicked += OnSavePointClicked;
                    marker.Bind(captured, vm.NormPos01, false);
                    _spawnedMarkers.Add(marker);
                }
            }

            if (savePointListContent != null && savePointListRowTemplate != null)
            {
                foreach (var vm in _markers)
                {
                    if (vm.Config == null)
                        continue;

                    var row = Instantiate(savePointListRowTemplate, savePointListContent);
                    row.gameObject.SetActive(true);
                    var captured = vm.Config;
                    row.Clicked -= OnSavePointClicked;
                    row.Clicked += OnSavePointClicked;
                    row.Bind(captured, false);
                    _spawnedListRows.Add(row);
                }
            }

            if (_markers.Count > 0 && _markers[0].Config != null)
                SelectSavePoint(_markers[0].Config);
            else
                RefreshPrimaryButton();
        }

        void ClearSavePointSpawned()
        {
            foreach (var m in _spawnedMarkers)
            {
                if (m != null)
                    Destroy(m.gameObject);
            }

            _spawnedMarkers.Clear();

            foreach (var r in _spawnedListRows)
            {
                if (r != null)
                    Destroy(r.gameObject);
            }

            _spawnedListRows.Clear();
            _markers.Clear();
        }

        void OnSavePointClicked(SavePoint sp)
        {
            SelectSavePoint(sp);
        }

        void SelectSavePoint(SavePoint sp)
        {
            _selectedSavePoint = sp;
            var id = sp?.SavePointId;

            foreach (var m in _spawnedMarkers)
            {
                if (m == null) continue;
                var bound = m.BoundSavePoint;
                m.SetSelected(bound != null && bound.SavePointId == id);
            }

            foreach (var row in _spawnedListRows)
            {
                if (row == null) continue;
                var bound = row.BoundSavePoint;
                row.SetSelected(bound != null && bound.SavePointId == id);
            }

            RefreshPrimaryButton();
        }

        void RefreshPrimaryButton()
        {
            if (btnPrimary == null)
                return;

            var glm = MainGameManager.Instance?.gameLogicManager;
            btnPrimary.interactable = glm != null && _selectedSavePoint != null;
            if (_btnPrimaryLabel != null)
                _btnPrimaryLabel.text = "传送";
        }

        void OnClickTeleport()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                Debug.LogWarning("[UIBedroomMapTravel] GameLogicManager missing.");
                return;
            }

            if (_selectedSavePoint == null)
                return;

            UIManager.Instance?.HidePanel(panelId);

            glm.PreparePlayerSwitchArea(_selectedMap.Id, true, targetPoint: _selectedSavePoint.TargetNamedPoint);

            //if (!SavePointUnlockHelper.TryTeleportToSavePoint(glm, _selectedSavePoint.SavePointId, out var reason))
            //    Debug.LogWarning("[UIBedroomMapTravel] Teleport failed: " + reason);
        }

    }
}
