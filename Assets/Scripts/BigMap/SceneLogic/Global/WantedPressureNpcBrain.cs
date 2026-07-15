using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Logic
{
    // Wanted 压迫守卫对 NPC Brain 的唯一写入入口
    static class WantedPressureNpcBrain
    {
        public static bool IsAvailable(NpcUnitLogicEntity npc)
        {
            return npc != null && !npc.MarkDestroyed && !npc.IsDead && npc.AIBrain != null;
        }

        public static void SyncHomeToFeet(NpcUnitLogicEntity npc)
        {
            if (!IsAvailable(npc))
            {
                return;
            }

            npc.AIBrain.HomePos = npc.Pos;
        }

        public static bool IsIdle(NpcUnitLogicEntity npc)
        {
            return IsAvailable(npc) && npc.AIBrain.CurrentState == npc.AIBrain.StateIdle;
        }

        public static bool IsInSearch(NpcUnitLogicEntity npc)
        {
            return IsAvailable(npc) && npc.AIBrain.CurrentState == npc.AIBrain.StateSearch;
        }

        public static bool TryBeginInvestigation(GameLogicManager logic, NpcUnitLogicEntity npc)
        {
            if (!IsAvailable(npc) || logic?.playerLogicEntity == null)
            {
                return false;
            }

            if (IsInSearch(npc))
            {
                return false;
            }

            npc.AIBrain.SuspiciousPos = logic.playerLogicEntity.Pos;
            npc.AIBrain.ChangeState(npc.AIBrain.StateSearch);
            return true;
        }

        public static void RefreshIdleMacro(NpcUnitLogicEntity npc)
        {
            if (!IsAvailable(npc))
            {
                return;
            }

            npc.AIBrain.RefreshIdlePolicy();
        }

        public static void EnterIdle(NpcUnitLogicEntity npc)
        {
            if (!IsAvailable(npc))
            {
                return;
            }

            npc.AIBrain.ChangeState(npc.AIBrain.StateIdle);
        }

        public static bool HasWantedMacro(NpcUnitLogicEntity npc)
        {
            return IsAvailable(npc)
                && npc.HasMacroMoveBehave
                && npc.MacroMoveBehaveAuthority == BaseUnitLogicEntity.EMacroBehaveAuthority.Wanted;
        }
    }
}
