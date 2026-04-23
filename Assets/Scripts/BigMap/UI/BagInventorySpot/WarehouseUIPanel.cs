using cfg.demo;
using My.Config;
using My.Map;
using My.Player;
using My.Player.Bag;
using My.UI;
using SuperScrollView;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Bag
{
    // 右侧仓库：单背包、类型筛选、与主背包拖拽互通；分类槽位索引见 PlayerInventorySystem.GetWarehouseSlotIndicesForItemTypeFilter。
    public class WarehouseUIPanel : PanelBase, IInputConsumer
    {
        public static WarehouseUIPanel Instance
        {
            get
            {
                var panel = UIManager.Instance != null ? UIManager.Instance.GetShowingPanel("WarehousePanel") : null;
                if (panel != null && panel is WarehouseUIPanel w)
                {
                    return w;
                }
                return null;
            }
        }

        public LoopGridView GridView;
        [Range(1, 10)]
        public int Columns = 5;
        public string ItemPrefabName = "OneItem";

        [Tooltip("顺序：全 / 普 / 币 / 装 / 挂 / 插，与代码中类型筛选值绑定")]
        public Button[] warehouseTypeFilterButtons;

        public Transform warehousePageTabsRoot;

        public int currentWarehousePage;

        public PlayerInventorySystem BindingInventory =>
            MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;

        public class WarehousePageTabEntry
        {
            public RectTransform Root;
            public Button Btn;
            public Image SelectHint;
            public Text PageCountText;
        }

        public List<WarehousePageTabEntry> warehousePageTabs = new List<WarehousePageTabEntry>();

        private static readonly int[] WarehouseTypeFilterValues =
        {
            -1,
            (int)EItemType.Normal,
            (int)EItemType.Currency,
            (int)EItemType.Equip,
            (int)EItemType.Pocket,
            (int)EItemType.Insertion,
        };

        private bool markDirty;
        private int typeFilter = -1;

        private void Awake()
        {
            GridView.InitGridView(0, OnGetItemByIndex);
            GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);

            BindWarehouseTypeFilters();
            BindWarehousePageTabsFromRoot();
        }

        private void BindWarehouseTypeFilters()
        {
            if (warehouseTypeFilterButtons == null)
            {
                return;
            }
            for (int i = 0; i < warehouseTypeFilterButtons.Length && i < WarehouseTypeFilterValues.Length; i++)
            {
                var btn = warehouseTypeFilterButtons[i];
                if (btn == null)
                {
                    continue;
                }
                int fv = WarehouseTypeFilterValues[i];
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    typeFilter = fv;
                    GridView.RefreshAllShownItem();
                });
            }
        }

        private void BindWarehousePageTabsFromRoot()
        {
            warehousePageTabs.Clear();
            if (warehousePageTabsRoot == null)
            {
                return;
            }
            for (int i = 0; i < warehousePageTabsRoot.childCount; i++)
            {
                var childOne = warehousePageTabsRoot.GetChild(i);
                var item = new WarehousePageTabEntry
                {
                    Root = childOne.GetComponent<RectTransform>(),
                    Btn = childOne.GetComponentInChildren<Button>(),
                };
                int page = i;
                if (item.Btn != null)
                {
                    item.Btn.onClick.RemoveAllListeners();
                    item.Btn.onClick.AddListener(() => SwitchWarehousePage(page));
                }

                var selTr = childOne.Find("Select");
                item.SelectHint = selTr != null ? selTr.GetComponent<Image>() : null;
                var hintTr = childOne.Find("Hint");
                item.PageCountText = hintTr != null ? hintTr.GetComponent<Text>() : null;

                if (item.SelectHint != null)
                {
                    item.SelectHint.gameObject.SetActive(false);
                }
                if (item.PageCountText != null)
                {
                    item.PageCountText.gameObject.SetActive(true);
                }

                warehousePageTabs.Add(item);
            }
        }

        public override void Show()
        {
            base.Show();
            foreach (var it in warehousePageTabs)
            {
                if (it.SelectHint != null)
                {
                    it.SelectHint.gameObject.SetActive(false);
                }
            }
            if (warehousePageTabs.Count > 0 && currentWarehousePage >= 0 && currentWarehousePage < warehousePageTabs.Count
                && warehousePageTabs[currentWarehousePage].SelectHint != null)
            {
                warehousePageTabs[currentWarehousePage].SelectHint.gameObject.SetActive(true);
            }
            OnWarehouseDataChanged();
        }

        private void Update()
        {
            if (markDirty)
            {
                OnWarehouseDataChanged();
                markDirty = false;
            }
        }

        public void RefreshContent()
        {
            markDirty = true;
        }

        private void OnWarehouseDataChanged()
        {
            var warehouseBag = GetWarehouseBag();
            if (warehouseBag != null)
            {
                GridView.SetListItemCount(warehouseBag.BasicCapacity + warehouseBag.MaxExtraCapacity);
            }
            GridView.RefreshAllShownItem();
            RefreshWarehousePageTabHints();
        }

        private PlayerBag GetWarehouseBag()
        {
            return BindingInventory.GetBagById((int)EPlayerBagId.Storage);
        }

        private void SwitchWarehousePage(int page)
        {
            if (page < 0 || page >= WarehouseConfig.PageCount)
            {
                return;
            }
            if (currentWarehousePage == page)
            {
                return;
            }

            foreach (var item in warehousePageTabs)
            {
                if (item.SelectHint != null)
                {
                    item.SelectHint.gameObject.SetActive(false);
                }
            }

            currentWarehousePage = page;
            if (page < warehousePageTabs.Count && warehousePageTabs[page].SelectHint != null)
            {
                warehousePageTabs[page].SelectHint.gameObject.SetActive(true);
            }

            OnWarehouseDataChanged();
        }

        private void RefreshWarehousePageTabHints()
        {
            var warehousePage = GetWarehouseBag();
            if (warehousePage == null)
            {
                return;
            }
            int used = 0;
            foreach (var s in warehousePage.NormalSlots)
            {
                if (s != null && !s.IsEmpty)
                {
                    used++;
                }
            }
            var summary = $"{used}/{warehousePage.NormalSlots.Count}";
            for (int i = 0; i < warehousePageTabs.Count; i++)
            {
                if (warehousePageTabs[i].PageCountText == null)
                {
                    continue;
                }
                warehousePageTabs[i].PageCountText.text = i == 0 ? summary : "";
            }
        }

        private LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            var warehousePage = GetWarehouseBag();
            if (warehousePage == null)
            {
                return item;
            }

            if (itemIndex < warehousePage.BasicCapacity)
            {
                var stack = warehousePage.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                int bid = (int)EPlayerBagId.Storage;
                cell.Bind(stack, itemIndex, EContainerType.Warehouse, bid, null);

                bool dim = stack != null && !stack.IsEmpty && !PassesTypeFilter(stack);
                cell.icon.color = dim ? new Color(1f, 1f, 1f, 0.28f) : Color.white;
                if (cell.countText != null)
                {
                    var c = cell.countText.color;
                    c.a = dim ? 0.35f : 1f;
                    cell.countText.color = c;
                }
            }
            else
            {
                cell.ClearEmpty();
            }
            return item;
        }

        private bool PassesTypeFilter(ItemStack stack)
        {
            if (typeFilter < 0)
            {
                return true;
            }
            var def = ItemCatalog.GetItemDef(stack.ItemID);
            if (def == null)
            {
                return true;
            }
            return (int)def.ItemType == typeFilter;
        }

        public bool OnConfirm()
        {
            return false;
        }

        public bool OnCancel()
        {
            UIManager.Instance.HidePanel("WarehousePanel");
            return true;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return false;
        }

        public bool OnHotkey(string keyName)
        {
            return false;
        }

        public bool OnScroll(float deltaY)
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
