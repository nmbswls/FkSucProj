using My.Map;
using My.Player.Bag;
using SuperScrollView;
using System;
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
        public GameObject UnrealvingHintObj;

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
            GridView.SetListItemCount(bindingObj.LootItems.Count);

            Loot.EnOnUnrealed += RemoveItemMask;

            RefreshUnrealingHint();
        }

        public void RefreshContent()
        {
            GridView.RefreshAllShownItem();

            RefreshUnrealingHint();
        }

        public void RemoveItemMask(int itemIndex)
        {
            GridView.RefreshItemByItemIndex(itemIndex);

            RefreshUnrealingHint();
        }

        private void RefreshUnrealingHint()
        {
            int unrealIdx = this.Loot.GetCurrUnrealed();
            if (unrealIdx == -1)
            {
                UnrealvingHintObj.SetActive(false);
            }
            else
            {
                UnrealvingHintObj.SetActive(true);

                var item = GridView.GetShownItemByItemIndex(unrealIdx); // 或 FindItemByItemIndex(index)
                if (item != null)
                {
                    // 该项的 RectTransform
                    UnrealvingHintObj.transform.position = item.transform.position;
                    return;
                }
            }
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

                if(Loot.IsRevealed(itemIndex))
                {
                    cell.Bind(stack, slotIndex, AnyContainerItemCell.EContainerType.LootPoint, 0, null);
                }
                else
                {
                    cell.Bind(stack, slotIndex, AnyContainerItemCell.EContainerType.LootPoint, 0, null, AnyContainerItemCell.EStyleType.Masked);
                }
            }
            else
            {
                //cell.ClearEmpty();
                item.gameObject.SetActive(false);
            }

            return item;
        }

        public void Update()
        {
            var dt = LogicTime.time;

            if(Loot != null)
            {
                Loot.TickUnReveal(dt);
            }
        }

        public override void Hide()
        {
            base.Hide();

            if(Loot != null)
            {
                Loot.EnOnUnrealed -= RemoveItemMask;
                Loot = null;
            }
        }
    }

}

