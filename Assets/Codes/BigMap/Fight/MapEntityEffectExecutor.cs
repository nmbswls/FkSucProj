using System;
using System.Collections;
using System.Collections.Generic;
using My.Map.Logic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem.HID;
using static My.GameLogicManager;
using static My.Map.Fight.FightStruct;

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
            if(ctx.TargetId == 0)
            {
                return;
            }
            var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
            if(targetEntity != null && targetEntity is LootPointLogicEntity lootPoint)
            {
                lootPoint.TryUnlockLootPoint();
            }
            else
            {
                Debug.LogError($"AbilityEffectExecutor4UnlockLootPoint not loot point {ctx.TargetId}");
            }
        }
    }

    public class AbilityEffectExecutor4UseLootPoint : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (ctx.TargetId == 0)
            {
                return;
            }
            var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (targetEntity != null && targetEntity is LootPointLogicEntity lootPoint)
            {
                lootPoint.TryUseLootPoint();
            }
            else
            {
                Debug.LogError($"AbilityEffectExecutor4UnlockLootPoint not loot point {ctx.TargetId}");
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
                id = realCfg.BulletId,
                maxLifetime = realCfg.lifeTime,

                TriggerOnLifeEnd = realCfg.TriggerOnLifeEnd,
                TriggerOnCollide = realCfg.TriggerOnCollide,

                isHoming = realCfg.isHoming,
                homingTime = realCfg.homingTime,

                ProjShape = realCfg.BulletShape,
                showRangeWarn = realCfg.showRangeWarn,
                lockAngle = realCfg.lockViewAngle
            };

            pData.OnHitEffects.AddRange(realCfg.HitEffects);
            pData.motionData = realCfg.MotionData;

            ILogicEntity? caster = null;
            if(ctx.SourceInfo.SrcEntityId != 0)
            {
                caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            }

            Vector2? bornPos = null;
            switch (realCfg.SpawnPos)
            {
                case MapAbilityEffectSpawnBulletCfg.ESpawnPos.TriggerPos:
                    {
                        bornPos = ctx.TriggerPos;
                    }
                    break;
                case MapAbilityEffectSpawnBulletCfg.ESpawnPos.CastPos:
                    {
                        bornPos = ctx.CastVec1;
                    }
                    break;
            }

            if(bornPos == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnBullet bornPos null");
                return;
            }

            Vector2? dir = null;
            switch (realCfg.SpawnDir)
            {
                case MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToCastPos:
                    {
                        dir = ctx.CastVec1.Value - bornPos;
                    }
                    break;
                case MapAbilityEffectSpawnBulletCfg.ESpawnDir.ToTriggerPos:
                    {
                        dir = ctx.TriggerPos.Value - bornPos;
                    }
                    break;
                case MapAbilityEffectSpawnBulletCfg.ESpawnDir.Random:
                    {
                        dir = UnityEngine.Random.insideUnitCircle.normalized;
                    }
                    break;
                case MapAbilityEffectSpawnBulletCfg.ESpawnDir.NoDir:
                    {
                        dir = Vector2.zero;
                    }
                    break;
            }

            if(dir == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnBullet no dir null");
                return;
            }

            long? homingTarget = null;
            if(realCfg.isHoming)
            {
                switch(realCfg.homingSelectPolicy)
                {
                    case Fight.FightStruct.ESelectPolicy.PrimaryTarget:
                        {
                            if(caster != null && caster is BaseUnitLogicEntity unit)
                            {
                                if(unit.combatStateComp.PrimaryTargetId != 0)
                                {
                                    homingTarget = unit.combatStateComp.PrimaryTargetId;
                                }
                            }
                        }
                        break;
                }

                if(homingTarget == null)
                {
                    Debug.LogError("AbilityEffectExecutor4SpawnBullet not found target");
                }
            }
            ctx.Env.projectileHolder.CreateLogicProjectile(pData, caster, bornPos.Value, dir.Value, homingTarget: homingTarget);
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

    public class AbilityEffectExecutor4DefaultInteract : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectDefaultInteractCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4UseItem err");
                return;
            }

            var eIdString = ctx.GetVariatyRawVal(realCfg.InteractEntityId);
            long.TryParse(eIdString, out var entityId);
            if(entityId == 0)
            {
                Debug.LogError("AbilityEffectExecutor4DefaultInteract entityId 0.");
                return;
            }
            var entity = ctx.Env.GetLogicEntity(entityId);

            if (entity == null)
            {
                Debug.LogError("AbilityEffectExecutor4DefaultInteract entityId not found.");
                return;
            }
            if(entity is GatherPointLogicEntity gatherPointEntity)
            {
                gatherPointEntity.DoGather();
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

            if(ctx.SourceInfo.SrcEntityId == 0)
            {
                return;
            }

            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);

            if(actor != null && actor is BaseUnitLogicEntity unitEntity)
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
            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false);
            if (actor == null || actor is not BaseUnitLogicEntity unitEntity)
            {
                return;
            }

            Vector2 dir;
            float duration;

            Vector2? dashEndP = null;
            // 尝试获取目标位置
            if (realCfg.IsLockTarget && ctx.TargetId != 0)
            {
                var target = ctx.Env.GetLogicEntity(ctx.TargetId);
                if (target != null)
                {
                    dashEndP = target.Pos;
                }
            }

            // 获取目标失败 从施法参数中获取
            if (dashEndP == null)
            {
                dashEndP = ctx.CastVec1;
            }

            // 仍没有 进行保底哦位移
            if (dashEndP == null)
            {
                dir = Vector2.right;
                duration = 0.01f;
                Debug.Log("dash baodi");
            }
            else
            {
                Vector2 or0gPos = actor.Pos;
                var diff = (dashEndP.Value - or0gPos);

                if (realCfg.IsFixPointMode)
                {
                    dir = diff.normalized;
                    duration = diff.magnitude / realCfg.DashSpeed - 0.02f;
                }
                else
                {
                    dir = diff.normalized;
                    duration = realCfg.DashDuration;
                }
            }

            unitEntity.StartDash(dir, duration, realCfg.DashSpeed, realCfg.OnHitEffects, true);
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
            if(ctx.SourceInfo.SrcType == ESourceType.Buff)
            {
                srcBuffId = ctx.SourceInfo.SrcBuffId;
            }

            // 当目标type为0时 在正常语境下 就是给目标使用
            if (realCfg.TargetType == 0)
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.TargetId, realCfg.BuffId, realCfg.Layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId : srcBuffId);
            }
            else
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.SourceInfo.SrcEntityId, realCfg.BuffId, realCfg.Layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId: srcBuffId);
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
            var castDir = ctx.CastVec1 - ctx.TriggerPos;
            // 通过hitbox 找到目标
            if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Square)
            {
                var realCenter = ctx.TriggerPos.Value + castDir.Value * realCfg.Length * 0.5f;

                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;

                candidates = ctx.Env.visionSenser.OverlapBoxAllEntity(realCenter, castDir.Value, new Vector2(realCfg.Width, realCfg.Length), filter);
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, realCenter, new Vector2(realCfg.Width, realCfg.Length), Color.red, 1f, dir: castDir);
            }
            else if(realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
            {
                var realCenter = ctx.TriggerPos.Value;
                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;
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
                        LogicFightEffectContext newCtx = new(ctx.Env, ctx.SourceInfo);

                        newCtx.TriggerPos = ctx.TriggerPos;
                        newCtx.CastVec1 = ctx.CastVec1;
                        newCtx.TargetId = candidate.Id;
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

            var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (targetEntity == null)
            {
                Debug.LogError($"AbilityEffectExecutor4RemoveBuff target err :{ctx.TargetId}");
                return;
            }

            ctx.Env.globalBuffManager.RemoveAllBuffById(ctx.TargetId, realCfg.BuffId);
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

            var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (targetEntity == null)
            {
                Debug.LogError($"AbilityEffectExecutor4IfBranch target err :{ctx.TargetId}");
                return;
            }

            bool isTrue = true;
            switch(realCfg.CheckType)
            {
                case MapAbilityEffectIfBranchCfg.ECheckType.HasBuff:
                    {
                        if(targetEntity.BuffManager.CheckHasBuff(targetEntity.Id, realCfg.Param1))
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
                        long val = targetEntity.GetAttr(realCfg.Param1);
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

            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if(actor == null || actor is not BaseUnitLogicEntity unitEntity)
            {
                Debug.LogError("AbilityEffectExecutor4ClickkkWindow cfg error");
                return;
            }

            //unitEntity.
            ctx.Env.viewer.ShowClickkkWindow(realCfg.WindowType, unitEntity.Pos, realCfg.Duration);
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

            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (actor == null || actor is not PlayerLogicEntity player)
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

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if(target == null)
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
            target.ApplyResourceChange(realCfg.ResourceId, realCfg.AddValue, realCfg.IsEnmity, realCfg.Flags, ctx.SourceInfo.SrcEntityId, extraAttrs);
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

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (target == null)
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

            target.ApplyResourceChange(realCfg.ResourceId, -realCfg.CostValue,realCfg.IsEnmity, realCfg.Flags, ctx.SourceInfo.SrcEntityId, extraAttrs);
        }
    }

    public class AbilityEffectExecutor4CastSkill : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectCastSkillCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err");
                return;
            }

            var casterId = ctx.SourceInfo.SrcEntityId;
            var caster = ctx.Env.GetLogicEntity(casterId);
            if(caster == null || caster.MarkDead)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err 2 ");
                return;
            }

            if(caster is not BaseUnitLogicEntity unitEntity)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err 3");
                return;
            }

            if(unitEntity.CheckHasState(AttrIdConsts.ForbidSkillOp))
            {
                return;
            }

            if(unitEntity.abilityController.IsActionable())
            {
                return;
            }

            ILogicEntity targetEntity = null;
            if (ctx.TargetId != 0)
            {
                targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
            }

            Vector2 castVec = ctx.CastVec1.Value;
            if (realCfg.UseTargetAsTarget)
            {
                castVec = targetEntity.Pos;
            }


            // 使用技能
            unitEntity.ablilityManager.UseSkill(realCfg.SkillId, castVec, realCfg.UseTargetAsTarget ? targetEntity : null);
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

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (target == null)
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

            target.ApplyResourceChange(AttrIdConsts.HP, -baseVal, true, EDmgFlag.None, ctx.SourceInfo.SrcEntityId, extraAttrs);

            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (realCfg.KnockBackForce > 0 && actor != null)
            {
                // 对目标击打
                if(realCfg.TargetType == 0)
                {
                    if(target is BaseUnitLogicEntity unitEntity)
                    {
                        var diff = target.Pos - actor.Pos;
                        unitEntity.ApplyKnockBack(diff, realCfg.KnockBackForce);
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

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);

            if (target == null || target is not IThrowTarget throwTarget)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource throwTarget err");
                return;
            }
            if (actor == null || actor is not IThrowLauncher throwLauncher)
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

            if(ctx.CastVec1 == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnEntity CastVec1 dir invalid");
                return;
            }

            record.Id = GameLogicManager.LogicEntityIdInst++;
            record.CfgId = realCfg.CfgId;
            record.EntityType = realCfg.EntityType;
            record.LifeTime = realCfg.LifeTime;
            record.Position = ctx.CastVec1.Value;

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

            //if(realCfg.Shape == MapAbilityEffectRangePreviewCfg.EShape.Circle)
            //{
            //    int effectId = ctx.Env.viewer.ShowRangeWarnEffect(1, realCfg.Radius, 0, ctx.TriggerPos.Value, Vector2.zero, realCfg.PreviewDuration);
            //    ctx.BindSceneFxIds.Add(effectId);
            //}
            //else if (realCfg.Shape == MapAbilityEffectRangePreviewCfg.EShape.Circle)
            //{
            //    int effectId = ctx.Env.viewer.ShowRangeWarnEffect(1, realCfg.Radius, 0, ctx.TriggerPos.Value, Vector2.zero, realCfg.PreviewDuration);
            //    ctx.BindSceneFxIds.Add(effectId);
            //}
        }
    }

    public class AbilityEffectExecutor4NextPhase : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectNextPhaseCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4NextPhase cfg error");
                return;
            }

            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (actor == null || actor is not BaseUnitLogicEntity unit)
            {
                return;
            }

            if(!unit.abilityController.IsRunning || unit.abilityController.CurrentCtx == null)
            {
                return;
            }
            var abilityCtx = unit.abilityController.CurrentCtx;
            var srcSkill = ctx.SourceInfo.SrcCfgId;

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

    public class AbilityEffectExecutor4TeleportTo : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectTeleportToCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4CostResource err");
                return;
            }

            var teleportTarget = ctx.TargetId;
            if(teleportTarget == 0)
            {
                teleportTarget = ctx.SourceInfo.SrcEntityId;
            }

            var target = ctx.Env.GetLogicEntity(teleportTarget);
            if (target == null || target.MarkDead)
            {
                Debug.LogError("target be invalid err");
                return;
            }

            if(ctx.CastVec1 == null)
            {
                Debug.LogError("target be invalid por err");
                return;
            }
            ctx.Env.EntityTeleportTo(ctx.TargetId, ctx.CastVec1??Vector2.zero);
        }
    }

}