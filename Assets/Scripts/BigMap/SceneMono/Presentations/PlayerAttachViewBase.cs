using UnityEngine;

namespace My.Map.Scene
{
    public readonly struct PlayerAttachViewContext
    {
        public readonly string AttachId;
        public readonly int SameTypeCount;
        public readonly int VisibleIndex;

        public PlayerAttachViewContext(string attachId, int sameTypeCount, int visibleIndex)
        {
            AttachId = attachId;
            SameTypeCount = Mathf.Max(0, sameTypeCount);
            VisibleIndex = Mathf.Max(0, visibleIndex);
        }
    }

    public abstract class PlayerAttachViewBase : MonoBehaviour
    {
        PlayerAttachViewContext _context;
        bool _configured;

        public void Configure(in PlayerAttachViewContext context)
        {
            if (_configured
                && _context.AttachId == context.AttachId
                && _context.SameTypeCount == context.SameTypeCount
                && _context.VisibleIndex == context.VisibleIndex)
            {
                return;
            }

            _context = context;
            _configured = true;
            OnConfigure(context);
        }

        protected abstract void OnConfigure(in PlayerAttachViewContext context);
    }
}
