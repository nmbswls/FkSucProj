using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public interface IRuneCellClickBehaviour
    {
        void OnRuneCellClick(RuneCellBase cell, PointerEventData eventData);
    }

    public interface IRuneCellDragSourceBehaviour
    {
        bool TryBeginDrag(RuneCellBase cell, PointerEventData eventData);
    }

    public interface IRuneCellDropTargetBehaviour
    {
        void HandleDrop(RuneCellBase target, RuneDragPayload payload, RuneDragDropController controller);
    }

    // 模仿 ItemCellBase：点击/拖拽/落点由外部策略注入
    public class RuneCellBase : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public Image bg;
        public Image icon;
        public Image lockOverlay;
        public Image addOverlay;
        public Image equippedMark;
        public Image maskOverlay;
        public TextMeshProUGUI nameText;

        protected string boundRuneId;
        protected int cellIndex;
        protected RuneInfoProvider hoverProvider;

        IRuneCellClickBehaviour _click;
        IRuneCellDragSourceBehaviour _dragSource;
        IRuneCellDropTargetBehaviour _dropTarget;

        public string BoundRuneId => boundRuneId;
        public int CellIndex => cellIndex;

        public enum EStyleType
        {
            Normal,
            Selected,
            Locked,
            AddIcon,
        }

        static readonly Color NormalBgColor = new Color(0.14f, 0.12f, 0.20f, 1f);
        static readonly Color SelectedBgColor = new Color(0.42f, 0.34f, 0.14f, 1f);
        static readonly Color EmptyBgColor = new Color(0.10f, 0.09f, 0.15f, 1f);

        protected virtual void Awake()
        {
            hoverProvider = GetComponent<RuneInfoProvider>();
        }

        public void SetRuneCellInteractions(
            IRuneCellClickBehaviour click,
            IRuneCellDragSourceBehaviour drag,
            IRuneCellDropTargetBehaviour drop)
        {
            _click = click;
            _dragSource = drag;
            _dropTarget = drop;
        }

        protected void SetBoundRune(string runeId, int index)
        {
            boundRuneId = runeId;
            cellIndex = index;
            hoverProvider?.SetOwnedRune(runeId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _click?.OnRuneCellClick(this, eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragSource?.TryBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            RuneDragDropController.Instance?.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            RuneDragDropController.Instance?.EndDrag(eventData.position);
        }

        public void OnDrop(PointerEventData eventData)
        {
            TryHandleExternalDrop(RuneDragDropController.Instance);
        }

        public void TryHandleExternalDrop(RuneDragDropController ctrl)
        {
            if (ctrl == null || ctrl.Payload == null || !ctrl.IsDragging)
            {
                return;
            }

            _dropTarget?.HandleDrop(this, ctrl.Payload, ctrl);
        }

        protected void ApplyRuneIcon(RuneData def)
        {
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.Icon))
            {
                sprite = SimpleResManager.Load<Sprite>(def.Icon);
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }

        public void RefreshCellStyle(EStyleType style)
        {
            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(style == EStyleType.Locked);
            }

            if (addOverlay != null)
            {
                addOverlay.gameObject.SetActive(style == EStyleType.AddIcon);
            }

            if (bg != null)
            {
                bg.color = style == EStyleType.Selected ? SelectedBgColor : (style == EStyleType.AddIcon ? EmptyBgColor : NormalBgColor);
            }
        }
    }
}
