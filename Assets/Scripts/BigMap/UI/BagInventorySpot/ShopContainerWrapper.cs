using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SuperScrollView;
using Unity.VisualScripting;
using My.Player.Bag;
using Config;
using static My.UI.AnyContainerItemCell;


namespace My.UI
{
    public class ShopContainerWrapper : MonoBehaviour
    {
        public TextMeshProUGUI LeftCount;
        public TextMeshProUGUI TotalCount;

        public AnyContainerItemCell InnerCell;

        public void Bind(long leftCount, ItemStack stack, int index, EContainerType containerType, int containerId, System.Action<int> onChangedCb, EStyleType style = EStyleType.Normal)
        {
            LeftCount.text = leftCount.ToString();
            TotalCount.text = leftCount.ToString();

            InnerCell.Bind(stack, index, containerType, containerId, onChangedCb, style);
        }
    }
}

