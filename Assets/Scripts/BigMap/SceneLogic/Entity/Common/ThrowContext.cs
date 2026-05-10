using System.Collections.Generic;
using My;
using UnityEngine;

namespace My.Map.Entity
{
    public sealed class ThrowContext
    {
        public long CtxId;
        public IThrowLauncher Launcher;
        public IThrowTarget Target;
        public string SrcAbilityId;
        public float StartTime;
        public float Duration;
        public int Priority;
        public EAbilityInterruptMask InterruptMask;
        public readonly List<long> TrackedBuffIds = new();
        public MapAbilityEffectThrowStartCfg SourceCfg;
        public GameLogicManager Env;
        readonly HashSet<int> _firedTimelineIndices = new();

        public readonly Dictionary<string, string> RunningVars = new();
        public ThrowQteSession ActiveQte;

        public GameLogicManager.LogicFightEffectContext BuildFightEffectContext(long targetEntityId, int throwTimelineEventIndex = -1)
        {
            var srcInfo = new GameLogicManager.EffectSourceInfo
            {
                SrcType = GameLogicManager.ESourceType.Throw,
                SrcEntityId = Launcher.Id,
                SrcAbilityId = SrcAbilityId,
            };
            var efCtx = new GameLogicManager.LogicFightEffectContext(Env, GameLogicManager.EFightCtxType.Ability, srcInfo);
            efCtx.TargetId = targetEntityId;
            efCtx.TriggerPos = Target.Pos;
            var delta = Target.Pos - Launcher.Pos;
            efCtx.CastVec1 = delta.sqrMagnitude > 1e-6f ? delta.normalized : Vector2.right;
            efCtx.ThrowTimelineEventIndex = throwTimelineEventIndex;
            foreach (var kv in RunningVars)
            {
                efCtx.RunningVariables[kv.Key] = kv.Value;
            }

            return efCtx;
        }

        void RunEffectList(List<MapFightEffectCfg> effects, long primaryTargetEntityId, int timelineIndex)
        {
            if (effects == null || Env == null)
            {
                return;
            }

            foreach (var eff in effects)
            {
                if (eff == null)
                {
                    continue;
                }

                var ctx = BuildFightEffectContext(primaryTargetEntityId, timelineIndex);
                Env.HandleLogicFightEffect(eff, ctx);
            }
        }

        public void DispatchTerminationEffects(ThrowEndReason reason)
        {
            var cfg = SourceCfg;
            if (cfg == null || Env == null)
            {
                return;
            }

            switch (reason)
            {
                case ThrowEndReason.Complete:
                    RunEffectList(cfg.OnThrowCompleteEffects, Target.Id, -1);
                    break;
                case ThrowEndReason.InterruptLauncher:
                    RunEffectList(cfg.OnInterruptLauncherEffects, Target.Id, -1);
                    break;
                case ThrowEndReason.InterruptTarget:
                    RunEffectList(cfg.OnInterruptTargetEffects, Target.Id, -1);
                    break;
                case ThrowEndReason.Superseded:
                    RunEffectList(cfg.OnSupersededEffects, Target.Id, -1);
                    break;
                case ThrowEndReason.QteBreakFree:
                    RunEffectList(cfg.OnQteBreakFreeEffects, Target.Id, -1);
                    break;
            }
        }

        public void TryDispatchTimelineEvents(float logicTimeNow)
        {
            var list = SourceCfg?.ThrowTimelineEvents;
            if (list == null || list.Count == 0 || Env == null)
            {
                return;
            }

            var elapsed = logicTimeNow - StartTime;
            for (var i = 0; i < list.Count; i++)
            {
                if (ActiveQte != null && !ActiveQte.Resolved && i > ActiveQte.HoldTimelineAfterIndex)
                {
                    continue;
                }

                if (_firedTimelineIndices.Contains(i))
                {
                    continue;
                }

                var row = list[i];
                if (row == null || row.Effects == null)
                {
                    continue;
                }

                if (elapsed < row.TimeFromStart)
                {
                    continue;
                }

                _firedTimelineIndices.Add(i);
                RunEffectList(row.Effects, Target.Id, i);
            }
        }
    }
}
