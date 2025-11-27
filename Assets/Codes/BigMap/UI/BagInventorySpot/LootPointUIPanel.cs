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
        public RectTransform UnrealvingHintObj;

        [Range(1, 10)]
        public int Columns = 5;
        public string ItemPrefabName = "ItemCellPrefab";

        private bool markDirty;

        void Awake()
        {
            QuitBtn.onClick.AddListener(() => { 
                UIOrchestrator.Instance.TryQuitLootDetailMode(); 
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
            markDirty = true;
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
                UnrealvingHintObj.gameObject.SetActive(false);
            }
            else
            {
                UnrealvingHintObj.gameObject.SetActive(true);

                var item = GridView.GetShownItemByItemIndex(unrealIdx); // 或 FindItemByItemIndex(index)
                if (item != null)
                {
                    // 该项的 RectTransform
                    var itemRT = item.transform as RectTransform;
                    var worldCenter = itemRT.TransformPoint(itemRT.rect.center);

                    // 4) 世界点 -> 屏幕点 -> ui 父节点的局部坐标
                    Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(UIManager.Instance.UICamera, worldCenter);
                    Canvas canvas = UnrealvingHintObj.GetComponentInParent<Canvas>();
                    Vector2 localInParent;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screenPt, canvas.worldCamera, out localInParent);

                    // 5) 设置到 UI（确保锚点合理）
                    UnrealvingHintObj.anchoredPosition = localInParent;

                    //UnrealvingHintObj.transform.position = item.transform.position + new Vector3(0.5f, -0.5f);
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
            var dt = LogicTime.deltaTime;

            if(Loot != null)
            {
                Loot.TickUnReveal(dt);
            }

            if(markDirty)
            {
                GridView.RefreshAllShownItem();

                RefreshUnrealingHint();
                markDirty = false;
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

