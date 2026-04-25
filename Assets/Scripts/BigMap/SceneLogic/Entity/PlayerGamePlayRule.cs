using My;
using My.Map.Entity;
using UnityEngine;

namespace My.Map
{
    // 玩家侧「基础规则」集中地：与玩法相关的可复用判定（偷袭、蹲伏交互条件等）统一写在此类中，
    // 避免散落在 Presenter / EffectExecutor 等处。后续新增同类规则请优先加到这里并在此文件顶部补充说明。
    public static class PlayerGamePlayRule
    {
        public const float SneakVisionRange = 6f;
        public const float SneakVisionFovDeg = 150f;

        public static bool IsPlayerBehindNpcForSneak(NpcUnitLogicEntity npc, PlayerLogicEntity player)
        {
            if (npc == null || player == null)
            {
                return false;
            }

            var vision = MainGameManager.Instance?.VisionSenser2D;
            if (vision == null)
            {
                return false;
            }

            return !vision.SimpleCanSee(npc.Pos, npc.CurrentLook, player.Pos, SneakVisionRange, SneakVisionFovDeg);
        }

        public static bool CanNpcBeSneakTarget(NpcUnitLogicEntity npc)
        {
            if (npc == null || npc.MarkDestroyed || npc.IsDead || npc.MarkUnsensored)
            {
                return false;
            }

            if (npc.IsInCombat)
            {
                return false;
            }

            if (npc.CheckHasState(AttrIdConsts.NoInteract) || npc.CheckHasState(AttrIdConsts.NoSelect))
            {
                return false;
            }

            return true;
        }

        public static bool CanPlayerSneakThisNpc(PlayerLogicEntity player, NpcUnitLogicEntity npc)
        {
            if (player == null || npc == null || !player.IsSpecialCrouchStance)
            {
                return false;
            }

            if (!CanNpcBeSneakTarget(npc))
            {
                return false;
            }

            return IsPlayerBehindNpcForSneak(npc, player);
        }
    }
}
