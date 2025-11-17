using My.Player.Bag;
using My.UI.Bag;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;
using static My.UI.AnyContainerItemCell;


namespace My.UI
{


    public class ItemPopupMenu : PanelBase
    {
        public static void Show(AnyContainerItemCell cell, ItemStack stack, int index, Vector2 screenPos)
        {

            var panel = UIManager.Instance.ShowPanel("ItemPopup", null) as ItemPopupMenu;

            panel.currentCell = cell;
            panel.currentStack = stack;
            panel.currentIndex = index;

            panel.gameObject.SetActive(true);

            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out local);
                panel.transform.localPosition = local;
            }

            // 根据物品可用性禁用按钮
            //bool canUse = currentIsInventory && FakeItemDatabase.CanUse(stack.ItemID);
            //UseBtn.interactable = canUse;

            //SplitBtn.interactable = currentIsInventory && stack.Count > 1;
            //DropBtn.interactable = currentIsInventory;
        }

        public static void Close()
        {
            UIManager.Instance.HidePanel("ItemPopup");
        }


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
            //Panel.gameObject.SetActive(false);

            UseBtn.onClick.AddListener(OnClickUse);
            SplitBtn.onClick.AddListener(OnClickSplit);
            //DropBtn.onClick.AddListener(OnClickDrop);
            //CloseBtn.onClick.AddListener(Close);
        }


        private void OnClickUse()
        {
            if (currentCell.ContainerType == EContainerType.Inventory)
            {
                PlayerBagUIPanel.Instance?.UseItem(currentIndex);
            }
            Close();
        }

        private void OnClickSplit()
        {
            if (currentCell.ContainerType != EContainerType.Inventory) { Close(); return; }
            // 简化：固定拆分数量为一半，实际可弹窗输入
            int half = currentStack.Count / 2;
            if (half > 0)
            {
                PlayerBagUIPanel.Instance?.SplitItem(currentIndex, half);
            }
            Close();
        }

        private void OnClickDrop()
        {
            if (currentCell.ContainerType != EContainerType.Inventory) { Close(); return; }
            // 简化：全部丢弃
            PlayerBagUIPanel.Instance?.DropItemToGround(currentIndex, currentStack.Count);
            Close();
        }

        public override void Hide()
        {
            base.Hide();

            currentCell = null;
            currentStack = null;
        }
    }
}