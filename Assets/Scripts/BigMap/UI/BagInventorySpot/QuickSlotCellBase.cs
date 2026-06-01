using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.Player.Bag;
using TMPro;
using UnityEngine;

namespace My.UI
{
    public abstract class QuickSlotCellBase : ItemCellBase
    {
        protected void BindQuickSlotDisplay(QuickSlotBinding binding)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;

            if (binding.IsEmpty)
            {
                SetBoundStack(null);
                if (icon != null)
                {
                    icon.enabled = false;
                }

                if (debugNameStr != null)
                {
                    debugNameStr.text = string.Empty;
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

            if (icon != null)
            {
                icon.enabled = true;
            }

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

        protected static QuickSlotBinding GetWeaponBinding(int slotIndex)
        {
            var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
            if (qb == null || slotIndex < 0 || slotIndex >= qb.WeaponSlots.Length)
            {
                return QuickSlotBinding.Empty;
            }

            return qb.WeaponSlots[slotIndex];
        }

        protected static QuickSlotBinding GetConsumableBinding(int slotIndex)
        {
            var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
            if (qb == null || slotIndex < 0 || slotIndex >= qb.ConsumableSlots.Length)
            {
                return QuickSlotBinding.Empty;
            }

            return qb.ConsumableSlots[slotIndex];
        }
    }
}
