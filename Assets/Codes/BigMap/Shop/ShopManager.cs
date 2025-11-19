
using System.Collections.Generic;
using My.Player.Bag;

namespace My
{

    public class NormalShop : IShopProvider
    {
        public int ShopId { get; set; }

        public List<(ItemStack, bool)> ShopItems { get { return shopItems; } }

        private List<(ItemStack, bool)> shopItems = new();

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
            List<int> getMapShopIds = new() { 2, 4, 6 };
            for (int i = 0; i < getMapShopIds.Count; i++)
            {
                int shopId = getMapShopIds[i];
                var shop = new NormalShop();

                shop.ShopItems.Add(new(new ItemStack() { ItemID = "flower_01", Count = 5 }, true));
                shop.ShopItems.Add(new(new ItemStack() { ItemID = "flower_02", Count = 4 }, true));
                shop.ShopItems.Add(new(new ItemStack() { ItemID = "flower_03", Count = 3 }, true));

                Shops[shopId] = shop;
            }
        }
    }

}