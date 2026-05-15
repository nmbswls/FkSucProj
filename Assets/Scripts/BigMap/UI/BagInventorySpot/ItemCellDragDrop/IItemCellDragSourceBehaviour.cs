using UnityEngine.EventSystems;

namespace My.UI
{
    public interface IItemCellDragSourceBehaviour
    {
        bool TryBeginDrag(ItemCellBase cell, PointerEventData eventData);
    }
}
