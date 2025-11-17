using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FakeHintTextManager : MonoBehaviour
{
    private static FakeHintTextManager _instance;
    private Canvas _canvas;

    [Header("Defaults")]
    public Font defaultFont;
    public int defaultFontSize = 24;
    public Color defaultColor = Color.white;

    [Header("Motion")]
    public Vector2 startOffset = Vector2.zero;      // 初始屏幕偏移
    public Vector2 drift = new Vector2(0, 60f);     // 每秒向上的漂移像素
    public float lifetime = 1.2f;                   // 存活时间
    public float fadeOutTime = 0.8f;                // 淡出时长

    // 管理的飘字实例
    private readonly List<FloatingItem> _items = new List<FloatingItem>();
    private readonly Queue<int> _toRemove = new Queue<int>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCanvas();
    }

    void EnsureCanvas()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            GameObject cv = new GameObject("FloatingTextCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = cv.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = cv.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }
    }

    // 统一驱动的 Update（你也可以改成在自己的全局 Tick 中手动调用 TickAll(Time.deltaTime)）
    void Update()
    {
        TickAll(Time.deltaTime);
    }

    // 对外接口：屏幕坐标
    public static void Show(string text, Vector2 screenPosition, Color? color = null, int? fontSize = null)
    {
        var inst = GetOrCreateInstance();
        inst.CreateItem(text, screenPosition, color ?? inst.defaultColor, fontSize ?? inst.defaultFontSize);
    }

    // 对外接口：世界坐标
    public static void ShowWorld(string text, Vector3 worldPosition, Camera cam = null, Color? color = null, int? fontSize = null)
    {
        var inst = GetOrCreateInstance();
        cam = cam ? cam : Camera.main;
        Vector2 screenPos = cam ? (Vector2)cam.WorldToScreenPoint(worldPosition) : (Vector2)worldPosition;
        Show(text, screenPos, color, fontSize);
    }

    private static FakeHintTextManager GetOrCreateInstance()
    {
        if (_instance != null) return _instance;
        GameObject go = new GameObject("FloatingTextManager_Auto", typeof(FakeHintTextManager));
        _instance = go.GetComponent<FakeHintTextManager>();
        _instance.EnsureCanvas();
        return _instance;
    }

    // 创建一个飘字数据项并生成其 UI
    private void CreateItem(string message, Vector2 screenPos, Color color, int fontSize)
    {
        GameObject go = new GameObject("FloatingText", typeof(Text));
        go.transform.SetParent(_canvas.transform, false);

        var txt = go.GetComponent<Text>();
        txt.text = message;
        txt.font = defaultFont ? defaultFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0); // 屏幕像素定位
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = screenPos + startOffset;

        // 注册到列表，由管理器统一 Tick
        _items.Add(new FloatingItem
        {
            go = go,
            rt = rt,
            txt = txt,
            driftPerSecond = drift,
            lifetime = lifetime,
            fadeOutTime = fadeOutTime,
            elapsed = 0f
        });
    }

    // 统一驱动所有飘字
    public void TickAll(float dt)
    {
        _toRemove.Clear();
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            item.elapsed += dt;

            // 位置漂移
            item.rt.anchoredPosition += item.driftPerSecond * dt;

            // 淡出
            float fadeStart = Mathf.Max(0f, item.lifetime - item.fadeOutTime);
            if (item.elapsed >= fadeStart)
            {
                float a = Mathf.InverseLerp(item.lifetime, fadeStart, item.elapsed);
                var c = item.txt.color;
                c.a = a;
                item.txt.color = c;
            }

            // 结束
            if (item.elapsed >= item.lifetime)
            {
                if (item.go) Destroy(item.go);
                _items.RemoveAt(i);
                //_toRemove.Enqueue(i);
            }
            else
            {
                _items[i] = item; // 结构体或类都安全，这里保持赋值
            }
        }

        //// 从后往前删除，避免索引错乱
        //while (_toRemove.Count > 0)
        //{
        //    int idx = _toRemove.Dequeue();
        //    if (idx >= 0 && idx < _items.Count)
        //    {
        //        _items.RemoveAt(idx);
        //        // 注意：移除后索引变化，本轮我们只记录将要删除的索引并逐个处理，
        //        // 如果担心索引错乱，可改为倒序遍历或记录对象引用再统一清理。
        //    }
        //}
    }

    // 飘字数据结构（非 MonoBehaviour，纯数据，由管理器驱动）
    private struct FloatingItem
    {
        public GameObject go;
        public RectTransform rt;
        public Text txt;

        public Vector2 driftPerSecond;
        public float lifetime;
        public float fadeOutTime;
        public float elapsed;
    }
}
