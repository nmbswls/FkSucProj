using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SuperScrollView;
using Unity.VisualScripting;
using My.Player.Bag;
using Config;
using static UnityEditor.Progress;


namespace My.UI
{
    public class AnyContainerItemCell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public enum EContainerType
        {
            Inventory,
            LootPoint,
            SpecialInventory,
            Shop,
        }

        public Image bg;
        public Image icon;
        public RectTransform countRect;
        public TextMeshProUGUI countText;
        //public GameObject emptyOverlay;
        public Image maskOverlay;
        public Image lockOverlay;
        public Image addOverlay;

        public TextMeshProUGUI debugNameStr;

        public enum EStyleType
        {
            Normal,
            Red,
            AddIcon,
            Locked,
            Masked,
        }
        public EStyleType StyleType;

        public int Index;           // 在所在列表的索引
        private ItemStack boundStack;
        private System.Action<int> onChanged;

        public EContainerType ContainerType;
        public int ContainerId;

        protected FakeItemConf? cacheConf;

        public void Bind(ItemStack stack, int index, EContainerType containerType, int containerId, System.Action<int> onChangedCb, EStyleType style = EStyleType.Normal)
        {
            boundStack = stack;
            Index = index;
            ContainerType = containerType;
            ContainerId = containerId;

            

            bool hasItem = stack != null && stack.Count > 0;
            //emptyOverlay?.SetActive(!hasItem);
            icon.enabled = hasItem;

            if(hasItem)
            {
                var conf = FakeItemDatabase.GetItem(stack.ItemID);
                debugNameStr.text = conf.ItemId;
                onChanged = onChangedCb;
                var maxStack = FakeItemDatabase.GetMaxStackByType(stack.ItemID, containerType);
                countRect.gameObject.SetActive(hasItem && maxStack > 1);
            }
            else
            {
                debugNameStr.text = "";
                countRect.gameObject.SetActive(false);
            }
            

            if (hasItem)
            {
                cacheConf = FakeItemDatabase.GetItem(stack.ItemID);

                //icon.sprite = FakeItemDatabase.GetIcon(stack.ItemID);
                countText.text = stack.Count.ToString();
            }

            RefreshCellStyle(style);
        }


        public void RefreshCellStyle(EStyleType style)
        {
            maskOverlay.gameObject.SetActive(false);
            lockOverlay.gameObject.SetActive(false);
            addOverlay.gameObject.SetActive(false);

            if (style == EStyleType.Red)
            {
                bg.color = Color.red;
            }
            else
            {
                bg.color = Color.white;
            }

            if(style == EStyleType.Masked)
            {
                maskOverlay.gameObject.SetActive(true);
            }

            if(style == EStyleType.AddIcon)
            {
                addOverlay.gameObject.SetActive(true);
            }
        }

        public void ClearEmpty()
        {
            boundStack = null;
            icon.enabled = false;

            Index = -1;
            ContainerType = 0;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (maskOverlay.gameObject.activeSelf) return;

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
            if (maskOverlay.gameObject.activeSelf) return;
            ItemPopupMenu.Close();
            ItemDragDropController.Instance.BeginDrag(boundStack, ContainerType, ContainerId, Index);
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

