using cfg.demo;
using My.Config;
using My.Map;
using My.Player.Bag;
using My.UI;
using SuperScrollView;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Bag
{
    /// <summary>
    /// 右侧仓库：多页、类型筛选、与主背包拖拽互通；数据在 <see cref="PlayerInventoryModel.WarehousePageBags"/>。
    /// </summary>
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

        // 与 PlayerBag 预制体槽位名兼容：特殊背包区隐藏，页签行复用为仓库分页。
        public RectTransform SpeBagPanel;
        public Button CollapseSpeBagBtn;
        public LoopGridView SpeGridView;

        /// <summary>当前仓库页 0..WarehouseConfig.PageCount-1（复用预制字段名）。</summary>
        public int CurrExpandSpeBag;

        public Transform SpecBagSelectionsTr;

        public class InnerSpeBagItem
        {
            public RectTransform Root;
            public Button Btn;
            public Image SelectHint;
            public TextMeshProUGUI StackCount;
        }

        public List<InnerSpeBagItem> SpeBagItems = new List<InnerSpeBagItem>();

        public PlayerInventoryModel BindingInventory =>
            MainGameManager.Instance.gameLogicManager.playerDataManager.inventoryModel;

        private bool markDirty;
        private int typeFilter = -1;
        private bool filterStripBuilt;

        private void Awake()
        {
            GridView.InitGridView(0, OnGetItemByIndex);
            GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);

            if (SpeBagPanel != null)
            {
                SpeBagPanel.gameObject.SetActive(false);
            }
            if (SpeGridView != null)
            {
                SpeGridView.gameObject.SetActive(false);
            }
            if (CollapseSpeBagBtn != null)
            {
                CollapseSpeBagBtn.gameObject.SetActive(false);
            }

            SpeBagItems.Clear();
            if (SpecBagSelectionsTr != null)
            {
                for (int i = 0; i < SpecBagSelectionsTr.childCount; i++)
                {
                    var childOne = SpecBagSelectionsTr.GetChild(i);
                    var item = new InnerSpeBagItem
                    {
                        Root = childOne.GetComponent<RectTransform>(),
                        Btn = childOne.GetComponentInChildren<Button>(),
                    };
                    int page = i;
                    item.Btn.onClick.RemoveAllListeners();
                    item.Btn.onClick.AddListener(() => SwitchPage(page));

                    var selTr = childOne.Find("Select");
                    item.SelectHint = selTr != null ? selTr.GetComponent<Image>() : null;
                    var hintTr = childOne.Find("Hint");
                    item.StackCount = hintTr != null ? hintTr.GetComponentInChildren<TextMeshProUGUI>() : null;

                    if (item.SelectHint != null)
                    {
                        item.SelectHint.gameObject.SetActive(false);
                    }
                    if (item.StackCount != null)
                    {
                        item.StackCount.gameObject.SetActive(true);
                    }

                    SpeBagItems.Add(item);
                }
            }

            CurrExpandSpeBag = 0;
        }

        public override void Show()
        {
            base.Show();
            EnsureTypeFilterStrip();
            foreach (var it in SpeBagItems)
            {
                if (it.SelectHint != null)
                {
                    it.SelectHint.gameObject.SetActive(false);
                }
            }
            if (SpeBagItems.Count > 0 && CurrExpandSpeBag >= 0 && CurrExpandSpeBag < SpeBagItems.Count
                && SpeBagItems[CurrExpandSpeBag].SelectHint != null)
            {
                SpeBagItems[CurrExpandSpeBag].SelectHint.gameObject.SetActive(true);
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
            var bag = CurrentBag();
            if (bag != null)
            {
                GridView.SetListItemCount(bag.BasicCapacity + bag.MaxExtraCapacity);
            }
            GridView.RefreshAllShownItem();
            RefreshPageTabHints();
        }

        private PlayerBag CurrentBag()
        {
            int page = Mathf.Clamp(CurrExpandSpeBag, 0, WarehouseConfig.PageCount - 1);
            return BindingInventory.GetBagById(WarehouseConfig.BagIdFirst + page);
        }

        private void SwitchPage(int page)
        {
            if (page < 0 || page >= WarehouseConfig.PageCount)
            {
                return;
            }
            if (CurrExpandSpeBag == page)
            {
                return;
            }

            foreach (var item in SpeBagItems)
            {
                if (item.SelectHint != null)
                {
                    item.SelectHint.gameObject.SetActive(false);
                }
            }

            CurrExpandSpeBag = page;
            if (page < SpeBagItems.Count && SpeBagItems[page].SelectHint != null)
            {
                SpeBagItems[page].SelectHint.gameObject.SetActive(true);
            }

            OnWarehouseDataChanged();
        }

        private void RefreshPageTabHints()
        {
            for (int i = 0; i < SpeBagItems.Count && i < WarehouseConfig.PageCount; i++)
            {
                var bag = BindingInventory.GetBagById(WarehouseConfig.BagIdFirst + i);
                if (bag == null || SpeBagItems[i].StackCount == null)
                {
                    continue;
                }
                int used = 0;
                foreach (var s in bag.NormalSlots)
                {
                    if (s != null && !s.IsEmpty)
                    {
                        used++;
                    }
                }
                SpeBagItems[i].StackCount.text = $"{used}/{bag.NormalSlots.Count}";
            }
        }

        private LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            var item = grid.NewListViewItem(ItemPrefabName);
            var cell = item.GetComponent<AnyContainerItemCell>();

            var bag = CurrentBag();
            if (bag == null)
            {
                return item;
            }

            if (itemIndex < bag.BasicCapacity)
            {
                var stack = bag.GetItemByIdx(itemIndex);
                item.gameObject.SetActive(true);
                int bid = WarehouseConfig.BagIdFirst + Mathf.Clamp(CurrExpandSpeBag, 0, WarehouseConfig.PageCount - 1);
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

        private void EnsureTypeFilterStrip()
        {
            if (filterStripBuilt)
            {
                return;
            }
            RectTransform mainBagRt = null;
            foreach (var rt in GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.gameObject.name == "MainBag")
                {
                    mainBagRt = rt;
                    break;
                }
            }
            if (mainBagRt == null)
            {
                return;
            }

            var strip = new GameObject("WarehouseTypeFilterStrip", typeof(RectTransform));
            strip.transform.SetParent(mainBagRt, false);
            var rtStrip = strip.GetComponent<RectTransform>();
            rtStrip.SetAsFirstSibling();
            rtStrip.anchorMin = new Vector2(0f, 1f);
            rtStrip.anchorMax = new Vector2(1f, 1f);
            rtStrip.pivot = new Vector2(0.5f, 1f);
            rtStrip.sizeDelta = new Vector2(-16f, 28f);
            rtStrip.anchoredPosition = new Vector2(0f, -6f);

            var le = strip.AddComponent<LayoutElement>();
            le.minHeight = 28f;
            le.preferredHeight = 28f;

            var hlg = strip.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(4, 4, 0, 0);

            void AddFilterButton(string label, int filterVal)
            {
                var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(strip.transform, false);
                var img = go.GetComponent<Image>();
                img.color = new Color(0.22f, 0.18f, 0.32f, 0.95f);
                var btn = go.GetComponent<Button>();
                var rtBtn = go.GetComponent<RectTransform>();
                rtBtn.sizeDelta = new Vector2(72f, 24f);

                var leB = go.AddComponent<LayoutElement>();
                leB.minWidth = 72f;
                leB.preferredWidth = 72f;

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGo.transform.SetParent(go.transform, false);
                var text = txtGo.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.text = label;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.92f, 0.88f, 1f, 1f);
                text.fontSize = 12;
                text.resizeTextForBestFit = true;
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = Vector2.zero;
                rtT.anchorMax = Vector2.one;
                rtT.offsetMin = Vector2.zero;
                rtT.offsetMax = Vector2.zero;

                int fv = filterVal;
                btn.onClick.AddListener(() =>
                {
                    typeFilter = fv;
                    GridView.RefreshAllShownItem();
                });
            }

            AddFilterButton("全", -1);
            AddFilterButton("普", (int)EItemType.Normal);
            AddFilterButton("币", (int)EItemType.Currency);
            AddFilterButton("装", (int)EItemType.Equip);
            AddFilterButton("挂", (int)EItemType.Pocket);
            AddFilterButton("插", (int)EItemType.Insertion);

            filterStripBuilt = true;
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
