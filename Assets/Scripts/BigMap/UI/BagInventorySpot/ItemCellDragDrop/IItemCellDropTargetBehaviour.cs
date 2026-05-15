namespace My.UI
{
    public interface IItemCellDropTargetBehaviour
    {
        void HandleDrop(ItemCellBase target, DragPayload payload, int dstIndex, ItemDragDropController controller);
    }
}
