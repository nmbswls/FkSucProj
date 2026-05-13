using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{

    public class SkillSlotView : MonoBehaviour
    {
        public int SlotIndex;
        public TMP_Text label;
        public Image icon;

        public void RefreshDisplay(PlayerSkillSystem sys)
        {
            if (sys == null) return;

            string id = sys.NormalSkillSlots[SlotIndex];
            if (label != null)
                label.text = string.IsNullOrEmpty(id) ? "-" : id;

            if (icon != null)
            {
                var cfg = !string.IsNullOrEmpty(id) ? SkillLibrary.GetSkillConfig(id) : null;
                if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
                {
                    var sp = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
                    icon.sprite = sp;
                    icon.enabled = sp != null;
                }
                else
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
        }
    }
}
