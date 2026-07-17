using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyEquipmentPickerCell : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI hintText;
        [SerializeField] Button clickButton;
        [SerializeField] GameObject selectedMark;

        string _entryId;
        Action<string> _onPick;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void Bind(string entryId, string displayName, string hint, bool selected, Action<string> onPick)
        {
            _entryId = entryId;
            _onPick = onPick;
            if (nameText != null)
            {
                nameText.text = displayName ?? string.Empty;
            }

            if (hintText != null)
            {
                hintText.text = hint ?? string.Empty;
            }

            if (selectedMark != null)
            {
                selectedMark.SetActive(selected);
            }
        }

        void OnClicked()
        {
            _onPick?.Invoke(_entryId);
        }
    }
}
