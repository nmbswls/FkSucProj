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
        public bool ImpactFired;
        readonly HashSet<int> _firedTimelineIndices = new();

        public GameLogicManager.LogicFightEffectContext BuildFightEffectContext(long targetEntityId)
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
            return efCtx;
        }

        public void DispatchThrowEffects(ThrowEventKind kind)
        {
            if (SourceCfg?.ThrowPhaseEffects == null || Env == null)
                return;

            foreach (var spec in SourceCfg.ThrowPhaseEffects)
            {
                if (spec == null || spec.Kind != kind || spec.Effects == null)
                    continue;

                foreach (var eff in spec.Effects)
                {
                    if (eff == null) continue;
                    var ctx = BuildFightEffectContext(Target.Id);
                    Env.HandleLogicFightEffect(eff, ctx);
                }
            }
        }

        public void TryDispatchTimelineEvents(float logicTimeNow)
        {
            var list = SourceCfg?.ThrowTimelineEvents;
            if (list == null || list.Count == 0 || Env == null)
                return;

            var elapsed = logicTimeNow - StartTime;
            for (var i = 0; i < list.Count; i++)
            {
                if (_firedTimelineIndices.Contains(i))
                    continue;

                var row = list[i];
                if (row == null || row.Effects == null)
                    continue;
                if (elapsed < row.TimeFromStart)
                    continue;

                _firedTimelineIndices.Add(i);
                foreach (var eff in row.Effects)
                {
                    if (eff == null) continue;
                    var ctx = BuildFightEffectContext(Target.Id);
                    Env.HandleLogicFightEffect(eff, ctx);
                }
            }
        }
    }
}
