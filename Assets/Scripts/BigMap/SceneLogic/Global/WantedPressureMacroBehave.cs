using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    // Wanted 压迫会话的 MacroMoveBehave 写入；首次采样结果写入 session 供读档重放
    static class WantedPressureMacroBehave
    {
        public static bool TryApplyResolveKind(
            NpcUnitLogicEntity npc,
            WantedPressureSession session,
            MapExportDatabase db,
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
                    return TryApplyMoveToDespawn(npc, session, db, out applied);
                case 2:
                    return TryApplyStandStill(npc, out applied);
                case 3:
                    return TryApplyPatrol(npc, session, db, out applied);
                default:
                    return false;
            }
        }

        public static bool TryApplyWalkingAway(
            NpcUnitLogicEntity npc,
            WantedPressureSession session,
            MapExportDatabase db,
            out bool applied)
        {
            return TryApplyMoveToDespawn(npc, session, db, out applied);
        }

        static bool TryApplyMoveToDespawn(
            NpcUnitLogicEntity npc,
            WantedPressureSession session,
            MapExportDatabase db,
            out bool applied)
        {
            applied = false;
            if (npc == null || session == null)
            {
                return false;
            }

            Vector2 exitPos;
            if (session.HasMoveToDespawnTarget)
            {
                exitPos = session.MoveToDespawnTarget;
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

                session.MoveToDespawnTarget = exitPos;
                session.HasMoveToDespawnTarget = true;
            }

            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
            if (move == null)
            {
                return false;
            }

            move.MoveToDespawnTarget = exitPos;
            move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.MoveToThenDespawn;
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
            applied = true;
            return true;
        }

        static bool TryApplyPatrol(
            NpcUnitLogicEntity npc,
            WantedPressureSession session,
            MapExportDatabase db,
            out bool applied)
        {
            applied = false;
            if (npc == null || session == null)
            {
                return false;
            }

            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted);
            if (move == null)
            {
                return false;
            }

            var ids = move.PatrolCycleNodeIds;
            ids.Clear();

            if (session.HasPersistedPatrol())
            {
                move.PatrolPortalNetworkId = session.PatrolPortalNetworkId ?? string.Empty;
                ids.AddRange(session.PatrolCycleNodeIds);
                move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.Patrol;
                applied = true;
                return true;
            }

            if (db == null)
            {
                return false;
            }

            int nPick = Mathf.Max(2, session.PatrolPickN > 0 ? session.PatrolPickN : 3);
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

            session.PatrolPortalNetworkId = resolvedNet ?? string.Empty;
            session.PatrolCycleNodeIds.Clear();
            session.PatrolCycleNodeIds.AddRange(sampledIds);

            move.PatrolPortalNetworkId = session.PatrolPortalNetworkId;
            ids.AddRange(session.PatrolCycleNodeIds);
            move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.Patrol;
            applied = true;
            return true;
        }
    }
}
