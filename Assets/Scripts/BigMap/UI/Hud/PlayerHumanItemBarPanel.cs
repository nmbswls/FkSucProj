using My;
using My.Player;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class PlayerHumanItemBarPanel : PanelBase
    {
        public const string PanelIdConst = "PlayerHumanItemBarPanel";

        const string WeaponSlotPrefix = "WeaponQuickSlot_";
        const string ConsumableSlotPrefix = "ConsumableQuickSlot_";
        const string ConsumableWheelName = "ConsumableWheel";
        const string WeaponColumnName = "WeaponColumn";
        const string ConsumableTemplateName = "ConsumableQuickSlotTemplate";
        const string LegacyConsumableTemplateName = "ItemQuickSlotTemplate";
        const string WeaponTemplateName = "WeaponQuickSlotTemplate";

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
        QuickSlotItemCell _consumableSlotTemplate;

        [SerializeField]
        QuickSlotItemCell _weaponSlotTemplate;

        [SerializeField]
        float _wheelRadius = 72f;

        [SerializeField]
        Vector2 _consumableSlotSize = new Vector2(45f, 45f);

        [SerializeField]
        Vector2 _weaponSlotSize = new Vector2(50f, 50f);

        [SerializeField]
        float _weaponSlotSpacing = 5f;

        QuickSlotItemCell[] _consumableSlots = new QuickSlotItemCell[HumanQuickBarDefs.ConsumableSlotCount];
        QuickSlotItemCell[] _weaponSlots = new QuickSlotItemCell[HumanQuickBarDefs.WeaponSlotCount];

        bool _barInitialized;
        bool _slotsBuilt;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_contentRoot == null)
            {
                _contentRoot = transform as RectTransform;
            }

            CleanupUnusedPrefabChildren();
            EnsureHierarchyRefs();
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

        public static void ShowCompanionForBagIfNeeded()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                return;
            }

            if (UIManager.Instance == null)
            {
                return;
            }

            var panel = UIManager.Instance.ShowPanel(PanelIdConst) as PlayerHumanItemBarPanel;
            panel?.Refresh();
        }

        public static void HideCompanionForBagIfNeeded()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                return;
            }

            TryHide();
        }

        public void EnsureQuickItemBarReady()
        {
            InitializeBarIfNeeded();
            EnsureSlots();
            RefreshSlotBindings();
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
            Refresh();
        }

        public void Refresh()
        {
            var lgm = MainGameManager.Instance?.gameLogicManager;
            if (lgm == null)
            {
                return;
            }

            bool showContent = ShouldShowBarContent(lgm);
            SetBarContentVisible(showContent);

            if (!showContent)
            {
                return;
            }

            lgm.playerDataManager?.HumanQuickBar?.PruneInvalidSlots();
            EnsureSlots();
            RefreshSlotBindings();
            OverworldHUDPanel.Instance?.SkilBar?.Refresh();
        }

        // 开放世界：随 HUD 受 IsHumanQuickBarAvailable 约束；基地内打开背包时始终显示以便编辑快捷栏
        static bool ShouldShowBarContent(GameLogicManager lgm)
        {
            if (lgm.IsInSecretBaseContext()
                && UIManager.Instance != null
                && UIManager.Instance.IsPanelVisible("PlayerBag"))
            {
                return true;
            }

            return lgm.IsHumanQuickBarAvailable();
        }

        void SetBarContentVisible(bool visible)
        {
            if (_consumableWheelParent != null)
            {
                _consumableWheelParent.gameObject.SetActive(visible);
            }

            if (_weaponColumnParent != null)
            {
                _weaponColumnParent.gameObject.SetActive(visible);
            }

            if (_centerSkillView != null)
            {
                _centerSkillView.gameObject.SetActive(visible);
            }
        }

        void CleanupUnusedPrefabChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var ch = transform.GetChild(i);
                var name = ch.name;
                if (name == "GridSlots"
                    || name == "DetailAnchorPos"
                    || name.StartsWith("ItemQuickSlotTemplate (", System.StringComparison.Ordinal)
                    || name == "WeaponQuickSlot_1")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(ch.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(ch.gameObject);
                    }
                }
            }
        }

        void InitializeBarIfNeeded()
        {
            if (_barInitialized)
            {
                return;
            }

            EnsureHierarchyRefs();

            if (_consumableSlotTemplate == null || _weaponSlotTemplate == null
                || _weaponColumnParent == null || _consumableWheelParent == null)
            {
                Debug.LogError("PlayerHumanItemBarPanel: missing wheel parents or slot templates.");
                return;
            }

            PrepareTemplate(_consumableSlotTemplate);
            PrepareTemplate(_weaponSlotTemplate);
            _barInitialized = true;
        }

        void EnsureHierarchyRefs()
        {
            if (_consumableWheelParent == null)
            {
                _consumableWheelParent = transform.Find(ConsumableWheelName) as RectTransform;
            }

            if (_weaponColumnParent == null)
            {
                _weaponColumnParent = transform.Find(WeaponColumnName) as RectTransform;
            }

            if (_consumableSlotTemplate == null)
            {
                _consumableSlotTemplate = FindTemplateCell(ConsumableTemplateName)
                    ?? FindTemplateCell(LegacyConsumableTemplateName);
            }

            if (_weaponSlotTemplate == null)
            {
                _weaponSlotTemplate = FindTemplateCell(WeaponTemplateName);
            }

            if (_centerSkillView == null)
            {
                var centerTr = transform.Find("CenterSkillSlot") ?? transform.Find("Round2");
                if (centerTr != null)
                {
                    _centerSkillView = centerTr.GetComponent<ItemBarCenterSkillView>();
                    if (_centerSkillView == null)
                    {
                        _centerSkillView = centerTr.gameObject.AddComponent<ItemBarCenterSkillView>();
                    }
                }
            }
        }

        QuickSlotItemCell FindTemplateCell(string name)
        {
            var tr = transform.Find(name);
            return tr != null ? tr.GetComponent<QuickSlotItemCell>() : null;
        }

        static void PrepareTemplate(QuickSlotItemCell template)
        {
            if (template == null)
            {
                return;
            }

            EnsureRootRaycastTarget(template.gameObject);
            foreach (var btn in template.GetComponentsInChildren<Button>(true))
            {
                btn.enabled = false;
            }

            template.gameObject.SetActive(false);
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

                cell.SetItemCellInteractions(
                    ItemCellInteractions.WeaponQuickSlot,
                    ItemCellInteractions.WeaponQuickSlot,
                    ItemCellInteractions.WeaponQuickSlot);
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

                cell.SetItemCellInteractions(
                    ItemCellInteractions.ConsumableQuickSlot,
                    ItemCellInteractions.ConsumableQuickSlot,
                    ItemCellInteractions.ConsumableQuickSlot);
            }
        }

        static QuickSlotItemCell SpawnSlot(
            QuickSlotItemCell template,
            RectTransform parent,
            string slotName,
            int layer)
        {
            var go = Instantiate(template.gameObject, parent);
            go.name = slotName;
            go.SetActive(true);
            SetLayerRecursively(go, layer);
            EnsureRootRaycastTarget(go);
            return go.GetComponent<QuickSlotItemCell>();
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
                        Destroy(ch.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(ch.gameObject);
                    }
                }
            }
        }

        static void EnsureRootRaycastTarget(GameObject root)
        {
            var img = root.GetComponent<Image>();
            if (img == null)
            {
                img = root.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0.02f);
            }

            img.raycastTarget = true;
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
                _weaponSlots[w]?.BindWeaponSlot(w, qb.ActiveWeaponIndex == w);
            }

            for (int c = 0; c < HumanQuickBarDefs.ConsumableSlotCount; c++)
            {
                _consumableSlots[c]?.BindConsumableSlot(c, qb.ActiveConsumableIndex == c);
            }

            _centerSkillView?.Refresh(qb.ResolveLeftClickSkillId());
        }
    }
}
