using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Fight;
using UnityEngine;
using static My.Map.Fight.FightStruct;

namespace My.Map.Entity
{
    // HAct 统一结算：H强度定盘子，H技巧定上下风；接触部位贡献与全身同量纲相加
    public static class HActResolver
    {
        public const int DefaultHAttackActId = 100;

        public struct HActResolveResult
        {
            public long ImpulseOnAttacker;
            public long ImpulseOnDefender;
            public long ImpulseOnPlayer;
            public long ImpulseOnEnemy;
            public long HpDamageOnDefender;
            public float PlayerClimaxWeight;
            public EBodyPart ContactPart;
            public long EffectiveHTechniqueAtk;
            public long EffectiveHTechniqueDef;
            public long EffectiveHStrengthAtk;
            public BaseUnitLogicEntity Attacker;
            public BaseUnitLogicEntity Defender;
        }

        public static bool TryResolve(
            int actId,
            BaseUnitLogicEntity player,
            BaseUnitLogicEntity enemy,
            float intensity,
            out HActResolveResult result,
            EBodyPart preferredContactPart = EBodyPart.None)
        {
            result = default;
            var act = CfgMgr.Cfgs.TbHActInfo.GetOrDefault(actId);
            if (act == null || player == null)
            {
                return false;
            }

            BaseUnitLogicEntity attacker;
            BaseUnitLogicEntity defender;
            if (act.IsPlayerPassive)
            {
                attacker = enemy;
                defender = player;
            }
            else
            {
                attacker = player;
                defender = enemy;
            }

            if (attacker == null || defender == null)
            {
                attacker ??= player;
                defender ??= player;
                if (ReferenceEquals(attacker, defender) && enemy != null)
                {
                    defender = enemy;
                }
            }

            return TryResolveWithRoles(
                act, attacker, defender, player, enemy, intensity, preferredContactPart, out result);
        }

        public static bool TryResolveWithRoles(
            int actId,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            float intensity,
            out HActResolveResult result,
            EBodyPart preferredContactPart = EBodyPart.None)
        {
            result = default;
            var act = CfgMgr.Cfgs.TbHActInfo.GetOrDefault(actId);
            if (act == null || attacker == null || defender == null)
            {
                return false;
            }

            var player = attacker as PlayerLogicEntity ?? defender as PlayerLogicEntity;
            var enemy = attacker as NpcUnitLogicEntity ?? defender as NpcUnitLogicEntity;
            return TryResolveWithRoles(
                act, attacker, defender, player, enemy, intensity, preferredContactPart, out result);
        }

        static bool TryResolveWithRoles(
            HActInfo act,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            BaseUnitLogicEntity player,
            BaseUnitLogicEntity enemy,
            float intensity,
            EBodyPart preferredContactPart,
            out HActResolveResult result)
        {
            result = default;
            if (act == null || attacker == null || defender == null)
            {
                return false;
            }

            intensity = Mathf.Max(0.01f, intensity);

            long hTechAtk = Math.Max(0, attacker.GetAttr(AttrIdConsts.HTechnique));
            long hTechDef = Math.Max(0, defender.GetAttr(AttrIdConsts.HTechnique));
            long hStrAtk = Math.Max(0, attacker.GetAttr(AttrIdConsts.HStrength));

            var contactPart = ResolveContactPart(
                act.Id,
                preferredContactPart,
                player as PlayerLogicEntity,
                attacker,
                defender);

            // 对抗：有效技巧/强度 = 全身 + 当前接触部位（仅玩家侧有部位表）
            ApplyPlayerPartContestBonus(
                player as PlayerLogicEntity,
                contactPart,
                attacker,
                defender,
                ref hTechAtk,
                ref hTechDef,
                ref hStrAtk);

            int defLevel = Mathf.Max(1, defender.GetUnitLevel());
            double C = 100.0 + 5.0 * defLevel;
            double contest = act.ContestCoef > 0f ? act.ContestCoef : 1f;

            double adv = (hTechAtk * 0.001 * contest + C) / (hTechDef * 0.001 + C);
            if (adv < 1e-6)
            {
                adv = 1e-6;
            }

            double strengthMul = Math.Max(0.01, hStrAtk * 0.001);
            double effectiveBase = act.HImpulseBase * intensity * strengthMul;

            double impTarget = effectiveBase * adv;
            double impSelf = effectiveBase * (1.0 / adv) * Math.Max(0f, act.SelfCoef);

            long impulseDefender = (long)(impTarget * 1000.0);
            long impulseAttacker = (long)(impSelf * 1000.0);
            long hpDamage = 0;
            if (act.HpFromImpulseCoef > 0f && impulseDefender > 0)
            {
                hpDamage = (long)(impulseDefender * act.HpFromImpulseCoef);
            }

            result = new HActResolveResult
            {
                ImpulseOnAttacker = impulseAttacker,
                ImpulseOnDefender = impulseDefender,
                ImpulseOnPlayer = ResolveSideImpulse(player, attacker, defender, impulseAttacker, impulseDefender),
                ImpulseOnEnemy = ResolveSideImpulse(enemy, attacker, defender, impulseAttacker, impulseDefender),
                HpDamageOnDefender = hpDamage,
                PlayerClimaxWeight = act.PlayerClimaxWeight > 0f ? act.PlayerClimaxWeight : 1f,
                ContactPart = contactPart,
                EffectiveHTechniqueAtk = hTechAtk,
                EffectiveHTechniqueDef = hTechDef,
                EffectiveHStrengthAtk = hStrAtk,
                Attacker = attacker,
                Defender = defender,
            };
            return true;
        }

