using System.Collections.Generic;
using My;
using cfg.demo;
using My.Config;
using My.MiniGame.Dream;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 卧室床铺：狩猎传送 / 入梦入口；UI 层级请在 Prefab 中搭建并拖引用。
    public class UIBedroomBedActPanel : PanelWithInput
    {
        public const string PanelIdConst = "UIBedroomBedAct";

        [Header("页签与页面")]
        [SerializeField] private RectTransform pageHunt;
        [SerializeField] private RectTransform pageDream;
        [SerializeField] private Button tabHunt;
        [SerializeField] private Button tabDream;

        [Header("狩猎：列表（左侧 Content + 行模板）")]
        [SerializeField] private RectTransform mapListContent;
        [SerializeField] private Button mapRowTemplate;

        [Header("狩猎：详情")]
        [SerializeField] private Image detailThumb;
        [SerializeField] private TextMeshProUGUI detailDesc;
        [SerializeField] private Button btnTeleport;

        [Header("入梦")]
        [SerializeField] private Button btnDream;

        [Header("关闭")]
        [SerializeField] private Button btnClose;

        private readonly List<MapAreaInfo> _huntMaps = new();
        private readonly List<Button> _spawnedMapRows = new();
        private TextMeshProUGUI _btnTeleportLabel;
        private MapAreaInfo _selected;

        // 地图列表：无悬停渐变，仅用底色区分选中
        private static readonly Color MapRowBgNormal = new Color(0.22f, 0.24f, 0.30f, 1f);
        private static readonly Color MapRowBgSelected = new Color(0.40f, 0.52f, 0.70f, 1f);

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

            if (btnTeleport != null)
                _btnTeleportLabel = btnTeleport.GetComponentInChildren<TextMeshProUGUI>(true);

            WireButtons();
        }

        private void WireButtons()
        {
            if (tabHunt != null)
            {
                tabHunt.onClick.RemoveAllListeners();
                tabHunt.onClick.AddListener(() => ShowPage(0));
            }

            if (tabDream != null)
            {
                tabDream.onClick.RemoveAllListeners();
                tabDream.onClick.AddListener(() => ShowPage(1));
            }

            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(TryCloseSelf);
            }

            if (btnTeleport != null)
            {
                btnTeleport.onClick.RemoveAllListeners();
                btnTeleport.onClick.AddListener(OnClickTeleport);
            }

            if (btnDream != null)
            {
                btnDream.onClick.RemoveAllListeners();
                btnDream.onClick.AddListener(OnClickOpenDream);
            }

            if (mapRowTemplate != null)
                ApplySimpleMapRowButton(mapRowTemplate);
        }

        private void Update()
        {
            if (!IsVisible) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                TryCloseSelf();
        }

        public override void Setup(object data = null)
        {
            CollectHuntMaps();
            RebuildMapList();
            SelectMap(_huntMaps.Count > 0 ? _huntMaps[0] : null);
            ShowPage(0);
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

        private void CollectHuntMaps()
        {
            _huntMaps.Clear();
            var tb = CfgMgr.Cfgs?.TbMapAreaInfo;
            if (tb?.DataList == null) return;

            foreach (var m in tb.DataList)
            {
                if (m == null || string.IsNullOrEmpty(m.Id)) continue;
                if (!m.HuntingTarget) continue;

                if(m.DayPeriodLimit == 1)
                {
                    continue;
                }

                var conds = m.HuntingUnlockConds;
                bool passed = true;
                foreach (var cond in conds)
                {
                    if (!MainGameManager.Instance.gameLogicManager.CheckCommonCond(cond))
                    {
                        passed = true;
                    }
                }

                if(passed)
                {
                    _huntMaps.Add(m);
                }
            }

            _huntMaps.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        private void ShowPage(int index)
        {
            if (pageHunt != null) pageHunt.gameObject.SetActive(index == 0);
            if (pageDream != null) pageDream.gameObject.SetActive(index == 1);
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
                Debug.LogError("[UIBedroomBedAct] mapListContent or mapRowTemplate not assigned in prefab.");
                return;
            }

            mapRowTemplate.gameObject.SetActive(false);

            foreach (var map in _huntMaps)
            {
                var row = Instantiate(mapRowTemplate, mapListContent);
                row.gameObject.SetActive(true);
                ApplySimpleMapRowButton(row);
                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = map.Name;
                var captured = map;
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectMap(captured));
                _spawnedMapRows.Add(row);
            }
        }

        private static void ApplySimpleMapRowButton(Button row)
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
                var on = _selected != null && ReferenceEquals(_selected, _huntMaps[i]);
                img.color = on ? MapRowBgSelected : MapRowBgNormal;
            }
        }

        private void SelectMap(MapAreaInfo map)
        {
            _selected = map;
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

            if (btnTeleport != null)
                btnTeleport.interactable = map != null && MainGameManager.Instance != null &&
                                            MainGameManager.Instance.gameLogicManager != null;

            if (_btnTeleportLabel != null)
                _btnTeleportLabel.text = "传送";

            RefreshMapListSelectionVisual();
        }

        private void OnClickTeleport()
        {
            if (_selected == null) return;
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                Debug.LogWarning("[UIBedroomBedAct] GameLogicManager missing.");
                return;
            }

            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);

            glm.PreparePlayerSwitchArea(_selected.Id, true);

            // 潜入目标地图：强制真身形态，衣装在 PostNewAreaLoaded / civil 流程中应用
            glm.ForcePlayerHumanMode(false);
        }

        private void OnClickOpenDream()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);
            DreamInfiltrationBootstrap.OpenEntry();
        }
    }
}
