using System.Collections.Generic;
using My;
using My.Config;
using My.Home;
using My.Map;
using My.Map.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class MapTownManagementPanel : PanelWithInput
    {
        public const string PanelIdConst = "MapTownManagementPanel";

        [SerializeField] private TextMeshProUGUI txtProsperity;
        [SerializeField] private TextMeshProUGUI txtPopulation;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private TextMeshProUGUI txtDetailName;
        [SerializeField] private TextMeshProUGUI txtDetailInfo;
        [SerializeField] private Slider workforceSlider;
        [SerializeField] private TextMeshProUGUI txtWorkforceValue;
        [SerializeField] private Button btnClose;

        private HomeDataManager.HomeFacilityInstance _selected;
        private readonly List<GameObject> _rowObjs = new();

        private void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
            }

            if (workforceSlider != null)
            {
                workforceSlider.onValueChanged.AddListener(OnWorkforceSlider);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeAoiPresentationShown();
        }

        public override void Show()
        {
            base.Show();
            SubscribeAoiPresentationShown();
            HomeTownViewController.SetManagementPanelOpen(true);
            Refresh();
        }

        public override void Hide()
        {
            UnsubscribeAoiPresentationShown();
            HomeTownViewController.SetManagementPanelOpen(false);
            base.Hide();
        }

        private void SubscribeAoiPresentationShown()
        {
            var aoi = SceneAOIManager.Instance;
            if (aoi == null)
            {
                return;
            }

            aoi.AfterPresentationShown -= OnAoiAfterPresentationShown;
            aoi.AfterPresentationShown += OnAoiAfterPresentationShown;
        }

        private void UnsubscribeAoiPresentationShown()
        {
            var aoi = SceneAOIManager.Instance;
            if (aoi == null)
            {
                return;
            }

            aoi.AfterPresentationShown -= OnAoiAfterPresentationShown;
        }

        private void OnAoiAfterPresentationShown(IScenePresentation pres, ILogicEntity entity)
        {
            HomeTownViewController.ApplyPresentationVisibilityForTownManagement(pres, entity);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            Refresh();
        }

        public void Refresh()
        {
            var hm = MainGameManager.Instance?.gameLogicManager?.homeDataManager;
            if (hm == null)
            {
                return;
            }

            if (txtProsperity != null)
            {
                txtProsperity.text = $"Prosperity: {hm.TownProsperity}";
            }

            if (txtPopulation != null)
            {
                txtPopulation.text = $"Population: {hm.TownCurrentPopulation} (Assigned: {hm.ComputeAssignedWorkforceTotal()})";
            }

            RebuildList(hm);
            RefreshDetail();
        }

        private void RebuildList(HomeDataManager hm)
        {
            if (listRoot == null)
            {
                return;
            }

            foreach (var go in _rowObjs)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _rowObjs.Clear();

            foreach (var p in hm.PlacementInfos)
            {
                if (p.Removed)
                {
                    continue;
                }

                var row = new GameObject($"town_row_{p.InstId}", typeof(RectTransform));
                row.transform.SetParent(listRoot, false);
                var rt = row.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 36);

                var btn = row.AddComponent<Button>();
                var img = row.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.25f, 0.85f);

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(row.transform, false);
                var leRt = labelGo.GetComponent<RectTransform>();
                leRt.anchorMin = Vector2.zero;
                leRt.anchorMax = Vector2.one;
                leRt.offsetMin = new Vector2(8, 4);
                leRt.offsetMax = new Vector2(-8, -4);
                var txt = labelGo.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 18;
                txt.text = $"{p.CfgRef?.Name ?? p.Id}  (id:{p.InstId})";
                txt.raycastTarget = false;

                var captured = p;
                btn.onClick.AddListener(() =>
                {
                    _selected = captured;
                    RefreshDetail();
                });

                _rowObjs.Add(row);
            }
        }

        private void RefreshDetail()
        {
            if (_selected == null)
            {
                if (txtDetailName != null)
                {
                    txtDetailName.text = "Select a building";
                }

                if (txtDetailInfo != null)
                {
                    txtDetailInfo.text = string.Empty;
                }

                if (workforceSlider != null)
                {
                    workforceSlider.gameObject.SetActive(false);
                }

                return;
            }

            if (txtDetailName != null)
            {
                txtDetailName.text = _selected.CfgRef != null ? _selected.CfgRef.Name : _selected.Id;
            }

            bool workplace = _selected.CfgRef != null && _selected.CfgRef.SupportsWorkforceAssignment;
            if (txtDetailInfo != null)
            {
                txtDetailInfo.text = workplace
                    ? $"Workplace — max workers: {_selected.CfgRef.MaxWorkforce}"
                    : "No workforce assignment for this building type.";
            }

            if (workforceSlider != null)
            {
                workforceSlider.gameObject.SetActive(workplace);
                if (workplace)
                {
                    workforceSlider.wholeNumbers = true;
                    workforceSlider.minValue = 0;
                    workforceSlider.maxValue = Mathf.Max(1, _selected.CfgRef.MaxWorkforce);
                    workforceSlider.SetValueWithoutNotify(_selected.ArrangePeopleNum);
                    if (txtWorkforceValue != null)
                    {
                        txtWorkforceValue.text = $"Workers: {_selected.ArrangePeopleNum}";
                    }
                }
            }
        }

        private void OnWorkforceSlider(float v)
        {
            if (_selected == null || _selected.CfgRef == null || !_selected.CfgRef.SupportsWorkforceAssignment)
            {
                return;
            }

            int iv = Mathf.RoundToInt(v);
            if (MainGameManager.Instance?.gameLogicManager?.homeDataManager.TrySetPlacementWorkforce(_selected.InstId, iv, out _) == true)
            {
                if (txtWorkforceValue != null)
                {
                    txtWorkforceValue.text = $"Workers: {iv}";
                }

                if (txtPopulation != null)
                {
                    var hm = MainGameManager.Instance.gameLogicManager.homeDataManager;
                    txtPopulation.text = $"Population: {hm.TownCurrentPopulation} (Assigned: {hm.ComputeAssignedWorkforceTotal()})";
                }
            }
        }

        public override bool OnCancel()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
            return true;
        }

        private static bool IsHomeMap()
        {
            var map = MainGameManager.Instance?.gameLogicManager?.AreaManager?.MapName;
            if (string.IsNullOrEmpty(map))
            {
                return false;
            }

            var cfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(map);
            return cfg != null && cfg.IsHome;
        }

        public static void TryOpenFromHud()
        {
            if (!IsHomeMap())
            {
                Debug.LogWarning("MapTownManagementPanel: not on home map.");
                return;
            }

            UIManager.Instance.ShowPanel(PanelIdConst);
        }
    }
}
