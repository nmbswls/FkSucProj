using My;
using My.Config;
using My.Map.Entity;
using My.Player;
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


        public const float BaseSuccessChance = 0.62f;
        public const float PhysicalFormPenalty = 0.00003f;
        public const float FailTempEnmity = 42f;


        //public static float GetBaseBackHitSuccessChange()
        //{
        //    return BaseSuccessChance;
        //}

        public static int GetDropBundleFromStaticZha(int score)
        {
            if(score <= 2)
            {
                return 10000;
            }
            else if(score <= 6)
            {
                return 10001;
            }
            else
            {
                return 10002;
            }
        }


        /// <summary>
        /// 计算对抗成功几率
        /// 10000万分比
        /// </summary>
        /// <param name="body1"></param>
        /// <param name="body2"></param>
        /// <returns></returns>
        public static long CalcBodyVsRate(long body1, long body2)
        {
            if (body1 <= 0) return 0;
            if (body2 <= 0) return 10000;

            if(body1 >= body2)
            {
                double v = 1 - (body2 * 0.001) / (2 * body1 * 0.001);
                return (long)(v * 10000);
            }
            else
            {
                double v = (body1 * 0.001) / (2 * body2 * 0.001);
                return (long)(v * 10000);
            }
        }

        /// <summary>
        /// 计算对抗成功几率
        /// 10000万分比
        /// </summary>
        /// <param name="body1"></param>
        /// <param name="body2"></param>
        /// <returns></returns>
        public static long CalcBodyVs(long body1, long body2)
        {
            double v = (body1 * 0.001 * body1 * 0.001) * 1.0 / (body1 * 0.001 * body1 * 0.001 + body2 * 0.001 * body2 * 0.001);
            return (long)(v * 10000);
        }

        public static string GetTrueHSpiritName(string npcBase, int playerLevel)
        {
            return "h_spirit_small_01";
        }

        public static int GetJingYuLevel(long jingyuVal)
        {
            var cfgs = CfgMgr.Cfgs.TbPlayerJingYuLevel.DataList;
            int level = 0;
            for (int i=0;i< cfgs.Count;i++)
            {
                int needAmount = cfgs[i].NeedAmount;
                if (jingyuVal < needAmount * 1000)
                {
                    break;
                }
                level = cfgs[i].Level - 1;
            }
            return level;
        }

        public static long GetFinalBlurtDmg(string npcCfgId, long sjPlus1, long sjPlus2)
        {
            var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(npcCfgId);
            var attr = CfgMgr.Cfgs.TbUnitNpcAttr.GetOrDefault(npcCfg?.AttrTemplateId??0);
            if(attr == null)
            {
                return 10000;
            }
            var amount = attr.BaseBlurtAmount;
            amount = (amount + sjPlus1 * 0.001f) * (1 + sjPlus2 * 0.0001f);
            if(amount <= 0)
            {
                return 0;
            }

            var dmgPerAmount = attr.BaseBlurtDmg;

            return (long)(amount * dmgPerAmount * 1000);
        }


        public static long GetHSpiritRestoreSan(string cfgId)
        {
            if (string.IsNullOrEmpty(cfgId))
            {
                return 5000;
            }

            //foreach (var row in CfgMgr.Cfgs.TbSpiritMonsterTypeBudget.DataList)
            //{
            //    if (row != null && row.NpcCfgId == cfgId)
            //    {
            //        return row.RestoreSan;
            //    }
            //}

            return 5000;
        }
        public static int GetPleasuAddByGazePower(int playerLevel, int gazePower)
        {
            if(gazePower > 5)
            {
                return 100;
            }
            return 0;
        }

        public static long GetClothesRawOverRate10000ForGameplay(GameLogicManager glm)
        {
            if (glm.PlayerHumanMode)
            {
                return 10000L;
            }

            var magicMgr = glm.playerDataManager.MagicClothes;
            if (magicMgr.IsLockedWithSelection)
            {
                return magicMgr.GetRawOverRate10000ForRefresh();
            }

            return 10000L;
        }

        public static int CalculateUnitAttractedLevel(GameLogicManager glm,  long will)
        {
            long rawOverRate = GetClothesRawOverRate10000ForGameplay(glm);
            var exposeRate10000 = PlayerGamePlayRule.CalculateBreakClothesInnerRate(glm.playerLogicEntity.GetAttr(AttrIdConsts.PlayerClothes), rawOverRate);

            float aMax = 100;


            var aBase = aMax * ((1 - exposeRate10000 * 0.0001) * (1 - exposeRate10000 * 0.0001) * (1 + glm.playerLogicEntity.GetAttr(AttrIdConsts.PlayerEstrusProgrss) * 1.0 / 100_000 * 0.4));
            var charm = glm.playerLogicEntity.GetAttr(AttrIdConsts.PlayerCharm);
            float weff = Math.Max(0, will - charm);

            float K = 100 + glm.playerDataManager.Level * 10;

            double sF = aBase * (K) / (K + weff);
            if(sF < 30)
            {
                return 0;
            }
            else if(sF < 60)
            {
                return 1;
            }
            else if(sF < 85)
            {
                return 2;
            }
            return 3;
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


        /// <summary>
        /// 返回的是0-10000的实际覆盖率
        /// 综合考虑衣物本身覆盖率
        /// </summary>
        /// <param name="currentClothes"></param>
        /// <param name="rawOverRate"></param>
        /// <returns></returns>
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

        public const string Item_JingYuan = "jingyuan";
        public static long GetCurrencyMaxStack(GameLogicManager glm, string itemId)
        {
            switch(itemId)
            {
                case Item_JingYuan:
                    {
                        var extraSlots = glm.playerDataManager.ProgressionSystem.GetFinalAttribute((int)EYCAttribute.ExtraJingYuanSlot);
                        return 600 + extraSlots;
                    }
                    break;
            }
            return 99999999;
        }
    }
}
