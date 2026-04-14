
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Player.Bag;
using UnityEngine;

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

        public List<int> GetVisibleShopItemIndices(GameLogicManager mgr)
        {
            var r = new List<int>();
            if (shopItems.Count == 0)
            {
                return r;
            }

            if (mgr == null)
            {
                for (int i = 0; i < shopItems.Count; i++)
                {
                    r.Add(i);
                }

                return r;
            }

            for (int i = 0; i < shopItems.Count; i++)
            {
                if (ShopGoodsDisplay.GetDisplayState(mgr, shopItems[i]) != EShopGoodsDisplayState.Hidden)
                {
                    r.Add(i);
                }
            }

            return r;
        }

        public bool TryBuyFromShop(int itemIdx, int count, int? dstBagIdx)
        {
            if (itemIdx < 0 || itemIdx >= shopItems.Count)
            {
                Debug.LogError($"TryBuyFromShop {itemIdx} {count}");
                return false;
            }

            var shopItem = shopItems[itemIdx];

            if (gameLogicManager != null)
            {
                if (!gameLogicManager.CheckCommonCondsAll(shopItem.VisibleConds))
                {
                    Debug.LogError("TryBuyFromShop item not visible by cond");
                    return false;
                }

                if (!gameLogicManager.CheckCommonCondsAll(shopItem.UnlockConds))
                {
                    Debug.LogError("TryBuyFromShop item locked by cond");
                    return false;
                }
            }

            var costItem = shopItem.CostItemId;
            if (!gameLogicManager.playerDataManager.CheckHaveItem(costItem, shopItem.CostCount))
            {
                Debug.LogError($"TryBuyFromShop cost not enough {costItem} {shopItem.CostCount}");
                return false;
            }

            string giveItem = shopItem.ItemId;
            long giveCount = shopItem.BuyCount;

            if (!gameLogicManager.playerDataManager.CanGainItems(giveItem, giveCount))
            {
                return false;
            }

            gameLogicManager.playerDataManager.CostItem(shopItem.CostItemId, shopItem.CostCount);

            long addCnt = gameLogicManager.playerDataManager.inventoryModel.MainBag.TryGiveItem(giveItem, giveCount);
            Debug.Log("TryBuyFromShop try buy " + giveItem + " " + addCnt);

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

            bag.RemoveAt(itemIdx, item.Count);

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

        // 配置：按 NPC 配置 id 查找商店 id（多条时取第一条）
        public bool TryGetShopIdByNpcId(string npcId, out int shopId)
        {
            shopId = 0;
            if (string.IsNullOrEmpty(npcId) || CfgMgr.Cfgs == null)
            {
                return false;
            }

            foreach (var s in CfgMgr.Cfgs.TbShop.DataList)
            {
                if (s.BindNpcId == npcId)
                {
                    shopId = s.Id;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetShopDefByNpcId(string npcId, out Shop shopDef)
        {
            shopDef = null;
            if (!TryGetShopIdByNpcId(npcId, out var sid))
            {
                return false;
            }

            shopDef = CfgMgr.Cfgs.TbShop.GetOrDefault(sid);
            return shopDef != null;
        }

        public IShopProvider GetShopProviderByNpcId(string npcId)
        {
            if (!TryGetShopIdByNpcId(npcId, out var sid))
            {
                return null;
            }

            return GetShop(sid);
        }

        public void TryRefreshOnDayChange()
        {
            foreach (var shop in Shops)
            {

            }
        }

        public void RefreshOnNightStart()
        {
            Shops.Clear();
            if (CfgMgr.Cfgs == null)
            {
                Debug.LogError("RefreshOnNightStart CfgMgr.Cfgs is null");
                return;
            }

            var goodsByShop = CfgMgr.Cfgs.TbShopGoods.DataList
                .GroupBy(g => g.ShopId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Sort).ToList());

            foreach (var shopCfg in CfgMgr.Cfgs.TbShop.DataList.OrderBy(s => s.Id))
            {
                var shop = new NormalShop(logicManager);
                shop.ShopId = shopCfg.Id;

                if (goodsByShop.TryGetValue(shopCfg.Id, out var lines))
                {
                    foreach (var g in lines)
                    {
                        var shopItem = new ShopItemInfo
                        {
                            ItemId = g.ItemId,
                            BuyCount = g.BuyCount,
                            CostItemId = g.CostItemId,
                            CostCount = g.CostCount,
                            LeftCount = -1,
                        };
                        if (g.VisibleConds != null)
                        {
                            shopItem.VisibleConds.AddRange(g.VisibleConds);
                        }

                        if (g.UnlockConds != null)
                        {
                            shopItem.UnlockConds.AddRange(g.UnlockConds);
                        }

                        shop.ShopItems.Add(shopItem);
                    }
                }

                Shops[shopCfg.Id] = shop;
            }
        }
    }

}
