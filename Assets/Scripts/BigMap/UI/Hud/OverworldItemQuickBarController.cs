using My;
using My.Player;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // ItemBar：武器槽 2 + 消耗轮盘 N（人类/未暴露真身且未发情时由 HUD 控制显隐）
    public class OverworldItemQuickBarController : MonoBehaviour
    {
        const string WeaponSlotPrefix = "WeaponQuickSlot_";
        const string ConsumableSlotPrefix = "ConsumableQuickSlot_";

        [SerializeField]
        RectTransform _weaponSlotsParent;

        [SerializeField]
        RectTransform _consumableSlotsParent;

        [SerializeField]
        QuickSlotItemCell _slotTemplate;

        [SerializeField]
        int _consumableSlotCount = HumanQuickBarDefs.ConsumableSlotCount;

        bool _initialized;

        public void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            if (_weaponSlotsParent == null)
            {
                var t = transform.Find("WeaponSlots") ?? transform.Find("GridSlots");
                _weaponSlotsParent = t as RectTransform;
            }

            if (_consumableSlotsParent == null)
            {
                var t = transform.Find("ConsumableSlots") ?? transform.Find("GridSlots");
                _consumableSlotsParent = t as RectTransform;
            }

            if (_slotTemplate == null)
            {
                var tplTr = transform.Find("ItemQuickSlotTemplate") ?? transform.Find("SkillSlotTemplate");
                if (tplTr != null)
                {
                    _slotTemplate = tplTr.GetComponent<QuickSlotItemCell>();
                }
            }

            if (_slotTemplate == null || _weaponSlotsParent == null || _consumableSlotsParent == null)
            {
                Debug.LogError("OverworldItemQuickBarController: missing template or slot parents.");
                return;
            }

            PrepareTemplate(_slotTemplate);
            _initialized = true;
        }

        static void PrepareTemplate(QuickSlotItemCell template)
        {
            EnsureRootRaycastTarget(template.gameObject);
            foreach (var btn in template.GetComponentsInChildren<Button>(true))
            {
                btn.enabled = false;
            }

            template.gameObject.SetActive(false);
        }

        public void EnsureSlots()
        {
            InitializeIfNeeded();
            if (!_initialized)
            {
                return;
            }

            int layer = gameObject.layer;
            if (layer == 0)
            {
                layer = 5;
            }

            RebuildSlots(_weaponSlotsParent, WeaponSlotPrefix, HumanQuickBarDefs.WeaponSlotCount, layer, true);
            RebuildSlots(_consumableSlotsParent, ConsumableSlotPrefix, _consumableSlotCount, layer, false);
        }

        void RebuildSlots(RectTransform parent, string prefix, int count, int layer, bool isWeapon)
        {
            ClearSlotInstances(parent, prefix);

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(_slotTemplate.gameObject, parent);
                go.name = prefix + i;
                go.SetActive(true);
                SetLayerRecursively(go, layer);
                EnsureRootRaycastTarget(go);

                var cell = go.GetComponent<QuickSlotItemCell>();
                if (cell == null)
                {
                    continue;
                }

                if (isWeapon)
                {
                    cell.SetItemCellInteractions(
                        ItemCellInteractions.WeaponQuickSlot,
                        ItemCellInteractions.WeaponQuickSlot,
                        ItemCellInteractions.WeaponQuickSlot);
                }
                else
                {
                    cell.SetItemCellInteractions(
                        ItemCellInteractions.ConsumableQuickSlot,
                        ItemCellInteractions.ConsumableQuickSlot,
                        ItemCellInteractions.ConsumableQuickSlot);
                }
            }
        }

        static void ClearSlotInstances(RectTransform parent, string prefix)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    Destroy(ch.gameObject);
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
            var rt = root.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
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

        public void RefreshFromPlayerData()
        {
            InitializeIfNeeded();
            if (!_initialized)
            {
                return;
            }

            var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return;
            }

            for (int w = 0; w < HumanQuickBarDefs.WeaponSlotCount; w++)
            {
                var tr = _weaponSlotsParent.Find(WeaponSlotPrefix + w);
                var cell = tr != null ? tr.GetComponent<QuickSlotItemCell>() : null;
                cell?.BindWeaponSlot(w, qb.ActiveWeaponIndex == w);
            }

            for (int c = 0; c < _consumableSlotCount; c++)
            {
                var tr = _consumableSlotsParent.Find(ConsumableSlotPrefix + c);
                var cell = tr != null ? tr.GetComponent<QuickSlotItemCell>() : null;
                cell?.BindConsumableSlot(c, qb.ActiveConsumableIndex == c);
            }
        }
    }
}
