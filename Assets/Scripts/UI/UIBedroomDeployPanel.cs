using System.Collections.Generic;
using cfg.demo;
using My.Map;
using My.MiniGame.Dream;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 基地出击：仅真身潜入（选地图 → 地图初始点）
    public class UIBedroomDeployPanel : PanelWithInput
    {
        public const string PanelIdConst = "UIBedroomDeploy";

        [Header("地图列表")]
        [SerializeField] RectTransform mapListContent;
        [SerializeField] BedroomDeployMapRowView mapRowTemplate;

        [Header("详情与操作")]
        [SerializeField] Image detailThumb;
        [SerializeField] TextMeshProUGUI detailDesc;
        [SerializeField] Button btnPrimary;
        [SerializeField] Button btnClose;

        readonly List<MapAreaInfo> _huntMaps = new();
        readonly List<BedroomDeployMapRowView> _spawnedMapRows = new();

        TextMeshProUGUI _btnPrimaryLabel;
        MapAreaInfo _selectedMap;

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
                btnPrimary.onClick.AddListener(OnClickInfiltrate);
            }

            if (mapRowTemplate != null)
                mapRowTemplate.gameObject.SetActive(false);
        }

        public override void Setup(object data = null)
        {
            BedroomDeployMapUtil.CollectHuntMaps(_huntMaps);
            RebuildMapList();
            SelectMap(_huntMaps.Count > 0 ? _huntMaps[0] : null);
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
                Debug.LogError("[UIBedroomDeploy] mapListContent or mapRowTemplate not assigned.");
                return;
            }

            foreach (var map in _huntMaps)
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

        void OnMapRowClicked(MapAreaInfo map)
        {
            SelectMap(map);
        }

        void SelectMap(MapAreaInfo map)
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

            for (var i = 0; i < _spawnedMapRows.Count && i < _huntMaps.Count; i++)
            {
                var on = _selectedMap != null && ReferenceEquals(_selectedMap, _huntMaps[i]);
                _spawnedMapRows[i].SetSelected(on);
            }

            RefreshPrimaryButton();
        }

        void RefreshPrimaryButton()
        {
            if (btnPrimary == null)
                return;

            var glm = MainGameManager.Instance?.gameLogicManager;
            btnPrimary.interactable = glm != null && _selectedMap != null;
            if (_btnPrimaryLabel != null)
                _btnPrimaryLabel.text = "潜入";
        }

        void OnClickInfiltrate()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                Debug.LogWarning("[UIBedroomDeploy] GameLogicManager missing.");
                return;
            }

            if (_selectedMap == null)
                return;

            UIManager.Instance?.HidePanel(panelId);
            var spawn = MapSpawnPointUtil.ResolveMapInitialSpawnPoint(_selectedMap.Id);
            glm.PreparePlayerSwitchArea(_selectedMap.Id, true, targetPoint: spawn);
            glm.ForcePlayerHumanMode(false);
        }
    }
}
