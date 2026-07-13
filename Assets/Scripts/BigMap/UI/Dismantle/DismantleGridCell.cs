using System;
using My.Player.Bag;
using UnityEngine.EventSystems;

namespace My.UI.Dismantle
{
    public sealed class DismantleGridCell : AnyContainerItemCell
    {
        sealed class SelectPolicy : IItemCellClickBehaviour
        {
            readonly Action _select;
            public SelectPolicy(Action select) { _select = select; }
            public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData) { _select?.Invoke(); }
        }

        public void BindSelection(ItemStack stack, int index, EContainerType containerType,
            bool selected, Action onSelected)
        {
            Bind(stack, index, containerType, 0, null,
                selected ? EStyleType.Selected : EStyleType.Normal);
            SetItemCellInteractions(new SelectPolicy(onSelected), null, null);
        }
    }
}
