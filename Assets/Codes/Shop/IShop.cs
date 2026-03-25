
using System.Collections.Generic;
using cfg.demo;
using My.Player.Bag;

namespace My
{
    public class ShopItemInfo
    {
        public string ItemId;
        public long BuyCount;

        public long LeftCount;

        public List<CommonCheckCond> BuyConditions = new();
        public bool ShowWhenHide = false;

        public string CostItemId;
        public long CostCount = 0;
    }



    public interface IShopProvider
    { 
        int ShopId { get; }

        List<ShopItemInfo> ShopItems { get; }

        bool TryBuyFromShop(int itemIdx, int count, int? dstBagIdx);

        bool TrySellFromBag(int bagId, int itemIdx);
    }

}