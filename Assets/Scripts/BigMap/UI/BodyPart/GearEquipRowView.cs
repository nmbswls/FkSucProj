using My.Config;
using My.UI;
using TMPro;using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    public sealed class GearEquipRowView : MonoBehaviour
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI HintText;
        public Button ActionButton;
        public TextMeshProUGUI ActionButtonText;

        ItemIdHoverProvider _itemHover;

        void Awake()
        {
            _itemHover = GetComponent<ItemIdHoverProvider>();
            if (_itemHover == null)
            {
                _itemHover = gameObject.AddComponent<ItemIdHoverProvider>();
            }
        }

        public void Bind(string title, string hint, string actionLabel, bool canAct, UnityEngine.Events.UnityAction onClick)
        {
            Bind(null, 1, title, hint, actionLabel, canAct, onClick);
        }

        public void Bind(
            string itemId,
            long stackCount,
            string title,
            string hint,
            string actionLabel,
            bool canAct,
            UnityEngine.Events.UnityAction onClick)
        {
            if (_itemHover != null)
            {
                if (string.IsNullOrEmpty(itemId))
                {
                    _itemHover.ClearItem();
                }
                else
                {
                    _itemHover.SetItem(itemId, stackCount);
                }
            }

            if (TitleText != null)
            {
                TitleText.text = title ?? string.Empty;
            }

            if (HintText != null)
            {
                HintText.text = hint ?? string.Empty;
                HintText.gameObject.SetActive(!string.IsNullOrEmpty(hint));
            }

            if (ActionButtonText != null)
            {
                ActionButtonText.text = actionLabel ?? string.Empty;
                ActionButtonText.gameObject.SetActive(ActionButton != null);
            }

            if (ActionButton != null)
            {
                ActionButton.onClick.RemoveAllListeners();
                ActionButton.interactable = canAct;
                ActionButton.gameObject.SetActive(!string.IsNullOrEmpty(actionLabel));
                if (canAct && onClick != null)
                {
                    ActionButton.onClick.AddListener(onClick);
                }
            }
        }
    }
}
