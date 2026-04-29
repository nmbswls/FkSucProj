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

        public const float MaxSensitiveBonus = 4.0f;

        public const float SneakVisionRange = 6f;
        public const float SneakVisionFovDeg = 150f;

        public static int GetPleasuAddByGazePower(int playerLevel, int gazePower)
        {
            return 1;
        }

        public static int CalculateUnitAttractedLevel(int playerLevel, long playerCharm, long exposeRate, long will)
        {
            long attactPower = playerCharm * 1 + (long)(exposeRate * 0.0001 * 50_000);

            float K = 100 + playerLevel * 10;
            long costRate = (long)((will * 0.001) / (will * 0.001 + K) * 10000);
            if(costRate > 9500)
            {
                costRate = 9500;
            }
            attactPower = (long)(attactPower * (10000 - costRate) * 0.0001);

            if(attactPower > 30_000)
            {
                return 1;
            }
            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="desireLevel"></param>
        /// <param name="distNow"></param>
        /// <param name="willProtect"></param>
        /// <returns></returns>
        public static long CalculatePlayerDesireAuraEffect(int desireLevel, float distNow, float willProtect)
        {
            long baseVal = 100; // 0.1 每秒
            float distMin = 0.5f;
            float distMax = 2.5f;

            float distRate = (distNow - distMin) / (distMax - distMin);
            distRate = Mathf.Clamp(distRate, 0, 1);
            distRate = 1 - distRate;
            
            return (long)(baseVal * (distRate) * willProtect);
        }

        /// <summary>
        /// 计算一般情况下魅力与意志对抗的抵抗力
        /// 永远在0-1之间
        /// </summary>
        /// <param name="playerLevel"></param>
        /// <param name="unitLevel"></param>
        /// <param name="playerCharm"></param>
        /// <param name="unitWill"></param>
        /// <returns></returns>
        public static float CalculateUnitWIillProtectParam(int playerLevel, int unitLevel, long playerCharm, long unitWill)
        {
            float k1 = 1.0f;
            var districtP = (playerCharm * 0.001) / ((playerCharm * 0.001) + unitWill * 0.001 * k1);
            return (float)districtP;
        }

        public static long GetFaQingIncreaseByJingYuLayer(long layer)
        {
            return 500;
        }

        public static long GetFinalArm(this ILogicEntity entity)
        {
            var armWhite = entity.GetAttr(AttrIdConsts.Arm_White);
            var armPercent = entity.GetAttr(AttrIdConsts.ArmPercent_White);

            var armExtra1 = entity.GetAttr(AttrIdConsts.Arm_Extra_1);

            return (long)(armWhite * (10000 + armPercent) * 0.0001 + armExtra1);
        }

        public static long GetFinalHPower(this ILogicEntity entity)
        {
            return entity.GetAttr(AttrIdConsts.HPower);
        }

        public static long GetFinalCharm(this PlayerLogicEntity player)
        {
            return player.GetAttr(AttrIdConsts.PlayerCharm);
        }

        /// <summary>
        /// 将敏感度应用渐进模型压缩
        /// </summary>
        /// <param name="rawValue"></param>
        /// <returns></returns>
        public static long CalculateSensitiveBonus(long rawValue)
        {
            float K = 200;
            long ret = (long)((1 + MaxSensitiveBonus * (rawValue) / (rawValue + K)) * 10000);
            return ret;
        }


        public static long CalcDmgReduceRate10000ByArm(int attackLevel, long armValue)
        {
            float K = 100 + attackLevel * 10;
            return (long)((armValue * 0.001) / (armValue * 0.001 + K) * 10000);
        }

        public static long CalcDmgReduceRate10000ByH(long hPowerAttacker, long hPowerTarget)
        {
            float K = 0.5f;
            return (long)((hPowerAttacker * 1.0f) / (hPowerAttacker + K * hPowerTarget) * 1000);
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