        // 显式指定 > NPC 会话槽 > HAct 推断
        static EBodyPart ResolveContactPart(
            int actId,
            EBodyPart preferred,
            PlayerLogicEntity player,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender)
        {
            if (preferred != EBodyPart.None)
            {
                return preferred;
            }

            var npc = attacker as NpcUnitLogicEntity ?? defender as NpcUnitLogicEntity;
            if (player != null && npc != null)
            {
                if (ReferenceEquals(player, attacker) &&
                    npc.HInteraction.Receive.TryGet(out var recv) &&
                    recv.ContactPart != EBodyPart.None)
                {
                    return recv.ContactPart;
                }

                if (ReferenceEquals(player, defender) &&
                    npc.HInteraction.Active.TryGet(out var active) &&
                    active.ContactPart != EBodyPart.None)
                {
                    return active.ContactPart;
                }
            }

            return HActContactPart.InferDefault(actId);
        }

        // 玩家攻：部位技巧+强度叠攻方；玩家受：部位技巧叠守方（强度仍是攻方盘子）
        static void ApplyPlayerPartContestBonus(
            PlayerLogicEntity player,
            EBodyPart contactPart,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            ref long hTechAtk,
            ref long hTechDef,
            ref long hStrAtk)
        {
            if (player == null || contactPart == EBodyPart.None)
            {
                return;
            }

            var bodyParts = player.LogicManager?.playerDataManager?.BodyPartSystem;
            if (bodyParts == null)
            {
                return;
            }

            bodyParts.GetCombatBonuses(contactPart, out var partTech, out var partStr);
            if (partTech <= 0 && partStr <= 0)
            {
                return;
            }

            if (ReferenceEquals(player, attacker))
            {
                hTechAtk += partTech;
                hStrAtk += partStr;
            }
            else if (ReferenceEquals(player, defender))
            {
                hTechDef += partTech;
            }
        }

        static long ResolveSideImpulse(
            BaseUnitLogicEntity side,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            long impulseAttacker,
            long impulseDefender)
        {
            if (side == null)
            {
                return 0;
            }

            if (ReferenceEquals(side, attacker))
            {
                return impulseAttacker;
            }

            if (ReferenceEquals(side, defender))
            {
                return impulseDefender;
            }

            return 0;
        }

        public static bool TryResolveAndApply(
            int actId,
            BaseUnitLogicEntity player,
            BaseUnitLogicEntity enemy,
            float intensity = 1f,
            bool applyHpDamage = true,
            EBodyPart preferredContactPart = EBodyPart.None)
        {
            if (!TryResolve(actId, player, enemy, intensity, out var result, preferredContactPart))
            {
                return false;
            }

            ApplyResult(result, applyHpDamage, 0f, actId);
            return true;
        }

        public static bool TryResolveAndApplyWithRoles(
            int actId,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            float intensity = 1f,
            bool applyHpDamage = true,
            float knockBackForce = 0f,
            EBodyPart preferredContactPart = EBodyPart.None)
        {
            if (!TryResolveWithRoles(
                    actId, attacker, defender, intensity, out var result, preferredContactPart))
            {
                return false;
            }

            ApplyResult(result, applyHpDamage, knockBackForce, actId);
            return true;
        }

