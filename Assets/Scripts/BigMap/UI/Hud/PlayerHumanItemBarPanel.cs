using My;
using My.Config;
using My.Map;
using My.Player;
using UnityEngine;
using DG.Tweening;
using My.Player.Bag;

namespace My.UI
{
    // 人类消耗品快捷栏：中心常驻显示当前选中道具，编辑时展开轮盘
    public class PlayerHumanItemBarPanel : PanelBase
    {
        public const string PanelIdConst = "PlayerHumanItemBarPanel";

        const string ConsumableSlotPrefix = "ConsumableQuickSlot_";

        [SerializeField]
        RectTransform _consumableWheelParent;

        [SerializeField]
        ItemBarCenterItemView _centerItemView;

        [SerializeField]
        ConsumableQuickSlotCell _consumableSlotTemplate;

        [SerializeField]
        float _wheelRadius = 72f;

        [SerializeField]
        Vector2 _consumableSlotSize = new Vector2(45f, 45f);

        [SerializeField]
        float _anchorMoveDuration = 0.25f;

        readonly ConsumableQuickSlotCell[] _consumableSlots =
            new ConsumableQuickSlotCell[HumanQuickBarDefs.ConsumableSlotCount];

        bool _slotsBuilt;
        bool _editing;
        UILayer _savedLayer = UILayer.HUD;
        Tween _anchorMoveTween;

        public static PlayerHumanItemBarPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as PlayerHumanItemBarPanel;
            }
        }

        public static bool IsBagOpen()
        {
            var ui = UIManager.Instance;
            return ui != null && ui.IsPanelVisible("PlayerBag");
        }

        public static bool IsQuickUseBlocked()
        {
            return IsBagOpen() || IsBagCompanionEditing();
        }

        public static bool IsBagCompanionEditing()
        {
            return Instance != null && Instance._editing;
        }

        public static void TryShow()
        {
            UIManager.Instance?.ShowPanel(PanelIdConst);
        }

        public static void TryHide()
        {
            UIManager.Instance?.HidePanel(PanelIdConst);
        }

        public static void RefreshFromGame()
        {
            Instance?.Refresh();
        }

        public static void ShowCompanionForBagIfNeeded()
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                return;
            }

            var panel = ui.IsPanelVisible(PanelIdConst)
                ? Instance
                : ui.ShowPanel(PanelIdConst, null, ShouldUseBagCompanionMode() ? UILayer.Popup : null)
                    as PlayerHumanItemBarPanel;

            panel?.SetEditing(true);
        }

        public static void HideCompanionForBagIfNeeded()
        {
            var panel = Instance;
            if (panel == null)
            {
                return;
            }

            panel.SetEditing(false);

            if (ShouldUseBagCompanionMode())
            {
                TryHide();
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

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;
            _savedLayer = UILayer.HUD;

            if (_consumableSlotTemplate != null)
            {
                _consumableSlotTemplate.gameObject.SetActive(false);
            }

            if (_consumableWheelParent != null)
            {
                _consumableWheelParent.gameObject.SetActive(false);
            }
        }

        public override void Setup(object data = null)
        {
            EnsureSlots();
            Refresh();
        }

        public override void Show()
        {
            base.Show();
            EnsureSlots();
            if (!_editing)
            {
                ApplyWheelVisible(false);
                MoveToAnchor(false);
            }
            Refresh();
        }

        public void Refresh()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            glm.playerDataManager?.HumanQuickBar?.PruneInvalidSlots();
            EnsureSlots();
            RefreshBindings(glm);
        }

        void SetEditing(bool editing)
        {
            _editing = editing;
            ApplyCompanionLayer(editing);
            ApplyWheelVisible(editing);
            MoveToAnchor(editing);
            Refresh();
        }

        void ApplyCompanionLayer(bool editing)
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                return;
            }

            var targetLayer = editing && ShouldUseBagCompanionMode() ? UILayer.Popup : _savedLayer;
            var layerRoot = ui.GetLayerRoot(targetLayer);
            if (layerRoot == null)
            {
                return;
            }

            layer = targetLayer;
            transform.SetParent(layerRoot, false);
            if (editing)
            {
                transform.SetAsLastSibling();
            }
        }

        void ApplyWheelVisible(bool visible)
        {
            if (_consumableWheelParent != null)
            {
                _consumableWheelParent.gameObject.SetActive(visible);
            }
        }

        void EnsureSlots()
        {
            if (_slotsBuilt || _consumableSlotTemplate == null || _consumableWheelParent == null)
            {
                if (_consumableSlotTemplate == null || _consumableWheelParent == null)
                {
                    Debug.LogError("[PlayerHumanItemBarPanel] Missing consumable wheel or slot template.");
                }
                return;
            }

            int layer = gameObject.layer;
            if (layer == 0)
            {
                layer = 5;
            }

            BuildConsumableWheel(layer);
            _slotsBuilt = true;
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
                if (!ch.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

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

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int c = 0; c < t.childCount; c++)
            {
                SetLayerRecursively(t.GetChild(c).gameObject, layer);
            }
        }

        void RefreshBindings(GameLogicManager glm)
        {
            var qb = glm.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return;
            }

            for (int i = 0; i < HumanQuickBarDefs.ConsumableSlotCount; i++)
            {
                _consumableSlots[i]?.Bind(i, qb.ActiveConsumableIndex == i);
            }

            var binding = qb.GetActiveConsumableBinding();
            string itemId = binding.IsEmpty ? null : binding.ItemId;
            long stackCount = ResolveConsumableDisplayCount(binding, glm.playerDataManager?.InventorySystem);
            bool usable = EvaluateActiveConsumableUsable(qb, glm);
            _centerItemView?.RefreshItem(itemId, stackCount, usable);
        }

        static long ResolveConsumableDisplayCount(QuickSlotBinding binding, PlayerInventorySystem inv)
        {
            if (binding.IsEmpty)
            {
                return 0;
            }

            if (binding.ItemInstanceId != 0)
            {
                if (inv != null && inv.TryFindCarriedStack(binding, out _, out var pinned))
                {
                    return pinned.Count;
                }

                return 1;
            }

            long total = inv != null ? inv.GetCarriedItemTotal(binding.ItemId) : 0;
            return total > 0 ? total : 1;
        }

        static bool EvaluateActiveConsumableUsable(PlayerHumanQuickBarSystem qb, GameLogicManager glm)
        {
            if (glm == null || !glm.IsHumanQuickBarAvailable())
            {
                return false;
            }

            if (IsQuickUseBlocked())
            {
                return false;
            }

            var binding = qb.GetActiveConsumableBinding();
            if (binding.IsEmpty)
            {
                return false;
            }

            var inv = glm.playerDataManager?.InventorySystem;
            if (inv == null || !inv.CheckQuickSlotBindingAvailable(binding))
            {
                return false;
            }

            return ItemCatalog.CanUse(binding.ItemId);
        }

        void OnDestroy()
        {
            _anchorMoveTween?.Kill();
        }

        void MoveToAnchor(bool editing)
        {
            var hud = OverworldHUDPanel.Instance;
            if (hud == null)
            {
                return;
            }

            var anchor = editing ? hud.ItemAnchor2 : hud.ItemAnchor;
            if (anchor == null)
            {
                return;
            }

            _anchorMoveTween?.Kill();
            _anchorMoveTween = transform
                .DOMove(anchor.position, _anchorMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject);
        }
    }
}
