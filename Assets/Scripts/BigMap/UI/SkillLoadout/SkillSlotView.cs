using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillSlotView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public SkillLoadoutSlotKind slotKind = SkillLoadoutSlotKind.Active;
        public int SlotIndex;
        public TMP_Text label;
        public Image icon;
        [SerializeField] SkillEquippedHoverProvider hoverProvider;

        string _boundSkillId;

        // 主动槽 0-2 为固定槽不允许拖拽卸下，被动槽全部允许；槽位必须有技能
        bool CanDragUnequip =>
            !string.IsNullOrEmpty(_boundSkillId)
            && (slotKind == SkillLoadoutSlotKind.Passive || SlotIndex >= 3);

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

            _boundSkillId = id;

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
            var panel = SkillLoadoutPanel.Current;
            if (panel == null)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!string.IsNullOrEmpty(_boundSkillId))
                {
                    panel.ShowEquippedSkillDetail(slotKind, SlotIndex, _boundSkillId);
                }

                return;
            }

            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            if (slotKind != SkillLoadoutSlotKind.Passive)
            {
                return;
            }

            panel.TryClearPassiveSlotFromUi(SlotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanDragUnequip)
            {
                return;
            }

            var sys = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            if (sys == null)
            {
                return;
            }

            if (sys.IsGrantedPassive(_boundSkillId) || sys.IsGrantedActive(_boundSkillId))
            {
                return;
            }

            var behavior = new SlotUnequipDragBehavior(slotKind, SlotIndex);
            behavior.OnBeginDragFromPool(_boundSkillId);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!SkillDragSession.IsDragging)
            {
                return;
            }

            SkillDragSession.FollowScreenPoint(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!SkillDragSession.IsDragging)
            {
                return;
            }

            SkillDragSession.EndDrag(eventData);
        }
    }
}
