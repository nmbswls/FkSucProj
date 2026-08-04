using System;
using System.Collections.Generic;
using Map.Entity;
using My.Map.Entity;

namespace My.Map.Fight
{
    public static class DamagePipeline
    {
        public static long BuildRawDamage(MapFightEffectApplyDamageCfg cfg, IFightAttrProvider src)
        {
            if (cfg == null)
            {
                return 0;
            }

            long dmgVal = cfg.BaseDamage;
            if (cfg.ExtraDamageRate != null && src != null)
            {
                foreach (var onePair in cfg.ExtraDamageRate)
                {
                    if (src.TryGetAttr(onePair.AttrId, out var getVal))
                    {
                        dmgVal += (long)(getVal * onePair.Val / 10000);
                    }
                }
            }

            return dmgVal;
        }

        public static long ResolveHImpulseRate10000(MapFightEffectApplyDamageCfg cfg, GameLogicManager.EffectSourceInfo sourceInfo)
        {
            float hRate = 0;
            if (cfg.HRate != 0)
            {
                hRate = cfg.HRate;
            }
            else if (!string.IsNullOrEmpty(sourceInfo?.SrcAbilityId))
            {
                var abCfg = AbilityLibrary.GetAbilityConfig(sourceInfo.SrcAbilityId);
                if (abCfg != null)
                {
                    switch (abCfg.HImpulseMode)
                    {
                        case EHImpluseMode.Light:
                            hRate = 0.5f;
                            break;
                        case EHImpluseMode.Middle:
                            hRate = 1f;
                            break;
                        case EHImpluseMode.Heavy:
                            hRate = 1.5f;
                            break;
                    }
                }
            }

            return (long)(hRate * 10000);
        }

        public static Dictionary<string, long> BuildPipelineExtraAttrs(
            MapFightEffectApplyDamageCfg cfg,
            IFightAttrProvider src,
            long hImpulseRate10000)
        {
            var extraAttrs = new Dictionary<string, long>();
            if (cfg?.ExtraAttrs != null)
            {
                foreach (var pair in cfg.ExtraAttrs)
                {
                    extraAttrs[pair.AttrId] = pair.Val;
                }
            }

            if (src != null)
            {
                FightAttrCapture.CaptureInto(src, extraAttrs, FightAttrCaptureKind.AllCombatSource);
            }

            extraAttrs[AttrIdConsts.HImpulse_Pipeline] = hImpulseRate10000;
            return extraAttrs;
        }

        public static long ResolveHpDeltaCore(
            long rawDeltaNegative,
            EDmgCategory dmgCategory,
            ResourceDeltaIntent intent,
            System.Func<long> getDefenderExtraDmgRate,
            System.Func<long> getDefenderArm,
            System.Func<long> getBasicJianShang,
            System.Func<long> getNonHJianShang)
        {
            if (rawDeltaNegative >= 0)
            {
                return rawDeltaNegative;
            }

            var rawDmg = System.Math.Abs(rawDeltaNegative);

            // HAct �����˺������ٳԶ����˺�/����/H���ɼ���/�������˵�ͨ�ù���
            if (dmgCategory == EDmgCategory.H)
            {
                // 跳过护甲等；只消费 intent 内已折算的 entity 同名 pipeline（如 HStrength_Pipeline）
                return -ResolveHCategoryHpDamage(rawDmg, intent);
            }

            long dmg = rawDmg;

            var extra1 = getDefenderExtraDmgRate();
            if (extra1 < -9500)
            {
                extra1 = -9500;
            }

            dmg = (long)(dmg * (10000 + extra1) * 0.0001);

            if (dmgCategory == EDmgCategory.Physics)
            {
                var srcLevel = intent.extraAttrs?.GetValueOrDefault(AttrIdConsts.SrcLevel_Pipeline) ?? 0;
                long selfArm = getDefenderArm();
                long armReduce10000 = PlayerGamePlayRule.CalcDmgReduceRate10000ByArm((int)srcLevel, selfArm);
                if (armReduce10000 > 8000)
                {
                    armReduce10000 = 8000;
                }

                dmg = (long)(dmg * (10000 - armReduce10000) * 0.0001);
            }

            var basicJs = getBasicJianShang();
            if (basicJs > 9000)
            {
                basicJs = 9000;
            }

            dmg = (long)(dmg * (10000 - basicJs) * 0.0001);

            {
                var nonH = getNonHJianShang();
                if (nonH > 9000)
                {
                    nonH = 9000;
                }

                dmg = (long)(dmg * (10000 - nonH) * 0.0001);
            }

            return -dmg;
        }

        // H 类最终扣血：读取边界已写入的 HStrength_Pipeline（部位贡献折成的同量纲毫点）
        public static long ResolveHCategoryHpDamage(long rawDmg, ResourceDeltaIntent intent)
        {
            if (rawDmg <= 0)
            {
                return 0;
            }

            long partStr = intent?.extraAttrs?.GetValueOrDefault(AttrIdConsts.HStrength_Pipeline) ?? 0;
            if (partStr <= 0)
            {
                return rawDmg;
            }

            var adjusted = (long)(rawDmg * 1000.0 / (1000.0 + partStr * 0.001));
            return adjusted < 1 ? 1 : adjusted;
        }

        public static (long, long) DistributeClimaxAndEstrusFromHImpulse(long hImpulse, IFightAttrProvider src)
        {
            src.TryGetAttr(AttrIdConsts.PhysicalResist, out var naishou); // ��ȡ����

            double p_k = 2.0;
            double p = (hImpulse * 0.001) / (hImpulse * 0.001 + naishou * 0.001 * p_k + 10); // ����p��͸�� һ���ֳ������Ϊ������ һ����ʩ�Ӹ��߳���
            double addClimax = hImpulse * p;
            long maxEstrus = 3_000;

            double addEstrus = 0;
            // ��ֹ��С�˺��в�
            if (hImpulse > 3000)
            {
                double e = 1.5;
                addEstrus = (maxEstrus * 0.001) * Math.Pow((1 - p), e); 
            }

            return ((long)(addClimax * 1000), (long)(addEstrus * 1000));
        }

        /// <summary>
        /// ͨ��h�����������������ֵ
        /// </summary>
        /// <param name="hImpulse"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        public static long CalculateDmgBonusedHImpulse(long hParam, long rawDmg, int level)
        {
            double M = 3.0f;
            double kScale = level * 10 + 10;
            double bonus = 1 + M * (rawDmg * 0.001) / (rawDmg * 0.001 + kScale);
            return (long)(hParam * bonus);
        }
    }
}
