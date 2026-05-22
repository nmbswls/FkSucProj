using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg.demo;
using My.Config;
using My.Player.Bag;
using UnityEngine.UI;
using TMPro;
using static UnityEditor.Progress;
using My.Map;
using System;
using My.Player;
using static My.Input.QuickPlayerInputBinder;
using My.Input;
using My.UI;

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
        /// 特殊背包面板根节点
        /// </summary>
        public RectTransform SpeBagPanel;
        public Button CollapseSpeBagBtn;
        public LoopGridView SpeGridView;
        public EPlayerBagId CurrExpandBagId; // 当前展开的特殊背包 BagId，0 表示未展开

        public Transform SpecBagSelectionsTr;

        public Button CloseButton;

        public class InnerSpeBagItem
        {
            public RectTransform Root;
            public Button Btn;
            public Image SelectHint;
            public TextMeshProUGUI StackCount;
        }

        public List<InnerSpeBagItem> SpeBagItems = new();

        public PlayerInventorySystem BindingInventory { get { return MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem; } }


        private bool markDirty = false;
        private void Awake()
        {
            GridView.InitGridView(0, OnMainGetItemByIndex);
            SpeGridView.InitGridView(0, OnSpeGetItemByIndex);

            GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);

            if(CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    UIManager.Instance.HidePanel("PlayerBag");
                });
            }
            

            List<EPlayerBagId> enableBags = new() { EPlayerBagId.Secret, EPlayerBagId.Pet };
            for (int i = 0; i < SpecBagSelectionsTr.childCount; i++)
            {
                var childOne = SpecBagSelectionsTr.GetChild(i);

                if (i >= enableBags.Count)
                {
                    childOne.gameObject.SetActive(false);
                    continue;
                }

                var item = new InnerSpeBagItem()
                {
                    Root = childOne.GetComponent<RectTransform>()
                };


                var btn = childOne.GetComponentInChildren<Button>();
                item.Btn = btn;

                EPlayerBagId bagId = enableBags[i];

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    SwitchSpeBag(bagId);
                });

                item.SelectHint = childOne.Find("Select").GetComponent<Image>();
                item.StackCount = childOne.Find("Hint").GetComponentInChildren<TextMeshProUGUI>();

                item.SelectHint.gameObject.SetActive(false);
                item.StackCount.gameObject.SetActive(false);

                SpeBagItems.Add(item);
            }

            CollapseSpeBagBtn.onClick.RemoveAllListeners();
            CollapseSpeBagBtn.onClick.AddListener(() =>
            {
                CloseSpeBag();
            });

            //gameObject.SetActive(false);
        }

        public override void Show()
        {
            base.Show();

            InitilaizeView();
            CloseSpeBag();

            PlayerHumanItemBarPanel.ShowCompanionForBagIfNeeded();
            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        public override void Hide()
        {
            PlayerHumanItemBarPanel.HideCompanionForBagIfNeeded();

            base.Hide();
        }

        public void InitilaizeView()
        {
            var mainBag = BindingInventory.GetBagById(0);
            GridView.SetListItemCount(mainBag.BasicCapacity + Math.Max(mainBag.MaxExtraCapacity, mainBag.ExtraSlots.Count));
        }

        public void RefreshContent()
        {
            markDirty = true;
            WarehouseUIPanel.Instance?.RefreshContent();
        }


        void OnDestroy()
        {
        }

        private void OnInventoryChanged(int idx)
        {
            GridView.RefreshAllShownItem(); // 单格变更后仅刷新已创建的可见项
        }

        /// <summary>
        /// 整表容量或数据变化时重建列表并刷新
        /// </summary>
        private void OnInventoryAllChanged()
        {
            var mainBag = BindingInventory.GetBagById(0);
            GridView.SetListItemCount(mainBag.BasicCapacity + Math.Max(mainBag.MaxExtraCapacity, mainBag.ExtraSlots.Count));
            GridView.RefreshAllShownItem();

            if (CurrExpandBagId != 0)
            {
                var speBag = BindingInventory.GetBagById((int)CurrExpandBagId);
                SpeGridView.SetListItemCount(speBag.BasicCapacity + speBag.ExtraSlots.Count + 1);
                SpeGridView.RefreshAllShownItem();
            }

            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        private void Update()
        {
            if (markDirty)
            {
                OnInventoryAllChanged();
                markDirty = false;
            }
        }

        LoopGridViewItem OnMainGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            // LoopGridView 新版回调：itemIndex 为扁平序号，与 row*列数+column 一致
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            var mainBag = BindingInventory.GetBagById(0);

            if (itemIndex < mainBag.BasicCapacity)
            {
                var stack = mainBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, EContainerType.Inventory, 0, null);
            }
            else if (itemIndex < mainBag.BasicCapacity + mainBag.ExtraSlots.Count)
            {
                var stack = mainBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, EContainerType.Inventory, 0, null, ItemCellBase.EStyleType.Red);
            }
            else
            {
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

            if (!ItemCatalog.CanUse(stack.ItemID))
            {
                return;
            }

            var useRow = ItemCatalog.GetPrimaryUse(stack.ItemID);
            if (useRow == null || !useRow.Usable)
            {
                return;
            }

            stack.RemoveFromStack(1);
            if (stack.Count <= 0) 
            {
                bag.ClearEmptyItems();
            }

            MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.ItemUseCd.TryGetValue(stack.ItemID, out var lastUseTime);
            if(lastUseTime != 0 && useRow.UseCd > 0 && LogicTime.time - lastUseTime < useRow.UseCd)
            {
                Debug.LogError($"use item fail cd {lastUseTime}");
                return;
            }

            if (useRow.UseType == EItemUseType.UseSkill)
            {
                //
                UIManager.Instance.HidePanel("PlayerBag");

                var skillName = useRow.S1;
                if (string.IsNullOrEmpty(skillName)) 
                {
                    Debug.LogError($"UseItem skill name invalid");
                    return;
                }
                OverworldHUDPanel.Instance.OnClickUseSkill(skillName, (ret) =>
                {
                    if(useRow.CostOnUse)
                    {
                        bag.TryCostItem(stack.ItemID, 1);
                    }
                });
                //MainGameManager.Instance.gameLogicManager.playerDataManager
            }
            else
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.abilityController.TryUseAbility("use_item", overrideParams: new Dictionary<string, string>()
                {
                    ["PhaseExecutingTime"] = useRow.UseTime.ToString(),
                    ["ItemId"] = stack.ItemID,
                }); ;
            }
            
            //UIBus.RaiseInventoryChanged(index);
            OnInventoryAllChanged();
        }

        public void SplitItem(int bagId, int index, long count)
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

        public void DropItemToGround(int bagId, int index, long count)
        {
            BindingInventory.DropItemToGround(bagId, index, count);
            OnInventoryAllChanged();
        }

        void CloseSpeBag()
        {
            SpeBagPanel.gameObject.SetActive(false);

            SpeGridView.SetListItemCount(0);
            SpeGridView.RefreshAllShownItem();
            this.CurrExpandBagId = 0;

            foreach (var item in SpeBagItems)
            {
                item.SelectHint.gameObject.SetActive(false);
            }
        }

        void SwitchSpeBag(EPlayerBagId badId)
        {
            var oldId = this.CurrExpandBagId;
            if (badId == oldId)
            {
                return;
            }
            foreach (var item in SpeBagItems)
            {
                item.SelectHint.gameObject.SetActive(false);
            }
            this.CurrExpandBagId = badId;
            if(oldId == 0)
            {
                
            }

            var bag = BindingInventory.GetBagById((int)CurrExpandBagId);
            SpeBagPanel.gameObject.SetActive(true);

            SpeGridView.SetListItemCount(bag.NormalSlots.Count + bag.ExtraSlots.Count + 1);
            SpeGridView.RefreshAllShownItem();

            List<EPlayerBagId> enableBags = new() { EPlayerBagId.Secret, EPlayerBagId.Pet };
            var findIdx = enableBags.IndexOf(badId);
            SpeBagItems[findIdx].SelectHint.gameObject.SetActive(true);
        }

        LoopGridViewItem OnSpeGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            if(CurrExpandBagId == 0)
            {
                return null;
            }

            // 与主背包相同：itemIndex 为扁平槽位序号
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();


            //int slotIndex = row * Columns + column;
            var specBag = BindingInventory.GetBagById((int)CurrExpandBagId);
            var speCt = specBag.StackContainerType;
            if (itemIndex < specBag.BasicCapacity)
            {
                var stack = specBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, speCt, (int)CurrExpandBagId, null);
            }
            // 扩展栏动态槽位
            else if(itemIndex < specBag.BasicCapacity + specBag.ExtraSlots.Count)
            {
                var stack = specBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, speCt, (int)CurrExpandBagId, null, ItemCellBase.EStyleType.Red);
            }
            else if(itemIndex == specBag.BasicCapacity + specBag.ExtraSlots.Count)
            {
                item.gameObject.SetActive(true);
                cell.Bind(null, itemIndex, speCt, (int)CurrExpandBagId, null, ItemCellBase.EStyleType.AddIcon);
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

        public bool OnHotkey(string keyName)
        {
            if(keyName == EInputKey.Bag.ToString())
            {
                UIManager.Instance.HidePanel("PlayerBag");
                return true;
            }
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

        public bool OnHoldStart(string holdKey)
        {
            return false;
        }

        public bool OnHoldUpdate(string holdKey)
        {
            return false;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            return false;
        }
    }

}


