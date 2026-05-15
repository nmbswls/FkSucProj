using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // HUD 快捷道具槽：数据来自 QuickSlotItemSet，数量用背包内持有总数展示。
    public class QuickSlotItemCell : ItemCellBase
    {
        public void BindSlot(int slotIndex)
        {
            SetItemCellInteractions(
                ItemCellInteractions.QuickSlot,
                ItemCellInteractions.QuickSlot,
                ItemCellInteractions.QuickSlot);

            SetIndexAndContainer(slotIndex, EContainerType.QuickBar, 0);
            SetOnChanged(null);

            var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            var inv = mdm?.InventorySystem;
            string id = (mdm != null && slotIndex >= 0 && slotIndex < mdm.QuickSlotItemSet.Length)
                ? mdm.QuickSlotItemSet[slotIndex]
                : null;

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

                RefreshCellStyle(ItemCellBase.EStyleType.Normal);
                return;
            }

            long total = inv != null ? inv.GetCarriedItemTotal(id) : 0;
            SetBoundStack(new ItemStack(id, total > 0 ? total : 1));
            cacheItemDef = ItemCatalog.GetItemDef(id);
            if (debugNameStr != null)
            {
                debugNameStr.text = cacheItemDef != null ? cacheItemDef.ItemId : id;
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

            RefreshCellStyle(ItemCellBase.EStyleType.Normal);
        }
    }
}
