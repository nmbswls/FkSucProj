using My;
using My.Player;
using My.Player.Bag;
using UnityEngine;

namespace My.UI
{
    public class WeaponQuickSlotCell : QuickSlotCellBase
    {
        protected override void Awake()
        {
            // 在基类 Awake 前先挂载武器专用 hover 提供者，
            // 使基类不会再额外挂 ItemCellHoverProvider
            if (GetComponent<ItemCellHoverProvider>() == null)
            {
                gameObject.AddComponent<WeaponCellHoverProvider>();
            }
            base.Awake();
        }

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
