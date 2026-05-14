using My.Player;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Bag
{
    // 背包内快捷道具栏单格：仅接受 UseSkill 类道具；拖出到无有效落点则清空该栏（由 ItemDragDropController.EndDrag 处理）。
    public class QuickBarItemSlotCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public int SlotIndex;
        public Image Background;
        public Image Icon;
        public TextMeshProUGUI CountText;

        public void BindDisplay(string itemIdOrEmpty)
        {
            if (CountText != null)
            {
                CountText.gameObject.SetActive(false);
            }

            if (string.IsNullOrEmpty(itemIdOrEmpty))
            {
                Icon.enabled = false;
                return;
            }

            Icon.enabled = true;
            var inv = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            long n = inv != null ? inv.GetCarriedItemTotal(itemIdOrEmpty) : 0;
            if (CountText != null && n > 1)
            {
                CountText.gameObject.SetActive(true);
                CountText.text = n.ToString();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var mdm = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mdm == null || SlotIndex < 0 || SlotIndex >= mdm.QuickSlotItemSet.Length)
            {
                return;
            }

            var id = mdm.QuickSlotItemSet[SlotIndex];
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var ctrl = ItemDragDropController.Instance;
            if (ctrl == null)
            {
                return;
            }

            ctrl.BeginDragFromQuickBar(id, SlotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance?.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance?.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var ctrl = ItemDragDropController.Instance;
            var payload = ctrl?.Payload;
            if (payload == null)
            {
                return;
            }

            ctrl.OnDropToQuickBarSlot(SlotIndex, payload);
        }
    }
}
