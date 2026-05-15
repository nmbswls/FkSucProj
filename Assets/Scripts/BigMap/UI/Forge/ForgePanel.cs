using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Forge
{
    public sealed class ForgePanel : PanelWithInput
    {
        public const string Pid = "ForgePanel";

        const int GridColumns = 3;
        const float CellW = 108f;
        const float CellH = 124f;
        const float GridSpacing = 8f;

        static readonly string[] CategoryTitles =
        {
            "武器锻造",
            "防具锻造",
            "杂项合成",
        };

        [SerializeField] Button closeButton;

        bool _runtimeLayoutBuilt;
        RectTransform _contentRoot;

        readonly List<RectTransform> _gridRects = new();
        readonly List<TextMeshProUGUI> _emptyHints = new();

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

            EnsureBuiltRuntime();
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

        void EnsureBuiltRuntime()
        {
            if (_runtimeLayoutBuilt)
            {
                return;
            }

            _runtimeLayoutBuilt = true;

            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = CreateImage("Dim", transform, new Color(0f, 0f, 0f, 0.55f));
            StretchFull(bg.rectTransform);

            var panel = CreateImage("PanelBody", transform, new Color(0.12f, 0.12f, 0.14f, 1f));
            var pr = panel.rectTransform;
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(720f, 520f);
            pr.anchoredPosition = Vector2.zero;

            var title = CreateTmp("Title", pr, "锻造", 28, TextAlignmentOptions.Center);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 48f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(pr, false);
            var closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(40f, 40f);
            closeRt.anchoredPosition = new Vector2(-8f, -8f);
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            closeButton = closeGo.GetComponent<Button>();
            var closeLabel = CreateTmp("CloseLabel", closeRt, "X", 20, TextAlignmentOptions.Center);
            StretchFull(closeLabel.rectTransform);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(pr, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(16f, 16f);
            scrollRt.offsetMax = new Vector2(-16f, -56f);
            scrollGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.09f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollRt, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            StretchFull(vpRt);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(vpRt, false);
            var contentRt = content.GetComponent<RectTransform>();
            StretchFull(contentRt);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            _contentRoot = contentRt;
            _gridRects.Clear();
            _emptyHints.Clear();

            for (int i = 0; i < 3; i++)
            {
                var sec = new GameObject("Section_" + i, typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
                sec.transform.SetParent(contentRt, false);
                var secRt = sec.GetComponent<RectTransform>();
                secRt.sizeDelta = new Vector2(0f, 160f);
                var le = sec.GetComponent<LayoutElement>();
                le.minHeight = 120f;
                le.preferredWidth = -1f;
                var secVlg = sec.GetComponent<VerticalLayoutGroup>();
                secVlg.spacing = 6f;
                secVlg.childAlignment = TextAnchor.UpperLeft;
                secVlg.childControlHeight = true;
                secVlg.childForceExpandHeight = false;
                secVlg.childControlWidth = true;
                secVlg.childForceExpandWidth = true;

                var catTitle = CreateTmp("CatTitle", secRt, CategoryTitles[i], 22, TextAlignmentOptions.Left);
                var catRt = catTitle.rectTransform;
                catRt.sizeDelta = new Vector2(0f, 32f);

                var hint = CreateTmp("EmptyHint", secRt, "暂无配方", 16, TextAlignmentOptions.Left);
                hint.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                var hintRt = hint.rectTransform;
                hintRt.sizeDelta = new Vector2(0f, 24f);
                _emptyHints.Add(hint);

                var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
                gridGo.transform.SetParent(secRt, false);
                var gridRt = gridGo.GetComponent<RectTransform>();
                gridRt.sizeDelta = new Vector2(0f, CellH + GridSpacing);
                var grid = gridGo.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(CellW, CellH);
                grid.spacing = new Vector2(GridSpacing, GridSpacing);
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = GridColumns;
                _gridRects.Add(gridRt);
            }
        }

        void RefreshRecipes()
        {
            if (_gridRects.Count != 3 || _emptyHints.Count != 3)
            {
                return;
            }

            var table = CfgMgr.Cfgs?.TbForgeRecipe;
            if (table?.DataList == null)
            {
                return;
            }

            var byType = new Dictionary<EForgeRecipeType, List<ForgeRecipe>>();
            byType[EForgeRecipeType.Weapon] = new List<ForgeRecipe>();
            byType[EForgeRecipeType.Armor] = new List<ForgeRecipe>();
            byType[EForgeRecipeType.Misc] = new List<ForgeRecipe>();

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
                FillGrid(_gridRects[ti], _emptyHints[ti], byType[t]);
                ti++;
            }

            if (_contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            }
        }

        void FillGrid(RectTransform grid, TextMeshProUGUI emptyHint, List<ForgeRecipe> recipes)
        {
            for (int i = grid.childCount - 1; i >= 0; i--)
            {
                Destroy(grid.GetChild(i).gameObject);
            }

            if (recipes == null || recipes.Count == 0)
            {
                emptyHint.gameObject.SetActive(true);
                grid.sizeDelta = new Vector2(grid.sizeDelta.x, CellH + GridSpacing);
                return;
            }

            emptyHint.gameObject.SetActive(false);
            foreach (var r in recipes)
            {
                CreateCell(grid, r);
            }

            int rows = Mathf.CeilToInt(recipes.Count / (float)GridColumns);
            float h = rows * (CellH + GridSpacing) + GridSpacing;
            grid.sizeDelta = new Vector2(grid.sizeDelta.x, Mathf.Max(CellH + GridSpacing, h));
        }

        void CreateCell(RectTransform grid, ForgeRecipe recipe)
        {
            var go = new GameObject("Cell_" + recipe.Id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(grid, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.26f, 1f);
            var btn = go.GetComponent<Button>();

            var title = CreateTmp("Title", go.GetComponent<RectTransform>(), "", 14, TextAlignmentOptions.TopLeft);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 0.35f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(6f, 0f);
            titleRt.offsetMax = new Vector2(-6f, -4f);

            var mat = CreateTmp("Mat", go.GetComponent<RectTransform>(), "", 11, TextAlignmentOptions.BottomLeft);
            mat.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            var matRt = mat.rectTransform;
            matRt.anchorMin = new Vector2(0f, 0f);
            matRt.anchorMax = new Vector2(1f, 0.35f);
            matRt.offsetMin = new Vector2(4f, 4f);
            matRt.offsetMax = new Vector2(-4f, 0f);

            var cell = go.AddComponent<ForgeRecipeCell>();
            cell.WireRefs(img, title, mat, btn);
            cell.Bind(recipe);
        }

        static Image CreateImage(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = c;
            return img;
        }

        static void StretchFull(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        static TextMeshProUGUI CreateTmp(string name, Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }
    }
}
