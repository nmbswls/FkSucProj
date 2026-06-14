using System.Collections.Generic;
using My;
using My.Config;
using My.Player;
using My.Player.Bag;
using My.UI.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI
{
    public class DragPayload
    {
        public string ItemId;
        public long ItemCnt;
        public long ItemInstanceId;

        public EContainerType SourceContainerType;
        public int SourceContainerId;
        public int SourceIndex;
    }

    // 控制道具拖拽的幽灵与 Payload；落点由各 Cell 通过 SetItemCellInteractions 注入的 IItemCellDropTargetBehaviour 处理。
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
        Vector2 _lastDragScreenPos;

        void Awake()
        {
            if (DragGhostGo != null)
            {
                DragGhostGo.gameObject.SetActive(false);
                ConfigureGhostRaycast(DragGhostGo);
            }

            if (TopCanvas == null)
            {
                TopCanvas = GetComponentInParent<Canvas>();
            }
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

            foreach (var graphic in ghostRoot.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
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
                ItemInstanceId = stack.ItemInstanceId,
                SourceContainerType = sourceType,
                SourceIndex = sourceIndex,
                SourceContainerId = sourceContainerId,
            };
            IsDragging = true;
            _dropHandledThisDrag = false;
            _lastDragScreenPos = UnityEngine.Input.mousePosition;

            if (DragGhostGo)
            {
                DragGhostGo.SetActive(true);
                ApplyDragGhostIcon(stack.ItemID);
                DragGhostCountText.text = stack.Count > 1 ? stack.Count.ToString() : "";
                DragGhostCountText.gameObject.SetActive(stack.Count > 1);
            }

            UpdateDrag(_lastDragScreenPos);
            return true;
        }

        public bool BeginDragFromQuickBar(QuickSlotBinding binding, int slotIndex, EContainerType quickBarType)
        {
            if (MainGameManager.Instance?.gameLogicManager?.CanEditQuickSlotBar() != true)
            {
                return false;
            }

            if (IsDragging)
            {
                return false;
            }

            if (binding.IsEmpty)
            {
                return false;
            }

            Payload = new DragPayload
            {
                ItemId = binding.ItemId,
                ItemCnt = 1,
                ItemInstanceId = binding.ItemInstanceId,
                SourceContainerType = quickBarType,
                SourceContainerId = 0,
                SourceIndex = slotIndex,
            };
            IsDragging = true;
            _dropHandledThisDrag = false;
            _lastDragScreenPos = UnityEngine.Input.mousePosition;

            if (DragGhostGo)
            {
                DragGhostGo.SetActive(true);
                ApplyDragGhostIcon(binding.ItemId);
                DragGhostCountText.text = "";
                DragGhostCountText.gameObject.SetActive(false);
            }

            UpdateDrag(_lastDragScreenPos);
            return true;
        }

        void ApplyDragGhostIcon(string itemId)
        {
            if (DragGhostImage == null)
            {
                return;
            }

            var def = ItemCatalog.GetItemDef(itemId);
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.SpriteName))
            {
                sprite = SimpleResManager.Load<Sprite>("Sprites/Item/" + def.SpriteName);
            }

            DragGhostImage.sprite = sprite;
            DragGhostImage.enabled = sprite != null;
            DragGhostImage.gameObject.SetActive(sprite != null);
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

            _lastDragScreenPos = screenPos;

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

        public void EndDrag(Vector2? screenPos = null)
        {
            if (IsDragging && Payload != null && !_dropHandledThisDrag)
            {
                TryHandleDropAtScreen(screenPos ?? _lastDragScreenPos);
            }

            var dropHandled = _dropHandledThisDrag;
            var p = Payload;
            IsDragging = false;
            _dropHandledThisDrag = false;
            Payload = null;
            if (DragGhostGo)
            {
                DragGhostGo.SetActive(false);
            }

            if (p != null && !dropHandled)
            {
                var qb = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.HumanQuickBar;
                if (qb != null)
                {
                    if (p.SourceContainerType == EContainerType.QuickBarWeapon
                        && p.SourceIndex >= 0
                        && p.SourceIndex < qb.WeaponSlots.Length)
                    {
                        qb.ClearWeaponSlot(p.SourceIndex);
                        OverworldHUDPanel.Instance?.MainBottomBar?.Refresh();
                        PlayerHumanItemBarPanel.RefreshFromGame();
                        PlayerBagUIPanel.Instance?.RefreshContent();
                    }
                    else if (p.SourceContainerType == EContainerType.QuickBarConsumable
                             && p.SourceIndex >= 0
                             && p.SourceIndex < qb.ConsumableSlots.Length)
                    {
                        qb.ClearConsumableSlot(p.SourceIndex);
                        PlayerHumanItemBarPanel.RefreshFromGame();
                    }
                }
            }
        }

        void TryHandleDropAtScreen(Vector2 screenPos)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            var pointerData = new PointerEventData(eventSystem)
            {
                position = screenPos,
            };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var hit = results[i];
                if (hit.gameObject == null || hit.gameObject.transform.IsChildOf(transform))
                {
                    continue;
                }

                var cell = hit.gameObject.GetComponentInParent<ItemCellBase>();
                if (cell == null)
                {
                    continue;
                }

                cell.TryHandleExternalDrop(this);
                if (_dropHandledThisDrag)
                {
                    return;
                }
            }
        }
    }
}
