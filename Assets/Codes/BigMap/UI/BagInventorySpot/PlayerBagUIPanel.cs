using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using My.Player.Bag;
using UnityEngine.UI;
using Config;

namespace My.UI.Bag
{
    public class PlayerBagUIPanel : PanelBase, IInputConsumer
    {
        public static PlayerBagUIPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("PlayerBag");
                if (panel != null && panel is PlayerBagUIPanel playerBag)
                {
                    return playerBag;
                }
                return null;
            }
        }

        public LoopGridView GridView;
        [Range(1, 10)]
        public int Columns = 5;
        public string ItemPrefabName = "ItemCellPrefab";

        /// <summary>
        /// 隐藏背包
        /// </summary>
        public RectTransform SpeBagPanel;
        public LoopGridView SpeGridView;
        public int CurrExpandSpeBag; // 当前展开的背包
        public List<Button> SpeBagBtns = new();



        public PlayerInventoryModel BindingInventory { get { return MainGameManager.Instance.gameLogicManager.playerDataManager.inventoryModel; } }


        private bool markDirty = false;
        private void Awake()
        {
            GridView.InitGridView(0, OnMainGetItemByIndex);
            SpeGridView.InitGridView(0, OnSpeGetItemByIndex);

            GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);

            var speBtnTrans = transform.Find("SpeBagBtns");
            for (int i = 0; i < speBtnTrans.childCount; i++)
            {
                var childOne = speBtnTrans.GetChild(i);
                var btn = childOne.GetComponent<Button>();
                SpeBagBtns.Add(btn);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    SwitchSpeBag(i);
                });
            }



            //gameObject.SetActive(false);
        }

        private void Update()
        {
            if (markDirty)
            {
                GridView.RefreshAllShownItem();
                markDirty = false;
            }
        }

        public void InitilaizeView()
        {
            var mainBag = BindingInventory.GetBagById(0);
            GridView.SetListItemCount(mainBag.BasicCapacity + mainBag.MaxExtraCapacity);
        }

        public void RefreshContent()
        {
            markDirty = true;
        }


        void OnDestroy()
        {
        }

        private void OnInventoryChanged(int idx)
        {
            GridView.RefreshAllShownItem(); // 简化，实际可局部刷新
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private void OnInventoryAllChanged()
        {
            var mainBag = BindingInventory.GetBagById(0);
            GridView.SetListItemCount(mainBag.BasicCapacity + mainBag.MaxExtraCapacity);
            GridView.RefreshAllShownItem();

            if (CurrExpandSpeBag != -1)
            {
                var speBag = BindingInventory.GetBagById(CurrExpandSpeBag);
                SpeGridView.SetListItemCount(speBag.BasicCapacity + speBag.MaxExtraCapacity);
                SpeGridView.RefreshAllShownItem();
            }
        }

        LoopGridViewItem OnMainGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
            // itemIndex = 行序号（row），列用 column 参数
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            var mainBag = BindingInventory.GetBagById(0);

            if (itemIndex < mainBag.BasicCapacity)
            {
                var stack = mainBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, AnyContainerItemCell.EContainerType.Inventory, 0, null);
            }
            else
            {
                //item.gameObject.SetActive(false);
                cell.ClearEmpty();
            }
            return item;
        }

        public void UseItem(int bagId, int index)
        {
            var bag = BindingInventory.GetBagById(bagId);
            if(bag == null)
            {
                Debug.LogError($"use item fail bag not found {bagId}");
                return;
            }

            var stack = bag.GetItemByIdx(index);
            if (stack == null || stack.IsEmpty) return;

            if (!FakeItemDatabase.CanUse(stack.ItemID)) return;

            stack.RemoveFromStack(1);
            if (stack.Count <= 0) 
            {
                bag.ClearEmptyItems();
            }

            MainGameManager.Instance.gameLogicManager.playerLogicEntity.PlayerAbilityController.TryUseItem(stack.ItemID);

            //UIBus.RaiseInventoryChanged(index);
            OnInventoryAllChanged();
        }

        public void SplitItem(int bagId, int index, int count)
        {
            var bag = BindingInventory.GetBagById(bagId);
            if (bag == null)
            {
                Debug.LogError($"SplitItem item fail bag not found {bagId}");
                return;
            }

            if (bag.TrySplit(index, count))
            {
                //UIBus.RaiseInventoryAllChanged();
            }

            OnInventoryAllChanged();
        }

        public void DropItemToGround(int bagId, int index, int count)
        {
            var bag = BindingInventory.GetBagById(bagId);
            if (bag == null)
            {
                Debug.LogError($"DropItemToGround item fail bag not found {bagId}");
                return;
            }

            var item = bag.GetItemByIdx(index);
            int dropCount = bag.RemoveAt(index, count);
            // 这里可生成场景掉落物，本示例仅移除
            //UIBus.RaiseInventoryAllChanged();
            if (dropCount > 0)
            {
                Vector2 centerPos = MainGameManager.Instance.playerScenePresenter.GetWorldPosition();
                MainGameManager.Instance.gameLogicManager.globalDropCollection.CreateDrop(item.ItemID, count, centerPos + UnityEngine.Random.insideUnitCircle * 0.3f, false, centerPos);
            }            

            OnInventoryAllChanged();
        }


        void SwitchSpeBag(int idx)
        {
            int oldIdx = this.CurrExpandSpeBag;
            if (this.CurrExpandSpeBag == idx)
            {
                return;
            }

            this.CurrExpandSpeBag = idx;
            if(oldIdx == -1)
            {
                
            }

            var bag = BindingInventory.GetBagById(CurrExpandSpeBag);
            SpeBagPanel.gameObject.SetActive(true);

            SpeGridView.SetListItemCount(bag.NormalSlots.Count + bag.ExtraSlots.Count + 1);
            SpeGridView.RefreshAllShownItem();
        }

        LoopGridViewItem OnSpeGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            if(CurrExpandSpeBag == -1)
            {
                return null;
            }

            // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
            // itemIndex = 行序号（row），列用 column 参数
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            //int slotIndex = row * Columns + column;
            var specBag = BindingInventory.GetBagById(CurrExpandSpeBag);
            if (itemIndex < specBag.BasicCapacity)
            {
                var stack = specBag.NormalSlots[itemIndex];
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, AnyContainerItemCell.EContainerType.SpecialInventory, CurrExpandSpeBag, null);
            }
            // 特殊
            else if(itemIndex < specBag.BasicCapacity + specBag.ExtraSlots.Count)
            {
                var stack = specBag.NormalSlots[itemIndex];
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, AnyContainerItemCell.EContainerType.SpecialInventory, CurrExpandSpeBag, null, AnyContainerItemCell.EStyleType.Red);
            }
            else if(itemIndex == specBag.BasicCapacity + specBag.ExtraSlots.Count)
            {
                item.gameObject.SetActive(true);
                cell.Bind(null, itemIndex, AnyContainerItemCell.EContainerType.SpecialInventory, CurrExpandSpeBag, null, AnyContainerItemCell.EStyleType.AddIcon);
            }
            else
            {
                item.gameObject.SetActive(false);
                //cell.ClearEmpty();
            }
            return item;
        }

        public bool OnConfirm()
        {
            return false;
        }

        public bool OnCancel()
        {
            UIManager.Instance.HidePanel("PlayerBag");
            return true;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return false;
        }

        public bool OnHotkey(int index)
        {
            return false;
        }

        public bool OnScroll(float deltaY)
        {
            return false;
        }

        public bool OnSpace()
        {
            return false;
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            return false;
        }
    }

}


