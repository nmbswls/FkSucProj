using My;
using My.Player.Bag;

namespace My.UI
{
    public class WeaponQuickSlotCell : QuickSlotCellBase
    {
        public void Bind(int slotIndex, bool selected)
        {
            SetItemCellInteractions(
                ItemCellInteractions.WeaponQuickSlot,
                ItemCellInteractions.WeaponQuickSlot,
                ItemCellInteractions.WeaponQuickSlot);

            SetIndexAndContainer(slotIndex, EContainerType.QuickBarWeapon, 0);
            SetOnChanged(null);
            BindQuickSlotDisplay(GetWeaponBinding(slotIndex));
            RefreshQuickSlotStyle(selected);
        }
    }
}
