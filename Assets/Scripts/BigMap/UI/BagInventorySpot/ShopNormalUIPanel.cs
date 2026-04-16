using My;
using My.Player.Bag;
using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{

    public class ShopNormalUIPanel : PanelBase
    {

        public static ShopNormalUIPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("ShopNormal");
                if (panel != null && panel is ShopNormalUIPanel playerBag)
                {
                    return playerBag;
                }
                return null;
            }
        }


        public IShopProvider BindShop;

        private List<int> visibleItemIndices = new();

        public LoopGridView GridView;

        public Button QuitBtn;

        [Range(1, 10)]
        public int Columns = 5;
        public string ItemPrefabName = "ItemCellPrefab";

        void Awake()
        {
            QuitBtn.onClick.AddListener(() => { 
                UIOrchestrator.Instance.TryQuitLootDetailMode(); 
            });
            GridView.InitGridView(0, OnGetItemByIndex);
        }

        private bool MarkDirty = false;

        void Update()
        {
            if(MarkDirty)
            {
                MarkDirty = false;
                GridView.RefreshAllShownItem();
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            IShopProvider shop = (IShopProvider)data;
            this.BindShop = shop;
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            visibleItemIndices = shop.GetVisibleShopItemIndices(glm);
            GridView.SetListItemCount(visibleItemIndices.Count + 1);
        }

        public void RefreshContent()
        {
            MarkDirty = true;
        }

        LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            if (BindShop == null) return null;
            // LoopGridView：itemIndex 为扁平序号，与行列换算一致
            // itemIndex = row * 列数 + column


            var item = grid.NewListViewItem(ItemPrefabName);
            var shopCell = item.GetComponent<ShopContainerWrapper>();

            if (itemIndex < visibleItemIndices.Count)
            {
                int realIdx = visibleItemIndices[itemIndex];
                var shopItem = BindShop.ShopItems[realIdx];
                item.gameObject.SetActive(true);

                var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
                var st = ShopGoodsDisplay.GetDisplayState(glm, shopItem);
                var style = st == EShopGoodsDisplayState.Locked
                    ? AnyContainerItemCell.EStyleType.Locked
                    : AnyContainerItemCell.EStyleType.Normal;

                shopCell.Bind(shopItem.LeftCount, new ItemStack(shopItem.ItemId, shopItem.BuyCount), realIdx, EContainerType.Shop, 0, null, style);
            }
            else
            {
                //cell.ClearEmpty(slotIndex, AnyContainerItemCell.EContainerType.LootPoint);
                item.gameObject.SetActive(false);
            }

            return item;
        }
    }

}

