using My.Player.Bag;
using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

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
            GridView.SetListItemCount(BindShop.ShopItems.Count + 1);
        }

        public void RefreshContent()
        {
            MarkDirty = true;
        }

        LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            if (BindShop == null) return null;
            // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
            // itemIndex = 行序号（row），列用 column 参数
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            if (itemIndex < BindShop.ShopItems.Count)
            {
                var shopItem = BindShop.ShopItems[itemIndex];
                item.gameObject.SetActive(true);
                
                cell.Bind(new ItemStack() { ItemID = shopItem.ItemId, Count = shopItem.BuyCount }, itemIndex, AnyContainerItemCell.EContainerType.Shop, 0, null);
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

