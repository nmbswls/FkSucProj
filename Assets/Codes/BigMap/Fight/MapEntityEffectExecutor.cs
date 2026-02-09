using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using My.Map.Logic;
using My.Player.Bag;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem.HID;
using static My.GameLogicManager;
using static My.Map.BaseUnitLogicEntity;
using static My.Map.Entity.MapAbilityEffectDashStartCfg;
using static My.Map.Fight.FightStruct;
using static My.Map.PlayerLogicEntity;
using static UnityEngine.GraphicsBuffer;

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
            if (targetEntity != null && targetEntity is ILootableObj lootObj)
            {
                lootObj.TryUseLootPoint();
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

            string bulletId = realCfg.BulletId;
            if (realCfg.OverrideBulletId.ValType == EOneVariatyType.String)
            {
                bulletId = ctx.GetVariatyRawVal(realCfg.OverrideBulletId);
            }

            var pData = new ProjectileData()
            {
                id = bulletId,
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
                    case ETargetSelectPolicy.PrimaryTarget:
                        {
                            if(caster != null && caster is NpcUnitLogicEntity npcUnit)
                            {
                                if(npcUnit.AggroSystem.CurrentTargetId != 0)
                                {
                                    homingTarget = npcUnit.AggroSystem.CurrentTargetId;
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
                return;
            }

            var useItemId = ctx.GetVariatyRawVal(realCfg.UseItemId);
            Debug.Log("use " + useItemId);

            var itemCfg = FakeItemDatabase.GetItem(useItemId);
            if(itemCfg == null || itemCfg.UseCfg1 == null)
            {
                Debug.LogError($"AbilityEffectExecutor4UseItem item not found {useItemId}");
                return;
            }

            ctx.Env.playerDataManager.inventoryModel.ItemUseCd[useItemId] = LogicTime.time;

            var srcIdxStr = ctx.GetVariatyRawVal(realCfg.UseItemSrcIdx);
            int.TryParse(srcIdxStr, out var srcIdx);

            ctx.Env.HandleUseItem(ctx.SourceInfo.SrcEntityId, 1, itemCfg.UseCfg1);
            if(itemCfg.UseCfg1.CostOnUse)
            {
                ctx.Env.playerDataManager.CostItem(useItemId, 1);
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

    public class AbilityFightExecutor4QueueMode : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectQueueModeCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4QueueMode cfg error");
                return;
            }

            var playerEntity = ctx.Env.GetLogicEntity(ctx.TargetId) as PlayerLogicEntity;

            if(realCfg.InEnter)
            {
                playerEntity.IsQueenMode = true;
            }
            else
            {
                playerEntity.IsQueenMode = false;
            }
        }
    }

    public class AbilityFightExecutor4KnockBack : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectKnockBackCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4KnockBack cfg error");
                return;
            }

            if (realCfg.KnockBackForce <= 0)
            {
                Debug.LogError("AbilityFightExecutor4KnockBack param inbalid");
                return;
            }

            // 对目标施加
            if(realCfg.TargetType == 0)
            {
                var targetUnit = ctx.Env.GetLogicEntity(ctx.TargetId) as BaseUnitLogicEntity;
                if (targetUnit == null)
                {
                    Debug.LogWarning("AbilityFightExecutor4KnockBack target not found.");
                    return;
                }

                Vector2? dir = null;
                switch (realCfg.DirType)
                {
                    case MapFightEffectKnockBackCfg.EKnockBackType.CastDir:
                        {
                            dir = ctx.CastVec1.Value;
                        }
                        break;
                    case MapFightEffectKnockBackCfg.EKnockBackType.Random:
                        {
                            dir = UnityEngine.Random.insideUnitCircle.normalized; 
                        }
                        break;
                }

                if(dir == null)
                {
                    Debug.LogError("AbilityFightExecutor4KnockBack dir err.");
                    return;
                }

                targetUnit.ApplyKnockBack(dir.Value, realCfg.KnockBackForce);
            }
            else
            {
                var srcUnit = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) as BaseUnitLogicEntity;
                if (srcUnit == null)
                {
                    //Debug.LogError("AbilityFightExecutor4KnockBack target not found.");
                    return;
                }

                var diff = ctx.CastVec1.Value - ctx.TriggerPos.Value;
                srcUnit.ApplyKnockBack(diff, realCfg.KnockBackForce);
            }
        }
    }


    public class AbilityFightExecutor4SpecialMoveTo : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectSpecialMoveToCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4SpecialMoveTo cfg error");
                return;
            }

            if (ctx.SourceInfo.SrcEntityId == 0)
            {
                return;
            }

            if (ctx.TargetId == 0)
            {
                return;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (target == null || actor == null || actor is not BaseUnitLogicEntity unitEntity)
            {
                return;
            }

            var duration = realCfg.Duration;

            ctx.Env.globalBuffManager.RequestAddBuff(actor.Id, "lock_move", overrideDuration: duration);
            ctx.Env.viewer.DoPlayerSpecialMove(target.Pos, actor.Pos, duration, () =>
            {
                unitEntity.TeleportTo(target.Pos);
            });
        }
    }

    public class AbilityFightExecutor4UseWeapon : AbilityEffectExecutor
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

            

            Vector2? dashEndP = null;
            // 尝试获取目标位置
            if(realCfg.DashMode == EDashMode.ToTarget)
            {
                var target = ctx.Env.GetLogicEntity(ctx.TargetId, false);
                if (target != null)
                {
                    dashEndP = target.Pos;
                }
            }
            else if(realCfg.DashMode == EDashMode.FixDistance)
            {
                Vector2 dashDir = Vector2.right;
                if (realCfg.DirMode == EDirMode.CastDir)
                {
                    if(ctx.CastVec1 != null)
                    {
                        dashDir = ctx.CastVec1.Value - actor.Pos;
                    }
                }
                else if(realCfg.DirMode == EDirMode.LookDir)
                {
                    dashDir = unitEntity.FinalLook;
                }
                else if(realCfg.DirMode == EDirMode.TmpLookDir)
                {
                    dashDir = unitEntity.CurrentLook;
                }

                var dist = realCfg.MaxDistance;
                if(dist < 0.5f)
                {
                    dist = 0.5f;
                }
                dashEndP = unitEntity.Pos + dashDir.normalized * dist;
            }
            else if (realCfg.DashMode == EDashMode.FixTime)
            {
                Vector2 dashDir = Vector2.right;
                if (realCfg.DirMode == EDirMode.CastDir)
                {
                    if (ctx.CastVec1 != null)
                    {
                        dashDir = ctx.CastVec1.Value - actor.Pos;
                    }
                }
                else if (realCfg.DirMode == EDirMode.LookDir)
                {
                    dashDir = unitEntity.FinalLook;
                }
                else if (realCfg.DirMode == EDirMode.TmpLookDir)
                {
                    dashDir = unitEntity.CurrentLook;
                }
                var dist = realCfg.DashSpeed * realCfg.DashDuration;
                if (dist < 0.5f)
                {
                    dist = 0.5f;
                }
                dashEndP = unitEntity.Pos + dashDir.normalized * dist;
            }

            // 获取目标失败 从施法参数中获取
            if (dashEndP == null)
            {
                dashEndP = ctx.CastVec1;
            }

            Vector2 dir;
            float duration;

            // 仍没有 进行保底哦位移
            if (dashEndP == null)
            {
                dir = Vector2.right;
                duration = 0.01f;
                Debug.Log("dash baodi");
            }

            Vector2 or0gPos = actor.Pos;
            var diff = (dashEndP.Value - or0gPos);
            dir = diff.normalized;
            duration = diff.magnitude / realCfg.DashSpeed - 0.02f;

            unitEntity.StartDash(dir, duration, realCfg.DashSpeed, realCfg.OnHitEffects, realCfg.IsGhost, dashWeaponName: realCfg.DashWeaponName);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public class AbilityEffectExecutor4ControlledMove : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg useItemCfg, LogicFightEffectContext ctx)
        {
            var realCfg = useItemCfg as MapAbilityEffectControlledMoveCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ControlledMove err");
                return;
            }

            BaseUnitLogicEntity unitEntity = null;
            if (realCfg.TargetType == 0)
            {
                unitEntity = ctx.Env.GetLogicEntity(ctx.TargetId, false) as BaseUnitLogicEntity;
            }
            else
            {
                unitEntity = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false) as BaseUnitLogicEntity;
            }


            if (unitEntity == null)
            {
                Debug.LogError("AbilityEffectExecutor4ControlledMove move unit not found");
                return;
            }

            if (ctx.CastVec1 == null)
            {
                Debug.LogError("AbilityEffectExecutor4ControlledMove move cast vel");
                return;
            }
            Vector2 targetPos = ctx.CastVec1.Value;
            float duration = realCfg.FixedDuration;

            var diff = targetPos - unitEntity.Pos;
            var speed = diff.magnitude / duration;
            unitEntity.ApplyControlledMove(ControlledMoveCtx.EType.Pull, diff.normalized, originSpeed: diff.magnitude * 8f, onHitEffects: null, minEndSpeed : 0.1f);
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

            int layer = realCfg.Layer;
            if (realCfg.Layer <= 0) layer = 1;
            // 当目标type为0时 在正常语境下 就是给目标使用
            if (realCfg.TargetType == 0)
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.TargetId, realCfg.BuffId, layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId : srcBuffId, overrideDuration:realCfg.Duration);
            }
            else
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.SourceInfo.SrcEntityId, realCfg.BuffId, layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId: srcBuffId, overrideDuration: realCfg.Duration);
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

            Vector2 realCenter = Vector2.zero;
            // 通过hitbox 找到目标
            if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Square)
            {
                realCenter = ctx.TriggerPos.Value + castDir.Value * realCfg.Length * 0.5f;

                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;

                candidates = ctx.Env.visionSenser.OverlapBoxAllEntity(realCenter, castDir.Value, new Vector2(realCfg.Width, realCfg.Length), filter);
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, realCenter, new Vector2(realCfg.Width, realCfg.Length), Color.red, 1f, dir: castDir);
            }
            else if(realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
            {
                if(realCfg.CenterPosType == 0)
                {
                    realCenter = ctx.TriggerPos.Value;
                }
                else
                {
                    realCenter = ctx.CastVec1.Value;
                }

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
                    if (realCfg.TargetEntityType != EEntityType.None && candidate.Type != realCfg.TargetEntityType)
                    {
                        continue;
                    }

                    Debug.Log("AbilityEffectExecutor4HitBox find logic target " + candidate.Id);

                    foreach (var e in realCfg.OnHitEffects) 
                    {
                        LogicFightEffectContext newCtx = new(ctx.Env, ctx.SourceInfo);

                        //newCtx.TriggerPos = ctx.TriggerPos;
                        //newCtx.CastVec1 = ctx.CastVec1;

                        newCtx.TriggerPos = candidate.Pos;
                        newCtx.CastVec1 = candidate.Pos - realCenter;
                        newCtx.CastVec2 = Vector2.zero;

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

    public class AbilityEffectExecutor4ShowCloseupWindow : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectShowCloseupWindowCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ShowCloseupWindow cfg error");
                return;
            }
            //unitEntity.
            ctx.Env.viewer.ShowPauseCloseupWindow(realCfg.WindowType, realCfg.Duration);
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


    public class AbilityEffectExecutor4ConvertAttach : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectConvertAttachCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ConvertAttach err");
                return;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (target == null || target is not PlayerLogicEntity playerEntity)
            {
                Debug.LogError("AbilityEffectExecutor4ConvertAttach err target invalid");
                return;
            }

            var srcActor1 = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if(srcActor1 == null || srcActor1 is not BaseUnitLogicEntity baseUnit)
            {
                Debug.LogError("AbilityEffectExecutor4ConvertAttach src invalid");
                return;
            }

            baseUnit.ConvertToAttachment();
            playerEntity.AddAttachingObjInfo(realCfg.AttachId, baseUnit.Id);
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
            if(caster == null || caster.MarkDestroyed)
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
            unitEntity.ablilityManager.UseSkill(realCfg.SkillId, castVec: castVec, target : realCfg.UseTargetAsTarget ? targetEntity : null);
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
                    {
                        record = new LogicEntityRecord4Npc();
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

    public class AbilityEffectExecutor4TriggerAlert : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectTriggerAlert;
            if (realCfg == null)
            {
                Debug.LogError("MapFightEffectTriggerAlert cfg error");
                return;
            }

            if (ctx.TargetId == 0)
            {
                return;
            }
            var actor = ctx.Env.GetLogicEntity(ctx.TargetId);

            if (actor == null || actor is not BaseUnitLogicEntity unitEntity)
            {
                return;
            }
            Debug.Log($"AbilityEffectExecutor4TriggerAlert try apply {ctx.TargetId}");
            unitEntity.StartEvilAlert(realCfg.AlertDuration);
        }
    }

    public class AbilityEffectExecutor4EasyEffect : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectEasyEffect;
            if (realCfg == null)
            {
                Debug.LogError("MapFightEffectEasyEffect cfg error");
                return;
            }

            if (ctx.SourceInfo.SrcEntityId == 0)
            {
                return;
            }
            var actor = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (actor == null)
            {
                return;
            }

            ctx.Env.viewer.ShowFakeFxEffect(realCfg.EffectText, actor.Pos);
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
            if (target == null || target.MarkDestroyed)
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

    public class AbilityEffectExecutor4HitAttach : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectHitAttachCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4HitAttach err");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if(caster == null || caster is not PlayerLogicEntity player)
            {
                Debug.LogError("AbilityEffectExecutor4HitAttach err");
                return;
            }

            player.HitAttachObjs();
        }
    }

    public class AbilityEffectExecutor4GiveItem : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectGiveItemCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4HitAttach err");
                return;
            }

            ctx.Env.playerDataManager.TryGiveItem(realCfg.ItemId, realCfg.Count, realCfg.SpecificBagId);
        }
    }
    

    public class AbilityEffectExecutor4HModeBlurt : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectHModeBlurtCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4HModeBlurt err");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster == null || caster is not NpcUnitLogicEntity npcUnit)
            {
                Debug.LogError("AbilityEffectExecutor4HModeBlurt unit not found.");
                return;
            }

            //
            if(ctx.Env.playerLogicEntity.IsQueenMode)
            {

            }
            else
            {
                var diff = ctx.Env.playerLogicEntity.Pos - npcUnit.Pos;
                // 
                bool absorb = false;
                if (diff.magnitude < 0.1f)
                {
                    absorb = true;
                }
                else if(diff.magnitude < 1.0f)
                {
                    var signedAngle = Vector2.SignedAngle(diff, npcUnit.CurrentLook);
                    if(signedAngle < 45)
                    {
                        absorb = true;
                    }
                }

                // 被主角吸收
                if(absorb)
                {
                    //ctx.Env.globalBuffManager.AddBuff();
                    Debug.Log("AbilityEffectExecutor4HModeBlurt sj to player");
                    //ctx.Env.viewer.ShowPauseCloseupWindow("jingyu", 0.5f);
                    ctx.Env.viewer.ShowFakeFxEffect("精浴", ctx.Env.playerLogicEntity.Pos);
                }
                else
                {
                    var dropPos = npcUnit.Pos + npcUnit.CurrentLook * 0.5f;
                    for(int i=0; i< 4;i++)
                    {
                        ctx.Env.globalDropCollection.CreateDrop("j_drop_small", 1, dropPos + UnityEngine.Random.insideUnitCircle * 0.5f, true, npcUnit.Pos);
                    }

                    ctx.Env.viewer.ShowFakeFxEffect("落地", dropPos);
                }
            }
        }
    }

    public class AbilityEffectExecutor4BroadcastAttract : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectBroadcastAttractCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4BroadcastAttract err");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster == null || caster is not PlayerLogicEntity playerEntity)
            {
                Debug.LogError("AbilityEffectExecutor4BroadcastAttract unit not found.");
                return;
            }

            ctx.Env.viewer.ShowNoiseEffect(0.8f, playerEntity.Pos);
        }
    }
    
}