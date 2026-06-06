using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    // 兼容旧 prefab 字段名，逻辑由 RuneOwnedCell 承担
    public sealed class RuneCellView : RuneOwnedCell
    {
        public Image IconImage
        {
            get => icon;
            set => icon = value;
        }

        public TextMeshProUGUI NameText
        {
            get => nameText;
            set => nameText = value;
        }

        public TextMeshProUGUI DescText;
        public Image EquippedMark;
        public Button ClickButton;

        void Awake()
        {
            base.Awake();
            if (icon == null)
            {
                icon = IconImage;
            }

            if (nameText == null)
            {
                nameText = NameText;
            }

            if (equippedMark == null)
            {
                equippedMark = EquippedMark;
            }
        }

        public new void Bind(RuneData def, bool isEquipped, System.Action<RuneData> onClick)
        {
            base.Bind(null, def, isEquipped, false, 0);
        }
    }
}
