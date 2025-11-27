using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Logic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem.HID;
using static My.GameLogicManager;

namespace My.Map.Entity
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
                motiontype = realCfg.motionType,

                TriggerOnLifeEnd = realCfg.TriggerOnLifeEnd,
                TriggerOnCollide = realCfg.TriggerOnCollide,
            };

            pData.OnHitEffects.AddRange(realCfg.HitEffects);

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

            if (ctx.Actor == null || ctx.Actor is not BaseUnitLogicEntity unitEntity)
            {
                return;
            }

            Vector2 dir;
            float duration;
            if (realCfg.IsFixPointMode)
            {
                if(realCfg.IsTargetDir)
                {
                    Vector2 targetPos = ctx.Target.Pos;
                    Vector2 or0gPos = ctx.Actor.Pos;
                    var diff = (targetPos - or0gPos);
                    dir = diff.normalized;
                    duration = diff.magnitude / realCfg.DashSpeed - 0.02f;
                }
                else
                {
                    Vector2 targetPos = ctx.CastDir.Value;
                    Vector2 or0gPos = ctx.Actor.Pos;
                    var diff = (targetPos - or0gPos);
                    dir = diff.normalized;
                    duration = diff.magnitude / realCfg.DashSpeed - 0.02f;
                }
            }
            else
            {
                dir = ctx.CastDir.Value;
                duration = realCfg.DashDuration;
            }

            unitEntity.CreateDashIntent(dir, duration, realCfg.DashSpeed, realCfg.OnHitEffects);
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
                var realCenter = ctx.Position.Value + ctx.CastDir.Value * realCfg.Length * 0.5f;

                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.ActorFactionId;

                candidates = ctx.Env.visionSenser.OverlapBoxAllEntity(realCenter, ctx.CastDir.Value, new Vector2(realCfg.Width, realCfg.Length), filter);
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, realCenter, new Vector2(realCfg.Width, realCfg.Length), Color.red, 1f, dir:ctx.CastDir);
            }
            else if(realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
            {
                var realCenter = ctx.Position.Value;
                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.ActorFactionId;
                candidates = ctx.Env.visionSenser.OverlapCircleAllEntity(realCenter, realCfg.Radius, filter);

                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Circle, realCenter, new Vector2(realCfg.Radius, realCfg.Radius), Color.red, 1f);
            }
            
            if(candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Type != realCfg.TargetEntityType)
                    {
                        continue;
                    }

                    Debug.Log("AbilityEffectExecutor4HitBox find logic target " + candidate.Id);

                    foreach (var e in realCfg.OnHitEffects) 
                    {
                        LogicFightEffectContext newCtx = new(ctx.Env, ctx.SourceKey);

                        newCtx.Actor = ctx.Actor;
                        newCtx.CastDir = ctx.CastDir;
                        newCtx.Target = candidate;
                        ctx.Env.HandleLogicFightEffect(e, newCtx);
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

    public class AbilityEffectExecutor4OpenClickWindow : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectOpenClickWindowCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ClickkkWindow cfg error");
                return;
            }

            if(ctx.Actor is not BaseUnitLogicEntity unitEntity)
            {
                Debug.LogError("AbilityEffectExecutor4ClickkkWindow cfg error");
                return;
            }

            //unitEntity.
            ctx.Env.viewer.ShowClickkkWindow(realCfg.WindowType, ctx.Actor.Pos, realCfg.Duration);
        }
    }

    public class AbilityEffectExecutor4DeepZhaqu : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectDeepZhaquCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4DeepZhaqu cfg error");
                return;
            }

            if (ctx.Actor is not PlayerLogicEntity player)
            {
                Debug.LogError("AbilityEffectExecutor4DeepZhaqu cfg error");
                return;
            }

            ctx.Env.viewer.DoDeepZhaquSmallGame(1, null);
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


    public class AbilityEffectExecutor4ApplyDamage : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectApplyDamageCfg;

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

            var baseVal = realCfg.BaseDamage;
            foreach(var onePair in realCfg.ExtraDamageRate)
            {
                // todo 获取attr
                long getAttr = 0;
                baseVal += (long)(getAttr * onePair.Val * 0.0001f);
            }

            Dictionary<string, long> extraAttrs = null;
            if (realCfg.ExtraAttrs != null)
            {
                extraAttrs = new();
                foreach (var pair in realCfg.ExtraAttrs)
                {
                    extraAttrs[pair.AttrId] = pair.Val;
                }
            }

            ctx.Target.ApplyResourceChange(AttrIdConsts.HP, -baseVal, true, ctx.SourceKey, extraAttrs);

            if (realCfg.KnockBackForce > 0 && ctx.Actor != null)
            {
                // 对目标击打
                if(realCfg.TargetType == 0)
                {
                    if(ctx.Target is BaseUnitLogicEntity unitEntity)
                    {
                        var diff = ctx.Target.Pos - ctx.Actor.Pos;
                        unitEntity.CreateKnockBackIntent(diff, realCfg.KnockBackForce);
                    }
                }
            }
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

    public class AbilityEffectExecutor4SpawnEntity : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectSpawnEntityCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity cfg error");
                return;
            }

            LogicEntityRecord record = null;
            switch (realCfg.EntityType)
            {
                case EEntityType.Npc:
                case EEntityType.Monster:
                    {
                        record = new LogicEntityRecord4UnitBase();
                    }
                    break;
                default:
                    {
                        record = new LogicEntityRecord();
                    }
                    break;
            }

            if(record == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity record miss");
                return;
            }

            if(ctx.CastDir == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity cast dir invalid");
                return;
            }

            record.Id = GameLogicManager.LogicEntityIdInst++;
            record.CfgId = realCfg.CfgId;
            record.EntityType = realCfg.EntityType;
            record.LifeTime = realCfg.LifeTime;
            record.Position = ctx.CastDir.Value;

            ctx.Env.AddNewEntityRecord(record);
        }
    }

    public class AbilityEffectExecutor4RangePreview : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectRangePreviewCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity cfg error");
                return;
            }

            if(realCfg.Shape == MapAbilityEffectRangePreviewCfg.EShape.Circle)
            {
                int effectId = ctx.Env.viewer.ShowRangeWarnEffect(1, realCfg.Radius, 0, ctx.Position.Value, Vector2.zero, realCfg.PreviewDuration);
                ctx.BindSceneFxIds.Add(effectId);
            }
            else if (realCfg.Shape == MapAbilityEffectRangePreviewCfg.EShape.Circle)
            {
                int effectId = ctx.Env.viewer.ShowRangeWarnEffect(1, realCfg.Radius, 0, ctx.Position.Value, Vector2.zero, realCfg.PreviewDuration);
                ctx.BindSceneFxIds.Add(effectId);
            }
        }
    }

    public class AbilityEffectExecutor4NextPhase : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectNextPhaseCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity cfg error");
                return;
            }

            if(ctx.Actor == null || ctx.Actor is not BaseUnitLogicEntity unit)
            {
                return;
            }

            if(!unit.abilityController.IsRunning || unit.abilityController.CurrentCtx == null)
            {
                return;
            }
            var abilityCtx = unit.abilityController.CurrentCtx;
            if (abilityCtx.AbilityConfig.Id != realCfg.MatchSkill)
            {
                return;
            }

            if (abilityCtx.PhaseIndex < 0 || abilityCtx.PhaseIndex >= abilityCtx.AbilityConfig.Phases.Count)
            {
                return;
            }
            var phase = abilityCtx.AbilityConfig.Phases[abilityCtx.PhaseIndex];
            if(phase.PhaseName != realCfg.MatchPhase)
            {
                return;
            }

            Debug.Log("AbilityEffectExecutor4NextPhase try apply");
            abilityCtx.PhaseMarkSkip = true;
        }
    }

}