using My;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SecretBaseNpcGiftCell : MonoBehaviour
    {
        [SerializeField] Button clickButton;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI countText;

        string _itemId;
        System.Action<string> _onSelected;

        void Awake()
        {
            clickButton ??= GetComponent<Button>();
            icon ??= transform.Find("Icon")?.GetComponent<Image>();
            nameText ??= transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            countText ??= transform.Find("Count")?.GetComponent<TextMeshProUGUI>();

            if (clickButton != null)
            {
                clickButton.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_itemId))
                    {
                        _onSelected?.Invoke(_itemId);
                    }
                });
            }
        }

        public void Bind(string itemId, long count, bool selected, System.Action<string> onSelected)
        {
            _itemId = itemId;
            _onSelected = onSelected;

            var def = ItemCatalog.GetItemDef(itemId);
            if (nameText != null)
            {
                nameText.text = def != null && !string.IsNullOrEmpty(def.DisplayName)
                    ? def.DisplayName
                    : itemId;
            }

            if (countText != null)
            {
                countText.text = count > 1 ? count.ToString() : "";
            }

            if (icon != null)
            {
                Sprite sprite = null;
                if (def != null && !string.IsNullOrEmpty(def.SpriteName))
                {
                    sprite = SimpleResManager.Load<Sprite>("Sprites/" + def.SpriteName);
                }

                icon.enabled = sprite != null;
                icon.sprite = sprite;
            }

            if (clickButton != null)
            {
                var colors = clickButton.colors;
                colors.normalColor = selected ? new Color(0.85f, 0.95f, 1f) : Color.white;
                clickButton.colors = colors;
            }
        }
    }
}
