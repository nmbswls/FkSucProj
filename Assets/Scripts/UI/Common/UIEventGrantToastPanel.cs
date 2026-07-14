using System.Collections;
using System.Collections.Generic;
using My.Quest;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // Steam 风格成就/通识解锁 toast（右下角队列）
    public class UIEventGrantToastPanel : PanelBase
    {
        public const string PanelIdConst = "EventGrantToastPanel";

        public static UIEventGrantToastPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as UIEventGrantToastPanel;
            }
        }

        // 确保面板已按正常 Register/Show 路径打开，再入队一条 toast
        public static void ShowToast(string title, string subtitle, string body, Sprite icon = null)
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.ShowPanel(PanelIdConst);
            Instance?.Enqueue(title, subtitle, body, icon);
        }

        [Header("Settings")]
        public GameObject toastItemPrefab;
        public Transform toastContainer;
        public float spawnInterval = 0.45f;
        public int maxActiveItems = 3;

        readonly Queue<EventGrantToastData> _queue = new();
        readonly List<UIEventGrantToastItem> _active = new();
        bool _processing;
        bool _subscribed;

        struct EventGrantToastData
        {
            public string Title;
            public string Subtitle;
            public string Body;
            public Sprite Icon;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            EnsureRuntimeUi();
            BindBus();
        }

        public override void Show()
        {
            base.Show();
            EnsureRuntimeUi();
            BindBus();
        }

        public override void Hide()
        {
            UnbindBus();
            base.Hide();
        }

        public override void Teardown()
        {
            UnbindBus();
            base.Teardown();
        }

        void OnDestroy()
        {
            UnbindBus();
        }

        void EnsureRuntimeUi()
        {
            // toastContainer 由 prefab 挂好；仅条目模板可运行时兜底生成
            if (toastItemPrefab == null)
            {
                toastItemPrefab = BuildDefaultToastPrefab();
                toastItemPrefab.SetActive(false);
                toastItemPrefab.transform.SetParent(transform, false);
            }
        }

        static GameObject BuildDefaultToastPrefab()
        {
            var root = new GameObject("EventGrantToastItem", typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement), typeof(Image));
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(360f, 96f);
            var le = root.GetComponent<LayoutElement>();
            le.preferredHeight = 96f;
            le.minHeight = 96f;
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            bg.raycastTarget = false;

            var view = new GameObject("ViewRoot", typeof(RectTransform));
            var viewRt = view.GetComponent<RectTransform>();
            viewRt.SetParent(rootRt, false);
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = Vector2.zero;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(viewRt, false);
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(12f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.color = new Color(0.35f, 0.75f, 1f, 1f);
            iconImg.raycastTarget = false;

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            var titleGo = CreateTmp("Title", viewRt, font, 18f, FontStyles.Bold, new Vector2(80f, 52f), new Vector2(-12f, -8f));
            var subGo = CreateTmp("Subtitle", viewRt, font, 16f, FontStyles.Normal, new Vector2(80f, 28f), new Vector2(-12f, -32f));
            var bodyGo = CreateTmp("Body", viewRt, font, 13f, FontStyles.Normal, new Vector2(80f, 6f), new Vector2(-12f, -8f));
            bodyGo.color = new Color(0.75f, 0.78f, 0.85f, 1f);

            var item = root.AddComponent<UIEventGrantToastItem>();
            item.viewRoot = viewRt;
            item.iconImage = iconImg;
            item.titleText = titleGo;
            item.subtitleText = subGo;
            item.bodyText = bodyGo;
            return root;
        }

        static TextMeshProUGUI CreateTmp(string name, RectTransform parent, TMP_FontAsset font, float size, FontStyles style, Vector2 anchorPos, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(anchorPos.x, offsetMax.y);
            rt.offsetMax = new Vector2(offsetMax.x, -anchorPos.y);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        void BindBus()
        {
            if (_subscribed)
            {
                return;
            }

            PlayerEventBus.Subscribe<PlayerEventGrantClaimedEvent>(OnGrantClaimed);
            _subscribed = true;
        }

        void UnbindBus()
        {
            if (!_subscribed)
            {
                return;
            }

            PlayerEventBus.Unsubscribe<PlayerEventGrantClaimedEvent>(OnGrantClaimed);
            _subscribed = false;
        }

        void OnGrantClaimed(PlayerEventGrantClaimedEvent e)
        {
            string eyebrow = e.Category == cfg.demo.EEventGrantCategory.Knowledge
                ? "通识获取"
                : "成就解锁";
            ShowToast(eyebrow, e.Name, e.Desc, null);
        }

        public void Enqueue(string title, string subtitle, string body, Sprite icon)
        {
            _queue.Enqueue(new EventGrantToastData
            {
                Title = title ?? string.Empty,
                Subtitle = subtitle ?? string.Empty,
                Body = body ?? string.Empty,
                Icon = icon,
            });

            if (!_processing && isActiveAndEnabled)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        IEnumerator ProcessQueue()
        {
            _processing = true;
            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
                SpawnItem(data);
                yield return new WaitForSeconds(spawnInterval);
            }

            _processing = false;
        }

        void SpawnItem(EventGrantToastData data)
        {
            EnsureRuntimeUi();
            if (toastItemPrefab == null || toastContainer == null)
            {
                Debug.LogWarning("[UIEventGrantToastPanel] Missing prefab/container. Title=" + data.Title);
                return;
            }

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null)
                {
                    _active.RemoveAt(i);
                }
            }

            while (_active.Count >= maxActiveItems)
            {
                var oldest = _active[0];
                _active.RemoveAt(0);
                if (oldest != null)
                {
                    oldest.ForceExit();
                }
            }

            var go = Instantiate(toastItemPrefab, toastContainer);
            go.SetActive(true);
            var item = go.GetComponent<UIEventGrantToastItem>();
            if (item == null)
            {
                Destroy(go);
                return;
            }

            item.Initialize(data.Title, data.Subtitle, data.Body, data.Icon);
            _active.Add(item);
        }
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class UIEventGrantToastItem : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;
        public TextMeshProUGUI bodyText;
        public Image iconImage;
        public RectTransform viewRoot;
        public float slideDuration = 0.35f;
        public float lifeTime = 4.0f;
        public float fadeOutDuration = 0.45f;

        CanvasGroup _cg;
        bool _exiting;

        public void Initialize(string title, string subtitle, string body, Sprite icon)
        {
            if (titleText != null) titleText.text = title;
            if (subtitleText != null) subtitleText.text = subtitle;
            if (bodyText != null) bodyText.text = body;
            if (iconImage != null)
            {
                bool hasIcon = icon != null;
                iconImage.enabled = true;
                if (hasIcon)
                {
                    iconImage.sprite = icon;
                }
            }

            _cg = GetComponent<CanvasGroup>();
            _cg.alpha = 0f;
            if (viewRoot != null)
            {
                viewRoot.anchoredPosition = new Vector2(80f, 0f);
            }

            StartCoroutine(Animate());
        }

        public void ForceExit()
        {
            if (_exiting)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FadeAndDestroy(0.2f));
        }

        IEnumerator Animate()
        {
            float t = 0f;
            Vector2 from = viewRoot != null ? viewRoot.anchoredPosition : Vector2.zero;
            Vector2 to = Vector2.zero;
            while (t < slideDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / slideDuration);
                float e = 1f - (1f - u) * (1f - u);
                _cg.alpha = e;
                if (viewRoot != null)
                {
                    viewRoot.anchoredPosition = Vector2.Lerp(from, to, e);
                }

                yield return null;
            }

            _cg.alpha = 1f;
            if (viewRoot != null)
            {
                viewRoot.anchoredPosition = to;
            }

            yield return new WaitForSeconds(lifeTime);
            yield return FadeAndDestroy(fadeOutDuration);
        }

        IEnumerator FadeAndDestroy(float duration)
        {
            _exiting = true;
            float t = 0f;
            float start = _cg != null ? _cg.alpha : 1f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (_cg != null)
                {
                    _cg.alpha = Mathf.Lerp(start, 0f, t / duration);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
