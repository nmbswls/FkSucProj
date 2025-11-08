using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SuperScrollView;
using Bag;
using Unity.VisualScripting;



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
    private Bag.ItemStack boundStack;
    private System.Action<int> onChanged;

    public EContainerType ContainerType;

    public void Bind(Bag.ItemStack stack, int index, EContainerType containerType, System.Action<int> onChangedCb)
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
            countText.text = stack.Count > 1 ? stack.Count.ToString() : "";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //if (eventData.button == PointerEventData.InputButton.Right)
        if(boundStack != null)
        {
            ItemPopupMenu.Instance.Show(this, boundStack,  Index, eventData.position);
        }
        else
        {
            ItemPopupMenu.Instance.Close();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (boundStack == null || boundStack.Count == 0) return;
        ItemDragDropController.Instance.BeginDrag(boundStack, ContainerType, Index);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ItemDragDropController.Instance.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ItemDragDropController.Instance.EndDrag();
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
