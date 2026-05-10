namespace My.Map.Entity
{
    // 投技时间轴内单次 QTE：仅判空格；结果写入 ThrowContext.RunningVars[ResultVarKey]
    public sealed class ThrowQteSession
    {
        public string QteId;
        public string PromptText;
        public string ResultVarKey;
        public string SuccessValue;
        public string FailValue;
        public float TimeoutAtLogicTime;
        public int HoldTimelineAfterIndex;
        public bool Resolved;
    }
}
