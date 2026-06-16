using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class ProgressionHubTabBar : MonoBehaviour
    {
        public const int MaxVisibleTabCount = 4;
        const float TabWidth = 168f;
        const float ArrowWidth = 28f;

        static readonly Color ArrowNormalColor = new Color(0.55f, 0.58f, 0.65f, 1f);
        static readonly Color ArrowDisabledColor = new Color(0.35f, 0.36f, 0.4f, 0.45f);

        RectTransform _hubTabsRect;
        NonInteractiveScrollRect _scrollRect;
        RectTransform _viewport;
        RectTransform _content;
        HorizontalLayoutGroup _layout;
        Button _btnPrev;
        Button _btnNext;
        readonly List<RectTransform> _tabRects = new List<RectTransform>();
        int _firstVisibleIndex;

        public bool IsBuilt { get; private set; }

        public void BuildIfNeeded(Transform hubTabsRoot, IReadOnlyList<Button> orderedTabButtons)
        {
            if (hubTabsRoot == null || orderedTabButtons == null || orderedTabButtons.Count == 0)
            {
                return;
            }

            _hubTabsRect = hubTabsRoot as RectTransform;
            if (_hubTabsRect == null)
            {
                return;
            }

            EnsureScrollStructure(hubTabsRoot, orderedTabButtons);
            CacheTabRects(orderedTabButtons);
            RebuildLayout();
            RefreshArrowState();
        }

        public void NotifyTabSelected(int tabIndex)
        {
            if (!IsBuilt || _tabRects.Count == 0)
            {
                return;
            }

            tabIndex = Mathf.Clamp(tabIndex, 0, _tabRects.Count - 1);
            if (_tabRects.Count <= MaxVisibleTabCount)
            {
                _firstVisibleIndex = 0;
                ApplyScrollOffset();
                RefreshArrowState();
                return;
            }

            if (tabIndex < _firstVisibleIndex)
            {
                _firstVisibleIndex = tabIndex;
            }
            else if (tabIndex >= _firstVisibleIndex + MaxVisibleTabCount)
            {
                _firstVisibleIndex = tabIndex - MaxVisibleTabCount + 1;
            }

            _firstVisibleIndex = Mathf.Clamp(_firstVisibleIndex, 0, _tabRects.Count - MaxVisibleTabCount);
            ApplyScrollOffset();
            RefreshArrowState();
        }

        void EnsureScrollStructure(Transform hubTabsRoot, IReadOnlyList<Button> orderedTabButtons)
        {
            var scrollRoot = hubTabsRoot.Find("TabScrollArea");
            if (scrollRoot == null)
            {
                scrollRoot = CreateScrollRoot(hubTabsRoot);
            }

            _scrollRect = scrollRoot.GetComponent<NonInteractiveScrollRect>();
            if (_scrollRect == null)
            {
                _scrollRect = scrollRoot.gameObject.AddComponent<NonInteractiveScrollRect>();
            }

            _viewport = scrollRoot.Find("Viewport") as RectTransform;
            if (_viewport == null)
            {
                _viewport = CreateViewport(scrollRoot);
            }

            _content = _viewport.Find("Content") as RectTransform;
            if (_content == null)
            {
                _content = CreateContent(_viewport);
            }

            _layout = _content.GetComponent<HorizontalLayoutGroup>();
            if (_layout == null)
            {
                _layout = _content.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            ConfigureLayout(_layout);

            _btnPrev = EnsureArrowButton(hubTabsRoot, "BtnTabPrev", true);
            _btnNext = EnsureArrowButton(hubTabsRoot, "BtnTabNext", false);

            _btnPrev.onClick.RemoveAllListeners();
            _btnPrev.onClick.AddListener(() => ShiftVisible(-1));
            _btnNext.onClick.RemoveAllListeners();
            _btnNext.onClick.AddListener(() => ShiftVisible(1));

            ReparentTabs(_content, orderedTabButtons);
            ConfigureScrollRect(_scrollRect, _viewport, _content);
            ConfigureScrollRootLayout(scrollRoot as RectTransform, _btnPrev, _btnNext);
            IsBuilt = true;
        }

        static Transform CreateScrollRoot(Transform hubTabsRoot)
        {
            var go = new GameObject("TabScrollArea", typeof(RectTransform), typeof(NonInteractiveScrollRect));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hubTabsRoot, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(ArrowWidth, 0f);
            rt.offsetMax = new Vector2(-ArrowWidth, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        static RectTransform CreateViewport(Transform scrollRoot)
        {
            var go = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(scrollRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0.5f);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            var mask = go.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            return rt;
        }

        static RectTransform CreateContent(RectTransform viewport)
        {
            var go = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(viewport, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 48f);

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            return rt;
        }

        static void ConfigureLayout(HorizontalLayoutGroup layout)
        {
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 0f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);
        }

        static void ConfigureScrollRect(NonInteractiveScrollRect scrollRect, RectTransform viewport, RectTransform content)
        {
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false;
            scrollRect.elasticity = 0f;
            scrollRect.scrollSensitivity = 0f;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbar = null;
        }

        static void ConfigureScrollRootLayout(RectTransform scrollRoot, Button btnPrev, Button btnNext)
        {
            if (scrollRoot == null)
            {
                return;
            }

            scrollRoot.offsetMin = new Vector2(ArrowWidth, 0f);
            scrollRoot.offsetMax = new Vector2(-ArrowWidth, 0f);

            ConfigureArrowRect(btnPrev, true);
            ConfigureArrowRect(btnNext, false);
        }

        static void ConfigureArrowRect(Button button, bool isLeft)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(isLeft ? 0f : 1f, 0.5f);
            rt.anchorMax = new Vector2(isLeft ? 0f : 1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ArrowWidth, 48f);
            rt.anchoredPosition = new Vector2(isLeft ? ArrowWidth * 0.5f : -ArrowWidth * 0.5f, 0f);
        }

        static Button EnsureArrowButton(Transform hubTabsRoot, string name, bool isLeft)
        {
            var existing = hubTabsRoot.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(hubTabsRoot, false);

            var image = go.GetComponent<Image>();
            image.color = ArrowNormalColor;
            image.raycastTarget = true;

            var labelGo = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = isLeft ? "<" : ">";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22f;
            text.raycastTarget = false;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        static void ReparentTabs(RectTransform content, IReadOnlyList<Button> orderedTabButtons)
        {
            for (int i = 0; i < orderedTabButtons.Count; i++)
            {
                var button = orderedTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                var rt = button.GetComponent<RectTransform>();
                rt.SetParent(content, false);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(TabWidth, 48f);
                rt.anchoredPosition = Vector2.zero;

                var layoutElement = button.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = button.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.preferredWidth = TabWidth;
                layoutElement.preferredHeight = 48f;
            }
        }

        void CacheTabRects(IReadOnlyList<Button> orderedTabButtons)
        {
            _tabRects.Clear();
            for (int i = 0; i < orderedTabButtons.Count; i++)
            {
                var button = orderedTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                _tabRects.Add(button.GetComponent<RectTransform>());
            }
        }

        void RebuildLayout()
        {
            if (_content == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            if (_viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_viewport);
            }
        }

        void ShiftVisible(int delta)
        {
            if (_tabRects.Count <= MaxVisibleTabCount)
            {
                return;
            }

            _firstVisibleIndex = Mathf.Clamp(
                _firstVisibleIndex + delta,
                0,
                _tabRects.Count - MaxVisibleTabCount);
            ApplyScrollOffset();
            RefreshArrowState();
        }

        void ApplyScrollOffset()
        {
            if (_content == null || _tabRects.Count == 0)
            {
                return;
            }

            float offsetX = 0f;
            if (_firstVisibleIndex > 0 && _firstVisibleIndex < _tabRects.Count)
            {
                offsetX = _tabRects[_firstVisibleIndex].anchoredPosition.x;
            }

            _content.anchoredPosition = new Vector2(-offsetX, _content.anchoredPosition.y);
        }

        void RefreshArrowState()
        {
            bool needScroll = _tabRects.Count > MaxVisibleTabCount;
            if (_btnPrev != null)
            {
                _btnPrev.gameObject.SetActive(needScroll);
                _btnPrev.interactable = needScroll && _firstVisibleIndex > 0;
                ApplyArrowColor(_btnPrev);
            }

            if (_btnNext != null)
            {
                _btnNext.gameObject.SetActive(needScroll);
                _btnNext.interactable = needScroll && _firstVisibleIndex < _tabRects.Count - MaxVisibleTabCount;
                ApplyArrowColor(_btnNext);
            }
        }

        static void ApplyArrowColor(Button button)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = button.interactable ? ArrowNormalColor : ArrowDisabledColor;
            }
        }
    }
}
