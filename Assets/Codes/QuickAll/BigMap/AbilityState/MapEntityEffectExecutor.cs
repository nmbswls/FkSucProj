using Map.Entity.Throw;
using System;
using System.Collections;
using System.Collections.Generic;
using Unit.Ability.Effect;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static GameLogicManager;

namespace Map.Entity
{

    public abstract  class AbilityEffectExecutor
    {
        public virtual void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {


        }
    }

    public class AbilityEffectExecutor4UnlockLootPoint : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if(ctx.Target is LootPointLogicEntity lootPoint)
            {
                lootPoint.TryUnlockLootPoint();
            }
            else
            {
                Debug.LogError($"AbilityEffectExecutor4UnlockLootPoint not loot point {ctx.Target.Id}");
            }
        }
    }

    public class AbilityEffectExecutor4UseLootPoint : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (ctx.Target is LootPointLogicEntity lootPoint)
            {
                lootPoint.TryUseLootPoint();
            }
            else
            {
                Debug.LogError($"AbilityEffectExecutor4UseLootPoint not loot point {ctx.Target.Id}");
            }
        }
    }

    

    public class AbilityEffectExecutor4SpawnBullet : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectSpawnBulletCfg;
            if(realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnBullet cfg error");
                return;
            }

            var pData = new ProjectileData()
            {
                maxLifetime = realCfg.lifeTime,
                damage = 5,
                motiontype = realCfg.motionType,
            };

            switch (realCfg.motionType)
            {
                case EMotionType.Linear:
                    {
                        pData.motionData = new LinearMotionData()
                        {
                            speed = realCfg.speed,
                            radius = 0.1f
                        };
                    }
                    break;
            }
            
            ctx.Env.projectileHolder.CreateLogicProjectile(pData, ctx.Actor, ctx.Actor.Pos, ctx.CastDir ?? Vector2.right);
        }
    }
    


    public class AbilityEffectExecutor4UseItem : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg useItemCfg, LogicFightEffectContext ctx)
        {
            MapAbilityEffectUseItemCfg realCfg = useItemCfg as MapAbilityEffectUseItemCfg;
            if(realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseItem err");
            }


        }
    }

    public class AbilityEffectExecutor4UseWeapon : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectUseWeaponCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseWeapon cfg error");
                return;
            }

            if(ctx.Actor is BaseUnitLogicEntity unitEntity)
            {
                unitEntity.abilityController.ApplyUseWeaponHitBox(realCfg.WeaponName, realCfg.Duration, realCfg.OnHitEffects);
            }
        }
    }

    public class AbilityEffectExecutor4DashStart : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectDashStartCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseWeapon cfg error");
                return;
            }

            if (ctx.Actor is BaseUnitLogicEntity unitEntity)
            {
                unitEntity.CreateDashIntent(ctx.CastDir.Value, realCfg.DashDuration, realCfg.DashSpeed);
            }
        }
    }

    public class AbilityEffectExecutor4AddBuff : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectAddBuffCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseWeapon cfg error");
                return;
            }

            long? srcBuffId = null;
            if (ctx.SourceKey != null && ctx.SourceKey.Value.buffId != 0)
            {
                srcBuffId = ctx.SourceKey.Value.buffId;
            }

            // 当目标type为0时 在正常语境下 就是给目标使用
            if (realCfg.TargetType == 0)
            {
                
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.Target.Id, realCfg.BuffId, realCfg.Layer, casterId:ctx.SourceKey?.entityId ?? 0, srcBuffId : srcBuffId);
            }
            else
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.Actor.Id, realCfg.BuffId, realCfg.Layer, casterId: ctx.SourceKey?.entityId ?? 0, srcBuffId: srcBuffId);
            }
        }
    }

    public class AbilityEffectExecutor4HitBox : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectHitBoxCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseWeapon cfg error");
                return;
            }

            List<ILogicEntity> candidates = null;
            // 通过hitbox 找到目标
            if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Square)
            {
                candidates = ctx.Env.visionSenser.OverlapBoxAllEntity(ctx.Actor.Pos, ctx.CastDir.Value, new Vector2(realCfg.Width, realCfg.Length), realCfg.FilterParams);

                // 画矩形（size=宽高）
                float angleDeg = 0f;
                if (ctx.CastDir != null)
                {
                    angleDeg = Mathf.Atan2(ctx.CastDir.Value.y, ctx.CastDir.Value.x) * Mathf.Rad2Deg;
                }
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, ctx.Position.Value, new Vector2(realCfg.Width, realCfg.Length), Color.red, 0.3f, angleDeg: angleDeg);
            }
            else if(realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
            {
                candidates = ctx.Env.visionSenser.OverlapCircleAllEntity(ctx.Actor.Pos, realCfg.Radius, realCfg.FilterParams);
            }
            
            if(candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Type != realCfg.TargetEntityType)
                    {
                        continue;
                    }

                    Debug.LogError("AbilityEffectExecutor4HitBox find logic target " + candidate.Id);

                    foreach (var e in realCfg.OnHitEffects) 
                    {
                        LogicFightEffectContext newCtx = new(ctx.Env, ctx.SourceKey);

                        newCtx.Actor = ctx.Actor;
                        newCtx.CastDir = ctx.CastDir;
                        newCtx.Target = candidate;
                        ctx.Env.HandleLogicFightEffect(e, ctx);
                    }
                }
            }
        }
    }
    

    public class AbilityEffectExecutor4RemoveBuff : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectRemoveBuffCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4RemoveBuff cfg error");
                return;
            }

            if (ctx.Target == null)
            {
                Debug.LogError($"AbilityEffectExecutor4RemoveBuff target err :{ctx.Target?.Id ?? 0}");
                return;
            }

            ctx.Env.globalBuffManager.RemoveAllBuffById(ctx.Target.Id, realCfg.BuffId);
        }
    }

    public class AbilityEffectExecutor4IfBranch : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectIfBranchCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4IfBranch cfg error");
                return;
            }

            bool isTrue = true;
            switch(realCfg.CheckType)
            {
                case MapAbilityEffectIfBranchCfg.ECheckType.HasBuff:
                    {
                        if(ctx.Target.BuffManager.CheckHasBuff(ctx.Target.Id, realCfg.Param1))
                        {
                            isTrue = true;
                        }
                        else
                        {
                            isTrue = false;
                        }
                    }
                    break;
                case MapAbilityEffectIfBranchCfg.ECheckType.AttrGreater:
                    {
                        long val = ctx.Target.GetAttr(realCfg.Param1);
                        if (val > realCfg.Param3)
                        {
                            isTrue = true;
                        }
                        else
                        {
                            isTrue = false;
                        }
                    }
                    break;
            }

            if(isTrue)
            {
                Debug.Log("AbilityEffectExecutor4IfBranch true");

                foreach(var e in realCfg.TrueBranchEffects)
                {
                    ctx.Env.HandleLogicFightEffect(e, ctx);
                }
            }
            else
            {
                Debug.Log("AbilityEffectExecutor4IfBranch false");

                foreach (var e in realCfg.FalseBranchEffects)
                {
                    ctx.Env.HandleLogicFightEffect(e, ctx);
                }
            }
        }
    }


    public class AbilityEffectExecutor4AddResource : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectAddResourceCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4AddResource err");
                return;
            }

            if(ctx.Target == null)
            {
                Debug.LogError("AbilityEffectExecutor4AddResource err");
                return;
            }

            Dictionary<string, long> extraAttrs = null;
            if (realCfg.ExtraAttrInfos != null)
            {
                extraAttrs = new();
                foreach(var pair in realCfg.ExtraAttrInfos)
                {
                    extraAttrs[pair.AttrId] = pair.Val;
                }
            }
            ctx.Target.ApplyResourceChange(realCfg.ResourceId, realCfg.AddValue, false, ctx.SourceKey, extraAttrs);
        }
    }

    public class AbilityEffectExecutor4CostResource : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectCostResourceCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err");
                return;
            }

            if (ctx.Target == null)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err");
                return;
            }

            Dictionary<string, long> extraAttrs = null;
            if (realCfg.ExtraAttrInfos != null)
            {
                extraAttrs = new();
                foreach (var pair in realCfg.ExtraAttrInfos)
                {
                    extraAttrs[pair.AttrId] = pair.Val;
                }
            }

            ctx.Target.ApplyResourceChange(realCfg.ResourceId, -realCfg.CostValue, realCfg.Flags > 0, ctx.SourceKey, extraAttrs);
        }
    }

    public class AbilityEffectExecutor4ThrowStart : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectThrowStartCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err");
                return;
            }

            if (ctx.Target == null || ctx.Target is not IThrowTarget throwTarget)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource throwTarget err");
                return;
            }
            if (ctx.Actor == null || ctx.Actor is not IThrowLauncher throwLauncher)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource throwLauncher err");
                return;
            }
            ctx.Env.globalThrowManager.TryLaunchThrow(throwLauncher, throwTarget, "", realCfg.Duration, realCfg.ThrowMainBuffId, realCfg.Priority);
        }
    }
}