using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 播种栏单个种子槽（prefab 内预置，不在运行时 new）
    public sealed class FarmSeedSlotView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] GameObject selectedMark;

        public Button Button => button;

        public void SetEmpty()
        {
            if (label != null)
            {
                label.text = "-";
            }

            if (icon != null)
            {
                icon.enabled = false;
                icon.sprite = null;
            }

            if (button != null)
            {
                button.interactable = false;
            }

            if (selectedMark != null)
            {
                selectedMark.SetActive(false);
            }

            if (background != null)
            {
                background.color = new Color(0.2f, 0.24f, 0.28f, 0.95f);
            }
        }

        public void Bind(string displayName, Sprite sprite, long count, bool selected, bool usable)
        {
            if (label != null)
            {
                label.text = displayName + " x" + count;
            }

            if (icon != null)
            {
                icon.enabled = sprite != null;
                icon.sprite = sprite;
                icon.color = usable ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
            }

            if (button != null)
            {
                button.interactable = usable;
            }

            if (selectedMark != null)
            {
                selectedMark.SetActive(selected);
            }

            if (background != null)
            {
                background.color = selected
                    ? new Color(0.28f, 0.45f, 0.32f, 0.98f)
                    : new Color(0.2f, 0.24f, 0.28f, 0.95f);
            }
        }
    }
}
