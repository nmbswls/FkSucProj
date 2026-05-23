using cfg.demo;
using My.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public sealed class RuneCellView : MonoBehaviour
    {
        public Image IconImage;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescText;
        public Image EquippedMark;
        public Button ClickButton;

        RuneData _def;
        System.Action<RuneData> _onClick;

        public RuneData BoundDef => _def;

        public void Bind(RuneData def, bool isEquipped, System.Action<RuneData> onClick)
        {
            _def = def;
            _onClick = onClick;

            if (NameText != null)
            {
                NameText.text = def?.Name ?? string.Empty;
            }

            if (DescText != null)
            {
                DescText.text = def?.Desc ?? string.Empty;
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

            if (EquippedMark != null)
            {
                EquippedMark.gameObject.SetActive(isEquipped);
            }

            if (ClickButton != null)
            {
                ClickButton.onClick.RemoveAllListeners();
                ClickButton.onClick.AddListener(OnClick);
                ClickButton.interactable = def != null && def.RuneType == ERuneType.Equippable;
            }
        }

        void OnClick()
        {
            if (_def != null)
            {
                _onClick?.Invoke(_def);
            }
        }
    }
}
