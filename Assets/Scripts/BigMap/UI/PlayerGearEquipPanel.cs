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
        [SerializeField] Transform equippedContent;
        [SerializeField] Transform candidateContent;
        [SerializeField] GearEquipRowView infoRowTemplate;
        [SerializeField] GearEquipRowView actionRowTemplate;

        Transform _root;
        readonly List<GearEquipRowView> _localRows = new();
        readonly List<GearEquipRowView> _equippedRows = new();
        readonly List<GearEquipRowView> _candRows = new();

        PlayerEquipmentManager _eq;
        PlayerBodyPartSystem _bodyPart;
        EBodyPart _selectedPart = EBodyPart.Mouth;
        IPlayerProgressionHubHost _progressionHubHost;

        public bool IsHostedByHub => _progressionHubHost != null;

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

            var blocker = _root.Find("BlockerButton");
            if (blocker != null)
            {
                blocker.gameObject.SetActive(false);
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
            if (!IsHostedByHub)
            {
                Debug.LogError("[PlayerGearEquipPanel] Setup without hub host.");
                return;
            }

            if (transform.Find("BuiltRoot") == null)
            {
                Debug.LogError("[PlayerGearEquipPanel] Prefab missing BuiltRoot.");
                return;
            }

            BindRefs();
            WireHotspots();
            ApplyHostedChromeIfNeeded();
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[PlayerGearEquipPanel] Show without hub host.");
                return;
            }

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
                detailGearPoint.text = $"装备点数 {used}/{cap}";
            }

            BuildLocalStats(state);
            BuildEquippedList();
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

        void BuildEquippedList()
        {
            ClearRowViews(_equippedRows, equippedContent);
            if (equippedContent == null || _eq == null || actionRowTemplate == null)
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
                SpawnActionRow(equippedContent, _equippedRows, $"[{cost}] {name}", string.Empty, "卸下", true, () =>
                {
                    _eq.TryUnequip(_selectedPart, idx, out _);
                    RefreshAll();
                });
            }

            if (!any)
            {
                SpawnInfoRow(equippedContent, _equippedRows, "(未装备)");
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
                SpawnActionRow(candidateContent, _candRows, $"[{cost}] {name} x{st.Count}", hint, "装备", canEquip, () =>
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

        static void ClearRowViews(List<GearEquipRowView> rows, Transform content)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    Destroy(rows[i].gameObject);
                }
            }

            rows.Clear();
            if (content == null)
            {
                return;
            }

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                if (child.name.EndsWith("_Template"))
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
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
            row.Bind(title, hint, actionLabel, canAct, onClick);
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
