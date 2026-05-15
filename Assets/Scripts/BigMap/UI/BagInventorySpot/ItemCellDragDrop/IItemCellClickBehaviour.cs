using UnityEngine.EventSystems;

namespace My.UI
{
    public interface IItemCellClickBehaviour
    {
        void OnItemCellClick(ItemCellBase cell, PointerEventData eventData);
    }
}
