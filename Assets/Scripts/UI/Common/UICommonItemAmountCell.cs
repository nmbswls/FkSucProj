using My;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 通用只读物品展示：水平 icon + 数量，带超简化 hover
    public class UICommonItemAmountCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI countText;

        ItemIdHoverProvider _hoverProvider;
        string _itemId;

        void Awake()
        {
            icon ??= transform.Find("Icon")?.GetComponent<Image>();
            countText ??= transform.Find("Count")?.GetComponent<TextMeshProUGUI>();

            _hoverProvider = GetComponent<ItemIdHoverProvider>();
            if (_hoverProvider == null)
            {
                _hoverProvider = gameObject.AddComponent<ItemIdHoverProvider>();
            }

            if (_hoverProvider.boxTr == null)
            {
                _hoverProvider.boxTr = transform as RectTransform;
            }
        }

        public void Bind(string itemId, long count, bool nameOnlyHover = true)
        {
            _itemId = itemId;
            var safeCount = count > 0 ? count : 0;

            if (countText != null)
            {
                countText.text = safeCount.ToString();
            }

            ApplyIcon(itemId);

            if (_hoverProvider != null)
            {
                if (string.IsNullOrEmpty(itemId) || safeCount <= 0)
                {
                    _hoverProvider.ClearItem();
                }
                else
                {
                    _hoverProvider.SetItem(itemId, safeCount, nameOnlyHover);
                }
            }
        }

        public void Clear()
        {
            _itemId = null;
            if (countText != null)
            {
                countText.text = string.Empty;
            }

            if (icon != null)
            {
                icon.enabled = false;
                icon.sprite = null;
            }

            _hoverProvider?.ClearItem();
        }

        void ApplyIcon(string itemId)
        {
            if (icon == null)
            {
                return;
            }

            var def = string.IsNullOrEmpty(itemId) ? null : ItemCatalog.GetItemDef(itemId);
            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.SpriteName))
            {
                sprite = SimpleResManager.Load<Sprite>("Sprites/Item/" + def.SpriteName);
            }

            icon.enabled = sprite != null;
            icon.sprite = sprite;
        }
    }
}
