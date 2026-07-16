using System;
using System.Collections.Generic;
using System.Text;
using My;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.UI;
using SuperScrollView;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Market
{
    public sealed class MarketPanel : PanelBase, IDropHandler
    {
        public const string PanelIdConst = "MarketPanel";

        public LoopGridView GridView;
        public int Columns = 5;
        public string ItemPrefabName = "OneItem";
        public Button QuitBtn;
        public Button CancelListingBtn;
        public Button SettleBtn;
        public TextMeshProUGUI StatusText;
        public TextMeshProUGUI ExpectedPriceText;
        public string BuildingId = "market";
        public string LogicAreaId = "homestead_01";

        HumanCivilizationSystem _civilization;
        readonly List<HumanCivilizationSystem.MarketListing> _listings = new();
        bool _dirty;
        int _selectedListingIndex = -1;

        void Awake()
        {
            panelId = PanelIdConst;
            GridView?.InitGridView(0, OnGetItemByIndex);
            GridView?.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);
            QuitBtn?.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
            CancelListingBtn?.onClick.AddListener(CancelSelectedListing);
            SettleBtn?.onClick.AddListener(SettleNow);
            var dropZone = GetComponent<MarketDropZone>();
            if (dropZone != null) dropZone.Panel = this;
            if (ExpectedPriceText == null)
            {
                foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (text.gameObject.name == "ExpectedPrice")
                    {
                        ExpectedPriceText = text;
                        break;
                    }
                }
            }
        }

        public override void Show()
        {
            base.Show();
            UIOrchestrator.Instance?.EnsurePlayerBag();
            _civilization = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem?.HumanCivilization;
            if (_civilization != null) _civilization.OnMarketChanged += MarkDirty;
            Refresh();
        }

        public override void Hide()
        {
            if (_civilization != null) _civilization.OnMarketChanged -= MarkDirty;
            _civilization = null;
            base.Hide();
        }

        void Update()
        {
            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }
        }

        void MarkDirty() => _dirty = true;

        public void Refresh()
        {
            _listings.Clear();
            if (_civilization != null)
            {
                foreach (var listing in _civilization.MarketListings)
                {
                    if (listing.BuildingId == BuildingId) _listings.Add(listing);
                }
            }

            if (_selectedListingIndex < 0 || _selectedListingIndex >= _listings.Count)
            {
                _selectedListingIndex = -1;
            }

            GridView?.SetListItemCount(_listings.Count + 1);
            var status = new StringBuilder();
            var capacity = _civilization?.GetMarketSlotCapacity(BuildingId, LogicAreaId) ?? 0;
            status.Append($"市集格子：{_listings.Count}/{capacity}");
            status.Append($"\n上次售出：{_civilization?.LastMarketSoldItemCount ?? 0} 件，获得金币：{_civilization?.LastMarketGoldEarned ?? 0}");
            if (_civilization != null && _selectedListingIndex >= 0)
            {
                var listing = _listings[_selectedListingIndex];
                var preview = _civilization.GetMarketPricePreview(listing);
                status.Append($"\n\n{ItemCatalog.GetItemDef(listing.ItemId)?.DisplayName ?? listing.ItemId}：预计 {preview.ExpectedUnitRevenue} / 件，售出率 {preview.SaleChancePercent}%");
                foreach (var line in preview.PriceBreakdown)
                {
                    status.Append($"\n  {line}");
                }
            }
            else
            {
                status.Append("\n\n请选择挂牌商品查看详情");
            }
            if (CancelListingBtn != null) CancelListingBtn.gameObject.SetActive(_selectedListingIndex >= 0);
            if (ExpectedPriceText != null) ExpectedPriceText.text = status.ToString();
            else if (StatusText != null) StatusText.text = status.ToString();
        }

        LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
        {
            var item = grid.NewListViewItem(ItemPrefabName);
            var wrapper = item.GetComponent<ShopContainerWrapper>();
            if (wrapper == null) return item;

            if (itemIndex >= 0 && itemIndex < _listings.Count)
            {
                var listing = _listings[itemIndex];
                wrapper.Bind(listing.Count, new ItemStack(listing.ItemId, listing.Count), itemIndex,
                    EContainerType.Shop, 0, null, ItemCellBase.EStyleType.Normal);
                wrapper.InnerCell?.SetItemCellInteractions(new MarketSelectListingClick(this, itemIndex), null, null);
                item.gameObject.SetActive(true);
            }
            else
            {
                item.gameObject.SetActive(false);
            }

            return item;
        }

        public void TryAcceptDrop(ItemDragDropController controller)
        {
            var payload = controller?.Payload;
            if (payload == null || _civilization == null) return;

            var source = ResolveSource(payload);
            if (source == null) return;
            if (_civilization.TryListItem(BuildingId, LogicAreaId, source, payload.SourceIndex, payload.ItemCnt,
                out var failReason))
            {
                controller.MarkDropHandled();
                Refresh();
            }
            else
            {
                Debug.LogWarning($"[Market] Cannot list item: {failReason}");
            }
        }

        IItemContainer ResolveSource(DragPayload payload)
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            if (inv == null) return null;
            if (payload.SourceContainerType == EContainerType.Inventory) return inv.GetBagById(payload.SourceContainerId);
            if (payload.SourceContainerType == EContainerType.Warehouse) return inv.GetBagById((int)EPlayerBagId.Storage);
            if (payload.SourceContainerType == EContainerType.SpecialInventory) return inv.GetBagById(payload.SourceContainerId);
            return null;
        }

        void SettleNow()
        {
            _civilization?.SettleMarket();
            Refresh();
        }

        void SelectListing(int index)
        {
            _selectedListingIndex = index >= 0 && index < _listings.Count ? index : -1;
            Refresh();
        }

        void CancelSelectedListing()
        {
            if (_civilization == null || _selectedListingIndex < 0 || _selectedListingIndex >= _listings.Count)
            {
                return;
            }

            if (_civilization.TryCancelListing(_listings[_selectedListingIndex], out _))
            {
                _selectedListingIndex = -1;
                Refresh();
            }
        }

        sealed class MarketSelectListingClick : IItemCellClickBehaviour
        {
            readonly MarketPanel _panel;
            readonly int _index;

            public MarketSelectListingClick(MarketPanel panel, int index)
            {
                _panel = panel;
                _index = index;
            }

            public void OnItemCellClick(ItemCellBase cell, PointerEventData eventData)
            {
                if (eventData.button != PointerEventData.InputButton.Left || _panel?._civilization == null)
                {
                    return;
                }

                _panel.SelectListing(_index);
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            TryAcceptDrop(ItemDragDropController.Instance);
        }
    }
}
