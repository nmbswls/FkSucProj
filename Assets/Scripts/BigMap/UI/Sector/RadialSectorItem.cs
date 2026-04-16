
using TMPro;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class RadialSectorItem : MonoBehaviour
    {
        public Image sectorBg;   // 扇形底图（可用Image Filled）
        public RectTransform InfoRoot;
        public RectTransform SectRoot;

        public TextMeshProUGUI label;
        public Image icon;

        [HideInInspector] public string AbilityId;
        [HideInInspector] public int index;

        public void SetData(string abId, bool interactable, Color normal, float fillAmount)
        {
            AbilityId = abId;
            if (sectorBg != null)
            {
                sectorBg.fillAmount = fillAmount - 2 * 1.0f / 360f; // 填充=扇区角度/360
                sectorBg.color = normal;
            }
            //if (icon != null) icon.sprite = iconSprite;
            SetInteractable(interactable);
        }

        public void SetHighlight(bool on, Color normal, Color highlight)
        {
            if (sectorBg != null)
                sectorBg.color = on ? highlight : normal;
            if (icon != null)
                icon.color = on ? Color.white : new Color(1, 1, 1, 0.8f);
        }

        public void SetInteractable(bool interactable)
        {
            if (icon != null)
                icon.color = interactable ? icon.color : new Color(1, 1, 1, 0.35f);
            // 也可在这里加灰阶材质、禁用遮罩等
        }

    }
}