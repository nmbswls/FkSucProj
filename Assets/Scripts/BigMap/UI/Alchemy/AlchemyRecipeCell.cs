using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player.Alchemy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyRecipeCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI requirementText;
        [SerializeField] Button clickButton;

        AlchemyRecipe _recipe;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void Bind(AlchemyRecipe recipe)
        {
            _recipe = recipe;
            if (recipe == null)
            {
                return;
            }

            var def = ItemCatalog.GetItemDef(recipe.ResultItemId);
            if (titleText != null)
            {
                titleText.text = !string.IsNullOrEmpty(recipe.DisplayName)
                    ? recipe.DisplayName
                    : (def?.DisplayName ?? recipe.ResultItemId);
            }

            if (requirementText != null)
            {
                requirementText.text = BuildRequirementSummary(recipe);
            }

            ApplyIcon(def?.SpriteName);
            RefreshCraftableState();
        }

        public void RefreshCraftableState()
        {
            var panel = GetComponentInParent<AlchemyPanel>();
            bool can = panel != null && _recipe != null && panel.CanCraftRecipe(_recipe);
            if (clickButton != null)
            {
                clickButton.interactable = can;
            }
        }

        static string BuildRequirementSummary(AlchemyRecipe recipe)
        {
            var parts = new List<string>();
            if (recipe.VirtueRequirements != null)
            {
                for (int i = 0; i < recipe.VirtueRequirements.Count; i++)
                {
                    var req = recipe.VirtueRequirements[i];
                    if (req == null || req.VirtueId <= 0 || req.Value <= 0)
                    {
                        continue;
                    }

                    var def = AlchemyCatalog.GetVirtueDef(req.VirtueId);
                    parts.Add((def?.DisplayName ?? req.VirtueId.ToString()) + ">=" + req.Value);
                }
            }

            if (recipe.AspectRequirements != null)
            {
                for (int i = 0; i < recipe.AspectRequirements.Count; i++)
                {
                    var req = recipe.AspectRequirements[i];
                    if (req == null || req.AspectId <= 0 || req.Value <= 0)
                    {
                        continue;
                    }

                    var def = AlchemyCatalog.GetAspectDef(req.AspectId);
                    parts.Add((def?.DisplayName ?? req.AspectId.ToString()) + ">=" + req.Value);
                }
            }

            if (recipe.FixedMaterials != null)
            {
                for (int i = 0; i < recipe.FixedMaterials.Count; i++)
                {
                    var req = recipe.FixedMaterials[i];
                    if (req == null || string.IsNullOrEmpty(req.ItemId) || req.Count <= 0)
                    {
                        continue;
                    }

                    var def = ItemCatalog.GetItemDef(req.ItemId);
                    parts.Add((def?.DisplayName ?? req.ItemId) + "x" + req.Count);
                }
            }

            return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
        }

        void ApplyIcon(string spriteName)
        {
            if (icon == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                icon.enabled = false;
                return;
            }

            var sp = SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
            icon.sprite = sp;
            icon.enabled = sp != null;
        }

        void OnClicked()
        {
            GetComponentInParent<AlchemyPanel>()?.TryCraftRecipe(_recipe);
        }
    }
}
