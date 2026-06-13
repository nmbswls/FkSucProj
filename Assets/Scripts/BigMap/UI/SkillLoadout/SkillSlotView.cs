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
        [SerializeField] SkillEquippedHoverProvider hoverProvider;

        public void RefreshDisplay(PlayerSkillSystem sys)
        {
            if (sys == null)
            {
                return;
            }

            if (hoverProvider == null)
            {
                Debug.LogError("[SkillSlotView] Missing hoverProvider reference.", this);
                return;
            }

            string id = slotKind == SkillLoadoutSlotKind.Passive
                ? (SlotIndex >= 0 && SlotIndex < sys.PassiveSkillSlots.Length
                    ? sys.PassiveSkillSlots[SlotIndex]
                    : null)
                : (SlotIndex >= 0 && SlotIndex < sys.NormalSkillSlots.Length
                    ? sys.NormalSkillSlots[SlotIndex]
                    : null);

            var cfg = !string.IsNullOrEmpty(id) ? SkillLibrary.GetSkillConfig(id) : null;
            if (label != null)
            {
                if (string.IsNullOrEmpty(id))
                {
                    label.text = "-";
                }
                else if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
                {
                    label.text = cfg.Desc;
                }
                else
                {
                    label.text = id;
                }
            }

            if (icon != null)
            {
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

                icon.raycastTarget = true;
            }

            hoverProvider.Configure(id);
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
