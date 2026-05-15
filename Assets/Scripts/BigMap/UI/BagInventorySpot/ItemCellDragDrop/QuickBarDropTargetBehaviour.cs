using My;
using My.Player.Bag;
using UnityEngine;

namespace My.UI
{
    // HUD 快捷槽作为落点：分配 / 互换 QuickSlot。
    public class QuickBarDropTargetBehaviour : MonoBehaviour, IItemCellDropTargetBehaviour
    {
        public void HandleDrop(ItemCellBase target, DragPayload payload, int dstIndex, ItemDragDropController controller)
        {
            int dstSlotIndex = target.Index;
            if (MainGameManager.Instance?.gameLogicManager?.CanEditQuickSlotBar() != true)
            {
                return;
            }

            var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mdm == null || payload == null)
            {
                return;
            }

            if (payload.SourceContainerType == EContainerType.QuickBar)
            {
                if (payload.SourceIndex == dstSlotIndex)
                {
                    controller.MarkDropHandled();
                    return;
                }

                mdm.SwapQuickSlotIndices(payload.SourceIndex, dstSlotIndex);
                controller.MarkDropHandled();
                OverworldHUDPanel.Instance?.RefreshItemQuickBar();
                return;
            }

            if (payload.SourceContainerType == EContainerType.Inventory
                || payload.SourceContainerType == EContainerType.SpecialInventory
                || payload.SourceContainerType == EContainerType.Warehouse)
            {
                if (!mdm.TryAssignQuickSlot(dstSlotIndex, payload.ItemId, out var fail))
                {
                    Debug.Log("Quick bar drop rejected: " + fail);
                    return;
                }

                controller.MarkDropHandled();
                OverworldHUDPanel.Instance?.RefreshItemQuickBar();
            }
        }
    }
}
