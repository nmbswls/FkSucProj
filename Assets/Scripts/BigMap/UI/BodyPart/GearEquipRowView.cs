using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    public sealed class GearEquipRowView : MonoBehaviour
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI HintText;
        public Button ActionButton;
        public TextMeshProUGUI ActionButtonText;

        public void Bind(string title, string hint, string actionLabel, bool canAct, UnityEngine.Events.UnityAction onClick)
        {
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
