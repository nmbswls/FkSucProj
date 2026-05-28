using My.Config;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // EquippedBar 下单格：一件已装备物品
    public sealed class GearEquipEquippedCellView : MonoBehaviour
    {
        static readonly Color FilledFrameColor = new Color(0.42f, 0.36f, 0.58f, 1f);
        static readonly Color EmptyFrameColor = new Color(0.2f, 0.18f, 0.26f, 0.65f);

        [SerializeField] Image frameImage;
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI costText;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Button clickButton;

        ItemIdHoverProvider _itemHover;
        CanvasGroup _canvasGroup;
        string _boundItemId;

        public RectTransform IconRect => iconImage != null ? iconImage.rectTransform : transform as RectTransform;

        public string GetItemId() => _boundItemId;

        void Awake()
        {
            _itemHover = GetComponent<ItemIdHoverProvider>();
            if (_itemHover == null)
            {
                _itemHover = gameObject.AddComponent<ItemIdHoverProvider>();
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetVisualHidden(bool hidden)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = hidden ? 0f : 1f;
                _canvasGroup.blocksRaycasts = !hidden;
            }

            if (clickButton != null && hidden)
            {
                clickButton.interactable = false;
            }
        }

        public void BindEquipped(string itemId, int cost, string displayName, UnityAction onUnequip)
        {
            _boundItemId = itemId;
            SetVisualHidden(false);
            _itemHover?.SetItem(itemId, 1);

            if (nameText != null)
            {
                nameText.text = displayName ?? string.Empty;
            }

            if (costText != null)
            {
                costText.text = cost.ToString();
                costText.gameObject.SetActive(true);
            }

            if (frameImage != null)
            {
                frameImage.color = FilledFrameColor;
            }

            if (iconImage != null)
            {
                var sprite = ItemCatalog.GetIcon(itemId);
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => onUnequip?.Invoke());
                clickButton.interactable = onUnequip != null;
            }
        }

        public void BindEmpty()
        {
            _boundItemId = null;
            SetVisualHidden(false);
            _itemHover?.ClearItem();

            if (nameText != null)
            {
                nameText.text = "空";
            }

            if (costText != null)
            {
                costText.text = string.Empty;
                costText.gameObject.SetActive(false);
            }

            if (frameImage != null)
            {
                frameImage.color = EmptyFrameColor;
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.interactable = false;
            }
        }
    }
}
