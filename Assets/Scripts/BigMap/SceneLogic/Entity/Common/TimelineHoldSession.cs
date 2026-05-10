using My.Map;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技时间轴内一次「等待玩家时段输入」：命中窗内按 Space；结果写入 RunningVars[ResultVarKey]
    public sealed class TimelineHoldSession
    {
        public const float HitWindowMinNormalized = 0.35f;
        public const float HitWindowMaxNormalized = 0.75f;

        // 与 HUD 两环「半径数值重合」的时刻一致：命中窗的归一化时间中点
        public static float HitWindowCenterNormalized =>
            (HitWindowMinNormalized + HitWindowMaxNormalized) * 0.5f;

        public const string OutcomeSuccess = "1";
        public const string OutcomeFail = "0";

        public float StartLogicTime;
        public float TimeoutAtLogicTime;
        public string ResultVarKey;
        public bool Resolved;

        // 由效果执行器在触发该行时写入（非策划配置）：未结算前不派发时间轴上更晚的行
        public int HoldBlocksTimelineRowsAfterIndex;

        // 上一逻辑 tick 结束时的归一化进度，用于判定单帧跨过命中带仍算命中
        public float LastSampledNormalizedProgress;

        public float GetNormalizedProgress()
        {
            float dur = TimeoutAtLogicTime - StartLogicTime;
            if (dur <= 1e-4f)
            {
                return 1f;
            }

            return Mathf.Clamp01((LogicTime.time - StartLogicTime) / dur);
        }

        public bool IsInHitWindow(float normalizedProgress)
        {
            return normalizedProgress + 1e-5f >= HitWindowMinNormalized
                   && normalizedProgress - 1e-5f <= HitWindowMaxNormalized;
        }

        public bool SegmentIntersectsHitWindow(float fromProgress, float toProgress)
        {
            float lo = Mathf.Min(fromProgress, toProgress);
            float hi = Mathf.Max(fromProgress, toProgress);
            return hi >= HitWindowMinNormalized - 1e-5f && lo <= HitWindowMaxNormalized + 1e-5f;
        }
    }
}
