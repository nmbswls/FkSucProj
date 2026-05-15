using System.Collections.Generic;
using My;
using My.Config;
using My.Map;
using My.Player;
using My.Player.Bag;
using My.UI.Bag;
using UnityEngine;
using UnityEngine.EventSystems;

namespace My.UI
{
    // 背包格 / 容器格 / 快捷槽的点击与拖拽落点策略（非 Mono，由 Cell 在 Bind 时注入）。
    public static class ItemCellInteractions
    {
        public static readonly InventoryCellClickDragPolicy InventoryClickDrag = new InventoryCellClickDragPolicy();
        public static readonly ContainerCellDropPolicy ContainerDrop = new ContainerCellDropPolicy();
        public static readonly QuickSlotCellInteractionPolicy QuickSlot = new QuickSlotCellInteractionPolicy();
    }

    public sealed class InventoryCellClickDragPolicy : IItemCellClickBehaviour, IItemCellDragSourceBehaviour
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

    public sealed class ContainerCellDropPolicy : IItemCellDropTargetBehaviour
    {
        public void HandleDrop(ItemCellBase target, DragPayload payload, int dstIndex, ItemDragDropController controller)
        {
            switch (target.ContainerType)
            {
                case EContainerType.Inventory:
                case EContainerType.SpecialInventory:
                case EContainerType.Warehouse:
                    HandleInventoryFamilyDrop(target.ContainerId, payload, dstIndex, controller);
                    break;
                case EContainerType.LootPoint:
                    HandleLootDrop(target.ContainerId, payload, dstIndex, controller);
                    break;
                case EContainerType.Shop:
                    HandleShopDrop(payload, dstIndex, controller);
                    break;
                default:
                    break;
            }
        }

        static void HandleInventoryFamilyDrop(int bagId, DragPayload payload, int dstIndex, ItemDragDropController controller)
        {
            if (payload.SourceContainerType == EContainerType.QuickBar)
            {
                if (MainGameManager.Instance?.gameLogicManager?.CanEditQuickSlotBar() != true)
                {
                    controller.MarkDropHandled();
                    return;
                }

                MainGameManager.Instance.gameLogicManager.playerDataManager.ClearQuickSlot(payload.SourceIndex);
                controller.MarkDropHandled();
                OverworldHUDPanel.Instance?.RefreshItemQuickBar();
                PlayerBagUIPanel.Instance?.RefreshContent();
                return;
            }

            if (payload.SourceContainerType == EContainerType.LootPoint)
            {
                if (LootPointUIPanel.Instance == null || LootPointUIPanel.Instance.Loot == null)
                {
                    Debug.LogError("OnDropToInventory loot point status error");
                    return;
                }

                var srcContainer = LootPointUIPanel.Instance.Loot.GetLootItemContainer();
                var bag = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.GetBagById(bagId);
                var modified = ItemUtils.MoveOrMergeOrSwapItem(srcContainer, payload.SourceIndex, bag, dstIndex);
                if (modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
            else if (payload.SourceContainerType == EContainerType.Shop)
            {
                var buyItem = ShopNormalUIPanel.Instance.BindShop.ShopItems[payload.SourceIndex];
                if (buyItem.LeftCount > 1)
                {
                    ItemCountChooseBox.Show(buyItem.LeftCount, initVal: 1, (chooseCnt) =>
                    {
                        bool buy = ShopNormalUIPanel.Instance.BindShop.TryBuyFromShop(payload.SourceIndex, (int)chooseCnt, null);
                        if (buy)
                        {
                            ShopNormalUIPanel.Instance.RefreshContent();
                            PlayerBagUIPanel.Instance?.RefreshContent();
                            WarehouseUIPanel.Instance?.RefreshContent();
                        }
                    });
                }
                else
                {
                    bool buy = ShopNormalUIPanel.Instance.BindShop.TryBuyFromShop(payload.SourceIndex, 1, null);
                    if (buy)
                    {
                        ShopNormalUIPanel.Instance.RefreshContent();
                        PlayerBagUIPanel.Instance?.RefreshContent();
                        WarehouseUIPanel.Instance?.RefreshContent();
                    }
                }
            }
            else if (payload.SourceContainerType == EContainerType.Inventory
                     || payload.SourceContainerType == EContainerType.SpecialInventory
                     || payload.SourceContainerType == EContainerType.Warehouse)
            {
                int fromBag = payload.SourceContainerId;
                int toBag = bagId;
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                bool ok = inv.TrySwapOrMove(fromBag, payload.SourceIndex, toBag, dstIndex);
                if (ok)
                {
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
        }

        static void HandleLootDrop(int bagId, DragPayload payload, int dstIndex, ItemDragDropController controller)
        {
            if (LootPointUIPanel.Instance == null || LootPointUIPanel.Instance.Loot == null)
            {
                Debug.LogError("OnDropToInventory loot point status error");
                return;
            }

            if (payload.SourceContainerType == EContainerType.LootPoint)
            {
                var container = LootPointUIPanel.Instance.Loot.GetLootItemContainer();
                var modified = ItemUtils.MoveOrMergeOrSwapItem(container, payload.SourceIndex, container, dstIndex);
                if (modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                }
            }
            else if (payload.SourceContainerType == EContainerType.Inventory
                     || payload.SourceContainerType == EContainerType.SpecialInventory
                     || payload.SourceContainerType == EContainerType.Warehouse)
            {
                var container = LootPointUIPanel.Instance.Loot.GetLootItemContainer();
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                var fromBag = inv.GetBagById(payload.SourceContainerId);
                var modified = ItemUtils.MoveOrMergeOrSwapItem(fromBag, payload.SourceIndex, container, dstIndex);
                if (modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
        }

        static void HandleShopDrop(DragPayload payload, int dstIndex, ItemDragDropController controller)
        {
            if (ShopNormalUIPanel.Instance == null || ShopNormalUIPanel.Instance.BindShop == null)
            {
                Debug.LogError("OnDropToShop loot point status error");
                return;
            }

            if (payload.SourceContainerType == EContainerType.Inventory
                || payload.SourceContainerType == EContainerType.SpecialInventory
                || payload.SourceContainerType == EContainerType.Warehouse)
            {
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                var fromBag = inv.GetBagById(payload.SourceContainerId);
                if (payload.ItemCnt > 0)
                {
                    UIManager.Instance.ShowPanel("ItemCountChooseBox", new Dictionary<int, long>());
                }

                bool sell = ShopNormalUIPanel.Instance.BindShop.TrySellFromBag((int)fromBag.BagId, payload.SourceIndex);
                if (sell)
                {
                    ShopNormalUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
        }
    }

    public sealed class QuickSlotCellInteractionPolicy : IItemCellClickBehaviour, IItemCellDragSourceBehaviour, IItemCellDropTargetBehaviour
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
