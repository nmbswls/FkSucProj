using My.Player.Bag;
using My.UI.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static My.UI.AnyContainerItemCell;


namespace My.UI
{
    public class DragPayload
    {
        public ItemStack Stack;
        public My.UI.AnyContainerItemCell.EContainerType SourceType;
        public int SourceIndex;
    }

    /// <summary>
    /// 控制item之间拖动的处理
    /// </summary>
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

        public bool BeginDrag(ItemStack stack, EContainerType sourceType, int sourceIndex)
        {
            if (IsDragging) return false;
            if (stack == null || stack.IsEmpty) return false;

            Payload = new DragPayload
            {
                Stack = stack.Clone(), // 拖拽过程使用克隆数据
                SourceType = sourceType,
                SourceIndex = sourceIndex
            };
            IsDragging = true;

            if (DragGhostGo)
            {
                DragGhostGo.SetActive(true);

                //DragGhostImage.sprite = ItemDatabase.GetIcon(stack.ItemID);
                DragGhostImage.gameObject.SetActive(true);
                DragGhostCountText.text = stack.Count > 1 ? stack.Count.ToString() : "";
                DragGhostCountText.gameObject.SetActive(stack.Count > 1);
            }
            return true;
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (!IsDragging || DragGhostGo == null) return;


            RectTransform canvasRect = TopCanvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                TopCanvas.worldCamera,          // 注意这里
                out Vector2 localOnCanvas
            );

            //Vector3 worldOnCanvas = canvas.transform.TransformPoint(localOnCanvas);
            //Vector3 localOnTarget = targetRect.InverseTransformPoint(worldOnCanvas);

            DragGhostGo.transform.localPosition = localOnCanvas;
            if (DragGhostCountText != null)
            {
                DragGhostCountText.rectTransform.position = screenPos;
            }
        }

        public void EndDrag()
        {
            IsDragging = false;
            Payload = null;
            if (DragGhostGo) DragGhostGo.gameObject.SetActive(false);
        }


        public void OnCalculateDropResult(AnyContainerItemCell droppedItem, DragPayload payload, int dstIndex)
        {
            switch (droppedItem.ContainerType)
            {
                case EContainerType.Inventory:
                    {
                        OnDropToInventory(payload, dstIndex);
                        break;
                    }
            }
        }

        // 从拖拽落到背包格子
        public void OnDropToInventory(DragPayload payload, int dstIndex)
        {
            if (payload.SourceType == EContainerType.LootPoint)
            {
                // 优先尝试放到指定格
                int moved = PlayerBagUIPanel.Instance.BindingInventory.TryAddToIndexOrStack(payload.Stack, dstIndex);
                if (moved > 0)
                {
                    if (LootPointUIPanel.Instance.Loot != null)
                    {
                        LootPointUIPanel.Instance.Loot.RemoveFromIndex(payload.SourceIndex, moved);
                        LootPointUIPanel.Instance.RefreshContent();
                        PlayerBagUIPanel.Instance.RefreshContent();
                    }
                }
                else
                {
                    // 无法堆叠到目标格，尝试背包通道添加
                    //moved = InventoryUIController.Instance.BindingInventory.TryAdd(payload.Stack);
                    //LootPointUIController.Instance.Loot.RemoveFromIndex(payload.SourceIndex, moved);
                    //LootPointUIController.Instance.RefreshContent();
                    //InventoryUIController.Instance.RefreshContent();
                }
                //UIBus.RaiseInventoryAllChanged();
                //UIBus.RaiseLootAllChanged();
            }
            else if (payload.SourceType == EContainerType.Inventory)
            {
                // 背包内部移动/堆叠/交换
                bool ok = PlayerBagUIPanel.Instance.BindingInventory.TryMove(payload.SourceIndex, dstIndex);
                if (ok)
                {
                    UIManager.Instance.ShowPanel("PlayerBag");
                    PlayerBagUIPanel.Instance.RefreshContent();
                    //UIBus.RaiseInventoryAllChanged();
                }
            }
        }
    }

}


