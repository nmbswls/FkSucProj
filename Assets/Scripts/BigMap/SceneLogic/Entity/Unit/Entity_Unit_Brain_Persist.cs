using My.Map.Entity;
using My.Map.Entity.AI;
using My.Map.Logic;
using UnityEngine;

namespace My.Map.Unit
{
    public partial class AIBrainV2
    {
        public void ExportToNpcRecord(LogicEntityRecord4Npc record)
        {
            if (record == null || NpcEntity == null)
            {
                return;
            }

            var persistState = ResolvePersistState(CurrentState);
            if (persistState == EAIBrainPersistState.None)
            {
                ClearBrainPersistFields(record);
                return;
            }

            record.BrainPersistState = persistState;
            ExportCommonBlackboard(record);
            ExportAggro(record);

            switch (persistState)
            {
                case EAIBrainPersistState.Search:
                    ExportSearch(record);
                    break;
                case EAIBrainPersistState.Return:
                    ExportReturn(record);
                    break;
                case EAIBrainPersistState.ChaseWanted:
                    ExportChaseWanted(record);
                    break;
                case EAIBrainPersistState.PoisonBait:
                    record.BrainPoisonBaitTargetId = PoisonBaitTargetInteractInstId;
                    break;
                case EAIBrainPersistState.Attracted:
                    ExportAttractedFocus(record);
                    break;
            }
        }

        public bool TryRestoreFromNpcRecord(LogicEntityRecord4Npc record)
        {
            if (record == null || NpcEntity == null || NpcEntity.IsDead || NpcEntity.MarkDestroyed)
            {
                return false;
            }

            if (record.BrainPersistState == EAIBrainPersistState.None)
            {
                return false;
            }

            IsRestoringFromPersist = true;
            try
            {
                RestoreCommonBlackboard(record);
                Aggro.RestoreFromPersist(record.BrainAggroTargetId, record.BrainCombatEngaged, record.BrainThreats);

                switch (record.BrainPersistState)
                {
                    case EAIBrainPersistState.Sentry:
                        ChangeState(StateSentry);
                        break;
                    case EAIBrainPersistState.Combat:
                        if (!TryRestoreCombatState())
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.Return:
                        if (!TryRestoreReturnState(record))
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.Flee:
                        ChangeState(StateFlee);
                        break;
                    case EAIBrainPersistState.Search:
                        if (!TryRestoreSearchState(record))
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.ChaseWanted:
                        if (!TryRestoreChaseWantedState(record))
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.Attracted:
                        if (!TryRestoreAttractedState(record))
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.CharmedFollow:
                        if (!NpcEntity.CheckHasBuff("social_charmed"))
                        {
                            ChangeState(StateIdle);
                            break;
                        }
                        ChangeState(StateCharmedFollow);
                        break;
                    case EAIBrainPersistState.PoisonBait:
                        if (!TryRestorePoisonBaitState(record))
                        {
                            return false;
                        }
                        break;
                    case EAIBrainPersistState.HKnockdownFollowup:
                        if (Aggro.CurrentTargetId <= 0)
                        {
                            ChangeState(StateIdle);
                            return false;
                        }

                        if (LogicManager.GetLogicEntity(Aggro.CurrentTargetId, false) == null)
                        {
                            ChangeState(StateIdle);
                            return false;
                        }

                        ChangeState(StateHKnockdownFollowup);
                        break;
                    default:
                        return false;
                }

                TriggerUpdateImmediately();
                return true;
            }
            finally
            {
                IsRestoringFromPersist = false;
            }
        }

        EAIBrainPersistState ResolvePersistState(AIBaseState state)
        {
            if (state == null || state == StateIdle)
            {
                return EAIBrainPersistState.None;
            }

            if (state == StateSentry) return EAIBrainPersistState.Sentry;
            if (state == StateCombat) return EAIBrainPersistState.Combat;
            if (state == StateReturn) return EAIBrainPersistState.Return;
            if (state == StateFlee) return EAIBrainPersistState.Flee;
            if (state == StateSearch) return EAIBrainPersistState.Search;
            if (state == StateAttracted) return EAIBrainPersistState.Attracted;
            if (state == StateCharmedFollow) return EAIBrainPersistState.CharmedFollow;
            if (state == StatePoisonBait) return EAIBrainPersistState.PoisonBait;
            if (state == StateHKnockdownFollowup) return EAIBrainPersistState.HKnockdownFollowup;
            if (StateChaseWanted != null && state == StateChaseWanted) return EAIBrainPersistState.ChaseWanted;

            return EAIBrainPersistState.None;
        }

