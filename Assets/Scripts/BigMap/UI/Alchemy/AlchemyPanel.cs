using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.Player.Alchemy;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyPanel : PanelWithInput
    {
        public const string Pid = "AlchemyPanel";

        public sealed class OpenArgs
        {
            public string FurnaceId = AlchemyConstants.HandCraftFurnaceId;
            public List<string> ActiveToolIds = new();
        }

        [SerializeField] Button closeButton;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI furnaceNameText;
        [SerializeField] TextMeshProUGUI toolNameText;
        [SerializeField] TextMeshProUGUI slotHintText;
        [SerializeField] TextMeshProUGUI virtueSummaryText;
        [SerializeField] TextMeshProUGUI aspectSummaryText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button furnaceButton;
        [SerializeField] Button toolButton;
        [SerializeField] Button craftButton;
        [SerializeField] AlchemyCircularSlotLayout slotLayout;
        [SerializeField] AlchemyInputSlotView[] inputSlots;
        [SerializeField] GameObject rightPanelRoot;
        [SerializeField] GameObject materialPickerRoot;
        [SerializeField] GameObject slotDetailRoot;
        [SerializeField] TextMeshProUGUI slotDetailNameText;
        [SerializeField] TextMeshProUGUI slotDetailDescText;
        [SerializeField] Button slotUnequipButton;
        [SerializeField] RectTransform materialGridRoot;
        [SerializeField] AlchemyMaterialPickerCell materialPickerTemplate;
        [SerializeField] AlchemyEquipmentPickerOverlay equipmentPicker;

        readonly List<AlchemyMaterialPickerCell> _pickerCells = new();
        readonly List<string> _activeToolIds = new();
        readonly List<AlchemyInputSlot> _materialScratch = new();

        string[] _slotItemIds = System.Array.Empty<string>();
        string _furnaceId = AlchemyConstants.HandCraftFurnaceId;
        int _selectedSlotIndex = -1;
        PlayerInventorySystem _inventory;

        public static void Toggle(OpenArgs args = null)
        {
            if (UIManager.Instance.IsPanelVisible(Pid))
            {
                UIManager.Instance.HidePanel(Pid);
            }
            else
            {
                UIManager.Instance.ShowPanel(Pid, args);
            }
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            if (furnaceButton != null)
            {
                furnaceButton.onClick.RemoveAllListeners();
                furnaceButton.onClick.AddListener(OpenFurnacePicker);
            }

            if (toolButton != null)
            {
                toolButton.onClick.RemoveAllListeners();
                toolButton.onClick.AddListener(OpenToolPicker);
            }

            if (craftButton != null)
            {
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(TryStartCraft);
            }

            if (slotUnequipButton != null)
            {
                slotUnequipButton.onClick.RemoveAllListeners();
                slotUnequipButton.onClick.AddListener(UnequipSelectedSlot);
            }

            if (materialPickerTemplate != null)
            {
                materialPickerTemplate.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            BindInventory(null);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            ApplyOpenArgs(data);
        }

        void ApplyOpenArgs(object data)
        {
            _furnaceId = AlchemyConstants.HandCraftFurnaceId;
            _activeToolIds.Clear();
            if (data is OpenArgs args)
            {
                if (!string.IsNullOrEmpty(args.FurnaceId))
                {
                    _furnaceId = args.FurnaceId;
                }

                if (args.ActiveToolIds != null)
                {
                    for (int i = 0; i < args.ActiveToolIds.Count; i++)
                    {
                        var toolId = args.ActiveToolIds[i];
                        if (!string.IsNullOrEmpty(toolId) && !_activeToolIds.Contains(toolId))
                        {
                            _activeToolIds.Add(toolId);
                        }
                    }
                }
            }

            EnsureSlotArraySize();
        }

        public override void Show()
        {
            base.Show();
            _selectedSlotIndex = -1;
            BindInventory(MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem);
            if (_inventory != null && !AlchemyOwnershipUtil.IsFurnaceOwned(_inventory, _furnaceId))
            {
                _furnaceId = AlchemyConstants.HandCraftFurnaceId;
            }

            EnsureSlotArraySize();
            RefreshAll();
        }

        public override void Hide()
        {
            equipmentPicker?.Hide();
            BindInventory(null);
            base.Hide();
        }

        public override bool OnCancel()
        {
            if (equipmentPicker != null && equipmentPicker.IsOpen)
            {
                equipmentPicker.Hide();
                return true;
            }

            Close();
            return true;
        }

        void BindInventory(PlayerInventorySystem inventory)
        {
            if (_inventory == inventory)
            {
                return;
            }

            UnbindBagEvents(_inventory?.MainBag);
            UnbindBagEvents(_inventory?.WarehouseBag);
            _inventory = inventory;
            BindBagEvents(_inventory?.MainBag);
            BindBagEvents(_inventory?.WarehouseBag);
        }

        void BindBagEvents(PlayerBag bag)
        {
            if (bag != null)
            {
                bag.EvOnBagUpdate += RefreshAll;
            }
        }

        void UnbindBagEvents(PlayerBag bag)
        {
            if (bag != null)
            {
                bag.EvOnBagUpdate -= RefreshAll;
            }
        }

        void Close()
        {
            UIManager.Instance.HidePanel(Pid);
        }

        void OpenFurnacePicker()
        {
            if (equipmentPicker == null || _inventory == null)
            {
                return;
            }

            var ids = AlchemyOwnershipUtil.GetSelectableFurnaceIds(_inventory);
            equipmentPicker.ShowFurnacePicker(ids, _furnaceId, OnFurnacePicked);
        }

        void OpenToolPicker()
        {
            if (equipmentPicker == null || _inventory == null)
            {
                return;
            }

            var owned = AlchemyOwnershipUtil.GetOwnedToolIds(_inventory);
            equipmentPicker.ShowToolPicker(owned, _activeToolIds, OnToolsPicked);
        }

        void OnFurnacePicked(string furnaceId)
        {
            if (string.IsNullOrEmpty(furnaceId))
            {
                return;
            }

            _furnaceId = furnaceId;
            EnsureSlotArraySize();
            if (_selectedSlotIndex >= _slotItemIds.Length)
            {
                _selectedSlotIndex = -1;
            }

            SetStatus(string.Empty);
            RefreshAll();
        }

        void OnToolsPicked(IReadOnlyList<string> toolIds)
        {
            _activeToolIds.Clear();
            if (toolIds != null)
            {
                for (int i = 0; i < toolIds.Count; i++)
                {
                    var toolId = toolIds[i];
                    if (!string.IsNullOrEmpty(toolId) && !_activeToolIds.Contains(toolId))
                    {
                        _activeToolIds.Add(toolId);
                    }
                }
            }

            SetStatus(string.Empty);
            RefreshAll();
        }

        void RefreshAll()
        {
            RefreshHeader();
            RefreshInputSlots();
            RefreshRightPanel();
            RefreshMixSummary();
            RefreshCraftButton();
        }

        void RefreshHeader()
        {
            if (titleText != null)
            {
                titleText.text = "炼金";
            }

            var furnace = AlchemyCatalog.GetFurnace(_furnaceId);
            if (furnaceNameText != null)
            {
                furnaceNameText.text = furnace?.DisplayName ?? _furnaceId;
            }

            if (toolNameText != null)
            {
                toolNameText.text = BuildToolSummary();
            }

            int maxSlots = AlchemyCraftService.ResolveMaxMaterialSlots(_furnaceId);
            int filled = CountFilledSlots();
            if (slotHintText != null)
            {
                slotHintText.text = $"素材格 {filled}/{maxSlots}";
            }
        }

        string BuildToolSummary()
        {
            if (_activeToolIds.Count == 0)
            {
                return "工具：无";
            }

            if (_activeToolIds.Count == 1)
            {
                var tool = AlchemyCatalog.GetTool(_activeToolIds[0]);
                return "工具：" + (tool?.DisplayName ?? _activeToolIds[0]);
            }

            return $"工具：{_activeToolIds.Count} 件";
        }

        void RefreshInputSlots()
        {
            if (inputSlots == null)
            {
                return;
            }

            int maxSlots = AlchemyCraftService.ResolveMaxMaterialSlots(_furnaceId);
            slotLayout?.ApplyLayout(maxSlots);
            for (int i = 0; i < inputSlots.Length; i++)
            {
                var view = inputSlots[i];
                if (view == null)
                {
                    continue;
                }

                if (i >= maxSlots)
                {
                    view.SetLocked(i);
                    continue;
                }

                var itemId = i < _slotItemIds.Length ? _slotItemIds[i] : null;
                if (!string.IsNullOrEmpty(itemId))
                {
                    view.BindFilled(i, itemId, OnInputSlotClicked);
                }
                else
                {
                    view.BindEmpty(i, true, OnInputSlotClicked);
                }

                view.SetSelected(i == _selectedSlotIndex);
            }
        }

        void OnInputSlotClicked(int slotIndex)
        {
            int maxSlots = AlchemyCraftService.ResolveMaxMaterialSlots(_furnaceId);
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                return;
            }

            _selectedSlotIndex = slotIndex;
            SetStatus(string.Empty);
            RefreshInputSlots();
            RefreshRightPanel();
        }

        void RefreshRightPanel()
        {
            bool hasSelection = _selectedSlotIndex >= 0
                && _selectedSlotIndex < AlchemyCraftService.ResolveMaxMaterialSlots(_furnaceId);
            if (rightPanelRoot != null)
            {
                rightPanelRoot.SetActive(hasSelection);
            }

            if (!hasSelection)
            {
                return;
            }

            var itemId = GetSlotItemId(_selectedSlotIndex);
            bool filled = !string.IsNullOrEmpty(itemId);
            if (materialPickerRoot != null)
            {
                materialPickerRoot.SetActive(!filled);
            }

            if (slotDetailRoot != null)
            {
                slotDetailRoot.SetActive(filled);
            }

            if (filled)
            {
                RefreshSlotDetail(itemId);
            }
            else
            {
                RefreshMaterialPicker();
            }
        }

        void RefreshSlotDetail(string itemId)
        {
            var def = ItemCatalog.GetItemDef(itemId);
            if (slotDetailNameText != null)
            {
                slotDetailNameText.text = def?.DisplayName ?? itemId;
            }

            if (slotDetailDescText != null)
            {
                slotDetailDescText.text = BuildMaterialDetailText(itemId);
            }
        }

        static string BuildMaterialDetailText(string itemId)
        {
            var mat = AlchemyCatalog.GetMaterial(itemId);
            if (mat == null)
            {
                return string.Empty;
            }

            var virtues = AlchemyMixTextUtil.BuildVirtueSummaryFromValues(mat.Virtues);
            var aspects = AlchemyMixTextUtil.BuildAspectSummaryFromValues(mat.Aspects);
            return virtues + "\n" + aspects;
        }

        void RefreshMaterialPicker()
        {
            foreach (var cell in _pickerCells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _pickerCells.Clear();
            if (materialPickerTemplate == null || materialGridRoot == null || _inventory == null)
            {
                return;
            }

            var counts = CollectOwnedAlchemyMaterials();
            var placedCounts = CountPlacedMaterials();
            foreach (var pair in counts)
            {
                long available = pair.Value;
                placedCounts.TryGetValue(pair.Key, out var placed);
                if (available <= 0 && placed <= 0)
                {
                    continue;
                }

                var cell = Instantiate(materialPickerTemplate, materialGridRoot);
                cell.gameObject.SetActive(true);
                cell.Bind(pair.Key, available, placed, OnPickMaterial, OnUnequipMaterialFromList);
                _pickerCells.Add(cell);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(materialGridRoot);
        }

        Dictionary<string, long> CollectOwnedAlchemyMaterials()
        {
            var counts = new Dictionary<string, long>();
            if (_inventory == null)
            {
                return counts;
            }

            AccumulateBagSlots(_inventory.MainBag?.NormalSlots, counts);
            AccumulateBagSlots(_inventory.MainBag?.ExtraSlots, counts);
            AccumulateBagSlots(_inventory.WarehouseBag?.NormalSlots, counts);
            AccumulateBagSlots(_inventory.WarehouseBag?.ExtraSlots, counts);
            return counts;
        }

        static void AccumulateBagSlots(List<ItemStack> slots, Dictionary<string, long> counts)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var stack = slots[i];
                if (stack == null || stack.IsEmpty || !AlchemyCatalog.IsAlchemyMaterial(stack.ItemID))
                {
                    continue;
                }

                counts.TryGetValue(stack.ItemID, out var current);
                counts[stack.ItemID] = current + stack.Count;
            }
        }

        Dictionary<string, int> CountPlacedMaterials()
        {
            var counts = new Dictionary<string, int>();
            if (_slotItemIds == null)
            {
                return counts;
            }

            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                var itemId = _slotItemIds[i];
                if (string.IsNullOrEmpty(itemId))
                {
                    continue;
                }

                counts.TryGetValue(itemId, out var current);
                counts[itemId] = current + 1;
            }

            return counts;
        }

        long GetAvailableCount(string itemId)
        {
            var owned = CollectOwnedAlchemyMaterials();
            owned.TryGetValue(itemId, out var count);
            var placed = CountPlacedMaterials();
            placed.TryGetValue(itemId, out var inSlots);
            return count - inSlots;
        }

        void OnPickMaterial(string itemId)
        {
            if (_selectedSlotIndex < 0 || !string.IsNullOrEmpty(GetSlotItemId(_selectedSlotIndex)))
            {
                SetStatus("请选择空素材格");
                return;
            }

            if (!AlchemyCatalog.IsAlchemyMaterial(itemId))
            {
                SetStatus("该物品不可炼金");
                return;
            }

            if (GetAvailableCount(itemId) <= 0)
            {
                SetStatus("材料不足");
                return;
            }

            _slotItemIds[_selectedSlotIndex] = itemId;
            SetStatus(string.Empty);
            RefreshAll();
        }

        void UnequipSelectedSlot()
        {
            if (_selectedSlotIndex < 0)
            {
                return;
            }

            ClearSlot(_selectedSlotIndex);
        }

        void OnUnequipMaterialFromList(string itemId)
        {
            int index = FindFirstSlotWithItem(itemId);
            if (index >= 0)
            {
                ClearSlot(index);
            }
        }

        int FindFirstSlotWithItem(string itemId)
        {
            if (_slotItemIds == null || string.IsNullOrEmpty(itemId))
            {
                return -1;
            }

            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                if (_slotItemIds[i] == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotItemIds.Length)
            {
                return;
            }

            _slotItemIds[slotIndex] = null;
            SetStatus(string.Empty);
            RefreshAll();
        }

        void TryStartCraft()
        {
            if (_inventory == null)
            {
                SetStatus("背包不可用");
                return;
            }

            BuildFilledMaterialList(_materialScratch);
            if (_materialScratch.Count == 0)
            {
                SetStatus("请先投入材料");
                return;
            }

            if (!AlchemyCraftService.TryCraftBestMatchingRecipe(
                _inventory, _furnaceId, _activeToolIds, _materialScratch, out var recipe, out var reason))
            {
                SetStatus(TranslateReason(reason));
                RefreshCraftButton();
                return;
            }

            var resultName = ItemCatalog.GetItemDef(recipe?.ResultItemId)?.DisplayName ?? recipe?.DisplayName;
            SetStatus(string.IsNullOrEmpty(resultName) ? "炼金成功" : $"炼金成功：{resultName}");
            ClearAllSlots();
            RefreshAll();
        }

        void RefreshCraftButton()
        {
            if (craftButton == null)
            {
                return;
            }

            BuildFilledMaterialList(_materialScratch);
            bool canCraft = _inventory != null
                && _materialScratch.Count > 0
                && AlchemyCraftService.CanCraftBestMatchingRecipe(
                    _inventory, _furnaceId, _activeToolIds, _materialScratch, out _);
            craftButton.interactable = canCraft;
        }

        void RefreshMixSummary()
        {
            BuildFilledMaterialList(_materialScratch);
            if (_materialScratch.Count == 0)
            {
                if (virtueSummaryText != null)
                {
                    virtueSummaryText.text = "功效：-";
                }

                if (aspectSummaryText != null)
                {
                    aspectSummaryText.text = "属性：-";
                }

                return;
            }

            if (AlchemyCraftService.TryResolveMix(
                _furnaceId, _activeToolIds, _materialScratch, out var mix, out var reason))
            {
                if (virtueSummaryText != null)
                {
                    virtueSummaryText.text = AlchemyMixTextUtil.BuildVirtueSummary(mix.Virtues);
                }

                if (aspectSummaryText != null)
                {
                    aspectSummaryText.text = AlchemyMixTextUtil.BuildAspectSummary(mix.Aspects);
                }

                return;
            }

            if (virtueSummaryText != null)
            {
                virtueSummaryText.text = "功效：-";
            }

            if (aspectSummaryText != null)
            {
                aspectSummaryText.text = "属性：-";
            }

            if (!string.IsNullOrEmpty(reason))
            {
                SetStatus(TranslateReason(reason));
            }
        }

        void BuildFilledMaterialList(List<AlchemyInputSlot> output)
        {
            output.Clear();
            if (_slotItemIds == null)
            {
                return;
            }

            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                var itemId = _slotItemIds[i];
                if (!string.IsNullOrEmpty(itemId))
                {
                    output.Add(new AlchemyInputSlot(itemId));
                }
            }
        }

        void EnsureSlotArraySize()
        {
            int maxSlots = AlchemyCraftService.ResolveMaxMaterialSlots(_furnaceId);
            if (maxSlots < 1)
            {
                maxSlots = 1;
            }

            var next = new string[maxSlots];
            if (_slotItemIds != null)
            {
                int copy = Mathf.Min(_slotItemIds.Length, next.Length);
                for (int i = 0; i < copy; i++)
                {
                    next[i] = _slotItemIds[i];
                }
            }

            _slotItemIds = next;
        }

        int CountFilledSlots()
        {
            int count = 0;
            if (_slotItemIds == null)
            {
                return count;
            }

            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(_slotItemIds[i]))
                {
                    count++;
                }
            }

            return count;
        }

        string GetSlotItemId(int slotIndex)
        {
            if (_slotItemIds == null || slotIndex < 0 || slotIndex >= _slotItemIds.Length)
            {
                return null;
            }

            return _slotItemIds[slotIndex];
        }

        void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        static string TranslateReason(string reasonEn)
        {
            if (string.IsNullOrEmpty(reasonEn))
            {
                return string.Empty;
            }

            if (reasonEn.Contains("Missing required alchemy tools"))
            {
                return "缺少必需工具";
            }

            if (reasonEn.Contains("Insufficient materials"))
            {
                return "材料不足";
            }

            if (reasonEn.Contains("No matching alchemy recipe"))
            {
                return "没有匹配的配方";
            }

            if (reasonEn.Contains("No craftable alchemy recipe"))
            {
                return "当前组合无法炼金";
            }

            if (reasonEn.Contains("Not enough space for result item"))
            {
                return "背包空间不足";
            }

            if (reasonEn.Contains("No alchemy materials provided"))
            {
                return "请先投入材料";
            }

            if (reasonEn.Contains("Furnace not owned."))
            {
                return "未持有该炼金炉";
            }

            if (reasonEn.Contains("Tool not owned."))
            {
                return "未持有所选工具";
            }

            return reasonEn;
        }

        // 配方区第一版未展示，保留接口供 AlchemyRecipeCell 编译通过。
        public bool CanCraftRecipe(AlchemyRecipe recipe)
        {
            BuildFilledMaterialList(_materialScratch);
            return _inventory != null
                && AlchemyCraftService.CanCraft(_inventory, _furnaceId, _activeToolIds, _materialScratch, recipe);
        }

        public void TryCraftRecipe(AlchemyRecipe recipe)
        {
            BuildFilledMaterialList(_materialScratch);
            if (recipe == null || _inventory == null)
            {
                return;
            }

            if (!AlchemyCraftService.TryCraft(
                _inventory, _furnaceId, _activeToolIds, _materialScratch, recipe, out var reason))
            {
                SetStatus(string.IsNullOrEmpty(reason) ? "炼金失败" : TranslateReason(reason));
                RefreshAll();
                return;
            }

            SetStatus("炼金成功");
            ClearAllSlots();
            RefreshAll();
        }

        void ClearAllSlots()
        {
            if (_slotItemIds == null)
            {
                return;
            }

            for (int i = 0; i < _slotItemIds.Length; i++)
            {
                _slotItemIds[i] = null;
            }

            _selectedSlotIndex = -1;
        }
    }
}
