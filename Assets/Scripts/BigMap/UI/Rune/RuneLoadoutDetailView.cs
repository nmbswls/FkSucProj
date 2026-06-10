using System;

using System.Collections.Generic;

using cfg.demo;

using DG.Tweening;

using My.Config;

using My.Player;

using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace My.UI.Rune

{

    // 统筹符文详情面板：标题/描述/锁定态、升级布局树、选中升级说明与滑入动画

    public sealed class RuneLoadoutDetailView : MonoBehaviour

    {

        const string TemplateResourceRoot = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneLayout/";

        const float SlideDuration = 0.22f;

        const float SlideOffsetX = 72f;



        [SerializeField] Button closeButton;

        [SerializeField] GameObject lockOverlay;

        [SerializeField] TextMeshProUGUI titleText;

        [SerializeField] TextMeshProUGUI placeholderBodyText;

        [SerializeField] TextMeshProUGUI runeDescText;



        [SerializeField] GameObject layoutHostRoot;

        [SerializeField] Image bgRunePicture;

        [SerializeField] RectTransform templateRoot;

        [SerializeField] RuneUpgradeSlotView slotViewPrefab;



        [SerializeField] TextMeshProUGUI upgradeTitle;

        [SerializeField] TextMeshProUGUI upgradeBody;

        [SerializeField] TextMeshProUGUI upgradeLockHint;



        readonly Dictionary<int, RuneUpgradeSlotView> _slotViews = new();

        readonly Dictionary<int, string> _slotUpgradeIds = new();



        RuneUpgradeLayoutTemplate _activeTemplate;

        string _activeRuneId;

        int _selectedLayoutSlot;

        string _selectedUpgradeId;

        PlayerRuneSystem _runeSystem;

        Action _onDismissRequested;



        bool _visible;

        Vector2 _shownAnchoredPos;

        Tweener _slideTween;



        public bool IsVisible => _visible;

        public RectTransform AreaRoot => transform as RectTransform;



        void Awake()

        {

            EnsureReferences();

            WireChrome();

        }



        void OnDisable()

        {

            KillTween();

        }



        void EnsureReferences()

        {

            if (layoutHostRoot == null)

            {

                var child = transform.Find("LayoutHost");

                if (child != null)

                {

                    layoutHostRoot = child.gameObject;

                }

            }



            if (templateRoot == null && layoutHostRoot != null)

            {

                var child = layoutHostRoot.transform.Find("TemplateRoot");

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



        void WireChrome()

        {

            if (closeButton != null)

            {

                closeButton.onClick.RemoveAllListeners();

                closeButton.onClick.AddListener(RequestDismiss);

            }

        }



        public void BindDismissCallback(Action onDismiss)

        {

            _onDismissRequested = onDismiss;

        }



        public void CacheLayoutAnchors()

        {

            _shownAnchoredPos = Vector2.zero;

        }



        public void ResetVisualState()

        {

            KillTween();

            _visible = false;

            ClearContent();



            if (AreaRoot != null)

            {

                AreaRoot.gameObject.SetActive(false);

                AreaRoot.anchoredPosition = _shownAnchoredPos;

            }

        }



        public static bool ShouldShowForSlot(RuneSlotView slot)

        {

            if (slot?.Binder == null)

            {

                return false;

            }



            if (slot.Binder.SlotKind == RuneSlotKind.Fixed)

            {

                return true;

            }



            return slot.State == RuneSlotVisualState.Equipped;

        }



        public void Show(RuneSlotView slot, PlayerRuneSystem runeSystem, bool animate = true)

        {

            if (!ShouldShowForSlot(slot))

            {

                Hide(false);

                return;

            }



            _runeSystem = runeSystem;

            RefreshContent(slot);



            if (_visible)

            {

                if (AreaRoot != null)

                {

                    AreaRoot.anchoredPosition = _shownAnchoredPos;

                }



                return;

            }



            PlayShow(animate);

        }



        public void Refresh(RuneSlotView slot, PlayerRuneSystem runeSystem)

        {

            if (!_visible || !ShouldShowForSlot(slot))

            {

                return;

            }



            _runeSystem = runeSystem;

            RefreshContent(slot);

        }



        public void Hide(bool animate = true)

        {

            if (!_visible)

            {

                ClearContent();

                return;

            }



            _visible = false;

            if (!animate)

            {

                KillTween();

                ClearContent();

                if (AreaRoot != null)

                {

                    AreaRoot.gameObject.SetActive(false);

                    AreaRoot.anchoredPosition = _shownAnchoredPos;

                }



                return;

            }



            KillTween();

            var target = _shownAnchoredPos + new Vector2(SlideOffsetX, 0f);

            _slideTween = AreaRoot

                .DOAnchorPos(target, SlideDuration)

                .SetEase(Ease.InCubic)

                .SetUpdate(true)

                .OnComplete(() =>

                {

                    ClearContent();

                    if (AreaRoot != null)

                    {

                        AreaRoot.gameObject.SetActive(false);

                        AreaRoot.anchoredPosition = _shownAnchoredPos;

                    }

                });

        }



        void PlayShow(bool animate)

        {

            if (AreaRoot == null)

            {

                return;

            }



            _visible = true;

            KillTween();

            AreaRoot.gameObject.SetActive(true);



            if (!animate)

            {

                AreaRoot.anchoredPosition = _shownAnchoredPos;

                return;

            }



            AreaRoot.anchoredPosition = _shownAnchoredPos + new Vector2(SlideOffsetX, 0f);

            _slideTween = AreaRoot

                .DOAnchorPos(_shownAnchoredPos, SlideDuration)

                .SetEase(Ease.OutCubic)

                .SetUpdate(true);

        }



        void RefreshContent(RuneSlotView slot)

        {

            if (slot == null || slot.Binder == null)

            {

                ClearContent();

                return;

            }



            var provider = slot.GetComponent<RuneInfoProvider>();

            if (provider == null)

            {

                ShowPlaceholder(string.Empty);

                return;

            }



            if (titleText != null)

            {

                titleText.text = provider.GetDisplayName();

            }



            if (slot.Binder.SlotKind == RuneSlotKind.Fixed)

            {

                RefreshFixedSlot(slot, provider);

                return;

            }



            RefreshEquippedSlot(slot, provider);

        }



        void RefreshFixedSlot(RuneSlotView slot, RuneInfoProvider provider)

        {

            bool locked = slot.State == RuneSlotVisualState.Locked;

            SetLockOverlayVisible(locked);



            if (locked)

            {

                ShowPlaceholder(provider.GetDetailText());

                return;

            }



            string runeId = slot.Binder.FixedRuneId;

            ShowOwnedRuneDetail(runeId, provider);

        }



        void RefreshEquippedSlot(RuneSlotView slot, RuneInfoProvider provider)

        {

            SetLockOverlayVisible(false);

            string runeId = ResolveEquippedRuneId(slot, _runeSystem);

            ShowOwnedRuneDetail(runeId, provider);

        }



        void ShowOwnedRuneDetail(string runeId, RuneInfoProvider provider)

        {

            var runeDef = RuneCatalog.GetOrDefault(runeId);

            if (runeDef == null || _runeSystem == null || !_runeSystem.OwnsRune(runeId))

            {

                ShowPlaceholder(provider.GetDetailText());

                return;

            }



            if (runeDescText != null)

            {

                runeDescText.text = runeDef.Desc ?? string.Empty;

            }



            if (placeholderBodyText != null)

            {

                placeholderBodyText.gameObject.SetActive(false);

            }



            ShowLayoutForRune(runeId);

            RefreshSelectedUpgradeDetail();

        }



        void ClearContent()

        {

            HideLayout();

            ClearUpgradeDetail();

            SetLockOverlayVisible(false);



            if (titleText != null)

            {

                titleText.text = string.Empty;

            }



            if (runeDescText != null)

            {

                runeDescText.text = string.Empty;

            }



            if (placeholderBodyText != null)

            {

                placeholderBodyText.gameObject.SetActive(false);

                placeholderBodyText.text = string.Empty;

            }

        }



        void RequestDismiss()

        {

            _onDismissRequested?.Invoke();

        }



        void SetLockOverlayVisible(bool visible)

        {

            if (lockOverlay != null)

            {

                lockOverlay.SetActive(visible);

            }

        }



        void ShowPlaceholder(string message)

        {

            if (runeDescText != null)

            {

                runeDescText.text = string.Empty;

            }



            if (placeholderBodyText != null)

            {

                placeholderBodyText.gameObject.SetActive(true);

                placeholderBodyText.text = message ?? string.Empty;

            }



            HideLayout();

            ClearUpgradeDetail();

        }



        static string ResolveEquippedRuneId(RuneSlotView slot, PlayerRuneSystem runeSystem)

        {

            if (slot?.Binder == null || runeSystem == null)

            {

                return null;

            }



            return runeSystem.GetEquipped(slot.Binder.EquipSlot);

        }



        void ShowLayoutForRune(string runeId)

        {

            if (!string.Equals(_activeRuneId, runeId, StringComparison.Ordinal))

            {

                _selectedUpgradeId = null;

            }



            _activeRuneId = runeId;

            ClearTemplate();



            var runeDef = RuneCatalog.GetOrDefault(runeId);

            if (runeDef == null || string.IsNullOrEmpty(runeDef.LayoutTemplateId))

            {

                SetLayoutHostActive(false);

                return;

            }



            if (bgRunePicture != null)

            {

                bgRunePicture.sprite = SimpleResManager.Load<Sprite>($"Sprites/Rune/{runeDef.Icon}");

            }



            if (templateRoot == null || slotViewPrefab == null)

            {

                Debug.LogError("[RuneLoadoutDetailView] Missing templateRoot or slotViewPrefab.");

                SetLayoutHostActive(false);

                return;

            }



            string path = TemplateResourceRoot + runeDef.LayoutTemplateId;

            var prefab = Resources.Load<GameObject>(path);

            if (prefab == null)

            {

                Debug.LogWarning($"[RuneLoadoutDetailView] Layout template not found: {path}");

                SetLayoutHostActive(false);

                return;

            }



            SetLayoutHostActive(true);

            var instance = Instantiate(prefab, templateRoot, false);

            _activeTemplate = instance.GetComponent<RuneUpgradeLayoutTemplate>();

            if (_activeTemplate == null)

            {

                _activeTemplate = instance.AddComponent<RuneUpgradeLayoutTemplate>();

            }



            _activeTemplate.CollectSlots();

            BindUpgradeSlots(runeId);

            RefreshLayoutSlotStates();



            if (string.IsNullOrEmpty(_selectedUpgradeId))

            {

                SelectFirstVisibleUpgrade();

            }

            else

            {

                SetSelectedUpgrade(_selectedUpgradeId);

            }

        }



        void HideLayout()

        {

            ClearTemplate();

            SetLayoutHostActive(false);

            _activeRuneId = null;

            _selectedUpgradeId = null;

            _selectedLayoutSlot = 0;

        }



        void SetLayoutHostActive(bool active)

        {

            if (layoutHostRoot != null)

            {

                layoutHostRoot.SetActive(active);

            }

        }



        void RefreshLayoutSlotStates()

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



        void SetSelectedUpgrade(string upgradeId)

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



            RefreshLayoutSlotStates();

            RefreshSelectedUpgradeDetail();

        }



        void RefreshSelectedUpgradeDetail()

        {

            if (string.IsNullOrEmpty(_selectedUpgradeId))

            {

                ClearUpgradeDetail();

                return;

            }



            ShowUpgradeDetail(_selectedUpgradeId);

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

                    Debug.LogWarning(

                        $"[RuneLoadoutDetailView] Duplicate layout_slot {def.LayoutSlot} on rune {runeId}");

                }



                if (_activeTemplate.SlotAnchors.TryGetValue(def.LayoutSlot, out var anchor) && anchor != null)

                {

                    var slotView = Instantiate(slotViewPrefab, anchor, false);

                    var nodeView = BuildNodeView(def.UpgradeId);

                    bool isInitial = RuneUpgradeCatalog.IsInitialUpgrade(def);

                    slotView.Bind(def.LayoutSlot, nodeView, isInitial, OnLayoutSlotClicked);

                    _slotViews[def.LayoutSlot] = slotView;

                    _slotUpgradeIds[def.LayoutSlot] = def.UpgradeId;

                }

                else

                {

                    Debug.LogWarning(

                        $"[RuneLoadoutDetailView] Missing Slot_{def.LayoutSlot} in template for rune {runeId}");

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



            var state = _runeSystem.GetUpgradeNodeState(upgradeId);

            if (RuneUpgradeCatalog.IsInitialUpgrade(def) && _runeSystem.OwnsRune(def.BaseRuneId))

            {

                state = ERuneUpgradeNodeState.Unlocked;

            }



            return new RuneUpgradeNodeView

            {

                Def = def,

                State = state,

            };

        }



        void OnLayoutSlotClicked(int layoutSlot, string upgradeId)

        {

            _selectedLayoutSlot = layoutSlot;

            _selectedUpgradeId = upgradeId;

            RefreshLayoutSlotStates();

            RefreshSelectedUpgradeDetail();

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



        void ClearUpgradeDetail()

        {

            if (upgradeTitle != null)

            {

                upgradeTitle.text = string.Empty;

            }



            if (upgradeBody != null)

            {

                upgradeBody.text = string.Empty;

            }



            if (upgradeLockHint != null)

            {

                upgradeLockHint.text = string.Empty;

            }

        }



        void ShowUpgradeDetail(string upgradeId)

        {

            var def = RuneUpgradeCatalog.GetOrDefault(upgradeId);

            if (def == null)

            {

                ClearUpgradeDetail();

                return;

            }



            if (upgradeTitle != null)

            {

                upgradeTitle.text = def.Name ?? string.Empty;

            }



            if (upgradeBody != null)

            {

                upgradeBody.text = def.Desc ?? string.Empty;

            }



            if (upgradeLockHint == null)

            {

                return;

            }



            if (_runeSystem == null)

            {

                upgradeLockHint.text = string.Empty;

                return;

            }



            var state = _runeSystem.GetUpgradeNodeState(upgradeId);

            if (RuneUpgradeCatalog.IsInitialUpgrade(def) && _runeSystem.OwnsRune(def.BaseRuneId))

            {

                state = ERuneUpgradeNodeState.Unlocked;

            }



            upgradeLockHint.text = state switch

            {

                ERuneUpgradeNodeState.Unlocked => RuneUpgradeCatalog.IsInitialUpgrade(def)

                    ? "初始效果（已拥有）"

                    : "已解锁",

                ERuneUpgradeNodeState.Available => "使用对应道具解锁",

                _ => "暂不可解锁",

            };

        }



        void KillTween()

        {

            if (_slideTween != null && _slideTween.IsActive())

            {

                _slideTween.Kill();

            }



            _slideTween = null;

            AreaRoot?.DOKill();

        }

    }

}


