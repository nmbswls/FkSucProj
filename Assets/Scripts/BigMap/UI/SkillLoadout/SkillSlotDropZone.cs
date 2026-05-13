using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public enum SkillSlotDropMode
    {
        Fixed,
        CustomNormal,
    }

    public class SkillSlotDropZone : MonoBehaviour, IDropHandler
    {
        public SkillSlotView view;
        public SkillSlotDropMode mode;

        public void OnDrop(PointerEventData eventData)
        {
            if (!SkillDragSession.IsDragging)
                return;

            var panel = SkillLoadoutPanel.Current;
            if (panel == null || view == null)
                return;

            if (mode == SkillSlotDropMode.Fixed)
            {
                SkillDragSession.End();
                return;
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr?.SkillSystem == null)
            {
                SkillDragSession.End();
                return;
            }

            var skillId = SkillDragSession.DraggingSkillId;
            var sys = mgr.SkillSystem;

            var behavior = SkillDragSession.ActiveDropBehavior;
            if (behavior == null)
            {
                Debug.Log("Skill drop rejected: no_drop_behavior");
                SkillDragSession.End();
                return;
            }

            if (!behavior.TryDropOnCustomNormalSlot(panel, sys, view.SlotIndex, skillId, out var fail))
            {
                if (!string.IsNullOrEmpty(fail))
                    Debug.Log("Skill drop rejected: " + fail);
            }
            else
            {
                panel.RefreshAll();
            }

            SkillDragSession.End();
        }
    }
}
