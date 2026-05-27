using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // NotchRoot 下单格：代表 1 点装备容量（仅占用/空闲着色，不展示物品）
    public sealed class GearEquipNotchCellView : MonoBehaviour
    {
        static readonly Color OccupiedIconColor = new Color(0.88f, 0.74f, 0.32f, 1f);
        static readonly Color FreeIconColor = new Color(0.32f, 0.3f, 0.4f, 0.75f);

        [SerializeField] Image iconImage;

        public void BindOccupied()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = OccupiedIconColor;
                iconImage.enabled = true;
            }
        }

        public void BindFree()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = FreeIconColor;
                iconImage.enabled = true;
            }
        }
    }
}
