using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    // Wanted macro 写入 Override，并同步 LogicEntityRecord4Npc 供读档重放
    static class WantedPressureMacroBehave
    {
        public static bool TryApplyResolveKind(
            NpcUnitLogicEntity npc,
            WantedPressureSession session,
            MapExportDatabase db,
            int patrolPickN,
            out bool applied)
        {
            applied = false;
            if (npc == null || session == null || session.ResolveKind <= 0)
            {
                return false;
            }

            switch (session.ResolveKind)
            {
                case 1:
                    return TryApplyMoveToDespawn(npc, db, out applied);
                case 2:
                    return TryApplyStandStill(npc, out applied);
                case 3:
                    return TryApplyPatrol(npc, db, patrolPickN, out applied);
                default:
                    return false;
            }
        }

        public static bool TryApplyWalkingAway(
            NpcUnitLogicEntity npc,
            MapExportDatabase db,
            out bool applied)
        {
            return TryApplyMoveToDespawn(npc, db, out applied);
        }

        static bool TryApplyMoveToDespawn(
            NpcUnitLogicEntity npc,
            MapExportDatabase db,
            out bool applied)
        {
            applied = false;
            if (npc == null)
            {
                return false;
            }

            var rec = npc.NpcRecord;
            Vector2 exitPos;
            if (HasRecordMoveToDespawn(rec))
            {
                exitPos = rec.MoveToDespawnTarget;
            }
            else
            {
                if (db == null
                    || !DynamicPressureGuardUtil.TryPickRandomNamedPointPosition(
                        db,
                        ENamedPointType.GuardSpawner,
                        out exitPos))
                {
                    return false;
                }
            }

            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
            if (move == null)
            {
                return false;
            }

            move.MoveToDespawnTarget = exitPos;
            move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn;
            WriteMacroToRecord(rec, move);
            applied = true;
            return true;
        }

        static bool TryApplyStandStill(NpcUnitLogicEntity npc, out bool applied)
        {
            applied = false;
            if (npc == null)
            {
                return false;
            }

            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
            if (move == null)
            {
                return false;
            }

            move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove;
            WriteMacroToRecord(npc.NpcRecord, move);
            applied = true;
            return true;
        }

        static bool TryApplyPatrol(
            NpcUnitLogicEntity npc,
            MapExportDatabase db,
            int patrolPickN,
            out bool applied)
        {
            applied = false;
            if (npc == null)
            {
                return false;
            }

            var rec = npc.NpcRecord;
            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
            if (move == null)
            {
                return false;
            }

            var ids = move.PatrolCycleNodeIds;
            ids.Clear();

            if (HasRecordPatrol(rec))
            {
                move.PatrolPortalNetworkId = rec.PatrolPortalNetworkId ?? string.Empty;
                ids.AddRange(rec.PatrolCycleNodeIds);
                move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.Patrol;
                applied = true;
                return true;
            }

            if (db == null)
            {
                return false;
            }

            int nPick = Mathf.Max(2, patrolPickN > 0 ? patrolPickN : 3);
            var portalNetId = npc.BaseMoveBehaveInfo?.PatrolPortalNetworkId ?? string.Empty;
            var sampledIds = new List<string>();
            if (!DynamicPressureGuardUtil.TrySamplePatrolCycleIds(
                    db,
                    portalNetId,
                    nPick,
                    sampledIds,
                    out var resolvedNet))
            {
                return false;
            }

            move.PatrolPortalNetworkId = resolvedNet ?? string.Empty;
            ids.AddRange(sampledIds);
            move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.Patrol;
            WriteMacroToRecord(rec, move);
            applied = true;
            return true;
        }

        public static bool HasRecordPatrol(LogicEntityRecord4Npc rec)
        {
            return rec != null
                && rec.PatrolCycleNodeIds != null
                && rec.PatrolCycleNodeIds.Count >= 2;
        }

        public static bool HasRecordMoveToDespawn(LogicEntityRecord4Npc rec)
        {
            return rec != null
                && rec.MoveBehaveType == UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn;
        }

        public static void WriteMacroToRecord(LogicEntityRecord4Npc rec, UnitMoveBehaveInfo move)
        {
            if (rec == null || move == null)
            {
                return;
            }

            rec.MoveBehaveType = move.MoveBehaveMode;
            rec.MoveToDespawnTarget = move.MoveToDespawnTarget;
            rec.PatrolPortalNetworkId = move.PatrolPortalNetworkId ?? string.Empty;
            rec.PatrolCycleNodeIds ??= new List<string>();
            rec.PatrolCycleNodeIds.Clear();
            if (move.PatrolCycleNodeIds != null && move.PatrolCycleNodeIds.Count > 0)
            {
                rec.PatrolCycleNodeIds.AddRange(move.PatrolCycleNodeIds);
            }
        }

        public static void SyncEffectiveMacroToRecord(NpcUnitLogicEntity npc)
        {
            if (npc == null
                || !npc.HasMacroMoveBehave
                || npc.MacroMoveBehaveAuthority != BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted)
            {
                return;
            }

            WriteMacroToRecord(npc.NpcRecord, npc.MoveBehaveInfo);
        }
    }
}
