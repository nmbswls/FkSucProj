using System.Collections.Generic;
using My;
using My.Map;
using My.Player;
using My.Player.Bag;
using My.UI.Bag;
using UnityEngine;

namespace My.UI
{
    // 背包容器格上的落点：Inventory / SpecialInventory / Warehouse / LootPoint / Shop。
    public class ContainerItemDropBehaviour : MonoBehaviour, IItemCellDropTargetBehaviour
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
}
