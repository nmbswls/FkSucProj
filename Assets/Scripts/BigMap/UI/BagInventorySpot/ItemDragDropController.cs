using System.Collections.Generic;
using Config;
using My.Map;
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
        //public ItemStack Stack;
        public string ItemId;
        public long ItemCnt;

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
                //Stack = stack.Clone(), // 拖拽过程使用克隆数据
                ItemId = stack.ItemID,
                ItemCnt = stack.Count,

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
                case EContainerType.SpecialInventory:
                case EContainerType.Warehouse:
                    {
                        OnDropToInventory(droppedItem.ContainerId, payload, dstIndex);
                        break;
                    }
                case EContainerType.LootPoint:
                    {
                        OnDropToLootContainer(droppedItem.ContainerId, payload, dstIndex);
                        break;
                    }
                case EContainerType.Shop:
                    {
                        OnDropToShop(payload, dstIndex);
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
                var srcContainer = LootPointUIPanel.Instance.Loot.GetLootItemContainer();

                var bag = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.GetBagById(bagId);
                var modified = ItemUtils.MoveOrMergeOrSwapItem(srcContainer, payload.SourceIndex, bag, dstIndex);
                if (modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
            else if(payload.SourceContainerType == EContainerType.Shop)
            {
                var buyItem = ShopNormalUIPanel.Instance.BindShop.ShopItems[payload.SourceIndex];
                if(buyItem.LeftCount > 1)
                {
                    ItemCountChooseBox.Show(buyItem.LeftCount, initVal:1, (chooseCnt) => {

                        bool buy = ShopNormalUIPanel.Instance.BindShop.TryBuyFromShop(payload.SourceIndex, (int)chooseCnt, null);
                        if (buy)
                        {
                            ShopNormalUIPanel.Instance.RefreshContent();
                            PlayerBagUIPanel.Instance?.RefreshContent();
                            WarehouseUIPanel.Instance?.RefreshContent();
                        }
                    });
                }
                else
                {
                    bool buy = ShopNormalUIPanel.Instance.BindShop.TryBuyFromShop(payload.SourceIndex, 1, null);
                    if (buy)
                    {
                        ShopNormalUIPanel.Instance.RefreshContent();
                        PlayerBagUIPanel.Instance?.RefreshContent();
                        WarehouseUIPanel.Instance?.RefreshContent();
                    }
                }
                
            }
            else if (payload.SourceContainerType == EContainerType.Inventory
                || payload.SourceContainerType == EContainerType.SpecialInventory
                || payload.SourceContainerType == EContainerType.Warehouse)
            {
                int fromBag = payload.SourceContainerId;
                int toBag = bagId;

                // 背包内部移动/堆叠/交换
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                bool ok = inv.TrySwapOrMove(fromBag, payload.SourceIndex, toBag, dstIndex);
                if (ok)
                {
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
        }


        // loot点自己内部拖拽
        public void OnDropToLootContainer(int bagId, DragPayload payload, int dstIndex)
        {

            if (LootPointUIPanel.Instance == null || LootPointUIPanel.Instance.Loot == null)
            {
                Debug.LogError("OnDropToInventory loot point status error");
                return;
            }

            // 原地交换
            if (payload.SourceContainerType == EContainerType.LootPoint)
            {
                var container = LootPointUIPanel.Instance.Loot.GetLootItemContainer();

                var modified = ItemUtils.MoveOrMergeOrSwapItem(container, payload.SourceIndex, container, dstIndex);
                if(modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                }
            }
            // 从背包拖动到loot点
            else if (payload.SourceContainerType == EContainerType.Inventory
                || payload.SourceContainerType == EContainerType.SpecialInventory
                || payload.SourceContainerType == EContainerType.Warehouse)
            {
                var container = LootPointUIPanel.Instance.Loot.GetLootItemContainer();
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                var fromBag = inv.GetBagById(payload.SourceContainerId);

                var modified = ItemUtils.MoveOrMergeOrSwapItem(fromBag, payload.SourceIndex, container, dstIndex);
                if (modified)
                {
                    LootPointUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }
            }
        }

        // loot点自己内部拖拽
        public void OnDropToShop(DragPayload payload, int dstIndex)
        {

            if (ShopNormalUIPanel.Instance == null || ShopNormalUIPanel.Instance.BindShop == null)
            {
                Debug.LogError("OnDropToShop loot point status error");
                return;
            }

            // 尝试售卖
            if (payload.SourceContainerType == EContainerType.Inventory
                || payload.SourceContainerType == EContainerType.SpecialInventory
                || payload.SourceContainerType == EContainerType.Warehouse)
            {
                var inv = MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem;
                var fromBag = inv.GetBagById(payload.SourceContainerId);

                //
                if(payload.ItemCnt> 0)
                {
                    //ShopNormalUIPanel.Instance.ShowSellHint();
                    UIManager.Instance.ShowPanel("ItemCountChooseBox", new Dictionary<int, long>());
                }

                bool sell = ShopNormalUIPanel.Instance.BindShop.TrySellFromBag(fromBag.BagId, payload.SourceIndex);
                if (sell)
                {
                    ShopNormalUIPanel.Instance.RefreshContent();
                    PlayerBagUIPanel.Instance?.RefreshContent();
                    WarehouseUIPanel.Instance?.RefreshContent();
                }

                
            }
        }
    }

}


