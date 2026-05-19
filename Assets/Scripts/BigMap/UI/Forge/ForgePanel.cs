using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.UI;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Forge
{
    public sealed class ForgePanel : PanelWithInput
    {
        public const string Pid = "ForgePanel";

        static readonly string[] CategoryTitles =
        {
            "武器锻造",
            "防具锻造",
            "杂项合成",
        };

        static readonly EForgeRecipeType[] SectionTypes =
        {
            EForgeRecipeType.Weapon,
            EForgeRecipeType.Armor,
            EForgeRecipeType.Misc,
        };

        [SerializeField] RectTransform contentRoot;
        [SerializeField] ScrollRect mainScroll;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform sectionsRoot;
        [SerializeField] GameObject categorySectionTemplate;

        Coroutine _layoutScrollCo;
        PlayerInventorySystem _invEventsBound;

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

        void OnDestroy()
        {
            UnbindInventoryEvents();
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
            BindInventoryEventsIfNeeded();
            RefreshRecipes();
            ScheduleLayoutAndScrollTop();
        }

        public override void Hide()
        {
            UnbindInventoryEvents();
            base.Hide();
        }

        void BindInventoryEventsIfNeeded()
        {
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            if (inv == _invEventsBound)
            {
                return;
            }

            UnbindInventoryEvents();
            _invEventsBound = inv;
            if (_invEventsBound != null)
            {
                _invEventsBound.EventOnGainItem += OnInventoryMutated;
            }
        }

        void UnbindInventoryEvents()
        {
            if (_invEventsBound != null)
            {
                _invEventsBound.EventOnGainItem -= OnInventoryMutated;
            }

            _invEventsBound = null;
        }

        void OnInventoryMutated(EPlayerBagId _, string __, long ___)
        {
            RefreshCraftableOnVisibleCells();
        }

        void RefreshCraftableOnVisibleCells()
        {
            if (sectionsRoot == null)
            {
                return;
            }

            for (int i = 0; i < sectionsRoot.childCount; i++)
            {
                var sec = sectionsRoot.GetChild(i).GetComponent<ForgeCategorySection>();
                sec?.RefreshAllCellsCraftable();
            }
        }

        void ScheduleLayoutAndScrollTop()
        {
            if (_layoutScrollCo != null)
            {
                StopCoroutine(_layoutScrollCo);
            }

            _layoutScrollCo = StartCoroutine(CoDeferredLayoutAndScrollTop());
        }

        IEnumerator CoDeferredLayoutAndScrollTop()
        {
            yield return null;
            RebuildForgeLayout();
            yield return null;
            RebuildForgeLayout();
            if (mainScroll != null)
            {
                mainScroll.verticalNormalizedPosition = 1f;
            }

            _layoutScrollCo = null;
        }

        public override bool OnCancel()
        {
            UIManager.Instance.HidePanel(Pid);
            return true;
        }

        void ClearInstantiatedSections()
        {
            if (sectionsRoot == null)
            {
                return;
            }

            for (int i = sectionsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(sectionsRoot.GetChild(i).gameObject);
            }
        }

        void RefreshRecipes()
        {
            ClearInstantiatedSections();

            if (sectionsRoot == null || categorySectionTemplate == null)
            {
                Debug.LogWarning("[ForgePanel] sectionsRoot or categorySectionTemplate missing; skip refresh.");
                return;
            }

            IReadOnlyList<ForgeRecipe> sourceRows = null;
            var cfgs = CfgMgr.Cfgs;
            if (cfgs != null)
            {
                var table = cfgs.TbForgeRecipe;
                if (table?.DataList != null)
                {
                    sourceRows = table.DataList;
                }
            }

            if (sourceRows == null)
            {
                Debug.LogWarning("[ForgePanel] TbForgeRecipe unavailable; showing empty sections.");
                sourceRows = new List<ForgeRecipe>();
            }

            var byType = new Dictionary<EForgeRecipeType, List<ForgeRecipe>>
            {
                [EForgeRecipeType.Weapon] = new List<ForgeRecipe>(),
                [EForgeRecipeType.Armor] = new List<ForgeRecipe>(),
                [EForgeRecipeType.Misc] = new List<ForgeRecipe>(),
            };

            foreach (var row in sourceRows)
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

            for (int i = 0; i < SectionTypes.Length; i++)
            {
                var t = SectionTypes[i];
                byType.TryGetValue(t, out var list);
                if (list == null)
                {
                    list = new List<ForgeRecipe>();
                }

                var go = Instantiate(categorySectionTemplate, sectionsRoot, false);
                go.SetActive(true);
                var sec = go.GetComponent<ForgeCategorySection>();
                if (sec != null)
                {
                    sec.Init(t, CategoryTitles[i]);
                    sec.RefreshRecipes(list);
                }
            }

            RebuildForgeLayout();
        }

        void RebuildForgeLayout()
        {
            if (sectionsRoot != null)
            {
                for (int i = 0; i < sectionsRoot.childCount; i++)
                {
                    var sec = sectionsRoot.GetChild(i).GetComponent<ForgeCategorySection>();
                    sec?.RebuildLayoutSelf();
                }
            }

            if (contentRoot != null)
            {
                for (int i = 0; i < contentRoot.childCount; i++)
                {
                    if (contentRoot.GetChild(i) is RectTransform childRt)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(childRt);
                    }
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
