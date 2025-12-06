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

        public GameObject UseBtnGo;
        public GameObject UseBtn2Go;
        public GameObject SplitBtnGo;
        public GameObject DropBtnGo;
        public GameObject CloseBtnGo;

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

            UseBtn = UseBtnGo.GetComponentInChildren<Button>();
            UseBtn2 = UseBtn2Go.GetComponentInChildren<Button>();
            SplitBtn = SplitBtnGo.GetComponentInChildren<Button>();
            DropBtn = DropBtnGo.GetComponentInChildren<Button>();
            CloseBtn = CloseBtnGo.GetComponentInChildren<Button>();

            UseBtn.onClick.AddListener(OnClickUse);
            UseBtn2.onClick.AddListener(OnClickUse2);
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

            UseBtnGo.SetActive(false);
            UseBtn2Go.SetActive(false);
            SplitBtnGo.SetActive(false);
            DropBtnGo.SetActive(false);
            CloseBtnGo.SetActive(true);

            // 根据物品可用性禁用按钮
            //bool canUse = currentIsInventory && FakeItemDatabase.CanUse(stack.ItemID);
            //UseBtn.interactable = canUse;

            if (cell.ContainerType == EContainerType.Inventory
                || cell.ContainerType == EContainerType.SpecialInventory)
            {
                if (stack.Count > 1)
                {
                    SplitBtnGo.gameObject.SetActive(true);
                }

                var itemConf = FakeItemDatabase.GetItem(stack.ItemID);
                if(itemConf.CanDrop)
                {
                    DropBtnGo.gameObject.SetActive(true);
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

        private void OnClickUse2()
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
            //if (half > 0)
            //{
            //    PlayerBagUIPanel.Instance?.SplitItem(bagId, currentIndex, half);
            //}

            ItemCountChooseBox.Show(currentStack.Count, initVal: half,  confirmCallback :(chossed) =>
            {
                if (chossed > 0)
                {
                    PlayerBagUIPanel.Instance?.SplitItem(bagId, currentIndex, chossed);
                }
            });
            
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