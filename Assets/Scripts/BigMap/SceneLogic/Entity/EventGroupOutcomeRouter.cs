using System.Collections.Generic;

namespace My.Map
{
    public sealed class EventGroupOutcomeContext
    {
        public IEntityInteractable Owner;
        public int PlayerId;
        public string OutcomeKind;
        public string ActionId;
    }

    public sealed class EventGroupOutcomeRouter
    {
        public delegate bool OutcomeHandler(EventGroupOutcomeContext context, out string failReason);

        readonly Dictionary<string, OutcomeHandler> _handlers = new();

        public void Register(string outcomeKind, OutcomeHandler handler)
        {
            if (!string.IsNullOrEmpty(outcomeKind) && handler != null)
            {
                _handlers[outcomeKind] = handler;
            }
        }

        public bool TryResolve(EventGroupOutcomeContext context, out string failReason)
        {
            failReason = null;
            var outcomeKind = context?.OutcomeKind;
            // Backward compatibility for events created before OutcomeKind moved to the Output payload.
            if (string.IsNullOrEmpty(outcomeKind))
            {
                outcomeKind = context?.Owner?.GetRuntimeVariable("event_outcome_kind");
            }
            if (string.IsNullOrEmpty(outcomeKind)
                || !_handlers.TryGetValue(outcomeKind, out var handler))
            {
                failReason = "event_outcome_not_registered";
                return false;
            }

            return handler(context, out failReason);
        }
    }
}
