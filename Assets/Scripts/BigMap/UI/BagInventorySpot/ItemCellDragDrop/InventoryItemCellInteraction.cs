using My;
using My.Config;
using My.Player.Bag;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI
{
    // 背包容器格：点击弹出菜单；拖拽走背包逻辑。
    public class InventoryItemCellInteraction : MonoBehaviour, IItemCellClickBehaviour, IItemCellDragSourceBehaviour
    {
        public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData)
        {
            if (cell.maskOverlay != null && cell.maskOverlay.gameObject.activeSelf)
            {
                return;
            }

            var stack = cell.GetBoundStack();
            if (stack != null)
            {
                ItemPopupMenu.Show(cell, stack, cell.Index, eventData.position);
            }
            else
            {
                ItemPopupMenu.Close();
            }
        }

        public bool TryBeginDrag(ItemCellBase cell, PointerEventData eventData)
        {
            var stack = cell.GetBoundStack();
            if (stack == null || string.IsNullOrEmpty(stack.ItemID))
            {
                return false;
            }

            if (cell.maskOverlay != null && cell.maskOverlay.gameObject.activeSelf)
            {
                return false;
            }

            ItemPopupMenu.Close();
            if (stack.Count <= 0)
            {
                return false;
            }

            return ItemDragDropController.Instance != null
                   && ItemDragDropController.Instance.BeginDrag(stack, cell.ContainerType, cell.ContainerId, cell.Index);
        }
    }
}
