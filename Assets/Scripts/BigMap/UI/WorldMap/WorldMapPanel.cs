using System.Collections;
using System.Collections.Generic;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    /// <summary>
    /// 全屏大地图：底图 + 玩家与重要地标标记；M / Esc / Cancel 关闭
    /// </summary>
    public class WorldMapPanel : PanelWithInput
    {
        [SerializeField] private Image dimBackground;
        [SerializeField] private Image mapImage;
        [SerializeField] private RectTransform markersRoot;
        [SerializeField] private TextMeshProUGUI closeHintText;

        private readonly List<GameObject> spawnedMarkers = new();
        private WorldMapViewContext boundContext;

        public override bool CanFocus => true;
        public override int FocusPriority => 850;

        private void Update()
        {
            if (!IsVisible) return;
            // M 由 QuickPlayerInputBinder 的 TryToggle 关闭，避免与打开同一帧重复触发
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                UIManager.Instance?.HidePanel(WorldMapRuntime.PanelId);
        }

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (dimBackground == null) dimBackground = transform.Find("Dim")?.GetComponent<Image>();
            if (mapImage == null) mapImage = transform.Find("MapViewport/MapImage")?.GetComponent<Image>();
            if (markersRoot == null) markersRoot = transform.Find("MapViewport/MarkersRoot") as RectTransform;
            if (closeHintText == null) closeHintText = transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
        }

        public override void Setup(object data = null)
        {
            ClearMarkers();
            boundContext = data as WorldMapViewContext;
            if (boundContext == null) return;

            if (mapImage != null)
            {
                mapImage.sprite = boundContext.MapSprite;
                mapImage.color = boundContext.MapSprite != null ? Color.white : new Color(0.2f, 0.22f, 0.28f, 1f);
            }

            if (closeHintText != null)
                closeHintText.text = "M / Esc 关闭";

            if (dimBackground != null)
                dimBackground.color = new Color(0f, 0f, 0f, 0.55f);
        }

        public override void Show()
        {
            base.Show();
            StopAllCoroutines();
            StartCoroutine(RebuildMarkersWhenLayoutReady());
        }

        private IEnumerator RebuildMarkersWhenLayoutReady()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (boundContext == null || markersRoot == null) yield break;
            ClearMarkers();
            var rect = markersRoot.rect;
            foreach (var m in boundContext.Markers)
            {
                var go = CreateMarker(m, rect);
                spawnedMarkers.Add(go);
            }
        }

        private GameObject CreateMarker(WorldMapMarkerData data, Rect containerRect)
        {
            var go = new GameObject($"wm_{data.kind}_{spawnedMarkers.Count}", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(markersRoot, false);
            rt.sizeDelta = new Vector2(14f, 14f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = WorldToAnchored(data.worldPos, containerRect);

            var img = go.AddComponent<Image>();
            img.color = ColorForKind(data.kind);
            img.raycastTarget = false;

            if (!string.IsNullOrEmpty(data.label))
            {
                var textGo = new GameObject("label", typeof(RectTransform));
                var trt = (RectTransform)textGo.transform;
                trt.SetParent(rt, false);
                trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0f);
                trt.anchoredPosition = new Vector2(0f, 14f);
                trt.sizeDelta = new Vector2(160f, 28f);
                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = data.label;
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }

            return go;
        }

        private static Color ColorForKind(WorldMapLandmarkKind k)
        {
            return k switch
            {
                WorldMapLandmarkKind.Player => new Color(0.3f, 1f, 0.45f),
                WorldMapLandmarkKind.MajorInteract => new Color(0.45f, 0.75f, 1f),
                WorldMapLandmarkKind.MajorBoss => new Color(1f, 0.45f, 0.35f),
                _ => Color.gray
            };
        }

        private Vector2 WorldToAnchored(Vector2 worldPos, Rect containerRect)
        {
            if (boundContext == null) return Vector2.zero;
            var wMin = boundContext.WorldMin;
            var wMax = boundContext.WorldMax;
            if (Mathf.Approximately(wMax.x, wMin.x)) wMax.x = wMin.x + 0.01f;
            if (Mathf.Approximately(wMax.y, wMin.y)) wMax.y = wMin.y + 0.01f;

            float nx = Mathf.InverseLerp(wMin.x, wMax.x, worldPos.x);
            float ny = Mathf.InverseLerp(wMin.y, wMax.y, worldPos.y);
            nx = Mathf.Clamp01(nx);
            ny = Mathf.Clamp01(ny);

            return new Vector2(
                (nx - 0.5f) * containerRect.width,
                (ny - 0.5f) * containerRect.height);
        }

        private void ClearMarkers()
        {
            foreach (var g in spawnedMarkers)
            {
                if (g != null) Destroy(g);
            }
            spawnedMarkers.Clear();
        }

        public override void Teardown()
        {
            ClearMarkers();
            boundContext = null;
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(WorldMapRuntime.PanelId);
            return true;
        }

        public override bool OnHotkey(string keyName)
        {
            if (keyName == "M")
            {
                UIManager.Instance?.HidePanel(WorldMapRuntime.PanelId);
                return true;
            }
            return false;
        }
    }
}
