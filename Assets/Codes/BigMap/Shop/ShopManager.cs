
using System.Collections.Generic;
using My.Player.Bag;
using UnityEngine;
using static UnityEditor.Progress;

namespace My
{

    public class NormalShop : IShopProvider
    {
        public int ShopId { get; set; }

        public List<ShopItemInfo> ShopItems { get { return shopItems; } }

        private List<ShopItemInfo> shopItems = new();

        public GameLogicManager gameLogicManager { get; protected set; }
        public NormalShop(GameLogicManager logicManager)
        {
            this.gameLogicManager = logicManager;
        }

        public bool TryBuyFromShop(int itemIdx, int count, int? dstBagIdx)
        {
            if (itemIdx < 0 || itemIdx >= shopItems.Count)
            {
                Debug.LogError($"TryBuyFromShop {itemIdx} {count}");
                return false;
            }

            var shopItem = shopItems[itemIdx];

            var costItem = shopItem.CostItemId;
            if (!gameLogicManager.playerDataManager.CheckHaveItem(costItem, shopItem.CostCount))
            {
                Debug.LogError($"TryBuyFromShop cost not enough {costItem} {shopItem.CostCount}");
                return false;
            }

            string giveItem = shopItem.ItemId;
            long giveCount = shopItem.BuyCount;

            // 检查是否能放的下
            if (gameLogicManager.playerDataManager.CanGainItems(giveItem, giveCount))
            {
                return false;
            }

            gameLogicManager.playerDataManager.CostItem(shopItem.CostItemId, shopItem.CostCount);

            long addCnt = gameLogicManager.playerDataManager.inventoryModel.MainBag.TryAdd(new ItemStack()
            {
                ItemID = giveItem,
                Count = giveCount,
            });
            Debug.Log("TryBuyFromShop try buy " + giveItem + " " + addCnt);
            //if (dstBagIdx != null)
            //{

            //}

            return true;
        }


        public bool TrySellFromBag(int bagId, int itemIdx)
        {
            var bag = gameLogicManager.playerDataManager.inventoryModel.GetBagById(bagId);

            var item = bag.GetItemByIdx(itemIdx);
            if(item == null || item.Count <= 0)
            {
                return false;
            }

            // if can sell
            Debug.Log("TryBuyFromShop try buy " + giveItem + " " + addCnt);
            bag.RemoveAt(itemIdx, );

            return true;
        }
    }


    public class ShopDataManager
    {


        public GameLogicManager logicManager;
        public ShopDataManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }

        public Dictionary<int, IShopProvider> Shops = new();

        public IShopProvider GetShop(int shopId)
        {
            Shops.TryGetValue(shopId, out var shop);
            return shop;
        }
        public void TryRefreshOnDayChange()
        {
            foreach (var shop in Shops)
            {

            }
        }

        public void RefreshOnNightStart()
        {
            List<int> getMapShopIds = new() { 1, 2, 4, 6 };
            for (int i = 0; i < getMapShopIds.Count; i++)
            {
                int shopId = getMapShopIds[i];
                var shop = new NormalShop(logicManager);

                {
                    var shopItem = new ShopItemInfo();
                    shopItem.ItemId = "flower_01";
                    shopItem.BuyCount = 1;

                    shopItem.CostItemId = "gold";
                    shopItem.CostCount = 50;

                    shop.ShopItems.Add(shopItem);
                }

                {
                    var shopItem = new ShopItemInfo();
                    shopItem.ItemId = "flower_02";
                    shopItem.BuyCount = 1;

                    shopItem.CostItemId = "gold";
                    shopItem.CostCount = 50;

                    shop.ShopItems.Add(shopItem);
                }

                {
                    var shopItem = new ShopItemInfo();
                    shopItem.ItemId = "flower_03";
                    shopItem.BuyCount = 1;

                    shopItem.CostItemId = "gold";
                    shopItem.CostCount = 100;

                    shop.ShopItems.Add(shopItem);
                }

                Shops[shopId] = shop;
            }
        }
    }

}