using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class QuickSlotItemCell : ItemCellBase
    {
        public void BindWeaponSlot(int slotIndex, bool selected)
        {
            SetItemCellInteractions(
                ItemCellInteractions.WeaponQuickSlot,
                ItemCellInteractions.WeaponQuickSlot,
                ItemCellInteractions.WeaponQuickSlot);

            SetIndexAndContainer(slotIndex, EContainerType.QuickBarWeapon, 0);
            SetOnChanged(null);
            BindQuickSlot(GetWeaponBinding(slotIndex));
            RefreshCellStyle(selected ? ItemCellBase.EStyleType.Selected : ItemCellBase.EStyleType.Normal);
        }

        public void BindConsumableSlot(int slotIndex, bool selected)
        {
            SetItemCellInteractions(
                ItemCellInteractions.ConsumableQuickSlot,
                ItemCellInteractions.ConsumableQuickSlot,
                ItemCellInteractions.ConsumableQuickSlot);

            SetIndexAndContainer(slotIndex, EContainerType.QuickBarConsumable, 0);
            SetOnChanged(null);
            BindQuickSlot(GetConsumableBinding(slotIndex));
            RefreshCellStyle(selected ? ItemCellBase.EStyleType.Selected : ItemCellBase.EStyleType.Normal);
        }

        static QuickSlotBinding GetWeaponBinding(int slotIndex)
        {
            var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
            if (qb == null || slotIndex < 0 || slotIndex >= qb.WeaponSlots.Length)
            {
                return QuickSlotBinding.Empty;
            }

            return qb.WeaponSlots[slotIndex];
        }

        static QuickSlotBinding GetConsumableBinding(int slotIndex)
        {
            var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
            if (qb == null || slotIndex < 0 || slotIndex >= qb.ConsumableSlots.Length)
            {
                return QuickSlotBinding.Empty;
            }

            return qb.ConsumableSlots[slotIndex];
        }

        void BindQuickSlot(QuickSlotBinding binding)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;

            if (binding.IsEmpty)
            {
                SetBoundStack(null);
                icon.enabled = false;
                if (debugNameStr != null)
                {
                    debugNameStr.text = "";
                }

                if (countRect != null)
                {
                    countRect.gameObject.SetActive(false);
                }

                if (maskOverlay != null)
                {
                    maskOverlay.gameObject.SetActive(false);
                }

                return;
            }

            bool available = inv != null && inv.CheckQuickSlotBindingAvailable(binding);
            long displayCount = 1;
            ItemStack displayStack = null;

            if (binding.ItemInstanceId != 0)
            {
                if (inv != null && inv.TryFindCarriedStack(binding, out _, out var pinned))
                {
                    displayStack = pinned;
                    displayCount = pinned.Count;
                }
                else
                {
                    displayStack = new ItemStack(binding.ItemId, 1) { ItemInstanceId = binding.ItemInstanceId };
                    displayCount = 0;
                }
            }
            else
            {
                long total = inv != null ? inv.GetCarriedItemTotal(binding.ItemId) : 0;
                displayCount = total > 0 ? total : 1;
                displayStack = new ItemStack(binding.ItemId, displayCount);
            }

            SetBoundStack(displayStack);
            cacheItemDef = ItemCatalog.GetItemDef(binding.ItemId);
            if (debugNameStr != null)
            {
                debugNameStr.text = cacheItemDef != null ? cacheItemDef.DisplayName : binding.ItemId;
            }

            icon.enabled = true;
            ApplyItemIconSprite(binding.ItemId);

            if (countRect != null)
            {
                bool showCnt = binding.ItemInstanceId == 0 && displayCount > 1;
                countRect.gameObject.SetActive(showCnt);
                if (showCnt && countText != null)
                {
                    countText.text = displayCount.ToString();
                }
            }

            if (maskOverlay != null)
            {
                maskOverlay.gameObject.SetActive(!available);
            }
        }
    }
}
