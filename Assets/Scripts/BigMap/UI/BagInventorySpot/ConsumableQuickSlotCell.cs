using My;
using My.Player;
using My.Player.Bag;
using UnityEngine;

namespace My.UI
{
    public class ConsumableQuickSlotCell : QuickSlotCellBase
    {
        public void Bind(int slotIndex, bool selected)
        {
            SetItemCellInteractions(
                ItemCellInteractions.ConsumableQuickSlot,
                ItemCellInteractions.ConsumableQuickSlot,
                ItemCellInteractions.ConsumableQuickSlot);

            SetIndexAndContainer(slotIndex, EContainerType.QuickBarConsumable, 0);
            SetOnChanged(null);

            var binding = GetConsumableBinding(slotIndex);
            BindQuickSlotDisplay(binding);
            RefreshQuickSlotStyle(selected);
            RefreshEnchantRing(binding);
        }

        void RefreshEnchantRing(QuickSlotBinding binding)
        {
            if (enchantRingOverlay == null)
            {
                return;
            }

            bool show = false;
            if (!binding.IsEmpty)
            {
                var enchant = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ItemEnchant;
                show = enchant != null && enchant.IsEnchanted(binding.ItemId);
            }

            enchantRingOverlay.gameObject.SetActive(show);
        }
    }
}
