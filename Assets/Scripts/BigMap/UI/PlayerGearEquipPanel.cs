using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map.Logic;
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

        [SerializeField] Image portraitImage;
        [SerializeField] BodyPartHotspotView[] hotspots;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] BodyPartExpBarView partExpBar;
        [SerializeField] TextMeshProUGUI detailGearPoint;
        [SerializeField] GearEquipNotchBarView equipNotchBar;
        [SerializeField] GearEquipEquippedBarView equippedBar;
        [SerializeField] Transform localStatsContent;
        [SerializeField] PartPropInfoRowView infoRowTemplate;
        [SerializeField] GearEquipBagGridView bagGrid;

        [SerializeField] BodyPartFocusMarkView focusPartMark;
        [SerializeField] GearEquipTransferAnimView transferAnim;

        [SerializeField] BodyPartProgressView partProgressLine;

        Transform _root;
        readonly List<PartPropInfoRowView> _localRows = new();

        PlayerEquipmentManager _eq;
        PlayerBodyPartSystem _bodyPart;
        GameLogicManager _glm;
        EBodyPart _selectedPart = EBodyPart.None;
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
            _glm = MainGameManager.Instance?.gameLogicManager;
            BindRefs();
            WireHotspots();
            EnsureSelectedPart();
            RefreshAll();
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
            EnsureTransferAnim();
        }

        void EnsureTransferAnim()
        {
            if (transferAnim == null)
            {
                transferAnim = GetComponent<GearEquipTransferAnimView>();
            }

            if (transferAnim == null)
            {
                transferAnim = gameObject.AddComponent<GearEquipTransferAnimView>();
            }

            transferAnim.Configure(equippedBar, bagGrid);
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
            if (!BodyPartUiRules.IsSelectablePart(part, _glm))
            {
                return;
            }

            _selectedPart = part;
            RefreshAll();
        }

        void EnsureSelectedPart()
        {
            if (BodyPartUiRules.IsSelectablePart(_selectedPart, _glm))
            {
                return;
            }

            if (BodyPartUiRules.TryGetFirstSelectablePart(_glm, out var first))
            {
                _selectedPart = first;
                return;
            }

            _selectedPart = EBodyPart.None;
            Debug.LogWarning("[PlayerGearEquipPanel] No selectable body part.");
        }

        void RefreshAll()
        {
            if (_eq == null)
            {
                return;
            }

            EnsureSelectedPart();
            _eq.EnsureAllPartsBudget();
            RefreshHotspots();
            RefreshDetail();
            RefreshFocusMark();
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

                bool selectable = BodyPartUiRules.IsSelectablePart(hotspot.PartId, _glm);
                hotspot.SetLocked(!selectable);
                hotspot.SetSelected(selectable && hotspot.PartId == _selectedPart);
            }
        }

        void RefreshFocusMark()
        {
            if (focusPartMark == null)
            {
                return;
            }

            RectTransform target = null;
            if (_selectedPart != EBodyPart.None && hotspots != null)
            {
                for (int i = 0; i < hotspots.Length; i++)
                {
                    var hotspot = hotspots[i];
                    if (hotspot == null || hotspot.PartId != _selectedPart)
                    {
                        continue;
                    }

                    if (!BodyPartUiRules.IsSelectablePart(hotspot.PartId, _glm))
                    {
                        continue;
                    }

                    target = hotspot.FocusRect;
                    break;
                }
            }

            focusPartMark.FocusTo(target, target != null);
        }

        void RefreshDetail()
        {
            if (_selectedPart == EBodyPart.None)
            {
                partExpBar?.Refresh(_bodyPart, EBodyPart.None);
                partProgressLine?.Refresh(EBodyPart.None, 0);
                return;
            }

            var def = BodyPartCatalog.GetPartDef(_selectedPart);
            var state = _bodyPart?.GetPartState(_selectedPart);
            if (def == null)
            {
                return;
            }

            int used = _eq.GetUsedGearPoint(_selectedPart);
            int cap = _eq.GetPartGearPointCap(_selectedPart);

            if (detailTitle != null)
            {
                detailTitle.text = def.DisplayName;
            }

            partExpBar?.Refresh(_bodyPart, _selectedPart);

            if (detailGearPoint != null)
            {
                detailGearPoint.text = $"剩余点数 {Mathf.Max(0, cap - used)}";
            }

            equipNotchBar?.Refresh(used, cap);
            equippedBar?.Refresh(_eq, _selectedPart, OnUnequipRequested);
            BuildLocalStats(state);
            if (bagGrid != null)
            {
                bagGrid.gameObject.SetActive(cap > 0);
                if (cap > 0)
                {
                    bagGrid.Refresh(_eq, _selectedPart, null);
                    bagGrid.ConfigureEquipAnim(transferAnim, () => _selectedPart, RefreshAllAfterTransfer);
                }
            }

            partProgressLine?.Refresh(_selectedPart, state?.Level ?? 0);
        }

        void OnUnequipRequested(int equippedIndex)
        {
            if (_eq == null || _selectedPart == EBodyPart.None)
            {
                return;
            }

            EnsureTransferAnim();
            if (transferAnim != null && transferAnim.IsBusy)
            {
                return;
            }

            if (transferAnim != null
                && transferAnim.TryPlayUnequip(_eq, _selectedPart, equippedIndex, RefreshAllAfterTransfer))
            {
                return;
            }

            if (_eq.TryUnequip(_selectedPart, equippedIndex, out _))
            {
                RefreshAllAfterTransfer();
            }
        }

        void RefreshAllAfterTransfer()
        {
            RefreshAll();
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
