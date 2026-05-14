using System.Collections.Generic;
using My;
using My.Map;
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

        // 非挂起时段累积的「投技会话时钟」：TimeFromStart 与 Duration 均基于此，挂起时不扣墙钟
        float _throwTimelineProgressClock;
        float _throwTimelineLastWallTime;
        bool _throwTimelineWallAnchored;

        public readonly Dictionary<string, string> RunningVars = new();
        public TimelineHoldSession ActiveHold;

        // 出手方 Layer0 动画栈句柄；投技结束时 ReleaseAnimRequestForced
        public long LauncherHoldAnimHandle;

        // true：投技对齐进行中，用于 FreezeThrowTimelineDuringLauncherAlign；不经 Dash
        bool _launcherAlignClockFreezeActive;
        Vector2 _launcherAlignStartPos;
        Vector2 _launcherAlignEndPos;

        public float ThrowNonHoldElapsed => _throwTimelineProgressClock;

        // 表现层结算：写入 RunningVars、清空 ActiveHold（不调用 UI；HUD 自行关闭）
        public void CompleteActiveHold(bool success)
        {
            if (ActiveHold == null || ActiveHold.Resolved)
            {
                return;
            }

            TimelineHoldSession h = ActiveHold;
            h.Resolved = true;
            RunningVars[h.ResultVarKey] = success ? TimelineHoldSession.OutcomeSuccess : TimelineHoldSession.OutcomeFail;
            ActiveHold = null;
        }

        // 投技结束或打断时由 GlobalThrowManager 调用：丢弃未结算的时段输入等待（时间轴不再被阻塞）
        public void OnThrowTermination()
        {
            ActiveHold = null;
        }

        public void TryStartLauncherHoldAnim()
        {
            LauncherHoldAnimHandle = 0;
            if (Env == null || SourceCfg == null || Launcher == null)
            {
                return;
            }

            string tag = SourceCfg.LauncherHoldAnimTag;
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            var ent = Env.GetLogicEntity(Launcher.Id, false);
            if (ent is not LogicEntityBase le)
            {
                return;
            }

            string resolved = le.GetAnimOverride(tag.Trim());
            LauncherHoldAnimHandle = le.PushAnimRequest(new AnimPlayRequest
            {
                AnimName = resolved,
                Layer = 0,
                Source = EAnimRequestSource.ThrowContext,
                ReleasePolicy = EAnimReleasePolicy.None,
                AbilitySessionId = CtxId,
                AbilityPhaseIndex = -1,
            });
        }

        public void ClearLauncherHoldAnim()
        {
            if (LauncherHoldAnimHandle == 0 || Env == null || Launcher == null)
            {
                LauncherHoldAnimHandle = 0;
                return;
            }

            if (Env.GetLogicEntity(Launcher.Id, false) is LogicEntityBase le)
            {
                le.ReleaseAnimRequestForced(LauncherHoldAnimHandle);
            }

            LauncherHoldAnimHandle = 0;
        }

        public void TryBeginLauncherAlignSmooth()
        {
            _launcherAlignClockFreezeActive = false;
            if (Env == null || SourceCfg == null || Launcher == null || Target == null)
            {
                return;
            }

            if (!SourceCfg.AlignLauncherToTargetOnStart || SourceCfg.AlignLauncherDuration <= 1e-4f)
            {
                return;
            }

            if (Env.GetLogicEntity(Launcher.Id, false) is not BaseUnitLogicEntity unit)
            {
                return;
            }

            Vector2 dest;
            if (Env.viewer != null
                && Env.viewer.TryGetThrowGrappleLogicPos(Target.Id, SourceCfg.AlignTargetGrappleSocketPath, out var gripLogic))
            {
                dest = gripLogic + SourceCfg.AlignLauncherLogicOffset;
            }
            else
            {
                dest = Target.Pos + SourceCfg.AlignLauncherLogicOffset;
            }

            Vector2 delta = dest - unit.Pos;
            float dist = delta.magnitude;
            if (dist < 0.02f)
            {
                return;
            }

            _launcherAlignStartPos = unit.Pos;
            _launcherAlignEndPos = dest;
            _launcherAlignClockFreezeActive = true;
        }

        public void ClearLauncherAlignState()
        {
            _launcherAlignClockFreezeActive = false;
        }

        void AdvanceLauncherAlignInterpolated(float logicTimeNow)
        {
            if (!_launcherAlignClockFreezeActive || SourceCfg == null || Env == null || Launcher == null)
            {
                return;
            }

            if (Env.GetLogicEntity(Launcher.Id, false) is not BaseUnitLogicEntity unit)
            {
                _launcherAlignClockFreezeActive = false;
                return;
            }

            float dur = Mathf.Max(1e-4f, SourceCfg.AlignLauncherDuration);
            float u = Mathf.Clamp01((logicTimeNow - StartTime) / dur);
            Vector2 p = Vector2.Lerp(_launcherAlignStartPos, _launcherAlignEndPos, u);
            unit.TeleportTo(p);
            if (u >= 1f - 1e-5f)
            {
                _launcherAlignClockFreezeActive = false;
            }
        }

        public void StopLauncherControlledMoveIfAny()
        {
            if (Env == null || Launcher == null)
            {
                return;
            }

            if (Env.GetLogicEntity(Launcher.Id, false) is BaseUnitLogicEntity unit && unit.controlledMoveCtx != null)
            {
                unit.EndControlledMove(0);
            }
        }

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

        // 受害者挣脱：Src=受害者，TargetId=投技发起者；CastVec1 自受害者指向发起者
        public GameLogicManager.LogicFightEffectContext BuildTargetBreakFreeFightEffectContext(int throwTimelineEventIndex = -1)
        {
            var srcInfo = new GameLogicManager.EffectSourceInfo
            {
                SrcType = GameLogicManager.ESourceType.Throw,
                SrcEntityId = Target.Id,
                SrcAbilityId = SrcAbilityId,
            };
            var efCtx = new GameLogicManager.LogicFightEffectContext(Env, GameLogicManager.EFightCtxType.Ability, srcInfo);
            efCtx.TargetId = Launcher.Id;
            efCtx.TriggerPos = Launcher.Pos;
            var delta = Launcher.Pos - Target.Pos;
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

        void RunTargetBreakFreeEffectList(List<MapFightEffectCfg> effects)
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

                var ctx = BuildTargetBreakFreeFightEffectContext(-1);
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
                case ThrowEndReason.TargetBreakFree:
                    RunTargetBreakFreeEffectList(cfg.OnTargetBreakFreeEffects);
                    break;
            }
        }

        public void TryDispatchTimelineEvents(float logicTimeNow)
        {
            if (Env == null)
            {
                return;
            }

            AdvanceLauncherAlignInterpolated(logicTimeNow);

            if (!_throwTimelineWallAnchored)
            {
                _throwTimelineLastWallTime = StartTime;
                _throwTimelineWallAnchored = true;
            }

            float dtWall = Mathf.Max(0f, logicTimeNow - _throwTimelineLastWallTime);
            bool holdBlocksClock = ActiveHold != null && !ActiveHold.Resolved;
            bool alignFreeze = _launcherAlignClockFreezeActive
                && SourceCfg != null
                && SourceCfg.FreezeThrowTimelineDuringLauncherAlign
                && logicTimeNow < StartTime + SourceCfg.AlignLauncherDuration;
            if (!holdBlocksClock && !alignFreeze)
            {
                _throwTimelineProgressClock += dtWall;
            }

            _throwTimelineLastWallTime = logicTimeNow;

            var list = SourceCfg?.ThrowTimelineEvents;
            if (list == null || list.Count == 0)
            {
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (_firedTimelineIndices.Contains(i))
                {
                    continue;
                }

                if (ActiveHold != null && !ActiveHold.Resolved && i > ActiveHold.HoldBlocksTimelineRowsAfterIndex)
                {
                    continue;
                }

                var row = list[i];
                if (row == null || row.Effects == null)
                {
                    continue;
                }

                if (_throwTimelineProgressClock < row.TimeFromStart)
                {
                    continue;
                }

                _firedTimelineIndices.Add(i);
                RunEffectList(row.Effects, Target.Id, i);
            }
        }
    }
}
