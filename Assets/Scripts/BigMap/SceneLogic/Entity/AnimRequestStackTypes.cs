using System;

namespace My.Map
{
    // 动画请求来源（用于过滤释放、调试）
    public enum EAnimRequestSource : byte
    {
        Manual = 0,
        AbilityPhase = 1,
        Buff = 2,
        Dialog = 3,
    }

    // 何时允许从栈中移除该请求（可多选）
    [Flags]
    public enum EAnimReleasePolicy : int
    {
        None = 0,
        OnClipEnd = 1 << 0,
        OnPhaseExit = 1 << 1,
        OnAbilityEnd = 1 << 2,
    }

    public static class AnimReleasePolicyUtil
    {
        // 技能 phase 默认：阶段结束 / 技能结束 / 非循环片段播完 均可回收
        public const EAnimReleasePolicy DefaultAbilityPhase =
            EAnimReleasePolicy.OnClipEnd | EAnimReleasePolicy.OnPhaseExit | EAnimReleasePolicy.OnAbilityEnd;
    }

    public enum EAnimReleaseReason : byte
    {
        Manual = 0,
        ClipEnded = 1,
        PhaseCleanup = 2,
        AbilityEnded = 3,
    }

    public struct AnimPlayRequest
    {
        public string AnimName;
        public int Layer;
        public EAnimRequestSource Source;
        public EAnimReleasePolicy ReleasePolicy;
        public long AbilitySessionId;
        public int AbilityPhaseIndex;
    }

    public readonly struct AnimStackTopSnapshot
    {
        public readonly long Handle;
        public readonly string AnimName;
        public readonly int Layer;
        public readonly EAnimRequestSource Source;
        public readonly EAnimReleasePolicy ReleasePolicy;

        public AnimStackTopSnapshot(long handle, string animName, int layer, EAnimRequestSource source, EAnimReleasePolicy releasePolicy)
        {
            Handle = handle;
            AnimName = animName;
            Layer = layer;
            Source = source;
            ReleasePolicy = releasePolicy;
        }

        public bool IsEmpty => Handle == 0;
    }

    public sealed class AnimLayerRefreshEventArgs : EventArgs
    {
        public int Layer { get; }
        public AnimStackTopSnapshot? Top { get; }

        public AnimLayerRefreshEventArgs(int layer, AnimStackTopSnapshot? top)
        {
            Layer = layer;
            Top = top;
        }
    }
}
