using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 快捷栏中心：当前选中消耗品图标 + 按键提示 + 道具悬停信息。
    // 根节点 Image 始终作为底框显示，_itemIcon 控制道具图标，_emptyHint 控制空状态提示。
    public class ItemBarCenterItemView : MonoBehaviour
    {
        static readonly Color UsableColor = Color.white;
        static readonly Color DisabledColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField]
        Image _itemIcon;

        [SerializeField]
        TextMeshProUGUI _keyHint;

        [SerializeField]
        GameObject _emptyHint;

        ItemIdHoverProvider _itemHover;
        Color _keyHintDefaultColor = Color.white;

        void Awake()
        {
            _itemHover = GetComponent<ItemIdHoverProvider>();
            if (_itemHover == null)
            {
                _itemHover = gameObject.AddComponent<ItemIdHoverProvider>();
            }

            if (_keyHint != null)
            {
                _keyHint.text = "Q";
                _keyHintDefaultColor = _keyHint.color;
            }
        }

        public void RefreshItem(string itemId, long stackCount, bool usable)
        {
            bool hasItem = !string.IsNullOrEmpty(itemId);

            if (hasItem)
            {
                _itemHover?.SetItem(itemId, stackCount);
            }
            else
            {
                _itemHover?.ClearItem();
            }

            if (_emptyHint != null)
            {
                _emptyHint.SetActive(!hasItem);
            }

            if (_itemIcon != null)
            {
                if (hasItem)
                {
                    var iconSprite = ItemCatalog.GetIcon(itemId);
                    _itemIcon.enabled = iconSprite != null;
                    _itemIcon.sprite = iconSprite;
                }
                else
                {
                    _itemIcon.enabled = false;
                    _itemIcon.sprite = null;
                }

                _itemIcon.color = hasItem && usable ? UsableColor : DisabledColor;
            }

            if (_keyHint != null)
            {
                _keyHint.color = hasItem && usable ? _keyHintDefaultColor : DisabledColor;
            }
        }
    }
}
