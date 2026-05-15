using My;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI
{
    // HUD 快捷槽：左键使用；拖拽走 QuickBar 逻辑。
    public class QuickSlotItemInteraction : MonoBehaviour, IItemCellClickBehaviour, IItemCellDragSourceBehaviour
    {
        public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || cell.Index < 0)
            {
                return;
            }

            OverworldHUDPanel.Instance?.OnClickUseItem(cell.Index);
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
            if (MainGameManager.Instance?.gameLogicManager?.CanEditQuickSlotBar() != true)
            {
                return false;
            }

            return ItemDragDropController.Instance != null
                   && ItemDragDropController.Instance.BeginDragFromQuickBar(stack.ItemID, cell.Index);
        }
    }
}
