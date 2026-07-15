using System.Collections.Generic;
using My.Saving;

namespace My.Map.Logic
{
    // 动态压迫守卫 session 的运行时进度（与 ResolveKind 策略配置分离）
    public enum EWantedPressurePhase
    {
        // 已纳入 Wanted 管理，ResolveKind=0 的无宏观行为占位守卫
        None = 0,
        // 搜查前：等待 Search 结束并由 Wanted Tick 下发 macro
        AwaitingInvestigation = 1,
        // 搜查后 macro 已生效（kind=2/3）
        MacroActive = 2,
        // 离场中（kind=1，或 Cull/Trim 触发）
        WalkingAway = 3,
    }

    // Wanted 动态守卫注册表条目：ResolveKind 为策略，Phase 为进度，macro 快照供读档重放
    public sealed class WantedPressureSession
    {
        public long EntityId;
        // 策略配置（TbWantedGuardSpawnTier.pressure_behavior），生命周期内不变：0=无宏观行为，1=离场，2=站立，3=路网巡逻
        public int ResolveKind;
        public int PatrolPickN = 3;
        // 运行时进度，随搜查/macro/离场推进；与 ResolveKind 正交（如 ResolveKind=0 时恒为 None）
        public EWantedPressurePhase Phase = EWantedPressurePhase.None;
        public bool HasMoveToDespawnTarget;
        public UnityEngine.Vector2 MoveToDespawnTarget;
        public string PatrolPortalNetworkId = string.Empty;
        public List<string> PatrolCycleNodeIds = new List<string>();

        public static WantedPressureSession FromPersist(WantedPressureSessionPersist persist)
        {
            if (persist == null || persist.EntityId <= 0)
            {
                return null;
            }

            var phase = persist.Phase;
            if (phase == EWantedPressurePhase.MacroActive && persist.ResolveKind == 1)
            {
                phase = EWantedPressurePhase.WalkingAway;
            }

            var session = new WantedPressureSession
            {
                EntityId = persist.EntityId,
                ResolveKind = persist.ResolveKind,
                PatrolPickN = persist.PatrolPickN > 0 ? persist.PatrolPickN : 3,
                Phase = phase,
                HasMoveToDespawnTarget = persist.HasMoveToDespawnTarget,
                MoveToDespawnTarget = persist.MoveToDespawnTarget,
                PatrolPortalNetworkId = persist.PatrolPortalNetworkId ?? string.Empty,
            };

            session.PatrolCycleNodeIds.Clear();
            if (persist.PatrolCycleNodeIds != null && persist.PatrolCycleNodeIds.Count > 0)
            {
                session.PatrolCycleNodeIds.AddRange(persist.PatrolCycleNodeIds);
            }

            return session;
        }

        public WantedPressureSessionPersist ToPersist()
        {
            return new WantedPressureSessionPersist
            {
                EntityId = EntityId,
                ResolveKind = ResolveKind,
                PatrolPickN = PatrolPickN > 0 ? PatrolPickN : 3,
                Phase = Phase,
                HasMoveToDespawnTarget = HasMoveToDespawnTarget,
                MoveToDespawnTarget = MoveToDespawnTarget,
                PatrolPortalNetworkId = PatrolPortalNetworkId ?? string.Empty,
                PatrolCycleNodeIds = PatrolCycleNodeIds != null
                    ? new List<string>(PatrolCycleNodeIds)
                    : new List<string>(),
            };
        }

        public static EWantedPressurePhase ResolveInitialPhase(int resolveKind, bool beginInvestigationImmediately)
        {
            if (resolveKind <= 0)
            {
                return EWantedPressurePhase.None;
            }

            if (beginInvestigationImmediately)
            {
                return EWantedPressurePhase.AwaitingInvestigation;
            }

            return resolveKind == 1
                ? EWantedPressurePhase.WalkingAway
                : EWantedPressurePhase.MacroActive;
        }

        public static EWantedPressurePhase ResolvePhaseAfterInvestigation(int resolveKind)
        {
            return resolveKind == 1
                ? EWantedPressurePhase.WalkingAway
                : EWantedPressurePhase.MacroActive;
        }

        public bool ShouldBeginInvestigation()
        {
            return Phase == EWantedPressurePhase.AwaitingInvestigation && ResolveKind > 0;
        }

        public bool ShouldReplayMacroBehave()
        {
            return Phase == EWantedPressurePhase.MacroActive
                || Phase == EWantedPressurePhase.WalkingAway;
        }

        public bool HasPersistedPatrol()
        {
            return PatrolCycleNodeIds != null && PatrolCycleNodeIds.Count >= 2;
        }
    }
}
