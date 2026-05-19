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
                        dmgVal += (long)(getVal * onePair.Val * 0.0001f);
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
            System.Func<long> getDefenderHPower,
            System.Func<long> getBasicJianShang,
            System.Func<long> getNonHJianShang)
        {
            if (rawDeltaNegative >= 0)
            {
                return rawDeltaNegative;
            }

            var rawDmg = System.Math.Abs(rawDeltaNegative);
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
            else if (dmgCategory == EDmgCategory.H)
            {
                var srcHpower = intent.extraAttrs?.GetValueOrDefault(AttrIdConsts.HPower_Pipeline) ?? 0;
                long hReduce10000 = PlayerGamePlayRule.CalcDmgReduceRate10000ByH(srcHpower, getDefenderHPower());
                if (hReduce10000 > 8000)
                {
                    hReduce10000 = 8000;
                }

                dmg = (long)(dmg * (10000 - hReduce10000) * 0.0001);
            }

            var basicJs = getBasicJianShang();
            if (basicJs > 9000)
            {
                basicJs = 9000;
            }

            dmg = (long)(dmg * (10000 - basicJs) * 0.0001);

            if (dmgCategory != EDmgCategory.H)
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
    }
}
