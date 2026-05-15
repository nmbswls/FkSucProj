using cfg.demo;
using My.Config;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class AnyContainerItemCell : ItemCellBase
    {
        public void Bind(ItemStack stack, int index, EContainerType containerType, int containerId, System.Action<int> onChangedCb, ItemCellBase.EStyleType style = ItemCellBase.EStyleType.Normal)
        {
            EnsureInventoryCellBehaviours();

            SetBoundStack(stack);
            SetIndexAndContainer(index, containerType, containerId);
            SetOnChanged(onChangedCb);

            bool hasItem = stack != null && stack.Count > 0;
            icon.enabled = hasItem;

            if (hasItem)
            {
                var conf = ItemCatalog.GetItemDef(stack.ItemID);
                if (debugNameStr != null)
                {
                    debugNameStr.text = conf != null ? conf.ItemId : stack.ItemID;
                }

                var maxStack = ItemCatalog.GetMaxStackByType(stack.ItemID, containerType);
                if (countRect != null)
                {
                    countRect.gameObject.SetActive(hasItem && maxStack > 1);
                }
            }
            else
            {
                if (debugNameStr != null)
                {
                    debugNameStr.text = "";
                }

                if (countRect != null)
                {
                    countRect.gameObject.SetActive(false);
                }
            }

            if (hasItem)
            {
                cacheItemDef = ItemCatalog.GetItemDef(stack.ItemID);
                ApplyItemIconSprite(stack.ItemID);
                if (countText != null)
                {
                    countText.text = stack.Count.ToString();
                }
            }

            RefreshCellStyle(style);
        }

        void EnsureInventoryCellBehaviours()
        {
            if (GetComponent<InventoryItemCellInteraction>() == null)
            {
                gameObject.AddComponent<InventoryItemCellInteraction>();
            }

            if (GetComponent<ContainerItemDropBehaviour>() == null)
            {
                gameObject.AddComponent<ContainerItemDropBehaviour>();
            }

            RebuildBehaviourCache();
        }

        public void ClearEmpty()
        {
            SetBoundStack(null);
            if (icon != null)
            {
                icon.enabled = false;
            }

            SetIndexAndContainer(-1, 0, 0);
        }
    }
}
