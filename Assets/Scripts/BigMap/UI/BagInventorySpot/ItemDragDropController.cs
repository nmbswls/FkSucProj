using My;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class DragPayload
    {
        public string ItemId;
        public long ItemCnt;

        public EContainerType SourceContainerType;
        public int SourceContainerId;
        public int SourceIndex;
    }

    // 控制道具拖拽的幽灵与 Payload；落点由各 Cell 上的 IItemCellDropTargetBehaviour 处理。
    public class ItemDragDropController : PanelBase
    {
        public static ItemDragDropController Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("ItemDragDrop");
                if (panel != null && panel is ItemDragDropController itemDragDrop)
                {
                    return itemDragDrop;
                }

                return null;
            }
        }

        public GameObject DragGhostGo;
        public Image DragGhostImage;
        public TextMeshProUGUI DragGhostCountText;

        public Canvas TopCanvas;

        public DragPayload Payload { get; private set; }
        public bool IsDragging { get; private set; }

        bool _dropHandledThisDrag;

        void Awake()
        {
            if (DragGhostGo != null)
            {
                DragGhostGo.gameObject.SetActive(false);
            }

            if (TopCanvas == null)
            {
                TopCanvas = GetComponentInParent<Canvas>();
            }
        }

        public bool BeginDrag(ItemStack stack, EContainerType sourceType, int sourceContainerId, int sourceIndex)
        {
            if (IsDragging)
            {
                return false;
            }

            if (stack == null || stack.IsEmpty)
            {
                return false;
            }

            Payload = new DragPayload
            {
                ItemId = stack.ItemID,
                ItemCnt = stack.Count,
                SourceContainerType = sourceType,
                SourceIndex = sourceIndex,
                SourceContainerId = sourceContainerId,
            };
            IsDragging = true;
            _dropHandledThisDrag = false;

            if (DragGhostGo)
            {
                DragGhostGo.SetActive(true);
                DragGhostImage.gameObject.SetActive(true);
                DragGhostCountText.text = stack.Count > 1 ? stack.Count.ToString() : "";
                DragGhostCountText.gameObject.SetActive(stack.Count > 1);
            }

            return true;
        }

        public bool BeginDragFromQuickBar(string itemId, int slotIndex)
        {
            if (MainGameManager.Instance?.gameLogicManager?.CanEditQuickSlotBar() != true)
            {
                return false;
            }

            if (IsDragging)
            {
                return false;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                return false;
            }

            Payload = new DragPayload
            {
                ItemId = itemId,
                ItemCnt = 1,
                SourceContainerType = EContainerType.QuickBar,
                SourceContainerId = 0,
                SourceIndex = slotIndex,
            };
            IsDragging = true;
            _dropHandledThisDrag = false;

            if (DragGhostGo)
            {
                DragGhostGo.SetActive(true);
                DragGhostImage.gameObject.SetActive(true);
                DragGhostCountText.text = "";
                DragGhostCountText.gameObject.SetActive(false);
            }

            return true;
        }

        public void MarkDropHandled()
        {
            _dropHandledThisDrag = true;
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (!IsDragging || DragGhostGo == null)
            {
                return;
            }

            RectTransform canvasRect = TopCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                TopCanvas.worldCamera,
                out Vector2 localOnCanvas);

            DragGhostGo.transform.localPosition = localOnCanvas;
            if (DragGhostCountText != null)
            {
                DragGhostCountText.rectTransform.position = screenPos;
            }
        }

        public void EndDrag()
        {
            var dropHandled = _dropHandledThisDrag;
            var p = Payload;
            IsDragging = false;
            _dropHandledThisDrag = false;
            Payload = null;
            if (DragGhostGo)
            {
                DragGhostGo.SetActive(false);
            }

            if (p != null && p.SourceContainerType == EContainerType.QuickBar && !dropHandled)
            {
                var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
                if (mdm != null && p.SourceIndex >= 0 && p.SourceIndex < mdm.QuickSlotItemSet.Length)
                {
                    mdm.ClearQuickSlot(p.SourceIndex);
                    OverworldHUDPanel.Instance?.RefreshItemQuickBar();
                }
            }
        }
    }
}
