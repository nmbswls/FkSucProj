using System;
using cfg.demo;
using My.Config;
using My.Player.Cooking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Cooking
{
    public sealed class CookingRecipeListItem : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image selectionBar;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI iconFallbackText;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI metaText;
        [SerializeField] TextMeshProUGUI stateText;

        CookingRecipe _recipe;
        Action<CookingRecipe> _onSelected;
        public int RecipeId => _recipe?.Id ?? 0;

        void Awake()
        {
            button?.onClick.AddListener(() => _onSelected?.Invoke(_recipe));
        }

        public void Bind(
            CookingRecipe recipe,
            CookingCraftQuote quote,
            bool selected,
            Action<CookingRecipe> onSelected)
        {
            _recipe = recipe;
            _onSelected = onSelected;
            nameText.text = recipe.DisplayName;
            metaText.text = $"Lv.{recipe.Level}  {CookingUiText.Rarity(recipe.Rarity)} · {CookingUiText.PrimaryType(recipe.PrimaryType)}";

            stateText.text = !quote.IsUnlocked ? "未解锁" : quote.CanCraft ? "可制作" : "材料不足";
            stateText.color = !quote.IsUnlocked
                ? new Color(0.56f, 0.58f, 0.61f)
                : quote.CanCraft
                    ? new Color(0.45f, 0.78f, 0.56f)
                    : new Color(0.86f, 0.58f, 0.42f);

            SetSelected(selected);
            bool hasIcon = ApplyIcon(icon, CookingCatalog.ResolveIconSprite(recipe));
            iconFallbackText.gameObject.SetActive(!hasIcon);
            iconFallbackText.text = CookingUiText.PrimaryType(recipe.PrimaryType).Substring(0, 1);
        }

        public void SetSelected(bool selected)
        {
            background.color = selected
                ? new Color(0.19f, 0.24f, 0.25f, 1f)
                : new Color(0.11f, 0.13f, 0.14f, 0.96f);
            selectionBar.enabled = selected;
        }

        internal static bool ApplyIcon(Image target, string spriteName)
        {
            if (target == null)
            {
                return false;
            }
            var sprite = string.IsNullOrEmpty(spriteName)
                ? null
                : SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
            target.sprite = sprite;
            target.enabled = sprite != null;
            return sprite != null;
        }
    }
}
