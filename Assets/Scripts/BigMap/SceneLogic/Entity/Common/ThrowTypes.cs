namespace My.Map.Entity
{
    public enum ThrowEventKind
    {
        Accept,
        Align,
        Impact,
        Complete,
        InterruptLauncher,
        InterruptTarget,
        Superseded,
    }

    public enum ThrowEndReason
    {
        Complete,
        InterruptLauncher,
        InterruptTarget,
        Superseded,
    }

    public static class ThrowEndReasonUtil
    {
        public static ThrowEventKind ToEventKind(ThrowEndReason reason) =>
            reason switch
            {
                ThrowEndReason.Complete => ThrowEventKind.Complete,
                ThrowEndReason.InterruptLauncher => ThrowEventKind.InterruptLauncher,
                ThrowEndReason.InterruptTarget => ThrowEventKind.InterruptTarget,
                ThrowEndReason.Superseded => ThrowEventKind.Superseded,
                _ => ThrowEventKind.Complete,
            };
    }
}
