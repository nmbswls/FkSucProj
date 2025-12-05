using Config;
using My.Player.Bag;
using My.UI.Bag;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;


namespace My.UI
{


    public class ItemPopupMenu : PanelBase
    {
        public static void Show(AnyContainerItemCell cell, ItemStack stack, int index, Vector2 screenPos)
        {

            var panel = UIManager.Instance.ShowPanel("ItemPopup", null) as ItemPopupMenu;
            if(panel == null)
            {
                return;
            }


            panel.RefreshView(cell, stack, index);
            var canvas = panel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, screenPos, canvas.worldCamera, out local);
                panel.transform.localPosition = local;
            }
        }

        public static void Close()
        {
            UIManager.Instance.HidePanel("ItemPopup");
        }


        public RectTransform Panel;

        public Button UseBtn;
        public Button UseBtn2;
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
            DropBtn.onClick.AddListener(OnClickDrop);
            CloseBtn.onClick.AddListener(Close);
        }

        private void RefreshView(AnyContainerItemCell cell, ItemStack stack, int index)
        {
            currentCell = cell;
            currentStack = stack;
            currentIndex = index;

            gameObject.SetActive(true);

            UseBtn.gameObject.SetActive(false);
            UseBtn2.gameObject.SetActive(false);
            SplitBtn.gameObject.SetActive(false);

            DropBtn.gameObject.SetActive(false);
            CloseBtn.gameObject.SetActive(true);

            // 根据物品可用性禁用按钮
            //bool canUse = currentIsInventory && FakeItemDatabase.CanUse(stack.ItemID);
            //UseBtn.interactable = canUse;

            if (cell.ContainerType == EContainerType.Inventory
                || cell.ContainerType == EContainerType.SpecialInventory)
            {
                if (stack.Count > 1)
                {
                    SplitBtn.gameObject.SetActive(true);
                }

                var itemConf = FakeItemDatabase.GetItem(stack.ItemID);
                if(itemConf.CanDrop)
                {
                    DropBtn.gameObject.SetActive(true);
                }
                var bag = MainGameManager.Instance.gameLogicManager.playerDataManager.inventoryModel.GetBagById(cell.ContainerId);
                if(bag != null)
                {
                    //var item = 
                }
            }
        }


        private void OnClickUse()
        {
            if (currentCell.ContainerType == EContainerType.Inventory)
            {
                PlayerBagUIPanel.Instance?.UseItem(0, currentIndex);
            }
            Close();
        }

        private void OnClickSplit()
        {
            if (currentCell.ContainerType != EContainerType.Inventory
                && currentCell.ContainerType != EContainerType.SpecialInventory) 
            { 
                Close(); 
                return; 
            }

            int bagId = currentCell.ContainerId;
            // 简化：固定拆分数量为一半，实际可弹窗输入
            long half = currentStack.Count / 2;
            if (half > 0)
            {
                PlayerBagUIPanel.Instance?.SplitItem(bagId, currentIndex, half);
            }
            Close();
        }

        private void OnClickDrop()
        {
            if (currentCell.ContainerType != EContainerType.Inventory
                && currentCell.ContainerType != EContainerType.SpecialInventory)
            {
                Close();
                return;
            }

            // 简化：全部丢弃
            PlayerBagUIPanel.Instance?.DropItemToGround(currentCell.ContainerId, currentIndex, currentStack.Count);
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