        public static void ApplyResult(
            HActResolveResult result,
            bool applyHpDamage,
            float knockBackForce = 0f,
            int sourceActId = 0)
        {
            var climaxWeight = result.PlayerClimaxWeight > 0f ? result.PlayerClimaxWeight : 1f;
            ApplyImpulseToUnit(result.Attacker, result.ImpulseOnAttacker, climaxWeight);
            ApplyImpulseToUnit(result.Defender, result.ImpulseOnDefender, climaxWeight);
            NoteNpcHInteractionContexts(result, sourceActId);

            if (applyHpDamage && result.HpDamageOnDefender > 0 && result.Defender != null)
            {
                ApplyHActHpDamage(
                    result.Attacker,
                    result.Defender,
                    result.HpDamageOnDefender,
                    result.ContactPart,
                    knockBackForce);
            }
        }

        static void NoteNpcHInteractionContexts(HActResolveResult result, int sourceActId)
        {
            if (sourceActId <= 0)
            {
                return;
            }

            var contactPart = result.ContactPart != EBodyPart.None
                ? result.ContactPart
                : HActContactPart.InferDefault(sourceActId);
            var glm = result.Attacker?.LogicManager ?? result.Defender?.LogicManager;

            if (result.Attacker is PlayerLogicEntity && result.Defender is NpcUnitLogicEntity npcRecv)
            {
                npcRecv.HInteraction.Receive.NoteAct(
                    sourceActId, contactPart, EHInteractionSource.CombatSkill);
                glm?.globalBuffManager?.AddBuff(
                    npcRecv.Id, "fcked_marked", 1,
                    overrideDuration: HInteractionSlot.DefaultHoldSeconds);
                return;
            }

            if (result.Attacker is NpcUnitLogicEntity npcAtk && result.Defender is PlayerLogicEntity)
            {
                npcAtk.HInteraction.Active.NoteAct(
                    sourceActId, contactPart, EHInteractionSource.CombatSkill);
            }
        }

        static void ApplyImpulseToUnit(BaseUnitLogicEntity unit, long impulse, float playerClimaxWeight = 1f)
        {
            if (unit == null || impulse <= 0)
            {
                return;
            }

            if (unit is PlayerLogicEntity player)
            {
                player.ApplyHImpulseDirectly(impulse, null, playerClimaxWeight);
            }
            else if (unit is NpcUnitLogicEntity npc)
            {
                npc.ApplyNpcHImpulse(impulse);
            }
        }

        public static void ApplyHActHpDamage(
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            long rawDamage,
            EBodyPart contactPart = EBodyPart.None,
            float knockBackForce = 0f)
        {
            if (defender == null || rawDamage <= 0)
            {
                return;
            }

            long? srcId = attacker != null ? attacker.Id : null;
            Vector2? srcPos = attacker != null ? attacker.Pos : null;
            Vector2? hitDir = null;
            if (srcPos.HasValue)
            {
                var diff = defender.Pos - srcPos.Value;
                if (diff.sqrMagnitude > 1e-8f)
                {
                    hitDir = diff.normalized;
                }
            }

            var extraAttrs = BuildHActHpExtraAttrs(defender, contactPart);

            defender.ApplyResourceChange(
                AttrIdConsts.HP,
                -rawDamage,
                true,
                EDmgFlag.None,
                srcId,
                extraAttrs,
                EDmgCategory.H,
                srcPos,
                hitDir);

            if (knockBackForce > 0f && srcPos.HasValue && hitDir.HasValue)
            {
                defender.ApplyKnockBack(hitDir.Value, knockBackForce);
            }
        }

        static Dictionary<string, long> BuildHActHpExtraAttrs(
            BaseUnitLogicEntity defender,
            EBodyPart contactPart)
        {
            if (contactPart == EBodyPart.None)
            {
                return null;
            }

            var extraAttrs = new Dictionary<string, long>(4)
            {
                [AttrIdConsts.HitPart_Pipeline] = (int)contactPart,
            };

            if (defender is PlayerLogicEntity playerDef)
            {
                playerDef.LogicManager?.playerDataManager?.BodyPartSystem
                    ?.CaptureCombatPipelineAttrs(contactPart, extraAttrs);
            }

            return extraAttrs;
        }
    }
}
