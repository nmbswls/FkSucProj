using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.UI.BodyPart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 部位养成 + 装备合并面板（Hub Tab: 部位）
    public class PlayerGearEquipPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "PlayerGearEquip";

        static readonly EBodyPart[] VisibleParts =
        {
            EBodyPart.Mouth,
            EBodyPart.Breast,
            EBodyPart.Womb,
            EBodyPart.Tail,
        };

        [SerializeField] Image portraitImage;
        [SerializeField] BodyPartHotspotView[] hotspots;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailLevel;
        [SerializeField] TextMeshProUGUI detailGearPoint;
        [SerializeField] GearEquipNotchBarView equipNotchBar;
        [SerializeField] GearEquipEquippedBarView equippedBar;
        [SerializeField] Transform localStatsContent;
        [SerializeField] PartPropInfoRowView infoRowTemplate;
        [SerializeField] GearEquipBagGridView bagGrid;

        Transform _root;
        readonly List<PartPropInfoRowView> _localRows = new();

        PlayerEquipmentManager _eq;
        PlayerBodyPartSystem _bodyPart;
        EBodyPart _selectedPart = EBodyPart.Mouth;
        IPlayerProgressionHubHost _progressionHubHost;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
            ApplyHostedChromeIfNeeded();
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null || _root == null)
            {
                return;
            }

            var closeTr = _root.Find("Window/Header/CloseBtn");
            var closeBtn = closeTr != null ? closeTr.GetComponent<Button>() : null;
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseSelfOrHub);
            }
        }

        public void CloseSelfOrHub()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return;
            }

            Debug.LogError("[PlayerGearEquipPanel] Not hosted by PlayerProgressionHubPanel.");
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            BindRefs();
            WireHotspots();
            ApplyHostedChromeIfNeeded();
        }

        public override void Show()
        {
            base.Show();
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            _eq = mgr?.EquipmentManager;
            _bodyPart = mgr?.BodyPartSystem;
            BindRefs();
            WireHotspots();
            RefreshAll();
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
        }

        void WireHotspots()
        {
            if (hotspots == null)
            {
                return;
            }

            for (int i = 0; i < hotspots.Length; i++)
            {
                var hotspot = hotspots[i];
                if (hotspot == null)
                {
                    continue;
                }

                hotspot.Bind(hotspot.PartId, OnHotspotClicked);
            }
        }

        void OnHotspotClicked(EBodyPart part)
        {
            if (!IsVisiblePart(part))
            {
                return;
            }

            _selectedPart = part;
            RefreshAll();
        }

        static bool IsVisiblePart(EBodyPart part)
        {
            for (int i = 0; i < VisibleParts.Length; i++)
            {
                if (VisibleParts[i] == part)
                {
                    return true;
                }
            }

            return false;
        }

        void RefreshAll()
        {
            if (_eq == null)
            {
                return;
            }

            _eq.EnsureAllPartsBudget();
            RefreshHotspots();
            RefreshDetail();
        }

        void RefreshHotspots()
        {
            if (hotspots == null)
            {
                return;
            }

            for (int i = 0; i < hotspots.Length; i++)
            {
                var hotspot = hotspots[i];
                if (hotspot == null)
                {
                    continue;
                }

                hotspot.SetSelected(hotspot.PartId == _selectedPart);
            }
        }

        void RefreshDetail()
        {
            var def = BodyPartCatalog.GetPartDef(_selectedPart);
            var state = _bodyPart?.GetPartState(_selectedPart);
            if (def == null)
            {
                return;
            }

            int lv = state?.Level ?? 1;
            long exp = state?.Exp ?? 0;
            long expNext = _bodyPart?.GetExpToNextLevel(_selectedPart) ?? 0;
            int used = _eq.GetUsedGearPoint(_selectedPart);
            int cap = _eq.GetPartGearPointCap(_selectedPart);

            if (detailTitle != null)
            {
                detailTitle.text = def.DisplayName;
            }

            if (detailLevel != null)
            {
                detailLevel.text = expNext > 0
                    ? $"等级 {lv}  经验 {exp}  距下级 {expNext}"
                    : $"等级 {lv}  经验 {exp}  (已满级)";
            }

            if (detailGearPoint != null)
            {
                detailGearPoint.text = $"剩余点数 {Mathf.Max(0, cap - used)}";
            }

            equipNotchBar?.Refresh(used, cap);
            equippedBar?.Refresh(_eq, _selectedPart, RefreshAll);
            BuildLocalStats(state);
            bagGrid?.Refresh(_eq, _selectedPart, RefreshAll);
        }

        void BuildLocalStats(BodyPartRuntimeState state)
        {
            ClearInfoRows();
            if (localStatsContent == null || infoRowTemplate == null)
            {
                return;
            }

            if (state?.LocalStats == null)
            {
                SpawnInfoRow("(无 Local 属性)");
                return;
            }

            bool any = false;
            foreach (var kv in state.LocalStats)
            {
                any = true;
                string name = BodyPartCatalog.GetLocalAttrDisplayName(kv.Key);
                SpawnInfoRow($"{name}: {kv.Value}");
            }

            if (!any)
            {
                SpawnInfoRow("(无 Local 属性)");
            }
        }

        void SpawnInfoRow(string text)
        {
            if (infoRowTemplate == null || localStatsContent == null)
            {
                return;
            }

            var row = Instantiate(infoRowTemplate, localStatsContent);
            row.gameObject.SetActive(true);
            row.Bind(text);
            _localRows.Add(row);
        }

        void ClearInfoRows()
        {
            for (int i = 0; i < _localRows.Count; i++)
            {
                if (_localRows[i] != null)
                {
                    Destroy(_localRows[i].gameObject);
                }
            }

            _localRows.Clear();

            if (localStatsContent == null || infoRowTemplate == null)
            {
                return;
            }

            for (int i = localStatsContent.childCount - 1; i >= 0; i--)
            {
                var child = localStatsContent.GetChild(i);
                if (child == infoRowTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            CloseSelfOrHub();
            return true;
        }

        public bool OnNavigate(Vector2 dir) => false;

        public bool OnHotkey(string keyName) => false;

        public bool OnScroll(float deltaY) => false;

        public bool OnClick(int button, Vector2 mousePos) => false;

        public bool OnHoldStart(string holdKey) => false;

        public bool OnHoldUpdate(string holdKey) => false;

        public bool OnHoldingEnd(string holdKey) => false;
    }
}
