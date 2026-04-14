
namespace My
{
    public static class ShopGoodsDisplay
    {
        public static EShopGoodsDisplayState GetDisplayState(GameLogicManager mgr, ShopItemInfo item)
        {
            if (item == null)
            {
                return EShopGoodsDisplayState.Hidden;
            }

            if (mgr == null)
            {
                return EShopGoodsDisplayState.Normal;
            }

            if (!glmCheckVisible(mgr, item))
            {
                return EShopGoodsDisplayState.Hidden;
            }

            if (!glmCheckUnlock(mgr, item))
            {
                return EShopGoodsDisplayState.Locked;
            }

            return EShopGoodsDisplayState.Normal;
        }

        static bool glmCheckVisible(GameLogicManager mgr, ShopItemInfo item)
        {
            return mgr.CheckCommonCondsAll(item.VisibleConds);
        }

        static bool glmCheckUnlock(GameLogicManager mgr, ShopItemInfo item)
        {
            return mgr.CheckCommonCondsAll(item.UnlockConds);
        }
    }
}