        static void ClearBrainPersistFields(LogicEntityRecord4Npc record)
        {
            record.BrainPersistState = EAIBrainPersistState.None;
            record.HasBrainHomePos = false;
            record.HasBrainSuspiciousPos = false;
            record.BrainAggroTargetId = 0;
            record.BrainCombatEngaged = false;
            record.BrainThreats?.Clear();

            record.BrainSearchPhase = 0;
            record.BrainSearchOrgPoint = Vector2.zero;
            record.BrainSearchLookEndTime = 0f;

            record.BrainReturnReason = 0;
            record.BrainReturnSourceId = string.Empty;
            record.HasBrainReturnTargetPos = false;
            record.BrainReturnTargetPos = Vector2.zero;
            record.BrainReturnMoveSpeedRate = 0.7f;
            record.BrainReturnReengageDelay = 5f;
            record.BrainReturnIgnoreSuspicion = false;
            record.BrainReturnIgnoreWanted = false;
            record.BrainReturnIgnoreAttract = false;
            record.BrainReturnHealthRecoverDuration = 0f;
            record.BrainReturnInvulnerable = false;

            record.BrainChaseTargetId = 0;
            record.BrainChaseChillEndTime = 0f;
            record.BrainPoisonBaitTargetId = 0;

            record.HasBrainAttractFocus = false;
            record.BrainAttractFocusPos = Vector3.zero;
            record.BrainAttractFocusPriority = 0f;
            record.BrainAttractFocusTimestamp = 0f;
            record.BrainAttractFocusType = 0;
            record.BrainAttractFocusSourceId = 0;
        }

        void ExportCommonBlackboard(LogicEntityRecord4Npc record)
        {
            if (HomePos.HasValue)
            {
                record.HasBrainHomePos = true;
                record.BrainHomePos = HomePos.Value;
            }
            else
            {
                record.HasBrainHomePos = false;
            }

            if (SuspiciousPos.HasValue)
            {
                record.HasBrainSuspiciousPos = true;
                record.BrainSuspiciousPos = SuspiciousPos.Value;
            }
            else
            {
                record.HasBrainSuspiciousPos = false;
            }
        }

        void RestoreCommonBlackboard(LogicEntityRecord4Npc record)
        {
            HomePos = record.HasBrainHomePos ? record.BrainHomePos : NpcEntity.Pos;
            SuspiciousPos = record.HasBrainSuspiciousPos ? record.BrainSuspiciousPos : null;
        }

        void ExportAggro(LogicEntityRecord4Npc record)
        {
            record.BrainAggroTargetId = Aggro.CurrentTargetId;
            record.BrainCombatEngaged = Aggro.CombatEngaged;
            if (record.BrainThreats == null)
            {
                record.BrainThreats = new System.Collections.Generic.List<NpcBrainThreatPersist>();
            }
            Aggro.ExportThreats(record.BrainThreats);
        }

        void ExportSearch(LogicEntityRecord4Npc record)
        {
            if (CurrentState is not AIStateSearch search)
            {
                return;
            }

            record.BrainSearchPhase = search.GetPersistPhase();
            record.BrainSearchOrgPoint = search.GetPersistOrgPoint();
            record.BrainSearchLookEndTime = search.GetPersistLookEndTime();
        }

        void ExportReturn(LogicEntityRecord4Npc record)
        {
            if (CurrentState is not AIStateReturn stateReturn)
            {
                return;
            }

            var ctx = stateReturn.GetReturnContext();
            record.BrainReturnReason = (int)ctx.Reason;
            record.BrainReturnSourceId = ctx.SourceId ?? string.Empty;
            if (ctx.TargetPosition.HasValue)
            {
                record.HasBrainReturnTargetPos = true;
                record.BrainReturnTargetPos = ctx.TargetPosition.Value;
            }
            else
            {
                record.HasBrainReturnTargetPos = false;
            }

            record.BrainReturnMoveSpeedRate = ctx.MoveSpeedRate;
            record.BrainReturnReengageDelay = ctx.ReengageDelay;
            record.BrainReturnIgnoreSuspicion = ctx.IgnoreSuspicion;
            record.BrainReturnIgnoreWanted = ctx.IgnoreWanted;
            record.BrainReturnIgnoreAttract = ctx.IgnoreAttract;
            record.BrainReturnHealthRecoverDuration = ctx.HealthRecoverDuration;
            record.BrainReturnInvulnerable = ctx.InvulnerableDuringReturn;
        }

