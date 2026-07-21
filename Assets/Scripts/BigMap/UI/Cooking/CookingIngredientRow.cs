using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace My.UI.Cooking
{
    public sealed class CookingIngredientRow : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI iconFallbackText;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI countText;
        [SerializeField] Image shortageMarker;

        Button _selectButton;
        Image _background;
        string _itemId;
        Action<string> _onSelected;

        void Awake()
        {
            _background = GetComponent<Image>();
            _selectButton = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            _selectButton.targetGraphic = _background;
            _selectButton.transition = Selectable.Transition.ColorTint;
        }

        public void Bind(string itemId, long owned, long required)
        {
            Bind(itemId, owned, required, null, false);
        }

        public void Bind(string itemId, long owned, long required, Action<string> onSelected, bool selected)
        {
            _itemId = itemId;
            _onSelected = onSelected;
            _selectButton ??= GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => _onSelected?.Invoke(_itemId));
            _selectButton.interactable = owned > 0;
            if (_background != null)
            {
                _background.color = selected
                    ? new Color(0.18f, 0.27f, 0.26f, 0.96f)
                    : new Color(0.095f, 0.11f, 0.105f, 0.9f);
            }
            var item = ItemCatalog.GetItemDef(itemId);
            nameText.text = item != null && !string.IsNullOrEmpty(item.DisplayName) ? item.DisplayName : itemId;
            bool enough = owned >= required;
            countText.text = $"{owned} / {required}";
            countText.color = enough ? new Color(0.72f, 0.78f, 0.73f) : new Color(0.92f, 0.48f, 0.38f);
            shortageMarker.enabled = !enough;
            bool hasIcon = CookingRecipeListItem.ApplyIcon(icon, item?.SpriteName);
            iconFallbackText.gameObject.SetActive(!hasIcon);
            iconFallbackText.text = nameText.text.Substring(0, 1);
        }
    }
}
