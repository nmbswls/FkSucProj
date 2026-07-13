using System.Collections.Generic;
using System.Text;
using My.Config;
using My.Player;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Dismantle
{
    public sealed class ItemDismantlePanel : PanelWithInput
    {
        public const string Pid = "ItemDismantlePanel";

        [SerializeField] Button closeButton;
        [SerializeField] Button bagTabButton;
        [SerializeField] Button warehouseTabButton;
        [SerializeField] Image bagTabBackground;
        [SerializeField] Image warehouseTabBackground;
        [SerializeField] Image selectedItemIcon;
        [SerializeField] TMP_Text selectedNameText;
        [SerializeField] TMP_Text ownedCountText;
        [SerializeField] TMP_Text outputPreviewText;
        [SerializeField] RectTransform gridRoot;
        [SerializeField] DismantleGridCell cellTemplate;
        [SerializeField] Button dismantleButton;
        [SerializeField] TMP_Text emptyHintText;

        static readonly Color SelectedTabColor = new Color(.43f, .31f, .15f, 1f);
        static readonly Color NormalTabColor = new Color(.20f, .20f, .20f, 1f);

        readonly List<DismantleGridCell> _cells = new();
        readonly List<ItemEntry> _entries = new();
        PlayerInventorySystem _inventory;
        EPlayerBagId _sourceBagId = EPlayerBagId.Default;
        string _selectedItemId;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId)) panelId = Pid;
            layer = UILayer.Popup;
            closeButton?.onClick.AddListener(Close);
            bagTabButton?.onClick.AddListener(() => SelectSource(EPlayerBagId.Default));
            warehouseTabButton?.onClick.AddListener(() => SelectSource(EPlayerBagId.Storage));
            dismantleButton?.onClick.AddListener(OnClickDismantle);
            if (cellTemplate != null) cellTemplate.gameObject.SetActive(false);
        }

        public override void Show()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                UIManager.Instance.HidePanel(Pid);
                return;
            }

            base.Show();
            BindInventory(glm.playerDataManager?.InventorySystem);
            Refresh();
        }

        public override void Hide()
        {
            BindInventory(null);
            base.Hide();
        }

        void BindInventory(PlayerInventorySystem inventory)
        {
            if (_inventory == inventory) return;
            if (_inventory?.MainBag != null) _inventory.MainBag.EvOnBagUpdate -= Refresh;
            if (_inventory?.WarehouseBag != null) _inventory.WarehouseBag.EvOnBagUpdate -= Refresh;
            _inventory = inventory;
            if (_inventory?.MainBag != null) _inventory.MainBag.EvOnBagUpdate += Refresh;
            if (_inventory?.WarehouseBag != null) _inventory.WarehouseBag.EvOnBagUpdate += Refresh;
        }

        void SelectSource(EPlayerBagId bagId)
        {
            if (_sourceBagId == bagId) return;
            _sourceBagId = bagId;
            _selectedItemId = null;
            Refresh();
        }

        public void Refresh()
        {
            RebuildEntries();
            RebuildGrid();
            RefreshTabs();
            RefreshSelection();
        }

        void RebuildEntries()
        {
            _entries.Clear();
            var bag = CurrentBag();
            if (bag == null) return;

            var counts = new Dictionary<string, long>();
            Accumulate(bag.NormalSlots, counts);
            Accumulate(bag.ExtraSlots, counts);
            foreach (var pair in counts)
            {
                if (ItemDismantleCatalog.CanDismantle(pair.Key))
                    _entries.Add(new ItemEntry(pair.Key, pair.Value));
            }
            _entries.Sort((a, b) => string.CompareOrdinal(a.ItemId, b.ItemId));

            if (!string.IsNullOrEmpty(_selectedItemId) && !counts.ContainsKey(_selectedItemId))
                _selectedItemId = null;
            if (string.IsNullOrEmpty(_selectedItemId) && _entries.Count > 0)
                _selectedItemId = _entries[0].ItemId;
        }

        static void Accumulate(List<ItemStack> slots, Dictionary<string, long> counts)
        {
            foreach (var stack in slots)
            {
                if (stack == null || stack.IsEmpty) continue;
                counts.TryGetValue(stack.ItemID, out var count);
                counts[stack.ItemID] = count + stack.Count;
            }
        }

        void RebuildGrid()
        {
            foreach (var cell in _cells)
                if (cell != null) Destroy(cell.gameObject);
            _cells.Clear();
            if (cellTemplate == null || gridRoot == null) return;

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                var cell = Instantiate(cellTemplate, gridRoot);
                cell.gameObject.SetActive(true);
                var containerType = _sourceBagId == EPlayerBagId.Storage
                    ? EContainerType.Warehouse
                    : EContainerType.Inventory;
                cell.BindSelection(new ItemStack(entry.ItemId, entry.Count), i, containerType,
                    entry.ItemId == _selectedItemId, () => SelectItem(entry.ItemId));
                _cells.Add(cell);
            }
            if (emptyHintText != null) emptyHintText.gameObject.SetActive(_entries.Count == 0);
        }

        void SelectItem(string itemId)
        {
            if (_selectedItemId == itemId) return;
            _selectedItemId = itemId;
            RebuildGrid();
            RefreshSelection();
        }

        void RefreshTabs()
        {
            bool bag = _sourceBagId == EPlayerBagId.Default;
            if (bagTabBackground != null) bagTabBackground.color = bag ? SelectedTabColor : NormalTabColor;
            if (warehouseTabBackground != null) warehouseTabBackground.color = bag ? NormalTabColor : SelectedTabColor;
        }

        void RefreshSelection()
        {
            var def = string.IsNullOrEmpty(_selectedItemId) ? null : ItemCatalog.GetItemDef(_selectedItemId);
            var count = def == null ? 0 : CurrentBag()?.GetItemCount(_selectedItemId) ?? 0;
            if (selectedNameText != null) selectedNameText.text = def?.DisplayName ?? "未选择物品";
            if (ownedCountText != null) ownedCountText.text = count > 0 ? $"持有 {count}" : string.Empty;
            if (dismantleButton != null) dismantleButton.interactable = count > 0;
            if (outputPreviewText != null)
                outputPreviewText.text = count > 0 ? BuildOutputText(_selectedItemId, 1) : "选择物品以预览分解产物";

            if (selectedItemIcon != null)
            {
                var sprite = def == null || string.IsNullOrEmpty(def.SpriteName)
                    ? null
                    : SimpleResManager.Load<Sprite>("Sprites/Item/" + def.SpriteName);
                selectedItemIcon.sprite = sprite;
                selectedItemIcon.enabled = sprite != null;
            }
        }

        void OnClickDismantle()
        {
            if (string.IsNullOrEmpty(_selectedItemId)) return;
            var maxAmount = CurrentBag()?.GetItemCount(_selectedItemId) ?? 0;
            if (maxAmount <= 0) return;
            var itemId = _selectedItemId;
            ItemCountChooseBox.Show(maxAmount, 1, amount => ConfirmDismantle(itemId, amount));
        }

        void ConfirmDismantle(string itemId, long amount)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            var sourceName = _sourceBagId == EPlayerBagId.Storage ? "仓库" : "随身背包";
            var message = new StringBuilder();
            message.AppendLine($"从{sourceName}分解 {def?.DisplayName ?? itemId} ×{amount}");
            message.AppendLine();
            message.Append(BuildOutputText(itemId, amount));
            YesNoMsgBox.Show("确认分解", message.ToString(), () =>
            {
                if (!ItemDismantleService.TryDismantle(_sourceBagId, itemId, amount, out var reason))
                    YesNoMsgBox.Show("无法分解", reason);
            });
        }

        static string BuildOutputText(string itemId, long amount)
        {
            var sb = new StringBuilder("获得：\n");
            foreach (var output in ItemDismantleCatalog.BuildOutputs(itemId, amount))
            {
                var def = ItemCatalog.GetItemDef(output.ItemId);
                sb.AppendLine($"{def?.DisplayName ?? output.ItemId} ×{output.Count}");
            }
            return sb.ToString().TrimEnd();
        }

        PlayerBag CurrentBag()
        {
            if (_inventory == null) return null;
            return _sourceBagId == EPlayerBagId.Storage ? _inventory.WarehouseBag : _inventory.MainBag;
        }

        void Close() => UIManager.Instance.HidePanel(Pid);

        public override bool OnCancel()
        {
            Close();
            return true;
        }

        readonly struct ItemEntry
        {
            public ItemEntry(string itemId, long count) { ItemId = itemId; Count = count; }
            public string ItemId { get; }
            public long Count { get; }
        }
    }
}
