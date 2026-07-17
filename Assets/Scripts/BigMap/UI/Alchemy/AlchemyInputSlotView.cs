using System;
using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyInputSlotView : MonoBehaviour
    {
        [SerializeField] GameObject emptyRoot;
        [SerializeField] GameObject filledRoot;
        [SerializeField] GameObject selectedMark;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Button clickButton;

        int _slotIndex;
        Action<int> _onClick;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onClick?.Invoke(_slotIndex));
            }
        }

        public void BindEmpty(int slotIndex, bool enabled, Action<int> onClick)
        {
            _slotIndex = slotIndex;
            _onClick = onClick;
            SetSelected(false);
            if (emptyRoot != null)
            {
                emptyRoot.SetActive(true);
            }

            if (filledRoot != null)
            {
                filledRoot.SetActive(false);
            }

            if (clickButton != null)
            {
                clickButton.interactable = enabled;
            }
        }

        public void BindFilled(int slotIndex, string itemId, Action<int> onClick)
        {
            _slotIndex = slotIndex;
            _onClick = onClick;
            SetSelected(false);
            if (emptyRoot != null)
            {
                emptyRoot.SetActive(false);
            }

            if (filledRoot != null)
            {
                filledRoot.SetActive(true);
            }

            var def = ItemCatalog.GetItemDef(itemId);
            if (nameText != null)
            {
                nameText.text = def?.DisplayName ?? itemId;
            }

            ApplyIcon(def?.SpriteName);
            if (clickButton != null)
            {
                clickButton.interactable = true;
            }
        }

        public void SetLocked(int slotIndex)
        {
            _slotIndex = slotIndex;
            _onClick = null;
            SetSelected(false);
            if (emptyRoot != null)
            {
                emptyRoot.SetActive(true);
            }

            if (filledRoot != null)
            {
                filledRoot.SetActive(false);
            }

            if (clickButton != null)
            {
                clickButton.interactable = false;
            }

            if (nameText != null)
            {
                nameText.text = string.Empty;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedMark != null)
            {
                selectedMark.SetActive(selected);
            }
        }

        void ApplyIcon(string spriteName)
        {
            if (icon == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(spriteName))
            {
                icon.enabled = false;
                return;
            }

            var sp = SimpleResManager.Load<Sprite>("Sprites/Item/" + spriteName);
            icon.sprite = sp;
            icon.enabled = sp != null;
        }
    }
}
