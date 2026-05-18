using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Forge
{
    [System.Serializable]
    public sealed class ForgeCategorySection
    {
        public TextMeshProUGUI categoryTitle;
        public TextMeshProUGUI emptyHint;
        public RectTransform grid;
    }

    public sealed class ForgePanel : PanelWithInput
    {
        public const string Pid = "ForgePanel";

        static readonly string[] CategoryTitles =
        {
            "武器锻造",
            "防具锻造",
            "杂项合成",
        };

        [SerializeField] RectTransform contentRoot;
        [SerializeField] ScrollRect mainScroll;
        [SerializeField] Button closeButton;
        [SerializeField] ForgeRecipeCell recipeCellTemplate;
        [SerializeField] ForgeCategorySection weaponSection;
        [SerializeField] ForgeCategorySection armorSection;
        [SerializeField] ForgeCategorySection miscSection;

        ForgeCategorySection[] _sections;

        public static void Toggle()
        {
            if (UIManager.Instance.IsPanelVisible(Pid))
            {
                UIManager.Instance.HidePanel(Pid);
            }
            else
            {
                UIManager.Instance.ShowPanel(Pid);
            }
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _sections = new[] { weaponSection, armorSection, miscSection };
            for (int i = 0; i < _sections.Length && i < CategoryTitles.Length; i++)
            {
                var s = _sections[i];
                if (s != null && s.categoryTitle != null && string.IsNullOrEmpty(s.categoryTitle.text))
                {
                    s.categoryTitle.text = CategoryTitles[i];
                }
            }

            if (recipeCellTemplate != null)
            {
                recipeCellTemplate.gameObject.SetActive(false);
            }

            if (contentRoot == null && mainScroll != null)
            {
                contentRoot = mainScroll.content;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => UIManager.Instance.HidePanel(Pid));
            }
        }

        public override void Show()
        {
            base.Show();
            RefreshRecipes();
        }

        public override bool OnCancel()
        {
            UIManager.Instance.HidePanel(Pid);
            return true;
        }

        void RefreshRecipes()
        {
            if (_sections == null || _sections.Length != 3)
            {
                return;
            }

            var table = CfgMgr.Cfgs?.TbForgeRecipe;
            if (table?.DataList == null)
            {
                return;
            }

            var byType = new Dictionary<EForgeRecipeType, List<ForgeRecipe>>
            {
                [EForgeRecipeType.Weapon] = new List<ForgeRecipe>(),
                [EForgeRecipeType.Armor] = new List<ForgeRecipe>(),
                [EForgeRecipeType.Misc] = new List<ForgeRecipe>(),
            };

            foreach (var row in table.DataList)
            {
                if (row == null || !ForgeRecipeUnlockUtil.IsUnlocked(row))
                {
                    continue;
                }

                if (!byType.TryGetValue(row.RecipeType, out var list))
                {
                    continue;
                }

                list.Add(row);
            }

            foreach (var list in byType.Values)
            {
                list.Sort(static (a, b) =>
                {
                    int c = a.Sort.CompareTo(b.Sort);
                    return c != 0 ? c : a.Id.CompareTo(b.Id);
                });
            }

            int ti = 0;
            foreach (EForgeRecipeType t in new[] { EForgeRecipeType.Weapon, EForgeRecipeType.Armor, EForgeRecipeType.Misc })
            {
                var sec = _sections[ti];
                if (sec != null && sec.grid != null && sec.emptyHint != null)
                {
                    FillGrid(sec.grid, sec.emptyHint, byType[t]);
                }

                ti++;
            }

            if (contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }
        }

        void FillGrid(RectTransform grid, TextMeshProUGUI emptyHint, List<ForgeRecipe> recipes)
        {
            if (grid == null || emptyHint == null || recipeCellTemplate == null)
            {
                return;
            }

            for (int i = grid.childCount - 1; i >= 0; i--)
            {
                Destroy(grid.GetChild(i).gameObject);
            }

            var glg = grid.GetComponent<GridLayoutGroup>();
            float cellH = glg != null ? glg.cellSize.y : 124f;
            float cellW = glg != null ? glg.cellSize.x : 108f;
            float spacingY = glg != null ? glg.spacing.y : 8f;
            float spacingX = glg != null ? glg.spacing.x : 8f;
            int columns = glg != null && glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.Max(1, glg.constraintCount)
                : 3;

            if (recipes == null || recipes.Count == 0)
            {
                emptyHint.gameObject.SetActive(true);
                grid.sizeDelta = new Vector2(grid.sizeDelta.x, cellH + spacingY);
                return;
            }

            emptyHint.gameObject.SetActive(false);
            foreach (var r in recipes)
            {
                var go = Instantiate(recipeCellTemplate.gameObject, grid, false);
                go.SetActive(true);
                var cell = go.GetComponent<ForgeRecipeCell>();
                if (cell != null)
                {
                    cell.Bind(r);
                }
            }

            int rows = Mathf.CeilToInt(recipes.Count / (float)columns);
            float h = rows * (cellH + spacingY) + spacingY;
            grid.sizeDelta = new Vector2(grid.sizeDelta.x, Mathf.Max(cellH + spacingY, h));
        }
    }
}
