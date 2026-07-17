using System.Collections.Generic;
using cfg.demo;
using My.Player.Alchemy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    // 按 tier 分块的配方区，格子由 prefab 内模板克隆。
    public sealed class AlchemyRecipeTierSection : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tierTitle;
        [SerializeField] TextMeshProUGUI emptyHint;
        [SerializeField] RectTransform grid;
        [SerializeField] AlchemyRecipeCell cellTemplate;

        public int Tier { get; private set; }

        void Awake()
        {
            if (cellTemplate != null)
            {
                cellTemplate.gameObject.SetActive(false);
            }
        }

        public void Init(int tier, string title)
        {
            Tier = tier;
            if (tierTitle != null)
            {
                tierTitle.text = title;
            }
        }

        public void RefreshRecipes(IReadOnlyList<AlchemyRecipe> recipes)
        {
            if (grid == null || cellTemplate == null)
            {
                return;
            }

            var tpl = cellTemplate.transform;
            for (int i = grid.childCount - 1; i >= 0; i--)
            {
                var ch = grid.GetChild(i);
                if (ch == tpl)
                {
                    continue;
                }

                Destroy(ch.gameObject);
            }

            int count = recipes != null ? recipes.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(cellTemplate.gameObject, grid, false);
                go.SetActive(true);
                go.GetComponent<AlchemyRecipeCell>()?.Bind(recipes[i]);
            }

            if (emptyHint != null)
            {
                emptyHint.gameObject.SetActive(count == 0);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        }

        public void RefreshAllCellsCraftable()
        {
            if (grid == null)
            {
                return;
            }

            for (int i = 0; i < grid.childCount; i++)
            {
                grid.GetChild(i).GetComponent<AlchemyRecipeCell>()?.RefreshCraftableState();
            }
        }
    }
}
