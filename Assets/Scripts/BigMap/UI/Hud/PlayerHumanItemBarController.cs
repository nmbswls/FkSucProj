using My;
using My.Player;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 武器槽 2（右侧）+ 消耗轮盘 8（环形，prefab 预摆放）+ 中心 LMB 技能
    public class PlayerHumanItemBarController : MonoBehaviour
    {
        const string WeaponSlotPrefix = "WeaponQuickSlot_";
        const string ConsumableSlotPrefix = "ConsumableQuickSlot_";
        const string ConsumableWheelName = "ConsumableWheel";
        const string WeaponColumnName = "WeaponColumn";

        [SerializeField]
        RectTransform _weaponColumnParent;

        [SerializeField]
        RectTransform _consumableWheelParent;

        [SerializeField]
        ItemBarCenterSkillView _centerSkillView;

        [SerializeField]
        QuickSlotItemCell[] _consumableSlots = new QuickSlotItemCell[HumanQuickBarDefs.ConsumableSlotCount];

        [SerializeField]
        QuickSlotItemCell[] _weaponSlots = new QuickSlotItemCell[HumanQuickBarDefs.WeaponSlotCount];

        bool _initialized;

        public void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            EnsureHierarchyRefs();
            CollectSlotsFromHierarchyIfNeeded();

            if (!ValidateSlotSetup())
            {
                return;
            }

            PrepareSlotCells();
            _initialized = true;
        }

        void EnsureHierarchyRefs()
        {
            if (_consumableWheelParent == null)
            {
                _consumableWheelParent = transform.Find(ConsumableWheelName) as RectTransform;
            }

            if (_weaponColumnParent == null)
            {
                _weaponColumnParent = transform.Find(WeaponColumnName) as RectTransform
                    ?? transform.Find("WeaponSlots") as RectTransform
                    ?? transform.Find("GridSlots") as RectTransform;
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

        void CollectSlotsFromHierarchyIfNeeded()
        {
            if (_consumableWheelParent == null || _weaponColumnParent == null)
            {
                return;
            }

            if (!HasAnyAssignedSlot(_consumableSlots))
            {
                FillSlotArrayFromChildren(_consumableWheelParent, ConsumableSlotPrefix, _consumableSlots);
            }

            if (!HasAnyAssignedSlot(_weaponSlots))
            {
                FillSlotArrayFromChildren(_weaponColumnParent, WeaponSlotPrefix, _weaponSlots);
            }
        }

        static bool HasAnyAssignedSlot(QuickSlotItemCell[] slots)
        {
            if (slots == null)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        static void FillSlotArrayFromChildren(RectTransform parent, string prefix, QuickSlotItemCell[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                var tr = parent.Find(prefix + i);
                arr[i] = tr != null ? tr.GetComponent<QuickSlotItemCell>() : null;
            }
        }

        bool ValidateSlotSetup()
        {
            if (_consumableWheelParent == null || _weaponColumnParent == null)
            {
                Debug.LogError("PlayerHumanItemBarController: missing ConsumableWheel or WeaponColumn.");
                return false;
            }

            for (int i = 0; i < HumanQuickBarDefs.ConsumableSlotCount; i++)
            {
                if (_consumableSlots == null || i >= _consumableSlots.Length || _consumableSlots[i] == null)
                {
                    Debug.LogError(
                        $"PlayerHumanItemBarController: missing prefab slot {ConsumableSlotPrefix}{i}. "
                        + "Assign ConsumableWheel slots in PlayerHumanItemBarPanel prefab.");
                    return false;
                }
            }

            for (int i = 0; i < HumanQuickBarDefs.WeaponSlotCount; i++)
            {
                if (_weaponSlots == null || i >= _weaponSlots.Length || _weaponSlots[i] == null)
                {
                    Debug.LogError(
                        $"PlayerHumanItemBarController: missing prefab slot {WeaponSlotPrefix}{i}. "
                        + "Assign WeaponColumn slots in PlayerHumanItemBarPanel prefab.");
                    return false;
                }
            }

            return true;
        }

        void PrepareSlotCells()
        {
            for (int i = 0; i < _consumableSlots.Length; i++)
            {
                PrepareSlotCell(_consumableSlots[i], true);
            }

            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                PrepareSlotCell(_weaponSlots[i], false);
            }
        }

        static void PrepareSlotCell(QuickSlotItemCell cell, bool consumable)
        {
            if (cell == null)
            {
                return;
            }

            EnsureRootRaycastTarget(cell.gameObject);
            foreach (var btn in cell.GetComponentsInChildren<Button>(true))
            {
                btn.enabled = false;
            }

            cell.gameObject.SetActive(true);
            cell.SetItemCellInteractions(
                consumable ? ItemCellInteractions.ConsumableQuickSlot : ItemCellInteractions.WeaponQuickSlot,
                consumable ? ItemCellInteractions.ConsumableQuickSlot : ItemCellInteractions.WeaponQuickSlot,
                consumable ? ItemCellInteractions.ConsumableQuickSlot : ItemCellInteractions.WeaponQuickSlot);
        }

        public void EnsureSlots()
        {
            InitializeIfNeeded();
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

        public void RefreshFromPlayerData()
        {
            InitializeIfNeeded();
            if (!_initialized)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var qb = glm?.playerDataManager?.HumanQuickBar;
            if (qb == null)
            {
                return;
            }

            for (int w = 0; w < HumanQuickBarDefs.WeaponSlotCount && w < _weaponSlots.Length; w++)
            {
                _weaponSlots[w]?.BindWeaponSlot(w, qb.ActiveWeaponIndex == w);
            }

            for (int c = 0; c < HumanQuickBarDefs.ConsumableSlotCount && c < _consumableSlots.Length; c++)
            {
                _consumableSlots[c]?.BindConsumableSlot(c, qb.ActiveConsumableIndex == c);
            }

            _centerSkillView?.Refresh(qb.ResolveLeftClickSkillId());
        }
    }
}
