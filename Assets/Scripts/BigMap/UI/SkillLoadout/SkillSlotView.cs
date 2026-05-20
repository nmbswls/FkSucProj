using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillSlotView : MonoBehaviour, IPointerClickHandler
    {
        public SkillLoadoutSlotKind slotKind = SkillLoadoutSlotKind.Active;
        public int SlotIndex;
        public TMP_Text label;
        public Image icon;

        public void RefreshDisplay(PlayerSkillSystem sys)
        {
            if (sys == null) return;

            string id = slotKind == SkillLoadoutSlotKind.Passive
                ? (SlotIndex >= 0 && SlotIndex < sys.PassiveSkillSlots.Length
                    ? sys.PassiveSkillSlots[SlotIndex]
                    : null)
                : sys.NormalSkillSlots[SlotIndex];
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            if (slotKind != SkillLoadoutSlotKind.Passive)
            {
                return;
            }

            var panel = SkillLoadoutPanel.Current;
            if (panel == null)
            {
                return;
            }

            panel.TryClearPassiveSlotFromUi(SlotIndex);
        }
    }
}
