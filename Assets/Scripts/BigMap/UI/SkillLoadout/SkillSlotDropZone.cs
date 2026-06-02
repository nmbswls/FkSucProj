using UnityEngine;
using UnityEngine.EventSystems;

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
            SkillDragSession.TryCommitDropToZone(this);
        }
    }
}
