
using System.Collections.Generic;
using cfg.demo;
using My.Player.Bag;

namespace My
{
    // 商品在界面上的最小三态：隐藏 / 可见但锁定 / 正常可购
    public enum EShopGoodsDisplayState
    {
        Hidden,
        Locked,
        Normal,
    }

    public class ShopItemInfo
    {
        public string ItemId;
        public long BuyCount;

        public long LeftCount;

        public List<CommonCheckCond> BuyConditions = new();
        public bool ShowWhenHide = false;

        // 全部满足才显示；空列表表示始终尝试显示（再由解锁条件决定锁定）
        public List<CommonCheckCond> VisibleConds = new();
        // 全部满足才可购买；可见但不满足时表现为锁定展示
        public List<CommonCheckCond> UnlockConds = new();

        public string CostItemId;
        public long CostCount = 0;
    }



    public interface IShopProvider
    { 
        int ShopId { get; }

        List<ShopItemInfo> ShopItems { get; }

        List<int> GetVisibleShopItemIndices(GameLogicManager mgr);

        bool TryBuyFromShop(int itemIdx, int count, int? dstBagIdx);

        bool TrySellFromBag(int bagId, int itemIdx);
    }

}