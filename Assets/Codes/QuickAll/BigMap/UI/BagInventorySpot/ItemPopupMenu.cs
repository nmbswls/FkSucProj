using Bag;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;
using static AnyContainerItemCell;

public class ItemPopupMenu : MonoBehaviour
{
    public static ItemPopupMenu Instance;

    public RectTransform Panel;
    public Button UseBtn;
    public Button SplitBtn;
    public Button DropBtn;
    public Button CloseBtn;

    private AnyContainerItemCell currentCell;
    private ItemStack currentStack;
    private int currentIndex;

    void Awake()
    {
        Instance = this;
        Panel.gameObject.SetActive(false);

        UseBtn.onClick.AddListener(OnClickUse);
        SplitBtn.onClick.AddListener(OnClickSplit);
        //DropBtn.onClick.AddListener(OnClickDrop);
        //CloseBtn.onClick.AddListener(Close);
    }

    public void Show(AnyContainerItemCell cell, ItemStack stack, int index, Vector2 screenPos)
    {
        currentCell = cell;
        currentStack = stack;
        currentIndex = index;

        Panel.gameObject.SetActive(true);

        var canvas = Panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out local);
            Panel.anchoredPosition = local;
        }

        // 根据物品可用性禁用按钮
        //bool canUse = currentIsInventory && FakeItemDatabase.CanUse(stack.ItemID);
        //UseBtn.interactable = canUse;

        //SplitBtn.interactable = currentIsInventory && stack.Count > 1;
        //DropBtn.interactable = currentIsInventory;
    }

    private void OnClickUse()
    {
        if (currentCell.ContainerType == EContainerType.Inventory)
        {
            InventoryUIController.Instance.UseItem(currentIndex);
        }
        Close();
    }

    private void OnClickSplit()
    {
        if (currentCell.ContainerType != EContainerType.Inventory)  { Close(); return; }
        // 简化：固定拆分数量为一半，实际可弹窗输入
        int half = currentStack.Count / 2;
        if (half > 0)
        {
            InventoryUIController.Instance.SplitItem(currentIndex, half);
        }
        Close();
    }

    private void OnClickDrop()
    {
        if (currentCell.ContainerType != EContainerType.Inventory) { Close(); return; }
        // 简化：全部丢弃
        InventoryUIController.Instance.DropItemToGround(currentIndex, currentStack.Count);
        Close();
    }

    public void Close()
    {
        Panel.gameObject.SetActive(false);
        currentCell = null;
        currentStack = null;
    }
}