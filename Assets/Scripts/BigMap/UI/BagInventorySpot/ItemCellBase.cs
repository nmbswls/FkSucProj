using cfg.demo;
using My;
using My.Config;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI
{
    // 背包格与快捷道具格共用的视图根：点击/拖拽/落点由外部 SetItemCellInteractions 注入。
    public abstract class ItemCellBase : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Image bg;
        public Image icon;
        public RectTransform countRect;
        public TextMeshProUGUI countText;
        public Image maskOverlay;
        public Image lockOverlay;
        public Image addOverlay;
        public TextMeshProUGUI debugNameStr;

        public int Index { get; protected set; }
        public EContainerType ContainerType { get; protected set; }
        public int ContainerId { get; protected set; }

        protected ItemStack boundStack;
        protected System.Action<int> onChangedCallback;
        protected ItemData cacheItemDef;

        public enum EStyleType
        {
            Normal,
            Red,
            AddIcon,
            Locked,
            Masked,
            Selected,
        }

        public EStyleType StyleType { get; protected set; }

        IItemCellClickBehaviour _click;
        IItemCellDragSourceBehaviour _dragSource;
        IItemCellDropTargetBehaviour _dropTarget;

        public ItemStack GetBoundStack() => boundStack;

        protected void SetBoundStack(ItemStack stack)
        {
            boundStack = stack;
        }

        protected void SetIndexAndContainer(int index, EContainerType containerType, int containerId)
        {
            Index = index;
            ContainerType = containerType;
            ContainerId = containerId;
        }

        protected void SetOnChanged(System.Action<int> cb)
        {
            onChangedCallback = cb;
        }

        public void SetItemCellInteractions(
            IItemCellClickBehaviour click,
            IItemCellDragSourceBehaviour drag,
            IItemCellDropTargetBehaviour drop)
        {
            _click = click;
            _dragSource = drag;
            _dropTarget = drop;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_click != null)
            {
                _click.OnItemCellClick(this, eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_dragSource != null)
            {
                _dragSource.TryBeginDrag(this, eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance?.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ItemDragDropController.Instance?.EndDrag();
            ItemPopupMenu.Close();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var ctrl = ItemDragDropController.Instance;
            var payload = ctrl?.Payload;
            if (payload == null || ctrl == null)
            {
                return;
            }

            if (_dropTarget != null)
            {
                _dropTarget.HandleDrop(this, payload, Index, ctrl);
                return;
            }

            Debug.LogWarning("ItemCellBase: missing drop behaviour on " + name);
        }

        public void RefreshCellStyle(EStyleType style)
        {
            StyleType = style;
            if (maskOverlay != null)
            {
                maskOverlay.gameObject.SetActive(false);
            }

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(false);
            }

            if (addOverlay != null)
            {
                addOverlay.gameObject.SetActive(false);
            }

            if (bg != null)
            {
                if (style == EStyleType.Red)
                {
                    bg.color = Color.red;
                }
                else if (style == EStyleType.Selected)
                {
                    bg.color = new Color(1f, 0.92f, 0.55f, 1f);
                }
                else
                {
                    bg.color = Color.white;
                }
            }

            if (style == EStyleType.Masked && maskOverlay != null)
            {
                maskOverlay.gameObject.SetActive(true);
            }

            if (style == EStyleType.Locked && lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(true);
            }

            if (style == EStyleType.AddIcon && addOverlay != null)
            {
                addOverlay.gameObject.SetActive(true);
            }
        }

        protected void ApplyItemIconSprite(string itemId)
        {
            if (icon == null)
            {
                return;
            }

            var def = ItemCatalog.GetItemDef(itemId);
            if (def == null || string.IsNullOrEmpty(def.SpriteName))
            {
                return;
            }

            var sp = SimpleResManager.Load<Sprite>("Sprites/Item" + def.SpriteName);
            if (sp != null)
            {
                icon.sprite = sp;
            }
        }
    }
}
