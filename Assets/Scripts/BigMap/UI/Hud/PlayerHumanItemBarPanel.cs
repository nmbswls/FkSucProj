using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class PlayerHumanItemBarPanel : PanelBase
    {
        public const string PanelIdConst = "PlayerHumanItemBarPanel";

        const string WeaponSlotPrefix = "WeaponQuickSlot_";
        const string ConsumableSlotPrefix = "ConsumableQuickSlot_";

        public static PlayerHumanItemBarPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as PlayerHumanItemBarPanel;
            }
        }

        [SerializeField]
        RectTransform _contentRoot;

        [SerializeField]
        RectTransform _weaponColumnParent;

        [SerializeField]
        RectTransform _consumableWheelParent;

        [SerializeField]
        ItemBarCenterSkillView _centerSkillView;

        [SerializeField]
        ConsumableQuickSlotCell _consumableSlotTemplate;

        [SerializeField]
        WeaponQuickSlotCell _weaponSlotTemplate;

        [SerializeField]
        float _wheelRadius = 72f;

        [SerializeField]
        Vector2 _consumableSlotSize = new Vector2(45f, 45f);

        [SerializeField]
        Vector2 _weaponSlotSize = new Vector2(50f, 50f);

        [SerializeField]
        float _weaponSlotSpacing = 5f;

        [SerializeField]
        GameObject _disableHint;

        [SerializeField]
        TextMeshProUGUI _disableHintText;

        // ---- 折叠/展开模式相关 ----
        // 折叠态根节点：仅显示当前激活消耗品图标 + [Q] 提示
        // 在 Prefab 中拖拽赋值；若未赋值则跳过视觉切换
        [SerializeField]
        GameObject _collapsedRoot;

        // 折叠态图标（显示当前激活消耗品的 icon）
        [SerializeField]
        Image _collapsedItemIcon;

        // 折叠态按键提示文字（例如 "[Q] 使用"）
        [SerializeField]
        TextMeshProUGUI _collapsedKeyHint;

        // 展开态根节点：包含消耗品轮盘 + 武器列等完整内容
        // 若未赋值，默认用自身 RectTransform 作为展开视图
        [SerializeField]
        GameObject _expandedRoot;

        enum EItemBarMode { Collapsed, Expanded }
        EItemBarMode _barMode = EItemBarMode.Collapsed;

        WeaponQuickSlotCell[] _weaponSlots = new WeaponQuickSlotCell[HumanQuickBarDefs.WeaponSlotCount];
        ConsumableQuickSlotCell[] _consumableSlots = new ConsumableQuickSlotCell[HumanQuickBarDefs.ConsumableSlotCount];

        bool _barInitialized;
        bool _slotsBuilt;
        bool _bagCompanionMode;

        // ShowPanel 内部 Setup/Show 会 Refresh，需在 ShowPanel 调用前置位
        static bool s_bagCompanionShowPending;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;
        }

        public static void TryShow()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.ShowPanel(PanelIdConst);
        }

        public static void TryHide()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.HidePanel(PanelIdConst);
        }

        public static void RefreshFromGame()
        {
            Instance?.Refresh();
        }

        public static bool IsBagCompanionEditing()
        {
            return Instance != null && Instance._bagCompanionMode;
        }

        public static void ShowCompanionForBagIfNeeded()
        {
            if (ShouldUseBagCompanionMode())
            {
                // 秘密基地上下文：以 Popup 层显示
                if (UIManager.Instance == null)
                {
                    return;
                }

                s_bagCompanionShowPending = true;
                try
                {
                    var panel = UIManager.Instance.ShowPanel(PanelIdConst, null, UILayer.Popup) as PlayerHumanItemBarPanel;
                    if (panel != null)
                    {
                        panel._bagCompanionMode = true;
                        panel.ApplyBarMode(EItemBarMode.Expanded);
                        panel.Refresh();
                    }
                }
                finally
                {
                    s_bagCompanionShowPending = false;
                }
            }
            else
            {
                // 普通大地图：面板已作为 HUD 显示，只需切换到展开态
                var inst = Instance;
                if (inst != null)
                {
                    inst._bagCompanionMode = true;
                    inst.ApplyBarMode(EItemBarMode.Expanded);
                    inst.Refresh();
                }
            }
        }

        public static void HideCompanionForBagIfNeeded()
        {
            if (ShouldUseBagCompanionMode())
            {
                // 秘密基地：整体隐藏
                if (Instance != null)
                {
                    Instance._bagCompanionMode = false;
                }
                TryHide();
            }
            else
            {
                // 普通大地图：收回展开态，恢复折叠
                var inst = Instance;
                if (inst != null)
                {
                    inst._bagCompanionMode = false;
                    inst.ApplyBarMode(EItemBarMode.Collapsed);
                    inst.Refresh();
                }
            }
        }

        static bool ShouldUseBagCompanionMode()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm != null && glm.IsInSecretBaseContext())
            {
                return true;
            }

            var ui = UIManager.Instance;
            return ui != null && ui.IsPanelVisible(SecretBaseHudPanel.PanelIdConst);
        }

        public override void Setup(object data = null)
        {
            InitializeBarIfNeeded();
            EnsureSlots();
            Refresh();
        }

        public override void Show()
        {
            base.Show();
            EnsureSlots();
            // 正常 HUD 显示时，始终从折叠态开始；背包打开后由 ShowCompanionForBagIfNeeded 切换
            if (!_bagCompanionMode && !s_bagCompanionShowPending)
            {
                ApplyBarMode(EItemBarMode.Collapsed);
            }
            Refresh();
        }

        public void Refresh()
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null)
            {
                return;
            }

            lgm.playerDataManager?.HumanQuickBar?.PruneInvalidSlots();
            EnsureSlots();
            RefreshSlotBindings();
            RefreshDisableHint(lgm);
            OverworldHUDPanel.Instance?.SkilBar?.Refresh();
        }

        void RefreshDisableHint(GameLogicManager lgm)
        {
            if (_disableHint == null)
            {
                return;
            }

            bool showHint = !lgm.IsHumanQuickBarAvailable() && !IsBagQuickBarEditingActive(lgm);
            _disableHint.SetActive(showHint);
        }

        bool IsBagQuickBarEditingActive(GameLogicManager lgm)
        {
            if (lgm == null || lgm.IsHumanQuickBarAvailable())
            {
                return false;
            }

            if (_bagCompanionMode || s_bagCompanionShowPending)
            {
                return true;
            }

            var ui = UIManager.Instance;
            return ui != null && ui.IsPanelVisible("PlayerBag");
        }

        void InitializeBarIfNeeded()
        {
            if (_barInitialized)
            {
                return;
            }

            if (_consumableSlotTemplate == null || _weaponSlotTemplate == null
                || _weaponColumnParent == null || _consumableWheelParent == null)
            {
                Debug.LogError("PlayerHumanItemBarPanel: missing wheel parents or slot templates.");
                return;
            }

            _consumableSlotTemplate.gameObject.SetActive(false);
            _weaponSlotTemplate.gameObject.SetActive(false);
            _barInitialized = true;
        }

        void EnsureSlots()
        {
            InitializeBarIfNeeded();
            if (!_barInitialized || _slotsBuilt)
            {
                return;
            }

            int layer = gameObject.layer;
            if (layer == 0)
            {
                layer = 5;
            }

            BuildWeaponSlots(layer);
            BuildConsumableWheel(layer);
            _slotsBuilt = true;
        }

        void BuildWeaponSlots(int layer)
        {
            ClearSlotInstances(_weaponColumnParent, WeaponSlotPrefix);

            float step = _weaponSlotSize.y + _weaponSlotSpacing;
            float startY = step * 0.5f;

            for (int i = 0; i < HumanQuickBarDefs.WeaponSlotCount; i++)
            {
                var cell = SpawnSlot(_weaponSlotTemplate, _weaponColumnParent, WeaponSlotPrefix + i, layer);
                _weaponSlots[i] = cell;

                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = _weaponSlotSize;
                rt.anchoredPosition = new Vector2(0f, startY - i * step);
            }
        }

        void BuildConsumableWheel(int layer)
        {
            ClearSlotInstances(_consumableWheelParent, ConsumableSlotPrefix);

            int count = HumanQuickBarDefs.ConsumableSlotCount;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                var cell = SpawnSlot(_consumableSlotTemplate, _consumableWheelParent, ConsumableSlotPrefix + i, layer);
                _consumableSlots[i] = cell;

                var rt = cell.GetComponent<RectTransform>();
                float angleDeg = 90f - i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = _consumableSlotSize;
                rt.anchoredPosition = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * _wheelRadius;
            }
        }

        static T SpawnSlot<T>(T template, RectTransform parent, string slotName, int layer) where T : Component
        {
            var go = Object.Instantiate(template.gameObject, parent);
            go.name = slotName;
            go.SetActive(true);
            SetLayerRecursively(go, layer);
            return go.GetComponent<T>();
        }

        static void ClearSlotInstances(RectTransform parent, string prefix)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(ch.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(ch.gameObject);
                    }
                }
            }
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int c = 0; c < t.childCount; c++)
            {
                SetLayerRecursively(t.GetChild(c).gameObject, layer);
            }
        }

        void RefreshSlotBindings()
        {
            if (!_barInitialized)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var qb = glm?.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return;
            }

            for (int w = 0; w < HumanQuickBarDefs.WeaponSlotCount; w++)
            {
                _weaponSlots[w]?.Bind(w, qb.ActiveWeaponIndex == w);
            }

            for (int c = 0; c < HumanQuickBarDefs.ConsumableSlotCount; c++)
            {
                _consumableSlots[c]?.Bind(c, qb.ActiveConsumableIndex == c);
            }

            if (_centerSkillView == null)
            {
                return;
            }

            if (glm != null && glm.IsInSecretBaseContext())
            {
                _centerSkillView.gameObject.SetActive(false);
            }
            else
            {
                _centerSkillView.gameObject.SetActive(true);
                var pdm = glm.playerDataManager;
                var skillSystem = pdm?.SkillSystem;
                float remainingSec = skillSystem != null && skillSystem.HasTempSkill
                    ? skillSystem.TempSkillRemainingSec
                    : 0f;
                _centerSkillView.Refresh(
                    qb.ResolveLeftClickSkillId(),
                    skillSystem != null && skillSystem.HasTempSkill,
                    remainingSec);
            }

            // 折叠态图标同步
            if (_barMode == EItemBarMode.Collapsed)
            {
                RefreshCollapsedIcon(qb);
            }
        }

        // 切换折叠/展开模式并重新定位面板
        void ApplyBarMode(EItemBarMode mode)
        {
            _barMode = mode;

            bool expanded = mode == EItemBarMode.Expanded;

            // 切换视图根节点可见性
            if (_collapsedRoot != null)
            {
                _collapsedRoot.SetActive(!expanded);
            }
            if (_expandedRoot != null)
            {
                _expandedRoot.SetActive(expanded);
            }

            // 若未设置独立根节点，则直接切换轮盘和武器列的活跃状态
            if (_expandedRoot == null)
            {
                if (_consumableWheelParent != null)
                {
                    _consumableWheelParent.gameObject.SetActive(expanded);
                }
                if (_weaponColumnParent != null)
                {
                    _weaponColumnParent.gameObject.SetActive(expanded);
                }
                if (_centerSkillView != null)
                {
                    _centerSkillView.gameObject.SetActive(expanded);
                }
            }

            // 根据模式移动面板到对应锚点
            MoveToAnchor(expanded);
        }

        void MoveToAnchor(bool expanded)
        {
            var hud = OverworldHUDPanel.Instance;
            if (hud == null)
            {
                return;
            }

            var anchor = expanded ? hud.ItemAnchor2 : hud.ItemAnchor;
            if (anchor == null)
            {
                return;
            }

            // 直接赋世界坐标，在同一 Canvas 下等效于对齐锚点位置
            transform.position = anchor.position;
        }

        // 刷新折叠态的消耗品图标
        void RefreshCollapsedIcon(PlayerHumanQuickBarSystem qb)
        {
            if (_collapsedItemIcon == null)
            {
                return;
            }

            var slots = qb.ConsumableSlots;
            int activeIdx = qb.ActiveConsumableIndex;
            if (activeIdx >= 0 && activeIdx < slots.Length && !slots[activeIdx].IsEmpty)
            {
                var icon = ItemCatalog.GetIcon(slots[activeIdx].ItemId);
                _collapsedItemIcon.sprite = icon;
                _collapsedItemIcon.enabled = icon != null;
            }
            else
            {
                _collapsedItemIcon.enabled = false;
            }
        }
    }
}
