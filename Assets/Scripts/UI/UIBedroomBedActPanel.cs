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
                if (m.IsHome) continue;
                if (m.Id == "game_init") continue;
                _huntMaps.Add(m);
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
                var label = row.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = map.Name;
                var captured = map;
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectMap(captured));
                _spawnedMapRows.Add(row);
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
        }

        private void OnClickOpenDream()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HidePanel(panelId);
            DreamInfiltrationBootstrap.OpenEntry();
        }
    }
}
