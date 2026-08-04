using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg.demo;
using My.Config;
using My.Player.Bag;
using UnityEngine.UI;
using TMPro;
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

        // 主背包页签（与普通背包平级，切换后复用同一格子区域）
        public Transform MainBagTabsRoot;
        public EPlayerBagId CurrMainBagId = EPlayerBagId.Default;

        /// <summary>
        /// 特殊背包面板根节点
        /// </summary>
        public RectTransform SpeBagPanel;
        public Button CollapseSpeBagBtn;
        public LoopGridView SpeGridView;
        public EPlayerBagId CurrExpandBagId; // 当前展开的特殊背包 BagId，0 表示未展开

        public Transform SpecBagSelectionsTr;

        public Button CloseButton;
        public Button JingYuanButton;

        // 大件背包：常驻悬浮于主背包右下角
        public GameObject BigBagDockRoot;
        public LoopGridView BigBagGridView;
        public TextMeshProUGUI BigBagTitleText;
        public EPlayerBagId BigBagId = EPlayerBagId.Big;
        [Range(1, 2)]
        public int BigBagColumns = 2;
        public string BigBagItemPrefabName = "OneItem";
        [Min(0f)]
        public float BigBagDockSingleRowHeight = 118f;
        [Min(0f)]
        public float BigBagDockAdditionalRowHeight = 88f;

        public class InnerMainBagTabItem
        {
            public RectTransform Root;
            public Button Btn;
            public Image SelectHint;
            public TextMeshProUGUI Label;
            public EPlayerBagId BagId;
        }

        public class InnerSpeBagItem
        {
            public RectTransform Root;
            public Button Btn;
            public Image SelectHint;
            public TextMeshProUGUI StackCount;
            public EPlayerBagId BagId;
        }

        static readonly EPlayerBagId[] MainBagTabOrder =
        {
            EPlayerBagId.Default,
            EPlayerBagId.Mind,
            EPlayerBagId.Important,
        };

        static readonly string[] MainBagTabLabels =
        {
            "背包",
            "精神",
            "珍贵",
        };

        static readonly EPlayerBagId[] SpeBagOrder =
        {
            EPlayerBagId.Secret,
            EPlayerBagId.Pet,
            EPlayerBagId.Plant,
            EPlayerBagId.Key,
            EPlayerBagId.Potion,
        };

        public List<InnerMainBagTabItem> MainBagTabs = new();
        public List<InnerSpeBagItem> SpeBagItems = new();

        public PlayerInventorySystem BindingInventory { get { return MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem; } }


        private bool markDirty = false;
        private PlayerBag boundBigBag;
        private void Awake()
        {
            GridView.InitGridView(0, OnMainGetItemByIndex);
            SpeGridView.InitGridView(0, OnSpeGetItemByIndex);
            InitBigBagView();

            GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);
            BindBigBagUpdateListener();

            BindMainBagTabs();

            if(CloseButton != null)
            {
                CloseButton.onClick.AddListener(() =>
                {
                    UIManager.Instance.HidePanel("PlayerBag");
                });
            }

            JingYuanButton?.onClick.AddListener(() => UIManager.Instance.ShowPanel(JingYuanCarriedPanel.PanelIdConst));
            

            if (SpecBagSelectionsTr == null)
            {
                return;
            }

            for (int i = 0; i < SpecBagSelectionsTr.childCount; i++)
            {
                var childOne = SpecBagSelectionsTr.GetChild(i);

                if (i >= SpeBagOrder.Length)
                {
                    childOne.gameObject.SetActive(false);
                    continue;
                }

                EPlayerBagId bagId = SpeBagOrder[i];
                if (BindingInventory.GetBagById((int)bagId) == null)
                {
                    childOne.gameObject.SetActive(false);
                    continue;
                }

                childOne.gameObject.SetActive(true);
                var item = new InnerSpeBagItem()
                {
                    Root = childOne.GetComponent<RectTransform>(),
                    BagId = bagId,
                };


                var btn = childOne.GetComponentInChildren<Button>();
                item.Btn = btn;

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

            if (CollapseSpeBagBtn != null)
            {
                CollapseSpeBagBtn.onClick.RemoveAllListeners();
                CollapseSpeBagBtn.onClick.AddListener(CloseSpeBag);
            }

            //gameObject.SetActive(false);
        }

        void InitBigBagView()
        {
            if (BigBagGridView != null)
            {
                BigBagGridView.InitGridView(0, OnBigGetItemByIndex);
                BigBagGridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, BigBagColumns);
                BigBagGridView.ArrangeType = GridItemArrangeType.BottomLeftToTopRight;
            }

            if (BigBagTitleText != null)
            {
                BigBagTitleText.text = "\u5927\u4ef6";
            }
        }

        void RefreshBigBag()
        {
            if (BigBagGridView == null)
            {
                return;
            }

            var bag = BindingInventory.GetBagById((int)BigBagId);
            bool visible = bag != null && bag.BasicCapacity > 0;
            if (BigBagDockRoot != null)
            {
                BigBagDockRoot.SetActive(visible);
            }

            if (!visible)
            {
                ResizeBigBagDock(0);
                BigBagGridView.SetListItemCount(0);
                return;
            }

            ResizeBigBagDock(bag.BasicCapacity);
            BigBagGridView.SetListItemCount(bag.BasicCapacity);
            BigBagGridView.RefreshAllShownItem();
        }

        void ResizeBigBagDock(int capacity)
        {
            if (BigBagDockRoot == null)
            {
                return;
            }

            var dockRect = BigBagDockRoot.GetComponent<RectTransform>();
            if (dockRect == null)
            {
                return;
            }

            int rows = capacity <= 0 ? 1 : Mathf.CeilToInt((float)capacity / Mathf.Max(1, BigBagColumns));
            dockRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                BigBagDockSingleRowHeight + (rows - 1) * BigBagDockAdditionalRowHeight);
        }

        void BindBigBagUpdateListener()
        {
            var currentBag = BindingInventory?.GetBagById((int)BigBagId);
            if (ReferenceEquals(boundBigBag, currentBag))
            {
                return;
            }

            if (boundBigBag != null)
            {
                boundBigBag.EvOnBagUpdate -= OnBigBagUpdated;
            }

            boundBigBag = currentBag;
            if (boundBigBag != null)
            {
                boundBigBag.EvOnBagUpdate += OnBigBagUpdated;
            }
        }

        void OnBigBagUpdated()
        {
            markDirty = true;
        }

        void BindMainBagTabs()
        {
            MainBagTabs.Clear();
            if (MainBagTabsRoot == null)
            {
                var mainBag = transform.Find("MainBag/MainBagTabs");
                if (mainBag != null)
                {
                    MainBagTabsRoot = mainBag;
                }
            }

            if (MainBagTabsRoot == null)
            {
                return;
            }

            for (int i = 0; i < MainBagTabsRoot.childCount && i < MainBagTabOrder.Length; i++)
            {
                var childOne = MainBagTabsRoot.GetChild(i);
                var bagId = MainBagTabOrder[i];
                var item = new InnerMainBagTabItem
                {
                    Root = childOne.GetComponent<RectTransform>(),
                    BagId = bagId,
                };

                var btn = childOne.GetComponentInChildren<Button>();
                item.Btn = btn;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SwitchMainBagTab(bagId));
                }

                var selectTr = childOne.Find("Select");
                item.SelectHint = selectTr != null ? selectTr.GetComponent<Image>() : null;
                if (item.SelectHint != null)
                {
                    item.SelectHint.gameObject.SetActive(false);
                }

                item.Label = childOne.GetComponentInChildren<TextMeshProUGUI>();
                if (item.Label != null && i < MainBagTabLabels.Length)
                {
                    item.Label.text = MainBagTabLabels[i];
                }

                MainBagTabs.Add(item);
            }
        }

        public override void Show()
        {
            base.Show();

            SwitchMainBagTab(EPlayerBagId.Default, force: true);
            CloseSpeBag();
            BindBigBagUpdateListener();
            RefreshBigBag();

            PlayerHumanItemBarPanel.ShowCompanionForBagIfNeeded();
            PlayerHumanItemBarPanel.RefreshFromGame();
        }

        public override void Hide()
        {
            HumanArmarCatalog.EndAppraisalSession();
            PlayerHumanItemBarPanel.HideCompanionForBagIfNeeded();
            PlayerHumanItemBarPanel.RefreshFromGame();

            base.Hide();
        }

        public void InitilaizeView()
        {
            GridView.SetListItemCount(GetMainBagGridItemCount(CurrMainBagId));
        }

        int GetMainBagGridItemCount(EPlayerBagId bagId)
        {
            var bag = BindingInventory.GetBagById((int)bagId);
            if (bag == null)
            {
                return 0;
            }

            return bag.BasicCapacity + Math.Max(bag.MaxExtraCapacity, bag.ExtraSlots.Count);
        }

        EContainerType ResolveContainerTypeForBag(EPlayerBagId bagId)
        {
            if (bagId == EPlayerBagId.Default)
            {
                return EContainerType.Inventory;
            }

            return EContainerType.SpecialInventory;
        }

        void SwitchMainBagTab(EPlayerBagId bagId, bool force = false)
        {
            if (!force && bagId == CurrMainBagId)
            {
                return;
            }

            CurrMainBagId = bagId;
            foreach (var tab in MainBagTabs)
            {
                if (tab.SelectHint != null)
                {
                    tab.SelectHint.gameObject.SetActive(tab.BagId == bagId);
                }
            }

            InitilaizeView();
            GridView.RefreshAllShownItem();
        }

        public void RefreshContent()
        {
            markDirty = true;
            WarehouseUIPanel.Instance?.RefreshContent();
        }


        void OnDestroy()
        {
            if (boundBigBag != null)
            {
                boundBigBag.EvOnBagUpdate -= OnBigBagUpdated;
                boundBigBag = null;
            }
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
            GridView.SetListItemCount(GetMainBagGridItemCount(CurrMainBagId));
            GridView.RefreshAllShownItem();

            if (CurrExpandBagId != 0)
            {
                var speBag = BindingInventory.GetBagById((int)CurrExpandBagId);
                if (speBag != null)
                {
                    SpeGridView.SetListItemCount(speBag.BasicCapacity + speBag.ExtraSlots.Count + 1);
                    SpeGridView.RefreshAllShownItem();
                }
                else
                {
                    CurrExpandBagId = 0;
                    SpeGridView.SetListItemCount(0);
                }
            }

            RefreshBigBag();

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

            var mainBag = BindingInventory.GetBagById((int)CurrMainBagId);
            if (mainBag == null)
            {
                cell.ClearEmpty();
                return item;
            }

            var containerType = ResolveContainerTypeForBag(CurrMainBagId);
            int bagId = (int)CurrMainBagId;

            if (itemIndex < mainBag.BasicCapacity)
            {
                var stack = mainBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, containerType, bagId, null);
            }
            else if (itemIndex < mainBag.BasicCapacity + mainBag.ExtraSlots.Count)
            {
                var stack = mainBag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                cell.Bind(stack, itemIndex, containerType, bagId, null, ItemCellBase.EStyleType.Red);
            }
            else
            {
                cell.ClearEmpty();
            }
            return item;
        }

        LoopGridViewItem OnBigGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            var item = grid.NewListViewItem(BigBagItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();
            var bigBag = BindingInventory.GetBagById((int)BigBagId);
            if (bigBag == null || itemIndex >= bigBag.BasicCapacity)
            {
                cell.ClearEmpty();
                return item;
            }

            var stack = bigBag.GetItemByIdx(itemIndex);
            item.gameObject.SetActive(true);
            cell.Bind(stack, itemIndex, EContainerType.SpecialInventory, (int)BigBagId, null);
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
            if (useRow == null)
            {
                return;
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
                    if(ret && ItemCatalog.ShouldConsumeOnUse(useRow))
                    {
                        BindingInventory.TryConsumeItemUse(bag, index, useRow);
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
                    ["ItemSrcIdx"] = index.ToString(),
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
            if (bag == null)
            {
                CloseSpeBag();
                return;
            }
            SpeBagPanel.gameObject.SetActive(true);

            SpeGridView.SetListItemCount(bag.NormalSlots.Count + bag.ExtraSlots.Count + 1);
            SpeGridView.RefreshAllShownItem();

            var findIdx = SpeBagItems.FindIndex(x => x.BagId == badId);
            if (findIdx >= 0 && SpeBagItems[findIdx].SelectHint != null)
            {
                SpeBagItems[findIdx].SelectHint.gameObject.SetActive(true);
            }
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
            if (specBag == null)
            {
                cell.ClearEmpty();
                return item;
            }
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
