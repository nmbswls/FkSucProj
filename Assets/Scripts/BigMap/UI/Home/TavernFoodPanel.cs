using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Home;
using My.Player;
using My.Player.Bag;
using My.UI.Cooking;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Home
{
    public sealed class TavernFoodPanel : PanelWithInput
    {
        public const string PanelIdConst = "TavernFoodPanel";

        readonly List<string> _foodIds = new();
        PlayerInventorySystem _inventory;
        string _selectedItemId;
        int _selectedCount = 1;
        RectTransform _listRoot;
        TextMeshProUGUI _selectionText;
        TextMeshProUGUI _statusText;
        TextMeshProUGUI _hotTagText;
        TextMeshProUGUI _slotText;
        RectTransform _slotRoot;
        Button _fillButton;
        string _townId;

        public static TavernFoodPanel Open()
        {
            return UIManager.Instance.ShowPanel(PanelIdConst) as TavernFoodPanel;
        }

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            BuildUi();
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _inventory = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            _townId = MainGameManager.Instance?.gameLogicManager?.homeDataManager?.CurrentTownId;
            RefreshFoodList();
        }

        public override void Show()
        {
            base.Show();
            if (_inventory?.WarehouseBag != null)
            {
                _inventory.WarehouseBag.EvOnBagUpdate -= RefreshFoodList;
                _inventory.WarehouseBag.EvOnBagUpdate += RefreshFoodList;
            }
            RefreshFoodList();
        }

        public override void Hide()
        {
            if (_inventory?.WarehouseBag != null) _inventory.WarehouseBag.EvOnBagUpdate -= RefreshFoodList;
            base.Hide();
        }

        public override bool OnCancel()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
            return true;
        }

        void BuildUi()
        {
            var root = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(.5f, .5f);
            root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(.5f, .5f);
            root.sizeDelta = new Vector2(520f, 420f);
            var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = new Color(.08f, .1f, .12f, .98f);
            var title = CreateText(transform, "Title", "Tavern Orders", 22, FontStyles.Bold, new Vector2(22f, -18f), new Vector2(-22f, -48f));
            title.color = new Color(.86f, .76f, .52f, 1f);
            CreateText(transform, "Hint", "Fill a tavern order from the secretbase warehouse.", 13, FontStyles.Normal, new Vector2(22f, -52f), new Vector2(-22f, -78f));
            _hotTagText = CreateText(transform, "HotTags", "Popular tags: -", 14, FontStyles.Bold, new Vector2(22f, -80f), new Vector2(-22f, -108f));
            _listRoot = CreateRect(transform, "FoodList", new Vector2(22f, -112f), new Vector2(-22f, -230f));
            var list = _listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 5f;
            list.childForceExpandHeight = false;
            list.childControlHeight = true;
            _selectionText = CreateText(transform, "Selection", "Select a food", 15, FontStyles.Bold, new Vector2(22f, -246f), new Vector2(-22f, -278f));
            _statusText = CreateText(transform, "Status", string.Empty, 13, FontStyles.Normal, new Vector2(22f, -282f), new Vector2(-22f, -316f));
            _slotRoot = CreateRect(transform, "DishSlots", new Vector2(22f, -322f), new Vector2(-22f, -358f));
            var slotLayout = _slotRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 8f;
            slotLayout.childForceExpandWidth = true;
            slotLayout.childControlWidth = true;
            _slotText = CreateText(transform, "Slots", "Slots: -", 12, FontStyles.Normal, new Vector2(22f, -360f), new Vector2(-22f, -382f));
            CreateButton(transform, "Decrease", "-", new Vector2(300f, -278f), new Vector2(336f, -244f), () => SetSelectedCount(_selectedCount - 1));
            CreateButton(transform, "Increase", "+", new Vector2(340f, -278f), new Vector2(376f, -244f), () => SetSelectedCount(_selectedCount + 1));
            _fillButton = CreateButton(transform, "Fill", "Fill Food", new Vector2(-182f, 20f), new Vector2(-22f, 62f), FillSelected);
            CreateButton(transform, "Close", "Close", new Vector2(22f, 20f), new Vector2(112f, 62f), () => UIManager.Instance.HidePanel(PanelIdConst));
        }

        void RefreshFoodList()
        {
            if (_listRoot == null) return;
            for (int i = _listRoot.childCount - 1; i >= 0; i--) Destroy(_listRoot.GetChild(i).gameObject);
            _foodIds.Clear();
            var seen = new HashSet<string>();
            AddFoodFromBag(_inventory?.WarehouseBag, seen);
            RefreshTavernSummary();
            if (string.IsNullOrEmpty(_selectedItemId) || !_foodIds.Contains(_selectedItemId)) _selectedItemId = _foodIds.Count > 0 ? _foodIds[0] : null;
            _selectedCount = Mathf.Max(1, Mathf.Min(_selectedCount, GetSelectedCount()));
            for (int i = 0; i < _foodIds.Count; i++)
            {
                string id = _foodIds[i];
                long count = _inventory.WarehouseBag.GetItemCount(id);
                string name = ItemCatalog.GetItemDef(id)?.DisplayName ?? id;
                var button = CreateButton(_listRoot, id, $"{name}   x{count}", Vector2.zero, Vector2.zero, () => SelectFood(id));
                var layout = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                layout.minHeight = 34f;
            }
            UpdateSelectionText();
        }

        void AddFoodFromBag(PlayerBag bag, HashSet<string> seen)
        {
            if (bag == null) return;
            AddFoodFromSlots(bag.NormalSlots, seen);
            AddFoodFromSlots(bag.ExtraSlots, seen);
        }

        void AddFoodFromSlots(List<ItemStack> slots, HashSet<string> seen)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                string id = slots[i]?.ItemID;
                if (!string.IsNullOrEmpty(id) && slots[i].Count > 0 && ItemTagCatalog.HasTag(ItemCatalog.GetItemDef(id), EItemTag.Food) && seen.Add(id)) _foodIds.Add(id);
            }
        }

        void SelectFood(string itemId)
        {
            _selectedItemId = itemId;
            _selectedCount = 1;
            UpdateSelectionText();
        }

        void SetSelectedCount(int count)
        {
            _selectedCount = Mathf.Clamp(count, 1, Mathf.Max(1, GetSelectedCount()));
            UpdateSelectionText();
        }

        int GetSelectedCount()
        {
            long count = _inventory?.WarehouseBag?.GetItemCount(_selectedItemId) ?? 0;
            return string.IsNullOrEmpty(_selectedItemId) ? 0 : (int)Mathf.Clamp((float)count, 0f, int.MaxValue);
        }

        void UpdateSelectionText()
        {
            if (_selectionText == null) return;
            if (string.IsNullOrEmpty(_selectedItemId)) _selectionText.text = "No food in warehouse";
            else _selectionText.text = $"Selected: {ItemCatalog.GetItemDef(_selectedItemId)?.DisplayName ?? _selectedItemId}   x{_selectedCount}";
        }

        void FillSelected()
        {
            if (string.IsNullOrEmpty(_selectedItemId) || _inventory?.WarehouseBag == null)
            {
                _statusText.text = "Select food first";
                return;
            }
            int available = GetSelectedCount();
            if (available <= 0)
            {
                _statusText.text = "Food is unavailable";
                RefreshFoodList();
                return;
            }
            int amount = Mathf.Clamp(_selectedCount, 1, available);
            string reason = string.Empty;
            var tavern = MainGameManager.Instance?.gameLogicManager?.tavernSystem;
            bool filled = tavern != null && tavern.TryFill(_townId, _selectedItemId, amount, _inventory, out reason);
            _statusText.text = filled ? $"Filled tavern slot: {amount}" : reason switch
            {
                "full" => "All tavern slots are occupied",
                "insufficient" => "Food is unavailable",
                _ => "Could not fill order",
            };
            RefreshFoodList();
        }

        void RefreshTavernSummary()
        {
            var system = MainGameManager.Instance?.gameLogicManager?.tavernSystem;
            if (system == null) return;
            var tags = system.GetActiveTags(_townId);
            if (_hotTagText != null)
            {
                _hotTagText.text = $"Popular tags: {CookingUiText.Style(tags[0])} / {CookingUiText.Style(tags[1])}";
            }
            var state = system.GetState(_townId, false);
            if (_slotText != null)
            {
                var values = new List<string>();
                if (_slotRoot != null)
                {
                    for (int i = _slotRoot.childCount - 1; i >= 0; i--) Destroy(_slotRoot.GetChild(i).gameObject);
                }
                for (int i = 0; i < TavernSystem.SlotCount; i++)
                {
                    var slot = state?.Slots[i];
                    string label = string.IsNullOrEmpty(slot?.ItemId)
                        ? $"[{i + 1}] Empty"
                        : $"{ItemCatalog.GetItemDef(slot.ItemId)?.DisplayName ?? slot.ItemId}\nx{slot.Count}";
                    values.Add(label.Replace("\n", " "));
                    if (_slotRoot != null)
                    {
                        var cell = CreateButton(_slotRoot, $"DishSlot{i + 1}", label, Vector2.zero, Vector2.zero, () => { });
                        cell.GetComponent<LayoutElement>().preferredWidth = 145f;
                        cell.GetComponent<LayoutElement>().preferredHeight = 34f;
                    }
                }
                _slotText.text = string.Join("   ", values);
            }
        }

        static RectTransform CreateRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = min;
            rect.offsetMax = max;
            return rect;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, FontStyles style, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(parent, name, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var rect = CreateRect(parent, name, min, max);
            if (min == Vector2.zero && max == Vector2.zero)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(0f, 34f);
            }
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.18f, .24f, .28f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var text = CreateText(rect, "Label", label, 14, FontStyles.Normal, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
