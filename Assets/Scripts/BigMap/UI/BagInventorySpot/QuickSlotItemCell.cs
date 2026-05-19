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
            BindItemId(GetWeaponItemId(slotIndex));
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
            BindItemId(GetConsumableItemId(slotIndex));
            RefreshCellStyle(selected ? ItemCellBase.EStyleType.Selected : ItemCellBase.EStyleType.Normal);
        }

        string GetWeaponItemId(int slotIndex)
        {
            var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mdm == null || slotIndex < 0 || slotIndex >= mdm.WeaponQuickSlotItemSet.Length)
            {
                return null;
            }

            return mdm.WeaponQuickSlotItemSet[slotIndex];
        }

        string GetConsumableItemId(int slotIndex)
        {
            var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mdm == null || slotIndex < 0 || slotIndex >= mdm.ConsumableQuickSlotItemSet.Length)
            {
                return null;
            }

            return mdm.ConsumableQuickSlotItemSet[slotIndex];
        }

        void BindItemId(string id)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;

            if (string.IsNullOrEmpty(id))
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

                return;
            }

            long total = inv != null ? inv.GetCarriedItemTotal(id) : 0;
            SetBoundStack(new ItemStack(id, total > 0 ? total : 1));
            cacheItemDef = ItemCatalog.GetItemDef(id);
            if (debugNameStr != null)
            {
                debugNameStr.text = cacheItemDef != null ? cacheItemDef.DisplayName : id;
            }

            icon.enabled = true;
            ApplyItemIconSprite(id);

            if (countRect != null)
            {
                bool showCnt = total > 1;
                countRect.gameObject.SetActive(showCnt);
                if (showCnt && countText != null)
                {
                    countText.text = total.ToString();
                }
            }
        }
    }
}
