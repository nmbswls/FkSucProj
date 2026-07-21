using System;
using cfg.demo;
using My.Config;
using My.Map.Fight;
using My.Player;
using UnityEngine;

namespace My.Map.Entity
{
    // HAct 统一结算：H强度定盘子，H技巧定上下风，self_coef 定自反馈开口
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
            public BaseUnitLogicEntity Attacker;
            public BaseUnitLogicEntity Defender;
        }

        // 由玩家+NPC 与表内 is_player_passive 决定攻受角色
        public static bool TryResolve(
            int actId,
            BaseUnitLogicEntity player,
            BaseUnitLogicEntity enemy,
            float intensity,
            out HActResolveResult result)
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
                // 缺一侧时：仍用在场单位占位，避免静态入口直接失败
                attacker ??= player;
                defender ??= player;
                if (ReferenceEquals(attacker, defender) && enemy != null)
                {
                    defender = enemy;
                }
            }

            return TryResolveWithRoles(act, attacker, defender, player, enemy, intensity, out result);
        }

        // 技能等已明确攻受时使用
        public static bool TryResolveWithRoles(
            int actId,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            float intensity,
            out HActResolveResult result)
        {
            result = default;
            var act = CfgMgr.Cfgs.TbHActInfo.GetOrDefault(actId);
            if (act == null || attacker == null || defender == null)
            {
                return false;
            }

            var player = attacker as PlayerLogicEntity ?? defender as PlayerLogicEntity;
            var enemy = attacker as NpcUnitLogicEntity ?? defender as NpcUnitLogicEntity;
            return TryResolveWithRoles(act, attacker, defender, player, enemy, intensity, out result);
        }

        static bool TryResolveWithRoles(
            HActInfo act,
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            BaseUnitLogicEntity player,
            BaseUnitLogicEntity enemy,
            float intensity,
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

            int defLevel = Mathf.Max(1, defender.GetUnitLevel());
            double C = 100.0 + 5.0 * defLevel;
            double contest = act.ContestCoef > 0f ? act.ContestCoef : 1f;

            // H技巧对抗
            double adv = (hTechAtk * 0.001 * contest + C) / (hTechDef * 0.001 + C);
            if (adv < 1e-6)
            {
                adv = 1e-6;
            }

            // H强度放大动作基础量
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
                Attacker = attacker,
                Defender = defender,
            };
            return true;
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
            bool applyHpDamage = true)
        {
            if (!TryResolve(actId, player, enemy, intensity, out var result))
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
            float knockBackForce = 0f)
        {
            if (!TryResolveWithRoles(actId, attacker, defender, intensity, out var result))
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
            NotePlayerDrivenNpcInteraction(result, sourceActId);

            if (applyHpDamage && result.HpDamageOnDefender > 0 && result.Defender != null)
            {
                ApplyHActHpDamage(result.Attacker, result.Defender, result.HpDamageOnDefender, knockBackForce);
            }
        }

        // 玩家对 NPC 推进冲击时：刷新 H 会话 + 延长 NpcFcked，供随后内射读取部位榨取
        static void NotePlayerDrivenNpcInteraction(HActResolveResult result, int sourceActId)
        {
            if (sourceActId <= 0 || result.Defender is not NpcUnitLogicEntity npc)
            {
                return;
            }

            if (result.Attacker is not PlayerLogicEntity)
            {
                return;
            }

            var glm = npc.LogicManager;
            var tracker = glm?.playerDataManager?.HInteraction;
            if (tracker == null)
            {
                return;
            }

            tracker.NoteActSettlement(npc.Id, sourceActId);
            glm.globalBuffManager?.AddBuff(npc.Id, "fcked_marked", 1, overrideDuration: HInteractionTracker.DefaultHoldSeconds);
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

        // HAct 派生 HP 伤害：仅走 H 类别纯扣血，不再经普攻减伤管线属性
        public static void ApplyHActHpDamage(
            BaseUnitLogicEntity attacker,
            BaseUnitLogicEntity defender,
            long rawDamage,
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

            // 不写入 HImpulse_Pipeline，避免伤再转冲击
            defender.ApplyResourceChange(
                AttrIdConsts.HP,
                -rawDamage,
                true,
                FightStruct.EDmgFlag.None,
                srcId,
                null,
                EDmgCategory.H,
                srcPos,
                hitDir);

            if (knockBackForce > 0f && srcPos.HasValue && hitDir.HasValue)
            {
                defender.ApplyKnockBack(hitDir.Value, knockBackForce);
            }
        }
    }
}
