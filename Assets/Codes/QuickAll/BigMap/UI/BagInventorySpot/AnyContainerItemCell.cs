using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SuperScrollView;
using Unity.VisualScripting;
using My.Player.Bag;


namespace My.UI
{
    public class AnyContainerItemCell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public enum EContainerType
        {
            Inventory,
            LootPoint,
        }

        public Image icon;
        public TextMeshProUGUI countText;
        public GameObject emptyOverlay;

        public int Index;           // 在所在列表的索引
        private ItemStack boundStack;
        private System.Action<int> onChanged;

        public EContainerType ContainerType;

        public Transform ValTr;

        public void Bind(ItemStack stack, int index, EContainerType containerType, System.Action<int> onChangedCb)
        {
            boundStack = stack;
            Index = index;
            ContainerType = containerType;

            onChanged = onChangedCb;

            bool hasItem = stack != null && stack.Count > 0;
            //emptyOverlay?.SetActive(!hasItem);
            icon.enabled = hasItem;



            countText.enabled = hasItem;

            if (hasItem)
            {
                icon.sprite = FakeItemDatabase.GetIcon(stack.ItemID);
                countText.text = stack.Count.ToString();

                if (stack.MaxStack > 1)
                {
                    ValTr.gameObject.SetActive(true);
                }
                else
                {
                    ValTr.gameObject.SetActive(false);
                }
            }
        }

        public void ClearEmpty(int index, EContainerType containerType)
        {
            boundStack = null;
            icon.enabled = false;

            Index = index;
            ContainerType = containerType;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //if (eventData.button == PointerEventData.InputButton.Right)
            if (boundStack != null)
            {
                ItemPopupMenu.Show(this, boundStack, Index, eventData.position);
            }
            else
            {
                ItemPopupMenu.Close();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (boundStack == null || boundStack.Count == 0) return;
            ItemPopupMenu.Close();
            ItemDragDropController.Instance.BeginDrag(boundStack, ContainerType, Index);
        }

        public void OnDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance.EndDrag();
            ItemPopupMenu.Close();
        }

        /// <summary>
        /// 被drop时
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrop(PointerEventData eventData)
        {
            var payload = ItemDragDropController.Instance.Payload;
            if (payload == null) return;

            ItemDragDropController.Instance.OnCalculateDropResult(this, payload, Index);
        }
    }
}

