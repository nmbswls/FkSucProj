using Config;
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
        public EContainerType SourceContainerType;
        public int SourceContainerId;
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

        public bool BeginDrag(ItemStack stack, EContainerType sourceType, int sourceContainerId, int sourceIndex)
        {
            if (IsDragging) return false;
            if (stack == null || stack.IsEmpty) return false;

            Payload = new DragPayload
            {
                Stack = stack.Clone(), // 拖拽过程使用克隆数据
                SourceContainerType = sourceType,
                SourceIndex = sourceIndex,
                SourceContainerId = sourceContainerId,
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
                        OnDropToInventory(droppedItem.ContainerId, payload, dstIndex);
                        break;
                    }
            }
        }

        // 从拖拽落到背包格子
        public void OnDropToInventory(int bagId, DragPayload payload, int dstIndex)
        {
            // 从loot点到背包
            if (payload.SourceContainerType == EContainerType.LootPoint)
            {
                if(LootPointUIPanel.Instance == null || LootPointUIPanel.Instance.Loot == null)
                {
                    Debug.LogError("OnDropToInventory loot point status error");
                    return;
                }
                var srcLoot = LootPointUIPanel.Instance.Loot;

                // 优先尝试放到指定格
                int moved = PlayerBagUIPanel.Instance.BindingInventory.AddItem(bagId, dstIndex, payload.Stack.ItemID, payload.Stack.Count);
                // 合并成功
                if (moved > 0)
                {
                    LootPointUIPanel.Instance.Loot.RemoveFromIndex(payload.SourceIndex, moved);
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance.RefreshContent();
                }
                // 尝试交换
                else
                {
                    var dstBag = PlayerBagUIPanel.Instance.BindingInventory.GetBagById(bagId);
                    if (dstBag == null)
                    {
                        return;
                    }

                    var toSwapBagItem = dstBag.GetItemByIdx(dstIndex);
                    if(toSwapBagItem == null)
                    {
                        Debug.LogError("OnDropToInventory something strange happen");
                        return;
                    }

                    var srcLootContainerType = srcLoot.GetContainerType();
                    int lootMaxStack = FakeItemDatabase.GetMaxStackByType(toSwapBagItem.ItemID, srcLootContainerType);
                    if(toSwapBagItem.Count > lootMaxStack)
                    {
                        Debug.LogError("OnDropToInventory try swap fail loot point cant have so much");
                        return;
                    }

                    var bagMaxStack = dstBag.GetMaxStack(payload.Stack.ItemID);
                    if (payload.Stack.Count > bagMaxStack)
                    {
                        Debug.LogError("OnDropToInventory try swap fail bag cant have so much");
                        return;
                    }

                    PlayerBagUIPanel.Instance.BindingInventory.AddItem(bagId, dstIndex, payload.Stack.ItemID, payload.Stack.Count);
                    LootPointUIPanel.Instance.Loot.RemoveFromIndex(payload.SourceIndex, payload.Stack.Count);
                    LootPointUIPanel.Instance.Loot.Add(toSwapBagItem, payload.SourceIndex);
                    //LootPointUIController.Instance.RefreshContent();
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance.RefreshContent();
                }
                //UIBus.RaiseInventoryAllChanged();
                //UIBus.RaiseLootAllChanged();
            }
            else if(payload.SourceContainerType == EContainerType.Shop)
            {



            }
            else if (payload.SourceContainerType == EContainerType.Inventory)
            {
                int fromBag = payload.SourceContainerId;
                int toBag = bagId;
                // 背包内部移动/堆叠/交换
                bool ok = PlayerBagUIPanel.Instance.BindingInventory.TrySwapOrMove(fromBag, payload.SourceIndex, toBag, dstIndex);
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