        void ExportChaseWanted(LogicEntityRecord4Npc record)
        {
            if (CurrentState is not AIStateChaseWanted chase)
            {
                return;
            }

            record.BrainChaseTargetId = chase.GetPersistTargetId();
            record.BrainChaseChillEndTime = chase.GetPersistChillEndTime();
        }

        void ExportAttractedFocus(LogicEntityRecord4Npc record)
        {
            var focus = NpcEntity.CurrentFocus;
            if (focus == null)
            {
                record.HasBrainAttractFocus = false;
                return;
            }

            record.HasBrainAttractFocus = true;
            record.BrainAttractFocusPos = focus.Position;
            record.BrainAttractFocusPriority = focus.BasePriority;
            record.BrainAttractFocusTimestamp = focus.Timestamp;
            record.BrainAttractFocusType = (int)focus.Type;
            record.BrainAttractFocusSourceId = focus.SourceID;
        }

        bool TryRestoreCombatState()
        {
            if (Aggro.CurrentTargetId <= 0)
            {
                ChangeState(StateIdle);
                return false;
            }

            var target = LogicManager.GetLogicEntity(Aggro.CurrentTargetId, false);
            if (target == null)
            {
                ChangeState(StateIdle);
                return false;
            }

            ChangeState(StateCombat);
            return true;
        }

        bool TryRestoreReturnState(LogicEntityRecord4Npc record)
        {
            Vector2? targetPos = record.HasBrainReturnTargetPos ? record.BrainReturnTargetPos : null;
            var ctx = new AIReturnContext(
                (EUnitReturnReason)record.BrainReturnReason,
                record.BrainReturnSourceId,
                targetPos,
                record.BrainReturnMoveSpeedRate,
                record.BrainReturnReengageDelay,
                record.BrainReturnIgnoreSuspicion,
                record.BrainReturnIgnoreWanted,
                record.BrainReturnIgnoreAttract,
                record.BrainReturnHealthRecoverDuration,
                record.BrainReturnInvulnerable);
            StateReturn.Prepare(ctx);
            ChangeState(StateReturn);
            return true;
        }

        bool TryRestoreSearchState(LogicEntityRecord4Npc record)
        {
            ChangeState(StateSearch);
            if (CurrentState is AIStateSearch search)
            {
                search.RestoreFromPersist(
                    record.BrainSearchPhase,
                    record.BrainSearchOrgPoint,
                    record.BrainSearchLookEndTime);
            }
            return true;
        }

        bool TryRestoreChaseWantedState(LogicEntityRecord4Npc record)
        {
            if (StateChaseWanted == null)
            {
                ChangeState(StateIdle);
                return false;
            }

            ChangeState(StateChaseWanted);
            StateChaseWanted.RestoreFromPersist(record.BrainChaseTargetId, record.BrainChaseChillEndTime);
            return true;
        }

        bool TryRestoreAttractedState(LogicEntityRecord4Npc record)
        {
            if (!record.HasBrainAttractFocus)
            {
                ChangeState(StateIdle);
                return false;
            }

            if (LogicTime.time - record.BrainAttractFocusTimestamp > AttractFocusMaxAgeSeconds)
            {
                ChangeState(StateIdle);
                return false;
            }

            NpcEntity.RestorePersistedFocus(
                record.BrainAttractFocusPos,
                record.BrainAttractFocusPriority,
                record.BrainAttractFocusTimestamp,
                (EStimulusType)record.BrainAttractFocusType,
                record.BrainAttractFocusSourceId);
            ChangeState(StateAttracted);
            return true;
        }

        bool TryRestorePoisonBaitState(LogicEntityRecord4Npc record)
        {
            if (record.BrainPoisonBaitTargetId <= 0)
            {
                ChangeState(StateIdle);
                return false;
            }

            var ip = LogicManager.GetLogicEntity(record.BrainPoisonBaitTargetId, false) as LogicEntityInteractPoint;
            if (ip == null || ip.MarkDestroyed || !ip.IsPoisonBaitWindowActive())
            {
                ChangeState(StateIdle);
                return false;
            }

            PoisonBaitTargetInteractInstId = record.BrainPoisonBaitTargetId;
            ChangeState(StatePoisonBait);
            return true;
        }
    }

    public partial class AIStateSearch
    {
        public int GetPersistPhase() => (int)_phase;
        public Vector2 GetPersistOrgPoint() => searchOrgPoint;
        public float GetPersistLookEndTime() => _lookAroundTimer;
    }

    public partial class AIStateChaseWanted
    {
        public long GetPersistTargetId() => wantedUnitId;
        public float GetPersistChillEndTime() => chaseChillTimer;
    }
}
