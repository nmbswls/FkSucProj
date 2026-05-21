using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.MiniGame.Dream;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 卧室/基地出击：人类传送（地图+已解锁存档点）/ 真身潜入（仅地图初始点）
    public class UIBedroomDeployPanel : PanelWithInput
    {
        public const string PanelIdConst = "UIBedroomDeploy";

        [Header("页签与页面")]
        [SerializeField] private RectTransform pageHuman;
        [SerializeField] private RectTransform pageInfiltrate;
        [SerializeField] private Button tabHuman;
        [SerializeField] private Button tabInfiltrate;

        [Header("地图列表")]
        [SerializeField] private RectTransform mapListContent;
        [SerializeField] private Button mapRowTemplate;

        [Header("人类页签：存档点列表（可与地图行模板共用）")]
        [SerializeField] private GameObject savePointSectionRoot;
        [SerializeField] private RectTransform savePointListContent;
        [SerializeField] private Button savePointRowTemplate;

        [Header("详情与操作")]
        [SerializeField] private Image detailThumb;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private Button btnPrimary;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnRumorIntel;

        private readonly List<MapAreaInfo> _huntMaps = new();
        private readonly List<Button> _spawnedMapRows = new();
        private readonly List<SavePoint> _savePointsForMap = new();
        private readonly List<Button> _spawnedSavePointRows = new();

        private TextMeshProUGUI _btnPrimaryLabel;
        private MapAreaInfo _selectedMap;
        private SavePoint _selectedSavePoint;
        private int _tabIndex;

        private static readonly Color RowBgNormal = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color RowBgSelected = new Color(0.40f, 0.52f, 0.70f, 1f);

        public override int FocusPriority => 805;

        private void Awake()
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

            WireButtons();
        }

        private void WireButtons()
        {
            if (tabHuman != null)
            {
                tabHuman.onClick.RemoveAllListeners();
                tabHuman.onClick.AddListener(() => ShowTab(0));
            }

            if (tabInfiltrate != null)
            {
                tabInfiltrate.onClick.RemoveAllListeners();
                tabInfiltrate.onClick.AddListener(() => ShowTab(1));
            }

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(TryCloseSelf);
            }

            if (btnPrimary != null)
            {
                btnPrimary.onClick.RemoveAllListeners();
                btnPrimary.onClick.AddListener(OnClickPrimary);
            }

            if (btnRumorIntel != null)
            {
                btnRumorIntel.onClick.RemoveAllListeners();
                btnRumorIntel.onClick.AddListener(OnClickOpenRumorIntel);
            }

            if (mapRowTemplate != null)
                ApplySimpleRowButton(mapRowTemplate);

            var spTpl = savePointRowTemplate != null ? savePointRowTemplate : mapRowTemplate;
            if (spTpl != null)
                ApplySimpleRowButton(spTpl);
        }

        public override void Setup(object data = null)
        {
            BedroomDeployMapUtil.CollectHuntMaps(_huntMaps);
            RebuildMapList();
            SelectMap(_huntMaps.Count > 0 ? _huntMaps[0] : null);
            ShowTab(0);
        }

        public override bool OnCancel()
        {
            TryCloseSelf();
            return true;
        }

        private void TryCloseSelf()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);
        }

        private void ShowTab(int index)
        {
            _tabIndex = index;
            if (pageHuman != null) pageHuman.gameObject.SetActive(index == 0);
            if (pageInfiltrate != null) pageInfiltrate.gameObject.SetActive(index == 1);

            var showSavePoints = index == 0;
            if (savePointSectionRoot != null)
                savePointSectionRoot.SetActive(showSavePoints);

            RefreshPrimaryButton();
        }

        private void RebuildMapList()
        {
            foreach (var b in _spawnedMapRows)
            {
                if (b != null) Destroy(b.gameObject);
            }

            _spawnedMapRows.Clear();
            if (mapListContent == null || mapRowTemplate == null)
            {
                Debug.LogError("[UIBedroomDeploy] mapListContent or mapRowTemplate not assigned.");
                return;
            }

            mapRowTemplate.gameObject.SetActive(false);

            foreach (var map in _huntMaps)
            {
                var row = Instantiate(mapRowTemplate, mapListContent);
                row.gameObject.SetActive(true);
                ApplySimpleRowButton(row);
                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = map.Name;
                var captured = map;
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectMap(captured));
                _spawnedMapRows.Add(row);
            }
        }

        private void RebuildSavePointList()
        {
            foreach (var b in _spawnedSavePointRows)
            {
                if (b != null) Destroy(b.gameObject);
            }

            _spawnedSavePointRows.Clear();
            _savePointsForMap.Clear();
            _selectedSavePoint = null;

            if (_tabIndex != 0 || _selectedMap == null)
            {
                RefreshPrimaryButton();
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null)
                _savePointsForMap.AddRange(SavePointUnlockHelper.GetUnlockedForMap(glm, _selectedMap.Id));

            var content = savePointListContent;
            var tpl = savePointRowTemplate != null ? savePointRowTemplate : mapRowTemplate;
            if (content == null || tpl == null)
            {
                RefreshPrimaryButton();
                return;
            }

            tpl.gameObject.SetActive(false);
            foreach (var sp in _savePointsForMap)
            {
                var row = Instantiate(tpl, content);
                row.gameObject.SetActive(true);
                ApplySimpleRowButton(row);
                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = sp.DisplayName;
                var captured = sp;
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectSavePoint(captured));
                _spawnedSavePointRows.Add(row);
            }

            if (_savePointsForMap.Count > 0)
                SelectSavePoint(_savePointsForMap[0]);

            RefreshSavePointListVisual();
            RefreshPrimaryButton();
        }

        private static void ApplySimpleRowButton(Button row)
        {
            if (row == null) return;
            row.transition = Selectable.Transition.None;
            var c = row.colors;
            c.fadeDuration = 0f;
            c.colorMultiplier = 1f;
            c.highlightedColor = c.normalColor;
            c.pressedColor = c.normalColor;
            c.selectedColor = c.normalColor;
            row.colors = c;
        }

        private void RefreshMapListSelectionVisual()
        {
            for (var i = 0; i < _spawnedMapRows.Count && i < _huntMaps.Count; i++)
            {
                var row = _spawnedMapRows[i];
                if (row == null) continue;
                var img = row.targetGraphic as Image;
                if (img == null) continue;
                var on = _selectedMap != null && ReferenceEquals(_selectedMap, _huntMaps[i]);
                img.color = on ? RowBgSelected : RowBgNormal;
            }
        }

        private void RefreshSavePointListVisual()
        {
            for (var i = 0; i < _spawnedSavePointRows.Count && i < _savePointsForMap.Count; i++)
            {
                var row = _spawnedSavePointRows[i];
                if (row == null) continue;
                var img = row.targetGraphic as Image;
                if (img == null) continue;
                var on = _selectedSavePoint != null && ReferenceEquals(_selectedSavePoint, _savePointsForMap[i]);
                img.color = on ? RowBgSelected : RowBgNormal;
            }
        }

        private void SelectMap(MapAreaInfo map)
        {
            _selectedMap = map;
            if (detailDesc != null)
                detailDesc.text = map != null ? map.Desc : "暂无地图配置。";

            if (detailThumb != null)
            {
                Sprite sp = null;
                if (map != null)
                    sp = Resources.Load<Sprite>($"UI/MapThumbs/{map.Id}");
                detailThumb.sprite = sp;
                detailThumb.color = sp != null ? Color.white : new Color(0.15f, 0.16f, 0.2f, 1f);
                DreamUISpriteUtil.EnsureWhiteSprite(detailThumb);
            }

            RebuildSavePointList();
            RefreshMapListSelectionVisual();
        }

        private void SelectSavePoint(SavePoint sp)
        {
            _selectedSavePoint = sp;
            RefreshSavePointListVisual();
            RefreshPrimaryButton();
        }

        private void RefreshPrimaryButton()
        {
            if (btnPrimary == null) return;

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (_tabIndex == 0)
            {
                btnPrimary.interactable = glm != null && _selectedSavePoint != null;
                if (_btnPrimaryLabel != null)
                    _btnPrimaryLabel.text = "传送";
            }
            else
            {
                btnPrimary.interactable = glm != null && _selectedMap != null;
                if (_btnPrimaryLabel != null)
                    _btnPrimaryLabel.text = "潜入";
            }
        }

        private void OnClickPrimary()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                Debug.LogWarning("[UIBedroomDeploy] GameLogicManager missing.");
                return;
            }

            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);

            if (_tabIndex == 0)
            {
                if (_selectedSavePoint == null) return;
                if (!SavePointUnlockHelper.TryTeleportToSavePoint(glm, _selectedSavePoint.SavePointId, out var reason))
                    Debug.LogWarning("[UIBedroomDeploy] Teleport failed: " + reason);
                return;
            }

            if (_selectedMap == null) return;
            var spawn = MapSpawnPointUtil.ResolveMapInitialSpawnPoint(_selectedMap.Id);
            glm.PreparePlayerSwitchArea(_selectedMap.Id, true, targetPoint: spawn);
            glm.ForcePlayerHumanMode(false);
        }

        private void OnClickOpenRumorIntel()
        {
            if (_selectedMap == null)
            {
                Debug.LogWarning("[UIBedroomDeploy] Select a map before opening intel shop.");
                return;
            }

            RumorIntelShopPanel.OpenForMap(_selectedMap.Id);
        }
    }
}
