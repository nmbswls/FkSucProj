using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.Player.Cooking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Cooking
{
    public sealed class CookingPanel : PanelWithInput
    {
        public const string Pid = "CookingPanel";

        public sealed class OpenArgs
        {
            public PlayerInventorySystem Inventory;
            public PlayerSystemManager Player;
        }

        [SerializeField] Button closeButton;
        [SerializeField] Button allFilterButton;
        [SerializeField] Button craftableFilterButton;
        [SerializeField] Image allFilterBackground;
        [SerializeField] Image craftableFilterBackground;
        [SerializeField] RectTransform recipeListRoot;
        [SerializeField] CookingRecipeListItem recipeTemplate;
        [SerializeField] TextMeshProUGUI emptyListText;

        [SerializeField] Image dishIcon;
        [SerializeField] TextMeshProUGUI dishIconFallbackText;
        [SerializeField] TextMeshProUGUI dishNameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] TextMeshProUGUI rarityText;
        [SerializeField] TextMeshProUGUI primaryTypeText;
        [SerializeField] TextMeshProUGUI styleText;
        [SerializeField] RectTransform ingredientListRoot;
        [SerializeField] CookingIngredientRow ingredientTemplate;
        [SerializeField] Button decreaseBatchButton;
        [SerializeField] Button increaseBatchButton;
        [SerializeField] TextMeshProUGUI batchText;
        [SerializeField] TextMeshProUGUI outputText;
        [SerializeField] Button craftButton;
        [SerializeField] TextMeshProUGUI craftButtonText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Slider craftProgress;
        [SerializeField] GameObject craftCompleteEffect;

        readonly List<CookingRecipeListItem> _recipeItems = new();
        readonly List<CookingIngredientRow> _ingredientRows = new();
        readonly HashSet<PlayerBag> _boundBags = new();
        readonly HashSet<string> _selectedCookingIngredients = new();

        PlayerInventorySystem _inventory;
        PlayerSystemManager _player;
        CookingRecipe _selectedRecipe;
        int _batchCount = 1;
        bool _onlyCraftable;
        bool _suppressInventoryRefresh;
        bool _isCrafting;
        Coroutine _craftRoutine;
        OpenArgs _openArgs;

        public static CookingPanel Open() => UIManager.Instance.ShowPanel(Pid) as CookingPanel;

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _openArgs = data as OpenArgs;
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId)) panelId = Pid;
            layer = UILayer.Popup;
            closeButton?.onClick.AddListener(Close);
            allFilterButton?.onClick.AddListener(() => SetFilter(false));
            craftableFilterButton?.onClick.AddListener(() => SetFilter(true));
            decreaseBatchButton?.onClick.AddListener(() => SetBatch(_batchCount - 1));
            increaseBatchButton?.onClick.AddListener(() => SetBatch(_batchCount + 1));
            craftButton?.onClick.AddListener(CraftSelected);
            EnsureCraftFeedbackUi();
            if (recipeTemplate != null) recipeTemplate.gameObject.SetActive(false);
            if (ingredientTemplate != null) ingredientTemplate.gameObject.SetActive(false);
        }

        void EnsureCraftFeedbackUi()
        {
            if (craftProgress == null && craftButton != null)
            {
                var go = new GameObject("CraftProgress", typeof(RectTransform), typeof(Image), typeof(Slider));
                go.transform.SetParent(craftButton.transform.parent, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-12f, 52f);
                rect.sizeDelta = new Vector2(184f, 8f);
                var background = go.GetComponent<Image>();
                background.color = new Color(.12f, .15f, .16f, .95f);
                craftProgress = go.GetComponent<Slider>();
                craftProgress.minValue = 0f;
                craftProgress.maxValue = 1f;
                craftProgress.value = 0f;
                craftProgress.interactable = false;
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(go.transform, false);
                var fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(1f, 1f);
                fillRect.offsetMax = new Vector2(-1f, -1f);
                fill.GetComponent<Image>().color = new Color(.76f, .63f, .32f, 1f);
                craftProgress.fillRect = fillRect;
                craftProgress.gameObject.SetActive(false);
            }

            if (craftCompleteEffect == null && craftButton != null)
            {
                var go = new GameObject("CraftCompleteEffect", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(craftButton.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(.95f, .82f, .42f, .3f);
                go.GetComponent<CanvasGroup>().alpha = 0f;
                craftCompleteEffect = go;
            }
        }

        public override void Show()
        {
            base.Show();
            _player = _openArgs?.Player ?? MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            BindInventory(_openArgs?.Inventory ?? _player?.InventorySystem);
            _batchCount = 1;
            _selectedCookingIngredients.Clear();
            RefreshAll(rebuildRecipes: true);
        }

        public override void Hide()
        {
            BindInventory(null);
            base.Hide();
        }

        public override bool OnCancel()
        {
            Close();
            return true;
        }

        void Close() => UIManager.Instance.HidePanel(Pid);

        void BindInventory(PlayerInventorySystem inventory)
        {
            foreach (var bag in _boundBags)
            {
                if (bag != null) bag.EvOnBagUpdate -= OnInventoryChanged;
            }
            _boundBags.Clear();
            _inventory = inventory;
            if (_inventory == null) return;

            BindBag(_inventory.MainBag);
            BindBag(_inventory.MindFacetBag);
            BindBag(_inventory.ImportantItemBag);
            BindBag(_inventory.WarehouseBag);
            BindBag(_inventory.FurnitureWarehouseBag);
            foreach (var bag in _inventory.SpeBags.Values) BindBag(bag);
        }

        void BindBag(PlayerBag bag)
        {
            if (bag != null && _boundBags.Add(bag)) bag.EvOnBagUpdate += OnInventoryChanged;
        }

        void OnInventoryChanged()
        {
            _selectedCookingIngredients.RemoveWhere(itemId => (_inventory?.GetItemTotal(itemId, includeWarehouse: true) ?? 0) <= 0);
            if (!_suppressInventoryRefresh) RefreshAll(rebuildRecipes: true);
        }

        void SetFilter(bool onlyCraftable)
        {
            if (_onlyCraftable == onlyCraftable) return;
            _onlyCraftable = onlyCraftable;
            RefreshAll(rebuildRecipes: true);
        }

        void SetBatch(int count)
        {
            _batchCount = Mathf.Clamp(count, 1, CookingCraftService.MaxBatchCount);
            RefreshAll(rebuildRecipes: false);
        }

        void RefreshAll(bool rebuildRecipes)
        {
            RefreshFilterVisuals();
            if (rebuildRecipes) RebuildRecipeList();
            RefreshRecipeSelectionVisuals();
            RefreshDetails();
        }

        void RefreshFilterVisuals()
        {
            var active = new Color(0.35f, 0.50f, 0.47f, 1f);
            var idle = new Color(0.16f, 0.18f, 0.19f, 1f);
            if (allFilterBackground != null) allFilterBackground.color = _onlyCraftable ? idle : active;
            if (craftableFilterBackground != null) craftableFilterBackground.color = _onlyCraftable ? active : idle;
        }

        void RebuildRecipeList()
        {
            for (int i = 0; i < _recipeItems.Count; i++) Destroy(_recipeItems[i].gameObject);
            _recipeItems.Clear();
            var recipes = CookingCatalog.GetAllRecipes();
            bool selectedVisible = false;
            CookingRecipe firstVisible = null;
            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (!RecipeMatchesSelectedIngredients(recipe)) continue;
                var quote = CookingCraftService.BuildQuote(_inventory, _player, recipe, 1);
                if (_onlyCraftable && !quote.CanCraft) continue;
                firstVisible ??= recipe;
                selectedVisible |= _selectedRecipe?.Id == recipe.Id;
                var item = Instantiate(recipeTemplate, recipeListRoot, false);
                item.gameObject.SetActive(true);
                item.Bind(recipe, quote, _selectedRecipe?.Id == recipe.Id, SelectRecipe);
                _recipeItems.Add(item);
            }
            if (!selectedVisible) _selectedRecipe = firstVisible;
            emptyListText.gameObject.SetActive(_recipeItems.Count == 0);
        }

        bool RecipeMatchesSelectedIngredients(CookingRecipe recipe)
        {
            if (_selectedCookingIngredients.Count == 0) return true;
            foreach (var itemId in _selectedCookingIngredients)
            {
                if (recipe?.IngredientItemIds == null || !recipe.IngredientItemIds.Contains(itemId)) return false;
            }
            return true;
        }

        void ToggleCookingIngredient(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!_selectedCookingIngredients.Add(itemId)) _selectedCookingIngredients.Remove(itemId);
            RebuildRecipeList();
            RefreshRecipeSelectionVisuals();
            RefreshDetails();
        }

        void SelectRecipe(CookingRecipe recipe)
        {
            _selectedRecipe = recipe;
            _batchCount = 1;
            statusText.text = string.Empty;
            RefreshAll(rebuildRecipes: false);
        }

        void RefreshRecipeSelectionVisuals()
        {
            for (int i = 0; i < _recipeItems.Count; i++)
            {
                _recipeItems[i].SetSelected(_recipeItems[i].RecipeId == (_selectedRecipe?.Id ?? 0));
            }
        }

        void RefreshDetails()
        {
            ClearIngredientRows();
            if (_selectedRecipe == null)
            {
                dishIcon.enabled = false;
                dishIconFallbackText.gameObject.SetActive(false);
                dishNameText.text = "未选择料理";
                descriptionText.text = string.Empty;
                craftButton.interactable = false;
                return;
            }

            var recipe = _selectedRecipe;
            var quote = CookingCraftService.BuildQuote(_inventory, _player, recipe, _batchCount);
            bool hasDishIcon = CookingRecipeListItem.ApplyIcon(dishIcon, CookingCatalog.ResolveIconSprite(recipe));
            dishIconFallbackText.gameObject.SetActive(!hasDishIcon);
            dishIconFallbackText.text = CookingUiText.PrimaryType(recipe.PrimaryType).Substring(0, 1);
            dishNameText.text = recipe.DisplayName;
            descriptionText.text = recipe.Description;
            levelText.text = $"制作档次  Lv.{recipe.Level}";
            rarityText.text = $"原料稀有度  {CookingUiText.Rarity(recipe.Rarity)}";
            primaryTypeText.text = $"主类型  {CookingUiText.PrimaryType(recipe.PrimaryType)}";
            styleText.text = BuildStyleText(recipe);

            for (int i = 0; i < recipe.IngredientItemIds.Count; i++)
            {
                long required = recipe.IngredientCounts[i] * _batchCount;
                long owned = _inventory?.GetItemTotal(recipe.IngredientItemIds[i], includeWarehouse: true) ?? 0;
                var row = Instantiate(ingredientTemplate, ingredientListRoot, false);
                row.gameObject.SetActive(true);
                string ingredientId = recipe.IngredientItemIds[i];
                row.Bind(ingredientId, owned, required, ToggleCookingIngredient, _selectedCookingIngredients.Contains(ingredientId));
                _ingredientRows.Add(row);
            }

            batchText.text = _batchCount.ToString();
            string materialFilter = _selectedCookingIngredients.Count == 0
                ? string.Empty
                : $" · 已选原料 {_selectedCookingIngredients.Count}";
            string qualityName = ItemCatalog.GetItemDef(recipe.QualityResultItemId)?.DisplayName ?? "优质料理";
            outputText.text = $"普通产物 {recipe.OutputCount * _batchCount} · {qualityName} 10%{materialFilter}";
            craftButton.interactable = quote.CanCraft && !_isCrafting;
            craftButtonText.text = quote.IsUnlocked ? "制作" : "尚未解锁";
            if (!quote.IsUnlocked) statusText.text = recipe.UnlockHint;
            else if (quote.Result == ECookingActionResult.InsufficientItems) statusText.text = "材料不足";
            else if (quote.Result == ECookingActionResult.InventoryFull) statusText.text = "背包空间不足";
            else if (quote.Result == ECookingActionResult.InvalidConfig) statusText.text = "配方配置错误";
            else if (!statusText.text.StartsWith("制作完成")) statusText.text = string.Empty;
        }

        static string BuildStyleText(CookingRecipe recipe)
        {
            var values = new List<string>();
            for (int i = 0; i < recipe.StyleTags.Count; i++)
            {
                string label = CookingUiText.Style(recipe.StyleTags[i]);
                if (!string.IsNullOrEmpty(label)) values.Add(label);
            }
            return values.Count == 0 ? "风格  无" : "风格  " + string.Join(" · ", values);
        }

        void ClearIngredientRows()
        {
            for (int i = 0; i < _ingredientRows.Count; i++) Destroy(_ingredientRows[i].gameObject);
            _ingredientRows.Clear();
        }

        void CraftSelected()
        {
            if (_selectedRecipe == null || _isCrafting) return;
            if (_craftRoutine != null) StopCoroutine(_craftRoutine);
            _craftRoutine = StartCoroutine(CraftRoutine());
        }

        IEnumerator CraftRoutine()
        {
            _isCrafting = true;
            if (craftProgress != null)
            {
                craftProgress.value = 0f;
                craftProgress.gameObject.SetActive(true);
            }
            RefreshDetails();
            const float duration = .38f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (craftProgress != null) craftProgress.value = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            CompleteCraft();
            _isCrafting = false;
            if (craftProgress != null) craftProgress.gameObject.SetActive(false);
            RefreshAll(rebuildRecipes: true);
            _craftRoutine = null;
        }

        void CompleteCraft()
        {
            _suppressInventoryRefresh = true;
            ECookingActionResult result;
            long output;
            long qualityOutput;
            try
            {
                result = CookingCraftService.TryCraft(_inventory, _player, _selectedRecipe, _batchCount, out output, out qualityOutput);
            }
            finally
            {
                _suppressInventoryRefresh = false;
            }
            statusText.text = result == ECookingActionResult.Success
                ? $"制作完成 · 普通 {output - qualityOutput} · 优质 {qualityOutput}"
                : ResultText(result);
            if (result == ECookingActionResult.Success)
            {
                PlayCraftCompleteEffect();
            }
        }

        void PlayCraftCompleteEffect()
        {
            if (craftCompleteEffect == null) return;
            var group = craftCompleteEffect.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = 0.9f;
            craftCompleteEffect.transform.localScale = Vector3.one;
            StartCoroutine(FadeCraftCompleteEffect(group));
        }

        IEnumerator FadeCraftCompleteEffect(CanvasGroup group)
        {
            float elapsed = 0f;
            while (elapsed < .28f && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(.9f, 0f, elapsed / .28f);
                craftCompleteEffect.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.06f, elapsed / .28f);
                yield return null;
            }
            if (group != null) group.alpha = 0f;
        }

        static string ResultText(ECookingActionResult result) => result switch
        {
            ECookingActionResult.Locked => "配方尚未解锁",
            ECookingActionResult.InsufficientItems => "材料不足",
            ECookingActionResult.InventoryFull => "背包空间不足",
            ECookingActionResult.InvalidConfig => "配方配置错误",
            ECookingActionResult.UnexpectedInventoryFailure => "库存发生变化，材料已回退",
            _ => "无法制作",
        };
    }
}
