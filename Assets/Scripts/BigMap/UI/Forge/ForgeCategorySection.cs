using System.Collections.Generic;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Forge
{
    // 分区：标题、空态、Grid；格子由与本组件同预制体内的 RecipeCell_Template 克隆生成。
    // 高度由 VerticalLayoutGroup + GridLayoutGroup + ContentSizeFitter（prefab）驱动，无脚本写死高。
    public sealed class ForgeCategorySection : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI categoryTitle;
        [SerializeField] TextMeshProUGUI emptyHint;
        [SerializeField] RectTransform grid;
        [SerializeField] ForgeRecipeCell cellTemplate;

        public EForgeRecipeType RecipeType { get; private set; }

        public void Init(EForgeRecipeType type, string titleText)
        {
            RecipeType = type;
            if (categoryTitle != null)
            {
                categoryTitle.text = titleText;
            }
        }

        public void RefreshRecipes(List<ForgeRecipe> recipes)
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

            int n = recipes != null ? recipes.Count : 0;
            for (int i = 0; i < n; i++)
            {
                var go = Instantiate(cellTemplate.gameObject, grid, false);
                go.SetActive(true);
                var cell = go.GetComponent<ForgeRecipeCell>();
                if (cell != null)
                {
                    cell.Bind(recipes[i]);
                }
            }

            if (emptyHint != null)
            {
                emptyHint.gameObject.SetActive(n == 0);
            }

            RebuildLayoutSelf();
        }

        public void RefreshAllCellsCraftable()
        {
            if (grid == null)
            {
                return;
            }

            for (int i = 0; i < grid.childCount; i++)
            {
                grid.GetChild(i).GetComponent<ForgeRecipeCell>()?.RefreshCraftableState();
            }
        }

        // 子物体数量变化后让布局系统立即算一遍 preferred，避免 Scroll/ContentSizeFitter 延后一帧不齐。
        public void RebuildLayoutSelf()
        {
            if (grid == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);

            var stackRt = grid.parent as RectTransform;
            if (stackRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(stackRt);
            }

            var rootRt = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRt);

            if (rootRt.parent is RectTransform sectionsRoot)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sectionsRoot);
            }
        }
    }
}
