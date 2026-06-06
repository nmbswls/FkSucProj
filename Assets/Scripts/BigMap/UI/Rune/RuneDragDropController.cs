using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Rune
{
    // 模仿 ItemDragDropController，由 RunePanel 挂载并驱动
    public sealed class RuneDragDropController : MonoBehaviour
    {
        public static RuneDragDropController Instance { get; private set; }

        public RectTransform DragGhostRoot;
        public Image DragGhostImage;
        public Canvas TopCanvas;

        public RuneDragPayload Payload { get; private set; }
        public bool IsDragging { get; private set; }

        bool _dropHandledThisDrag;
        Vector2 _lastDragScreenPos;

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            EndDrag();
        }

        void Awake()
        {
            EnsureDragGhost();
            if (TopCanvas == null)
            {
                TopCanvas = GetComponentInParent<Canvas>();
            }
        }

        void EnsureDragGhost()
        {
            if (DragGhostRoot != null)
            {
                DragGhostRoot.gameObject.SetActive(false);
                ConfigureGhostRaycast(DragGhostRoot.gameObject);
                return;
            }

            var go = new GameObject("RuneDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            DragGhostRoot = go.GetComponent<RectTransform>();
            DragGhostRoot.SetParent(transform, false);
            DragGhostRoot.sizeDelta = new Vector2(72f, 72f);
            DragGhostImage = go.GetComponent<Image>();
            DragGhostImage.raycastTarget = false;
            DragGhostRoot.gameObject.SetActive(false);
            ConfigureGhostRaycast(go);
        }

        static void ConfigureGhostRaycast(GameObject ghostRoot)
        {
            var cg = ghostRoot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = ghostRoot.AddComponent<CanvasGroup>();
            }

            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        public bool BeginDrag(RuneDragPayload payload)
        {
            if (IsDragging || payload == null || string.IsNullOrEmpty(payload.RuneId))
            {
                return false;
            }

            Payload = payload;
            IsDragging = true;
            _dropHandledThisDrag = false;
            _lastDragScreenPos = UnityEngine.Input.mousePosition;
            EnsureDragGhost();
            DragGhostRoot.gameObject.SetActive(true);
            ApplyDragGhostIcon(payload.RuneId);
            UpdateDrag(_lastDragScreenPos);
            return true;
        }

        void ApplyDragGhostIcon(string runeId)
        {
            if (DragGhostImage == null)
            {
                return;
            }

            var def = RuneCatalog.GetOrDefault(runeId);
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.Icon))
            {
                sprite = SimpleResManager.Load<Sprite>(def.Icon);
            }

            DragGhostImage.sprite = sprite;
            DragGhostImage.enabled = sprite != null;
        }

        public void MarkDropHandled()
        {
            _dropHandledThisDrag = true;
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (!IsDragging || DragGhostRoot == null || TopCanvas == null)
            {
                return;
            }

            _lastDragScreenPos = screenPos;
            var canvasRect = TopCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                TopCanvas.worldCamera,
                out Vector2 localOnCanvas);
            DragGhostRoot.localPosition = localOnCanvas;
        }

        public void EndDrag(Vector2? screenPos = null)
        {
            if (IsDragging && Payload != null && !_dropHandledThisDrag)
            {
                TryHandleDropAtScreen(screenPos ?? _lastDragScreenPos);
            }

            IsDragging = false;
            _dropHandledThisDrag = false;
            Payload = null;
            if (DragGhostRoot != null)
            {
                DragGhostRoot.gameObject.SetActive(false);
            }
        }

        void TryHandleDropAtScreen(Vector2 screenPos)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            var pointerData = new PointerEventData(eventSystem) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var hit = results[i];
                if (hit.gameObject == null || hit.gameObject.transform.IsChildOf(transform))
                {
                    continue;
                }

                var slot = hit.gameObject.GetComponentInParent<RuneSlotView>();
                if (slot != null)
                {
                    slot.OnDrop(pointerData);
                    if (_dropHandledThisDrag)
                    {
                        return;
                    }
                }

                var cell = hit.gameObject.GetComponentInParent<RuneCellBase>();
                if (cell != null)
                {
                    cell.TryHandleExternalDrop(this);
                    if (_dropHandledThisDrag)
                    {
                        return;
                    }
                }
            }
        }
    }
}
