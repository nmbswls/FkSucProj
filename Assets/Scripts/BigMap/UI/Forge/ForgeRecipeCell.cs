using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
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
            string display = !string.IsNullOrEmpty(recipe.DisplayName)
                ? recipe.DisplayName
                : (def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : recipe.ResultItemId);
            if (titleText != null)
            {
                titleText.text = display;
            }

            if (materialHintText != null)
            {
                materialHintText.text = BuildMaterialSummary(recipe);
            }

            ApplyIcon(recipe, def);
            RefreshCraftableState();
        }

        /// <summary>背包变化后可由外部调用以更新按钮。</summary>
        public void RefreshCraftableState()
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            bool can = _recipe != null && ForgeCraftService.CanCraft(inv, _recipe);
            if (clickButton != null)
            {
                clickButton.interactable = can;
            }
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

                var md = ItemCatalog.GetItemDef(m.ItemId);
                string name = md != null && !string.IsNullOrEmpty(md.DisplayName) ? md.DisplayName : m.ItemId;
                parts += name + "x" + m.Count;
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

            var sp = SimpleResManager.Load<Sprite>("Sprites/Item" + spriteName);
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

            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            if (!ForgeCraftService.CanCraft(inv, _recipe))
            {
                Debug.LogWarning("[Forge] Cannot craft: materials or bag space insufficient.");
                return;
            }

            if (!ForgeCraftService.TryCraft(_recipe, out var reasonEn))
            {
                Debug.LogWarning("[Forge] Craft failed: " + reasonEn);
                GetComponentInParent<ForgeCategorySection>()?.RefreshAllCellsCraftable();
                return;
            }

            Debug.Log("[Forge] Craft ok recipe id=" + _recipe.Id + " result=" + _recipe.ResultItemId);
            GetComponentInParent<ForgeCategorySection>()?.RefreshAllCellsCraftable();
        }
    }
}
