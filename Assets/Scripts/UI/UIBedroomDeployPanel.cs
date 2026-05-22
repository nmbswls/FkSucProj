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
        [SerializeField] Button btnRumor;
        
        [SerializeField] Button btnClose;

        readonly List<AreaOverlayStateInfo> _huntMaps = new();
        readonly List<BedroomDeployMapRowView> _spawnedMapRows = new();

        TextMeshProUGUI _btnPrimaryLabel;
        AreaOverlayStateInfo _selectedMap;

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
                btnPrimary.onClick.AddListener(OnClickOpenRumorIntel);
            }
            

            if (btnPrimary != null)
            {
                btnPrimary.onClick.RemoveAllListeners();
                btnPrimary.onClick.AddListener(OnClickStartHunting);
            }

            if (mapRowTemplate != null)
                mapRowTemplate.gameObject.SetActive(false);
        }

        public override void Setup(object data = null)
        {
            //BedroomDeployMapUtil.CollectHuntMaps(_huntMaps);
            CollectHuntingTargetMap(_huntMaps);
            RebuildMapList();
            SelectMap(_huntMaps.Count > 0 ? _huntMaps[0] : null);
        }

        private void CollectHuntingTargetMap(List<AreaOverlayStateInfo> outMaps)
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

                if (!m.HuntingTarget)
                {
                    continue;
                }

                if (m.DayPeriodLimit != 0)
                {
                    if (m.DayPeriodLimit == 1 && dayPeriod != GameLogicManager.EDayPeriod.Day)
                    {
                        continue;
                    }
                    if (m.DayPeriodLimit == 2 && dayPeriod != GameLogicManager.EDayPeriod.Night)
                    {
                        continue;
                    }
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

        void OnClickStartHunting()
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
            glm.BeginInfiltrationRunSession();
            glm.PreparePlayerSwitchArea(_selectedMap.Id, true, targetPoint: spawn);
            glm.ForcePlayerHumanMode(false);
        }


        void OnClickOpenRumorIntel()
        {
            if (_selectedMap == null)
            {
                Debug.LogWarning("[UIBedroomMapTravel] Select a map before opening intel shop.");
                return;
            }

            RumorIntelShopPanel.OpenForMap(_selectedMap.Id);
        }
    }
}
