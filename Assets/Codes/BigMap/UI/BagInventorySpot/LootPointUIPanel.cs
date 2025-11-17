using My.Player.Bag;
using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{

    public class LootPointUIPanel : PanelBase
    {

        public static LootPointUIPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("LootPoint");
                if (panel != null && panel is LootPointUIPanel playerBag)
                {
                    return playerBag;
                }
                return null;
            }
        }

        public ILootableObj Loot;

        public LoopGridView GridView;

        public Button QuitBtn;

        [Range(1, 10)]
        public int Columns = 5;
        public string ItemPrefabName = "ItemCellPrefab";

        void Awake()
        {
            QuitBtn.onClick.AddListener(() => { 
                //UIOrchestrator.Instance.TryQuitLootDetailMode(); 
            });
            GridView.InitGridView(0, OnGetItemByIndex);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            ILootableObj bindingObj = (ILootableObj)data;
            this.Loot = bindingObj;
            GridView.SetListItemCount(bindingObj.LootItems.Count + 1);
        }

        public void RefreshContent()
        {
            GridView.RefreshAllShownItem();
        }

        LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
            // itemIndex = 行序号（row），列用 column 参数
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            int slotIndex = row * Columns + column;
            if (slotIndex < Loot.LootItems.Count)
            {
                var stack = Loot.LootItems[slotIndex];
                item.gameObject.SetActive(true);
                cell.Bind(stack, slotIndex, AnyContainerItemCell.EContainerType.LootPoint, null);
            }
            else
            {
                cell.ClearEmpty(slotIndex, AnyContainerItemCell.EContainerType.LootPoint);
                //item.gameObject.SetActive(false);
            }

            return item;
        }
    }

}

