using cfg.demo;
using My;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Forge
{
    public sealed class ForgeRecipeCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI materialHintText;
        [SerializeField] Button clickButton;

        ForgeRecipe _recipe;

        public void WireRefs(Image i, TextMeshProUGUI title, TextMeshProUGUI materials, Button btn)
        {
            icon = i;
            titleText = title;
            materialHintText = materials;
            clickButton = btn;
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void Bind(ForgeRecipe recipe)
        {
            _recipe = recipe;
            if (recipe == null)
            {
                return;
            }

            var def = ItemCatalog.GetItemDef(recipe.ResultItemId);
            var display = def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : recipe.ResultItemId;
            if (titleText != null)
            {
                titleText.text = display;
            }

            if (materialHintText != null)
            {
                materialHintText.text = BuildMaterialSummary(recipe);
            }

            ApplyIcon(recipe, def);
        }

        static string BuildMaterialSummary(ForgeRecipe recipe)
        {
            if (recipe.Materials == null || recipe.Materials.Count == 0)
            {
                return "";
            }

            var parts = string.Empty;
            for (int i = 0; i < recipe.Materials.Count; i++)
            {
                var m = recipe.Materials[i];
                if (m == null || string.IsNullOrEmpty(m.ItemId))
                {
                    continue;
                }

                if (parts.Length > 0)
                {
                    parts += " ";
                }

                parts += m.ItemId + "x" + m.Count;
            }

            return parts;
        }

        void ApplyIcon(ForgeRecipe recipe, ItemData def)
        {
            if (icon == null)
            {
                return;
            }

            string spriteName = null;
            if (!string.IsNullOrEmpty(recipe.IconSprite))
            {
                spriteName = recipe.IconSprite;
            }
            else if (def != null && !string.IsNullOrEmpty(def.SpriteName))
            {
                spriteName = def.SpriteName;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                icon.enabled = false;
                return;
            }

            var sp = SimpleResManager.Load<Sprite>("Sprites/" + spriteName);
            if (sp != null)
            {
                icon.sprite = sp;
                icon.enabled = true;
            }
            else
            {
                icon.enabled = false;
            }
        }

        void OnClicked()
        {
            if (_recipe == null)
            {
                return;
            }

            Debug.Log("[Forge] recipe click id=" + _recipe.Id + " result=" + _recipe.ResultItemId);
        }
    }
}
