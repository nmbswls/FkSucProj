using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SuperScrollView;
using Unity.VisualScripting;
using cfg.demo;
using My.Config;
using My.Player.Bag;
using static UnityEditor.Progress;


namespace My.UI
{
    public class AnyContainerItemCell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {


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

        public int Index;           // 当前格在所属容器中的槽位索引
        private ItemStack boundStack;
        private System.Action<int> onChanged;

        public EContainerType ContainerType;
        public int ContainerId;

        protected cfg.demo.ItemData cacheItemDef;

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
                var conf = ItemCatalog.GetItemDef(stack.ItemID);
                debugNameStr.text = conf != null ? conf.ItemId : stack.ItemID;
                onChanged = onChangedCb;
                var maxStack = ItemCatalog.GetMaxStackByType(stack.ItemID, containerType);
                countRect.gameObject.SetActive(hasItem && maxStack > 1);
            }
            else
            {
                debugNameStr.text = "";
                countRect.gameObject.SetActive(false);
            }
            

            if (hasItem)
            {
                cacheItemDef = ItemCatalog.GetItemDef(stack.ItemID);

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

            if (style == EStyleType.Locked)
            {
                lockOverlay.gameObject.SetActive(true);
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
        /// 拖拽结束时在其他格子上释放：把 Payload 交给控制器计算合并/交换
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

