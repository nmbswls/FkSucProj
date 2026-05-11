using My;
using My.Map;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My.Map.Entity
{
    public interface IThrowTarget
    {
        long Id { get; }

        Vector2 Pos { get; }
        bool CanBeThrow();

        void OnBeingThrowStart();

        void OnBeingThrowInterrupt();
    }

    public interface IThrowLauncher
    {
        long Id { get; }

        Vector2 Pos { get; }

        void OnThrownInterrupt();

        void OnThrowStart();
    }

    public class GlobalThrowManager
    {
        public GameLogicManager logicManager;
        public int MaxTriggerDepthPerFrame = 6;

        public static long ThrowCtxInstIdCounter = 1000;

        private readonly Dictionary<long, ThrowContext> _contextById = new();
        private readonly Dictionary<long, long> _targetToContextId = new();
        private readonly Dictionary<long, long> _launcherToContextId = new();

        public GlobalThrowManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }

        public void Tick(float dt)
        {
            TickRunningCtx();
        }

        float _fxAcc;

        public void TickRunningCtx()
        {
            foreach (var key in _contextById.Keys.ToList())
            {
                if (!_contextById.TryGetValue(key, out var ctx))
                {
                    continue;
                }

                ctx.TryDispatchTimelineEvents(LogicTime.time);

                if (ctx.Duration >= 0 && ctx.ThrowNonHoldElapsed > ctx.Duration)
                {
                    CleanOneThrowContext(ctx, ThrowEndReason.Complete);
                }
            }

            _fxAcc += LogicTime.deltaTime;
            if (_fxAcc >= 0.15f)
            {
                _fxAcc = 0f;
                foreach (var ctx in _contextById.Values)
                {
                }
            }
        }

        public bool TryGetThrowContextByTargetId(long targetId, out ThrowContext ctx)
        {
            if (_targetToContextId.TryGetValue(targetId, out var ctxId) && _contextById.TryGetValue(ctxId, out ctx))
            {
                return true;
            }

            ctx = null;
            return false;
        }

        public void EndThrowAsPlayerBreakFree(ThrowContext ctx)
        {
            if (ctx == null || !_contextById.ContainsKey(ctx.CtxId))
            {
                return;
            }

            CleanOneThrowContext(ctx, ThrowEndReason.PlayerBreakFree);
        }

        public bool TryLaunchThrow(IThrowLauncher launcher, IThrowTarget target, MapAbilityEffectThrowStartCfg cfg,
            string srcAbilityId)
        {
            cfg ??= new MapAbilityEffectThrowStartCfg();

            if (_launcherToContextId.TryGetValue(launcher.Id, out var launcherOldCtxId))
            {
                if (_contextById.TryGetValue(launcherOldCtxId, out _))
                {
                    Debug.LogError("cant throw when throwing " + launcher.Id);
                    return false;
                }

                Debug.LogError("interrupt status error wrong state (launcher)");
                _launcherToContextId.Remove(launcher.Id);
            }

            if (_targetToContextId.TryGetValue(target.Id, out var targetOldCtxId))
            {
                if (_contextById.TryGetValue(targetOldCtxId, out var targetOldCtx))
                {
                    if (cfg.Priority <= targetOldCtx.Priority)
                    {
                        Debug.Log("target is being throw no bigger priority");
                        return false;
                    }

                    CleanOneThrowContext(targetOldCtx, ThrowEndReason.Superseded);
                }
                else
                {
                    Debug.LogError("interrupt status error wrong state (target ctx missing)");
                    _targetToContextId.Remove(target.Id);
                }
            }

            var newCtx = new ThrowContext
            {
                CtxId = ThrowCtxInstIdCounter++,
                Launcher = launcher,
                Target = target,
                SrcAbilityId = srcAbilityId ?? string.Empty,
                Priority = cfg.Priority,
                StartTime = LogicTime.time,
                Duration = cfg.Duration,
                InterruptMask = default,
                SourceCfg = cfg,
                Env = logicManager,
            };

            launcher.OnThrowStart();
            target.OnBeingThrowStart();

            var hasTimeline = cfg.ThrowTimelineEvents != null && cfg.ThrowTimelineEvents.Count > 0;
            var legacyBuffOnly = !hasTimeline && !string.IsNullOrEmpty(cfg.ThrowMainBuffId);

            TrackBuff(newCtx, logicManager.globalBuffManager.AddBuff(launcher.Id, "throwing"));
            TrackBuff(newCtx, logicManager.globalBuffManager.AddBuff(target.Id, "throwing"));

            if (legacyBuffOnly)
            {
                TrackBuff(newCtx, logicManager.globalBuffManager.AddBuff(target.Id, cfg.ThrowMainBuffId, casterId: launcher.Id));
            }
            else if (hasTimeline && !string.IsNullOrEmpty(cfg.ThrowMainBuffId))
            {
                TrackBuff(newCtx, logicManager.globalBuffManager.AddBuff(target.Id, cfg.ThrowMainBuffId, casterId: launcher.Id));
            }

            _launcherToContextId[launcher.Id] = newCtx.CtxId;
            _targetToContextId[target.Id] = newCtx.CtxId;
            _contextById[newCtx.CtxId] = newCtx;

            newCtx.TryBeginLauncherAlignSmooth();
            newCtx.TryStartLauncherHoldAnim();
            newCtx.TryDispatchTimelineEvents(LogicTime.time);

            return true;
        }

        static void TrackBuff(ThrowContext ctx, long buffInstanceId)
        {
            if (buffInstanceId != 0)
            {
                ctx.TrackedBuffIds.Add(buffInstanceId);
            }
        }

        public bool TryInterruptThrowByLauncher(IThrowLauncher launcher, Fight.FightStruct.InterruptRequest req)
        {
            if (!_launcherToContextId.TryGetValue(launcher.Id, out var launcherCtxId))
            {
                return false;
            }

            if (!_contextById.TryGetValue(launcherCtxId, out var launcherCtx))
            {
                Debug.LogError("interrupt status error wrong state");
                _launcherToContextId.Remove(launcher.Id);
                return false;
            }

            if (req.source != Fight.FightStruct.EInterruptSource.Stun && req.source != Fight.FightStruct.EInterruptSource.System)
            {
                return false;
            }

            CleanOneThrowContext(launcherCtx, ThrowEndReason.InterruptLauncher);
            return true;
        }

        public bool TryInterruptThrowByTarget(IThrowTarget target, Fight.FightStruct.InterruptRequest req)
        {
            if (!_targetToContextId.TryGetValue(target.Id, out var ctxId))
            {
                return false;
            }

            if (!_contextById.TryGetValue(ctxId, out var ctx))
            {
                Debug.LogError("interrupt status error wrong state (target interrupt)");
                _targetToContextId.Remove(target.Id);
                return false;
            }

            if (req.source != Fight.FightStruct.EInterruptSource.Stun
                && req.source != Fight.FightStruct.EInterruptSource.System
                && req.source != Fight.FightStruct.EInterruptSource.Cast)
            {
                return false;
            }

            CleanOneThrowContext(ctx, ThrowEndReason.InterruptTarget);
            return true;
        }

        void CleanOneThrowContext(ThrowContext ctx, ThrowEndReason reason)
        {
            ctx.ClearLauncherHoldAnim();
            ctx.StopLauncherControlledMoveIfAny();
            ctx.OnThrowTermination();

            var launcher = ctx.Launcher;
            var victim = ctx.Target;

            ctx.DispatchTerminationEffects(reason);
 
            launcher.OnThrownInterrupt();
            victim.OnBeingThrowInterrupt();

            foreach (var buffId in ctx.TrackedBuffIds)
            {
                logicManager.globalBuffManager.RequestRemoveBuff(null, buffId);
            }

            _targetToContextId.Remove(victim.Id);
            _launcherToContextId.Remove(launcher.Id);
            _contextById.Remove(ctx.CtxId);

            if (victim is BaseUnitLogicEntity unit)
            {
                unit.ApplyKnockBack(UnityEngine.Random.insideUnitCircle, 0.2f);
            }
        }
    }
}
