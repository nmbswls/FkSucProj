using System;
using System.Collections.Generic;
using My.Config;
using My.Player;
using UnityEngine;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeLayoutHost : MonoBehaviour
    {
        const string TemplateResourceRoot = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneLayout/";

        [SerializeField] RectTransform templateRoot;
        [SerializeField] RuneUpgradeSlotView slotViewPrefab;

        void Awake()
        {
            EnsureReferences();
        }

        void EnsureReferences()
        {
            if (templateRoot == null)
            {
                var child = transform.Find("TemplateRoot");
                if (child != null)
                {
                    templateRoot = child as RectTransform;
                }
            }

            if (slotViewPrefab == null)
            {
                slotViewPrefab = Resources.Load<RuneUpgradeSlotView>(
                    "UI/Prefabs/PlayerProgressionHubPanelSub/RuneLayout/RuneUpgradeSlotView");
            }
        }

        readonly Dictionary<int, RuneUpgradeSlotView> _slotViews = new();
        readonly Dictionary<int, string> _slotUpgradeIds = new();

        RuneUpgradeLayoutTemplate _activeTemplate;
        string _activeRuneId;
        int _selectedLayoutSlot;
        string _selectedUpgradeId;
        PlayerRuneSystem _runeSystem;
        Action<string> _onUpgradeSelected;

        public string SelectedUpgradeId => _selectedUpgradeId;

        public void ShowForRune(string runeId, PlayerRuneSystem runeSystem, Action<string> onUpgradeSelected)
        {
            _runeSystem = runeSystem;
            _onUpgradeSelected = onUpgradeSelected;
            _activeRuneId = runeId;
            ClearTemplate();

            var runeDef = RuneCatalog.GetOrDefault(runeId);
            if (runeDef == null || string.IsNullOrEmpty(runeDef.LayoutTemplateId))
            {
                gameObject.SetActive(false);
                return;
            }

            if (templateRoot == null || slotViewPrefab == null)
            {
                Debug.LogError("[RuneUpgradeLayoutHost] Missing templateRoot or slotViewPrefab.");
                gameObject.SetActive(false);
                return;
            }

            string path = TemplateResourceRoot + runeDef.LayoutTemplateId;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[RuneUpgradeLayoutHost] Layout template not found: {path}");
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            var instance = Instantiate(prefab, templateRoot, false);
            _activeTemplate = instance.GetComponent<RuneUpgradeLayoutTemplate>();
            if (_activeTemplate == null)
            {
                _activeTemplate = instance.AddComponent<RuneUpgradeLayoutTemplate>();
            }

            _activeTemplate.CollectSlots();
            BindUpgradeSlots(runeId);
            RefreshStates();

            if (string.IsNullOrEmpty(_selectedUpgradeId))
            {
                SelectFirstVisibleUpgrade();
            }
            else
            {
                SetSelectedUpgrade(_selectedUpgradeId);
            }
        }

        public void Hide()
        {
            ClearTemplate();
            gameObject.SetActive(false);
        }

        public void RefreshStates()
        {
            if (_runeSystem == null || string.IsNullOrEmpty(_activeRuneId))
            {
                return;
            }

            foreach (var kv in _slotViews)
            {
                var slotView = kv.Value;
                if (slotView == null || string.IsNullOrEmpty(slotView.UpgradeId))
                {
                    continue;
                }

                var node = BuildNodeView(slotView.UpgradeId);
                bool isInitial = node?.Def != null && RuneUpgradeCatalog.IsInitialUpgrade(node.Def);
                var state = node?.State ?? ERuneUpgradeNodeState.Locked;
                if (isInitial && state != ERuneUpgradeNodeState.Unlocked)
                {
                    state = ERuneUpgradeNodeState.Unlocked;
                }

                slotView.Refresh(state, kv.Key == _selectedLayoutSlot);
            }
        }

        public void SetSelectedUpgrade(string upgradeId)
        {
            _selectedUpgradeId = upgradeId;
            _selectedLayoutSlot = 0;
            foreach (var kv in _slotUpgradeIds)
            {
                if (kv.Value == upgradeId)
                {
                    _selectedLayoutSlot = kv.Key;
                    break;
                }
            }

            RefreshStates();
            if (!string.IsNullOrEmpty(_selectedUpgradeId))
            {
                _onUpgradeSelected?.Invoke(_selectedUpgradeId);
            }
        }

        void BindUpgradeSlots(string runeId)
        {
            _slotViews.Clear();
            _slotUpgradeIds.Clear();

            var upgrades = RuneUpgradeCatalog.GetUpgradesForRune(runeId);
            var usedSlots = new HashSet<int>();
            for (int i = 0; i < upgrades.Count; i++)
            {
                var def = upgrades[i];
                if (def == null || def.LayoutSlot <= 0)
                {
                    continue;
                }

                if (!usedSlots.Add(def.LayoutSlot))
                {
                    Debug.LogWarning($"[RuneUpgradeLayoutHost] Duplicate layout_slot {def.LayoutSlot} on rune {runeId}");
                }

                if (_activeTemplate.SlotAnchors.TryGetValue(def.LayoutSlot, out var anchor) && anchor != null)
                {
                    var slotView = Instantiate(slotViewPrefab, anchor, false);
                    var nodeView = BuildNodeView(def.UpgradeId);
                    bool isInitial = RuneUpgradeCatalog.IsInitialUpgrade(def);
                    slotView.Bind(def.LayoutSlot, nodeView, isInitial, OnSlotClicked);
                    _slotViews[def.LayoutSlot] = slotView;
                    _slotUpgradeIds[def.LayoutSlot] = def.UpgradeId;
                }
                else
                {
                    Debug.LogWarning($"[RuneUpgradeLayoutHost] Missing Slot_{def.LayoutSlot} in template for rune {runeId}");
                }
            }
        }

        RuneUpgradeNodeView BuildNodeView(string upgradeId)
        {
            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);
            if (def == null || _runeSystem == null)
            {
                return null;
            }

            var state = _runeSystem.GetUpgradeNodeState(upgradeId, out var lockReason);
            if (RuneUpgradeCatalog.IsInitialUpgrade(def) && _runeSystem.OwnsRune(def.BaseRuneId))
            {
                state = ERuneUpgradeNodeState.Unlocked;
            }

            return new RuneUpgradeNodeView
            {
                Def = def,
                State = state,
                LockReason = lockReason,
            };
        }

        void OnSlotClicked(int layoutSlot, string upgradeId)
        {
            _selectedLayoutSlot = layoutSlot;
            _selectedUpgradeId = upgradeId;
            RefreshStates();
            _onUpgradeSelected?.Invoke(upgradeId);
        }

        void SelectFirstVisibleUpgrade()
        {
            int bestSlot = int.MaxValue;
            string bestId = null;
            foreach (var kv in _slotUpgradeIds)
            {
                if (kv.Key < bestSlot)
                {
                    bestSlot = kv.Key;
                    bestId = kv.Value;
                }
            }

            if (!string.IsNullOrEmpty(bestId))
            {
                SetSelectedUpgrade(bestId);
            }
        }

        void ClearTemplate()
        {
            _slotViews.Clear();
            _slotUpgradeIds.Clear();
            _activeTemplate = null;
            if (templateRoot != null)
            {
                for (int i = templateRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(templateRoot.GetChild(i).gameObject);
                }
            }
        }
    }
}
