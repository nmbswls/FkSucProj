using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public sealed class RuneEquipSlotView : MonoBehaviour
    {
        public ERuneEquipSlot Slot = ERuneEquipSlot.None;
        public TextMeshProUGUI SlotLabel;
        public Image IconImage;
        public TextMeshProUGUI NameText;
        public Button ClickButton;

        System.Action<ERuneEquipSlot> _onClick;

        public void Bind(ERuneEquipSlot slot, string runeId, System.Action<ERuneEquipSlot> onClick)
        {
            Slot = slot;
            _onClick = onClick;

            if (SlotLabel != null)
            {
                SlotLabel.text = RuneCatalog.GetSlotDisplayName(slot);
            }

            var def = RuneCatalog.GetOrDefault(runeId);
            if (NameText != null)
            {
                NameText.text = def != null ? def.Name : "空";
            }

            if (IconImage != null)
            {
                Sprite sprite = null;
                if (def != null && !string.IsNullOrEmpty(def.Icon))
                {
                    sprite = SimpleResManager.Load<Sprite>(def.Icon);
                }

                IconImage.sprite = sprite;
                IconImage.enabled = sprite != null;
            }

            if (ClickButton != null)
            {
                ClickButton.onClick.RemoveAllListeners();
                ClickButton.onClick.AddListener(() => _onClick?.Invoke(Slot));
                ClickButton.interactable = !string.IsNullOrEmpty(runeId);
            }
        }
    }
}
