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
        [SerializeField] Transform localStatsContent;
        [SerializeField] Transform candidateContent;
        [SerializeField] GearEquipRowView infoRowTemplate;
        [SerializeField] GearEquipRowView actionRowTemplate;
        [SerializeField] GearPointNotchBarView gearPointBar;
        [SerializeField] Transform charmGrid;
        [SerializeField] GearCharmSlotView charmSlotTemplate;

        Transform _root;
        readonly List<GearEquipRowView> _localRows = new();
        readonly List<GearEquipRowView> _candRows = new();
        readonly List<GearCharmSlotView> _charmSlots = new();

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
            if (_root == null)
            {
                return;
            }

            if (gearPointBar == null)
            {
                gearPointBar = _root.Find("Window/BodyRow/LeftPanel/GearPointBar")?.GetComponent<GearPointNotchBarView>();
            }

            if (charmGrid == null)
            {
                charmGrid = _root.Find("Window/BodyRow/LeftPanel/CharmBoard/CharmGrid");
            }

            if (charmSlotTemplate == null && charmGrid != null)
            {
                charmSlotTemplate = charmGrid.Find("CharmSlot_Template")?.GetComponent<GearCharmSlotView>();
            }
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

                var part = hotspot.PartId;
                hotspot.Bind(part, OnHotspotClicked);
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
            if (_eq == null || localStatsContent == null)
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
            int used = _eq?.GetUsedGearPoint(_selectedPart) ?? 0;
            int cap = _eq?.GetPartGearPointCap(_selectedPart) ?? 0;

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

            gearPointBar?.Refresh(used, cap);
            BuildLocalStats(state);
            BuildCharmGrid();
            BuildCandidates();
        }

        void BuildLocalStats(BodyPartRuntimeState state)
        {
            ClearRowViews(_localRows, localStatsContent);
            if (state?.LocalStats == null)
            {
                return;
            }

            bool any = false;
            foreach (var kv in state.LocalStats)
            {
                any = true;
                string name = BodyPartCatalog.GetLocalAttrDisplayName(kv.Key);
                SpawnInfoRow(localStatsContent, _localRows, $"{name}: {kv.Value}");
            }

            if (!any)
            {
                SpawnInfoRow(localStatsContent, _localRows, "(无 Local 属性)");
            }
        }

        void BuildCharmGrid()
        {
            ClearCharmSlots();
            if (_eq == null || charmGrid == null || charmSlotTemplate == null)
            {
                return;
            }

            var list = _eq.GetEquippedOnPart(_selectedPart);
            bool any = false;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                {
                    continue;
                }

                any = true;
                var itemDef = ItemCatalog.GetItemDef(slot.ItemId);
                int cost = ItemGearRules.GetSlotCost(itemDef);
                string name = itemDef != null ? itemDef.DisplayName : slot.ItemId;
                int idx = i;
                var view = SpawnCharmSlot();
                view.BindEquipped(slot.ItemId, cost, name, () =>
                {
                    _eq.TryUnequip(_selectedPart, idx, out _);
                    RefreshAll();
                });
            }

            if (!any)
            {
                SpawnCharmSlot().BindEmpty();
            }
        }

        GearCharmSlotView SpawnCharmSlot()
        {
            var view = Instantiate(charmSlotTemplate, charmGrid);
            view.gameObject.SetActive(true);
            _charmSlots.Add(view);
            return view;
        }

        void ClearCharmSlots()
        {
            for (int i = 0; i < _charmSlots.Count; i++)
            {
                if (_charmSlots[i] != null)
                {
                    Destroy(_charmSlots[i].gameObject);
                }
            }

            _charmSlots.Clear();
            if (charmGrid == null || charmSlotTemplate == null)
            {
                return;
            }

            for (int i = charmGrid.childCount - 1; i >= 0; i--)
            {
                var child = charmGrid.GetChild(i);
                if (child == charmSlotTemplate.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        void BuildCandidates()
        {
            ClearRowViews(_candRows, candidateContent);
            if (candidateContent == null || _eq == null || actionRowTemplate == null)
            {
                return;
            }

            var list = _eq.ListMainBagCandidates(_selectedPart);
            bool any = false;
            foreach (var pair in list)
            {
                any = true;
                int flatIdx = pair.bagFlatIndex;
                var st = pair.stack;
                var itemDef = ItemCatalog.GetItemDef(st.ItemID);
                int cost = ItemGearRules.GetSlotCost(itemDef);
                string name = itemDef != null ? itemDef.DisplayName : st.ItemID;
                bool canEquip = _eq.CanEquipFromMainBag(_selectedPart, flatIdx, out var reason);
                string hint = canEquip ? string.Empty : TranslateEquipReason(reason);
                SpawnActionRow(candidateContent, _candRows, st.ItemID, st.Count, $"[{cost}] {name} x{st.Count}", hint, "装备", canEquip, () =>
                {
                    _eq.TryEquipFromMainBag(_selectedPart, flatIdx, out _);
                    RefreshAll();
                });
            }

            if (!any)
            {
                SpawnInfoRow(candidateContent, _candRows, "(背包无可用装备)");
            }
        }

        static string TranslateEquipReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "无法装备";
            }

            return reason switch
            {
                "no_budget" => "点数不足",
                "no_gear_point" => "点数不足",
                "no_item" => "物品不存在",
                "wrong_part" => "部位不匹配",
                _ => reason,
            };
        }

        void SpawnInfoRow(Transform parent, List<GearEquipRowView> rows, string text)
        {
            if (infoRowTemplate == null || parent == null)
            {
                return;
            }

            var row = Instantiate(infoRowTemplate, parent);
            row.gameObject.SetActive(true);
            row.Bind(text, string.Empty, string.Empty, false, null);
            rows.Add(row);
        }

        void SpawnActionRow(
            Transform parent,
            List<GearEquipRowView> rows,
            string itemId,
            long stackCount,
            string title,
            string hint,
            string actionLabel,
            bool canAct,
            UnityEngine.Events.UnityAction onClick)
        {
            if (actionRowTemplate == null || parent == null)
            {
                return;
            }

            var row = Instantiate(actionRowTemplate, parent);
            row.gameObject.SetActive(true);
            row.Bind(itemId, stackCount, title, hint, actionLabel, canAct, onClick);
            rows.Add(row);
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
