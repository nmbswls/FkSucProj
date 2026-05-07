
using My.Map.Entity;
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

        [HideInInspector] public string SkillId;
        [HideInInspector] public int index;

        public void SetData(string skillId, bool interactable, Color normal, float fillAmount)
        {
            SkillId = skillId;
            if (sectorBg != null)
            {
                sectorBg.fillAmount = fillAmount - 2 * 1.0f / 360f; // 填充=扇区角度/360
                sectorBg.color = normal;
            }

            var skillCfg = SkillLibrary.GetSkillConfig(skillId);
            if (icon != null)
            {
                if (skillCfg != null && !string.IsNullOrEmpty(skillCfg.IconPath))
                {
                    icon.sprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{skillCfg.IconPath}");
                }
                else
                {
                    icon.sprite = null;
                }
            }

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