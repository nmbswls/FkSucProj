using My.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Rune
{
    // 模仿 ItemDragDropController，拖拽幽灵由 prefab 配置
    public sealed class RuneDragDropController : MonoBehaviour
    {
        public static RuneDragDropController Instance { get; private set; }

        [SerializeField] RectTransform dragGhostRoot;
        [SerializeField] Image dragGhostImage;
        [SerializeField] Canvas topCanvas;

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
            if (dragGhostRoot != null)
            {
                dragGhostRoot.gameObject.SetActive(false);
                ConfigureGhostRaycast(dragGhostRoot.gameObject);
            }

            if (topCanvas == null)
            {
                topCanvas = GetComponentInParent<Canvas>();
            }
        }

        static void ConfigureGhostRaycast(GameObject ghostRoot)
        {
            var cg = ghostRoot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                return;
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

            if (dragGhostRoot == null)
            {
                Debug.LogError("[RuneDragDropController] dragGhostRoot is not assigned.");
                return false;
            }

            Payload = payload;
            IsDragging = true;
            _dropHandledThisDrag = false;
            _lastDragScreenPos = UnityEngine.Input.mousePosition;
            dragGhostRoot.gameObject.SetActive(true);
            ApplyDragGhostIcon(payload.RuneId);
            UpdateDrag(_lastDragScreenPos);
            return true;
        }

        void ApplyDragGhostIcon(string runeId)
        {
            if (dragGhostImage == null)
            {
                return;
            }

            var def = RuneCatalog.GetOrDefault(runeId);
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.Icon))
            {
                sprite = SimpleResManager.Load<Sprite>(def.Icon);
            }

            dragGhostImage.sprite = sprite;
            dragGhostImage.enabled = sprite != null;
        }

        public void MarkDropHandled()
        {
            _dropHandledThisDrag = true;
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (!IsDragging || dragGhostRoot == null || topCanvas == null)
            {
                return;
            }

            _lastDragScreenPos = screenPos;
            var canvasRect = topCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                topCanvas.worldCamera,
                out Vector2 localOnCanvas);
            dragGhostRoot.localPosition = localOnCanvas;
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
            if (dragGhostRoot != null)
            {
                dragGhostRoot.gameObject.SetActive(false);
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
