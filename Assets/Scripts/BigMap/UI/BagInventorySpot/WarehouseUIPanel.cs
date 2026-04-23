using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.UI;
using SuperScrollView;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Bag
{
    // 右侧仓库：单背包、类型筛选、与主背包拖拽互通；页数随仓库容量与筛选结果变化。
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

        [Tooltip("每页行数，与 Columns 相乘为每页固定格数")]
        [Range(1, 30)]
        public int rowsPerPage = 9;

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

        readonly List<int> _visibleBagIndices = new List<int>();
        int _cachedPageCount = 1;
        int _cachedItemsPerPage = 1;

        int ItemsPerPage => Mathf.Max(1, Columns * rowsPerPage);

        private void Awake()
        {
            _cachedItemsPerPage = ItemsPerPage;
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
                    currentWarehousePage = 0;
                    OnWarehouseDataChanged();
                });
            }
        }

        WarehousePageTabEntry BuildTabEntry(Transform childOne, int pageIndex)
        {
            var item = new WarehousePageTabEntry
            {
                Root = childOne.GetComponent<RectTransform>(),
                Btn = childOne.GetComponentInChildren<Button>(),
            };
            if (item.Btn != null)
            {
                int page = pageIndex;
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

            return item;
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
                warehousePageTabs.Add(BuildTabEntry(childOne, i));
            }
        }

        void EnsurePageTabCapacity(int pageCount)
        {
            if (warehousePageTabsRoot == null || warehousePageTabsRoot.childCount == 0)
            {
                return;
            }

            var template = warehousePageTabsRoot.GetChild(0).gameObject;
            while (warehousePageTabs.Count < pageCount)
            {
                int pageIndex = warehousePageTabs.Count;
                var go = UnityEngine.Object.Instantiate(template, warehousePageTabsRoot);
                go.name = "PageTab_" + pageIndex;
                warehousePageTabs.Add(BuildTabEntry(go.transform, pageIndex));
            }

            for (int i = 0; i < warehousePageTabs.Count; i++)
            {
                bool show = i < pageCount;
                if (warehousePageTabs[i].Root != null)
                {
                    warehousePageTabs[i].Root.gameObject.SetActive(show);
                }
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
            currentWarehousePage = Mathf.Clamp(currentWarehousePage, 0, Mathf.Max(0, _cachedPageCount - 1));
            if (warehousePageTabs.Count > 0 && currentWarehousePage < warehousePageTabs.Count
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

        void RebuildVisibleBagIndices(PlayerBag bag)
        {
            _visibleBagIndices.Clear();
            if (bag == null)
            {
                return;
            }

            if (typeFilter < 0)
            {
                for (int i = 0; i < bag.BasicCapacity; i++)
                {
                    _visibleBagIndices.Add(i);
                }
                for (int j = 0; j < bag.MaxExtraCapacity; j++)
                {
                    _visibleBagIndices.Add(bag.BasicCapacity + j);
                }
            }
            else
            {
                var filtered = BindingInventory.GetWarehouseSlotIndicesForItemTypeFilter(typeFilter);
                if (filtered != null && filtered.Count > 0)
                {
                    for (int k = 0; k < filtered.Count; k++)
                    {
                        _visibleBagIndices.Add(filtered[k]);
                    }
                }
            }
        }

        int ComputePageCount(int totalVisibleSlots, int itemsPerPage)
        {
            if (totalVisibleSlots <= 0)
            {
                return 1;
            }
            return Mathf.Max(1, Mathf.CeilToInt(totalVisibleSlots / (float)itemsPerPage));
        }

        int CurrentPageGridItemCount()
        {
            int ipp = ItemsPerPage;
            int start = currentWarehousePage * ipp;
            int remain = Mathf.Max(0, _visibleBagIndices.Count - start);
            if (remain <= 0)
            {
                return typeFilter < 0 ? 0 : ipp;
            }
            return Mathf.Min(ipp, remain);
        }

        private void OnWarehouseDataChanged()
        {
            _cachedItemsPerPage = ItemsPerPage;
            var warehouseBag = GetWarehouseBag();
            RebuildVisibleBagIndices(warehouseBag);

            int pageCount = ComputePageCount(_visibleBagIndices.Count, _cachedItemsPerPage);
            _cachedPageCount = pageCount;
            currentWarehousePage = Mathf.Clamp(currentWarehousePage, 0, pageCount - 1);

            EnsurePageTabCapacity(pageCount);

            int gridCount = CurrentPageGridItemCount();
            if (gridCount <= 0 && typeFilter >= 0 && _visibleBagIndices.Count == 0)
            {
                gridCount = ippPlaceholder();
            }
            GridView.SetListItemCount(gridCount);
            GridView.RefreshAllShownItem();
            RefreshWarehousePageTabHints(pageCount);
            UpdatePageSelectHints();
        }

        int ippPlaceholder()
        {
            return Mathf.Min(ItemsPerPage, 1);
        }

        void UpdatePageSelectHints()
        {
            for (int i = 0; i < warehousePageTabs.Count; i++)
            {
                if (warehousePageTabs[i].SelectHint != null)
                {
                    warehousePageTabs[i].SelectHint.gameObject.SetActive(i == currentWarehousePage && i < _cachedPageCount);
                }
            }
        }

        private PlayerBag GetWarehouseBag()
        {
            return BindingInventory.GetBagById((int)EPlayerBagId.Storage);
        }

        private void SwitchWarehousePage(int page)
        {
            if (page < 0 || page >= _cachedPageCount)
            {
                return;
            }
            if (currentWarehousePage == page)
            {
                return;
            }

            currentWarehousePage = page;
            UpdatePageSelectHints();

            int gridCount = CurrentPageGridItemCount();
            if (gridCount <= 0 && typeFilter >= 0 && _visibleBagIndices.Count == 0)
            {
                gridCount = ippPlaceholder();
            }
            GridView.SetListItemCount(gridCount);
            GridView.RefreshAllShownItem();
            RefreshWarehousePageTabHints(_cachedPageCount);
        }

        private void RefreshWarehousePageTabHints(int pageCount)
        {
            var warehousePage = GetWarehouseBag();
            int used = 0;
            if (warehousePage != null)
            {
                foreach (var s in warehousePage.NormalSlots)
                {
                    if (s != null && !s.IsEmpty)
                    {
                        used++;
                    }
                }
            }

            int cap = warehousePage != null ? warehousePage.NormalSlots.Count : 0;
            var summary = warehousePage != null ? $"{used}/{cap}" : "";

            for (int i = 0; i < warehousePageTabs.Count; i++)
            {
                if (warehousePageTabs[i].PageCountText == null)
                {
                    continue;
                }
                if (i < pageCount)
                {
                    warehousePageTabs[i].PageCountText.text = i == 0 && !string.IsNullOrEmpty(summary)
                        ? $"{summary}  {i + 1}/{pageCount}"
                        : $"{i + 1}/{pageCount}";
                }
                else
                {
                    warehousePageTabs[i].PageCountText.text = "";
                }
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

            int ipp = ItemsPerPage;
            int start = currentWarehousePage * ipp;
            int global = start + itemIndex;

            if (global < 0 || global >= _visibleBagIndices.Count)
            {
                cell.ClearEmpty();
                return item;
            }

            int bagIdx = _visibleBagIndices[global];
            if (!warehousePage.IsSlotIdxValid(bagIdx))
            {
                cell.ClearEmpty();
                return item;
            }

            var stack = warehousePage.GetItemByIdx(bagIdx);
            item.gameObject.SetActive(true);
            int bid = (int)EPlayerBagId.Storage;
            cell.Bind(stack, bagIdx, EContainerType.Warehouse, bid, null);

            bool dim = typeFilter < 0 && stack != null && !stack.IsEmpty && !PassesTypeFilter(stack);
            cell.icon.color = dim ? new Color(1f, 1f, 1f, 0.28f) : Color.white;
            if (cell.countText != null)
            {
                var c = cell.countText.color;
                c.a = dim ? 0.35f : 1f;
                cell.countText.color = c;
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
