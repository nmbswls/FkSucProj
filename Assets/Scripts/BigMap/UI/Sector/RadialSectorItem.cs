
using My.Map.Entity;
using TMPro;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UI;
using static My.UI.MapPlayerRadialMenu;

namespace My.UI
{
    public class RadialSectorItem : MonoBehaviour
    {
        public Image sectorBg;   // 扇形底图（可用Image Filled）
        public RectTransform InfoRoot;
        public RectTransform SectRoot;

        public TextMeshProUGUI label;
        public Image icon;

        [HideInInspector] public int index;

        public RadialItem innerItem { get; private set; }

        public void SetData(RadialItem innerItem, Color normal, float fillAmount)
        {
            this.innerItem = innerItem;

            if(innerItem != null)
            {
                gameObject.SetActive(true);

                if (sectorBg != null)
                {
                    sectorBg.fillAmount = fillAmount - 2 * 1.0f / 360f; // 填充=扇区角度/360
                    sectorBg.color = normal;
                }

                Sprite iconSprite = null;
                if (innerItem.RadialFunc == ERadialFunc.UseSkill)
                {
                    var skillCfg = SkillLibrary.GetSkillConfig(innerItem.SkillId);

                    if (skillCfg != null && !string.IsNullOrEmpty(skillCfg.IconPath))
                    {
                        iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{skillCfg.IconPath}");
                    }
                }
                else if (innerItem.RadialFunc == ERadialFunc.ChangeHuman)
                {
                    iconSprite = SimpleResManager.Load<Sprite>($"Sprites/change_human_mode");
                }

                if (icon != null)
                {
                    icon.sprite = iconSprite;
                }
                SetInteractable(innerItem.Interactable);
            }
            else
            {
                gameObject.SetActive(false);
            }
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