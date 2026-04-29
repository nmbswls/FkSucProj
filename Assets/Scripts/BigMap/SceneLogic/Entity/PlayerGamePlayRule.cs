using My;
using My.Map.Entity;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace My.Map
{
    // 玩家侧「基础规则」集中地：与玩法相关的可复用判定（偷袭、蹲伏交互条件等）统一写在此类中，
    // 避免散落在 Presenter / EffectExecutor 等处。后续新增同类规则请优先加到这里并在此文件顶部补充说明。
    public static class PlayerGamePlayRule
    {
        public const float ClothesStartBreakLine = 0.7f;
        public const float DefaultClothesOverRate = 100;


        public const float SneakVisionRange = 6f;
        public const float SneakVisionFovDeg = 150f;


        public static long GetFinalArm(this ILogicEntity entity)
        {
            var armWhite = entity.GetAttr(AttrIdConsts.Arm_White);
            var armPercent = entity.GetAttr(AttrIdConsts.ArmPercent_White);

            var armExtra1 = entity.GetAttr(AttrIdConsts.Arm_Extra_1);

            return (long)(armWhite * (10000 + armPercent) * 0.0001 + armExtra1);
        }

        public static long CalcDmgReduceRate10000ByArm(int attackLevel, long armValue)
        {
            float K = 100 + attackLevel * 10;
            return (long)((armValue * 1.0f) / (armValue + K) * 1000);
        }

        public static long GetCharmWillCompare(long charmVal, long will)
        {
            long baseRate = 6000;
            long crushThread = 50_000;
            long minChange = 500;

            long rate = baseRate + (long)(((charmVal - will) * 1.0 / crushThread) / (10000 - baseRate));
            rate = Math.Clamp(rate, 0, 10000);
            return rate;
        }

        public static float GetBreakClothesParam(long currentClothes)
        {
            return Math.Max(0, 1 - (currentClothes) * 1.0f / (ClothesStartBreakLine * 100_000));
        }


        public static int CalculateBreakClothesInnerRate(long currentClothes, long rawOverRate)
        {
            float brokeParam = PlayerGamePlayRule.GetBreakClothesParam(currentClothes);
            long clothesOverRate = rawOverRate; // 衣物覆盖率

            int applyRate = (int)((1 - (clothesOverRate * 0.0001f * (1 - brokeParam))) * 10000);
            applyRate = applyRate / 500 * 500; // 使用500档位

            return applyRate;
        }

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
