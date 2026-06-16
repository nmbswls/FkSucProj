using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Logic;
using My.MapExport;
using My.Player;
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
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;
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

            pData.EntityHitResult = realCfg.BulletHitResult;
            pData.ExplodeEffects = realCfg.ExplodeEffects;
            pData.motionData = realCfg.MotionData;
            pData.maxPenetration = realCfg.bulletMaxPenetration;

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
                case MapAbilityEffectSpawnBulletCfg.ESpawnDir.AlignHoming:
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
            Vector2? homingTargetPos = null;
            if(realCfg.isHoming)
            {
                if (ctx.TargetId != 0)
                {
                    var explicitTarget = ctx.Env.GetLogicEntity(ctx.TargetId, false) as BaseUnitLogicEntity;
                    if (explicitTarget != null && !explicitTarget.MarkDestroyed && !explicitTarget.IsDead)
                    {
                        homingTarget = ctx.TargetId;
                    }
                }

                var casterUnit = caster as BaseUnitLogicEntity;
                if (homingTarget == null && realCfg.homingSelectPolicy == ETargetSelectPolicy.CastPoint)
                {
                    if (ctx.CastVec1.HasValue)
                    {
                        homingTargetPos = ctx.CastVec1.Value;
                    }
                    else
                    {
                        Debug.LogWarning("AbilityEffectExecutor4SpawnBullet: CastPoint homing requires CastVec1.");
                        return;
                    }
                }
                else if (homingTarget == null && casterUnit != null)
                {
                    switch(realCfg.homingSelectPolicy)
                    {
                        case ETargetSelectPolicy.PrimaryTarget:
                            {
                                homingTarget = casterUnit.CurrentTargetId;
                            }
                            break;
                        case ETargetSelectPolicy.NearestEnemyInRadius:
                            {
                                float acquireRadius = realCfg.nearestEnemyAcquireRadius > 0.01f
                                    ? realCfg.nearestEnemyAcquireRadius
                                    : 8f;
                                var best = EntityAbilityHelper.FindNearestEnemyInRadius(
                                    casterUnit.LogicManager,
                                    casterUnit,
                                    bornPos.Value,
                                    acquireRadius,
                                    casterUnit.Id);
                                homingTarget = best != null ? best.Id : 0;
                            }
                            break;
                    }
                }

                if (realCfg.homingSelectPolicy == ETargetSelectPolicy.NearestEnemyInRadius &&
                    (homingTarget == null || homingTarget == 0))
                {
                    Debug.LogWarning("AbilityEffectExecutor4SpawnBullet: no enemy in acquire radius, skip spawn.");
                    return;
                }
            }

            float parabolaLaunchZ = 0f;

            ctx.Env.projectileHolder.CreateLogicProjectile(
                pData,
                caster,
                bornPos.Value,
                dir.Value,
                homingTarget: homingTarget,
                homingTargetPos: homingTargetPos,
                parabolaLaunchZ: parabolaLaunchZ);
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

            var itemDef = ItemCatalog.GetItemDef(useItemId);
            var useRow = ItemCatalog.GetPrimaryUse(useItemId);
            if(itemDef == null || useRow == null)
            {
                Debug.LogError($"AbilityEffectExecutor4UseItem item not found {useItemId}");
                return;
            }

            ctx.Env.playerDataManager.InventorySystem.ItemUseCd[useItemId] = LogicTime.time;

            var srcIdxStr = ctx.GetVariatyRawVal(realCfg.UseItemSrcIdx);
            int.TryParse(srcIdxStr, out var srcIdx);

            ctx.Env.HandleUseItem(ctx.SourceInfo.SrcEntityId, 1, useRow);
            if(useRow.CostOnUse)
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

            var playerEntity = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) as PlayerLogicEntity;

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

    public class AbilityFightExecutor4NpcDirectControl : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectNpcDirectControlCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4NpcDirectControl cfg error");
                return;
            }

            var glm = ctx.Env;
            if (realCfg.InEnter)
            {
                var npc = glm.GetLogicEntity(ctx.TargetId) as NpcUnitLogicEntity;
                if (npc == null)
                {
                    Debug.LogError("AbilityFightExecutor4NpcDirectControl target npc missing");
                    return;
                }

                int playerId = glm.LocalPlayerId;
                string skillId = string.IsNullOrEmpty(ctx.SourceInfo.SrcAbilityId)
                    ? "h_mode_control"
                    : ctx.SourceInfo.SrcAbilityId;

                if (!glm.NpcDirectControl.TryEnter(glm, npc, playerId, skillId))
                {
                    Debug.LogWarning("AbilityFightExecutor4NpcDirectControl TryEnter failed");
                }
            }
            else
            {
                glm.NpcDirectControl.Exit();
            }
        }
    }

    public class AbilityFightExecutor4OverrideTarget : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectOverrideTargetCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4OverrideTarget cfg error");
                return;
            }

            var casterUnit = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) as BaseUnitLogicEntity;
            if(casterUnit.abilityController == null)
            {
                Debug.LogError("AbilityFightExecutor4OverrideTarget ctrl error");
                return;
            }
            if(casterUnit.abilityController.CurrentCtx == null)
            {
                Debug.LogError("AbilityFightExecutor4OverrideTarget no ability error");
                return;
            }

            if(casterUnit.abilityController.CurrentCtx.AbilityConfig.Id != ctx.SourceInfo.SrcAbilityId)
            {
                Debug.LogError("AbilityFightExecutor4OverrideTarget ability not match");
                return;
            }

            if(realCfg.IsRandomPick)
            {
                var filterParam = new EntityFilterParam()
                {
                    FilterParamLists = new() { EEntityType.Npc },
                };
                var iterList = ctx.Env.visionSenser.OverlapCircleAllEntity(
                    casterUnit.Pos,
                    3.5f,
                    filterParam,
                    MapLogicPosition.ResolveAttackHitHeight(casterUnit));
                List<BaseUnitLogicEntity> units = new();
                foreach (var it in iterList) 
                {
                    if(it is not BaseUnitLogicEntity unit)
                    {
                        continue;
                    }

                    if(unit.IsDead || unit.MarkDespawn || unit.MarkDestroyed || unit.MarkNoLogic)
                    {
                        continue;
                    }

                    units.Add(unit);
                }

                if (units.Count == 0) return;

                var chosenOne = units[UnityEngine.Random.Range(0, units.Count)];

                Debug.LogError($"AbilityFightExecutor4OverrideTarget change ability target to {chosenOne.Id}");
                casterUnit.abilityController.CurrentCtx.Target = chosenOne;
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

            if (realCfg.KnockBackForce <= 0f)
            {
                Debug.LogError("AbilityFightExecutor4KnockBack KnockBackForce invalid");
                return;
            }

            var applyTarget = realCfg.ApplyTarget;
            var applySelf = realCfg.ApplySelf;
            if (!applyTarget && !applySelf)
            {
                applyTarget = true;
            }

            if (applyTarget)
            {
                var targetUnit = ctx.Env.GetLogicEntity(ctx.TargetId) as BaseUnitLogicEntity;
                if (targetUnit == null)
                {
                    Debug.LogWarning("AbilityFightExecutor4KnockBack target not found.");
                }
                else
                {
                    var dir = ResolveKnockDirection(realCfg, ctx, targetUnit, recipientIsSelf: false);
                    if (dir == null)
                    {
                        Debug.LogError("AbilityFightExecutor4KnockBack dir err (target).");
                    }
                    else
                    {
                        targetUnit.ApplyKnockBack(dir.Value, realCfg.KnockBackForce);
                    }
                }
            }

            if (applySelf)
            {
                var selfUnit = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) as BaseUnitLogicEntity;
                if (selfUnit == null)
                {
                    Debug.LogWarning("AbilityFightExecutor4KnockBack caster not found.");
                }
                else
                {
                    var dir = ResolveKnockDirection(realCfg, ctx, selfUnit, recipientIsSelf: true);
                    if (dir == null)
                    {
                        Debug.LogError("AbilityFightExecutor4KnockBack dir err (self).");
                    }
                    else
                    {
                        selfUnit.ApplyKnockBack(dir.Value, realCfg.KnockBackForce);
                    }
                }
            }
        }

        static Vector2? ResolveKnockDirection(
            MapFightEffectKnockBackCfg cfg,
            LogicFightEffectContext ctx,
            BaseUnitLogicEntity recipient,
            bool recipientIsSelf)
        {
            var srcUnit = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) as BaseUnitLogicEntity;
            var targetUnit = ctx.Env.GetLogicEntity(ctx.TargetId) as BaseUnitLogicEntity;

            switch (cfg.DirType)
            {
                case MapFightEffectKnockBackCfg.EKnockBackType.None:
                    return null;

                case MapFightEffectKnockBackCfg.EKnockBackType.CastDir:
                    {
                        if (ctx.CastVec1 != null)
                        {
                            var d = ctx.CastVec1.Value;
                            if (d.sqrMagnitude < 1e-8f)
                            {
                                break;
                            }

                            return d.normalized;
                        }

                        if (srcUnit != null && srcUnit.FinalLook.sqrMagnitude > 1e-8f)
                        {
                            return srcUnit.FinalLook.normalized;
                        }

                        break;
                    }

                case MapFightEffectKnockBackCfg.EKnockBackType.AwayFromSrc:
                    {
                        if (recipientIsSelf)
                        {
                            Debug.LogWarning("AbilityFightExecutor4KnockBack AwayFromSrc 不适用于 ApplySelf（施法者与 Src 同体）。");
                            return null;
                        }

                        if (srcUnit == null || recipient == null)
                        {
                            return null;
                        }

                        var d = recipient.Pos - srcUnit.Pos;
                        if (d.sqrMagnitude < 1e-8f)
                        {
                            return null;
                        }

                        return d.normalized;
                    }

                case MapFightEffectKnockBackCfg.EKnockBackType.AwayFromTarget:
                    {
                        if (!recipientIsSelf)
                        {
                            Debug.LogWarning("AbilityFightExecutor4KnockBack AwayFromTarget 仅应在 ApplySelf 时使用。");
                            return null;
                        }

                        if (targetUnit == null || recipient == null)
                        {
                            return null;
                        }

                        var d = recipient.Pos - targetUnit.Pos;
                        if (d.sqrMagnitude < 1e-8f)
                        {
                            return null;
                        }

                        return d.normalized;
                    }

                case MapFightEffectKnockBackCfg.EKnockBackType.Random:
                    return UnityEngine.Random.insideUnitCircle.normalized;

                default:
                    return null;
            }

            return null;
        }
    }


    public class AbilityFightExecutor4ShowEffect : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectShowEffect;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4ShowEffect cfg error");
                return;
            }
            Vector2 p = Vector2.zero;
            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (realCfg.ShowMode == MapFightEffectShowEffect.EShowMode.TriggerPos)
            {
                p = ctx.TriggerPos == null ? Vector2.zero : ctx.TriggerPos.Value;
            }
            else if (realCfg.ShowMode == MapFightEffectShowEffect.EShowMode.TargetAligned)
            {
                p = target.Pos;
            }

            if(realCfg.IsFake)
            {
                ctx.Env.viewer.ShowFakeFxEffect(realCfg.EffectName, p);
            }
            else
            {
                ctx.Env.viewer.ShowSceneFxEffect(realCfg.EffectName, p, Vector2.right);
            }
        }
    }

    

    public class AbilityFightExecutor4RelocateGhostOrb : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectRelocateGhostOrbCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4RelocateGhostOrb cfg error");
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

            var spec = new My.Map.Scene.PlayerRelocateSpec
            {
                TransitStyle = My.Map.Scene.PlayerRelocateTransitStyle.GhostOrb,
                FinalLogicPos = target.Pos,
            };
            var duration = My.Map.Scene.PlayerRelocateTimings.GetTotalDuration(spec);

            ctx.Env.globalBuffManager.RequestAddBuff(actor.Id, "lock_move", overrideDuration: duration);
            ctx.Env.globalBuffManager.RequestAddBuff(actor.Id, "as_presentation", overrideDuration: duration);
            ctx.Env.viewer.DoPlayerRelocate(spec, () =>
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
                var animName = realCfg.AnimName;
                var castSnapshot = FightCastAttrUtil.CopyCacheAttrs(ctx.CacheAttrVal);
                var windowId = unitEntity.ApplyUseWeapon(
                    realCfg.WeaponName,
                    animName,
                    realCfg.Duration,
                    realCfg.OnHitEffects,
                    realCfg.MaxHit,
                    castSnapshot);
                ctx.OutHitWindowIds.Add(windowId);
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
                else if (realCfg.DirMode == EDirMode.InputDir)
                {
                    if (ctx.InputVec != null)
                    {
                        dashDir = ctx.InputVec.Value;
                    }
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
                else if (realCfg.DirMode == EDirMode.InputDir)
                {
                    if (ctx.InputVec != null)
                    {
                        dashDir = ctx.InputVec.Value;
                    }
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

            var moveContext = unitEntity.StartDash(dir, duration, realCfg.DashSpeed, realCfg.OnHitEffects, withGhost: realCfg.IsGhost, dashWeaponName: realCfg.DashWeaponName, 
                stopOnUnit:realCfg.EndOnHitUnit, stopOnWall: realCfg.StopOnWall, dashHitRadius: realCfg.DashOverrideHitRadius);
            if(realCfg.EndAbilityPhaseWhenEnds)
            {
                moveContext.EndPhaseWhenMoveEnds = true;
            }

            if(ctx.SourceInfo.SrcType == ESourceType.Ability)
            {
                moveContext.BindAbilityId = ctx.SourceInfo.SrcAbilityId;
                moveContext.BindAbilityPhaseIdx = ctx.SourceInfo.SrcAbilityPhaseId;
            }

            moveContext.EndOnHitUnit = realCfg.EndOnHitUnit;
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

            Vector2? targetPosOpt = null;
            if (realCfg.UseTriggerPos && ctx.TriggerPos.HasValue)
            {
                targetPosOpt = ctx.TriggerPos.Value;
            }
            else if (ctx.CastVec1 != null)
            {
                targetPosOpt = ctx.CastVec1.Value;
            }

            if (targetPosOpt == null)
            {
                Debug.LogError("AbilityEffectExecutor4ControlledMove move cast vel");
                return;
            }

            Vector2 targetPos = targetPosOpt.Value;
            if (realCfg.StopOffset > 0f)
            {
                var toTarget = targetPos - unitEntity.Pos;
                if (toTarget.sqrMagnitude > 1e-6f)
                {
                    targetPos -= toTarget.normalized * realCfg.StopOffset;
                }
            }

            float duration = realCfg.FixedDuration;
            var diff = targetPos - unitEntity.Pos;
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

            int layer = FightCastAttrUtil.ResolveIntFromRunningVar(
                ctx,
                realCfg.LayerCtxAttr,
                realCfg.Layer <= 0 ? 1 : realCfg.Layer);
            // 当目标type为0时 在正常语境下 就是给目标使用
            if (realCfg.SelfAdd)
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.SourceInfo.SrcEntityId, realCfg.BuffId, layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId: srcBuffId, overrideDuration: realCfg.Duration);
            }
            else if (realCfg.RevertAdd)
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.SourceInfo.SrcEntityId, realCfg.BuffId, layer, casterId: ctx.TargetId, srcBuffId: srcBuffId, overrideDuration: realCfg.Duration);
            }
            else
            {
                ctx.Env.globalBuffManager.RequestAddBuff(ctx.TargetId, realCfg.BuffId, layer, casterId: ctx.SourceInfo.SrcEntityId, srcBuffId: srcBuffId, overrideDuration: realCfg.Duration);

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

            var srcEntity = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false);
            float hitHeight = MapLogicPosition.ResolveAttackHitHeight(srcEntity);
            
            Vector2 hitBoxDir = Vector2.right; // 碰撞盒方向 影响判定区域计算
            Vector2 realCenter = ctx.TriggerPos.Value; // 碰撞中心 影响判定区计算

            switch (ctx.CtxType)
            {
                // 对于技能 碰撞方向为施法方向/面向
                case EFightCtxType.Ability:
                    {
                        hitBoxDir = ctx.CastVec1.Value - ctx.TriggerPos.Value;
                        // 计算offset
                        if(realCfg.IsDirRevert)
                        {
                            hitBoxDir = -hitBoxDir;
                        }

                        // 计算中心，对于技能 可能中心点在自身 也可能在施法点
                        if (realCfg.CenterPosType == 0)
                        {
                            realCenter = ctx.TriggerPos.Value;
                        }
                        else if (realCfg.CenterPosType == 1)
                        {
                            realCenter = ctx.CastVec1.Value;
                        }
                        else if(realCfg.CenterPosType == 2)
                        {
                            if (srcEntity != null && srcEntity is BaseUnitLogicEntity unitEntity)
                            {
                                realCenter = unitEntity.Pos;
                            }
                        }
                        break;
                    }
                // 对于子弹 碰撞方向为子弹施法方向
                case EFightCtxType.Bullet:
                    {
                        hitBoxDir = ctx.CastVec1.Value;
                        if (realCfg.IsDirRevert)
                        {
                            hitBoxDir = -hitBoxDir;
                        }
                        break;
                    }
            }

            
            if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Direction)
            {
                var checkOrigin = realCenter + hitBoxDir * realCfg.Length * 0.5f;

                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;

                var iterList = ctx.Env.visionSenser.OverlapBoxAllEntity(checkOrigin, hitBoxDir, new Vector2(realCfg.Width, realCfg.Length), filter, hitHeight: hitHeight);
                candidates = iterList.ToList();
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, checkOrigin, new Vector2(realCfg.Width, realCfg.Length), Color.red, 1f, dir: hitBoxDir);
            }
            else if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Square)
            {
                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;

                var iterList = ctx.Env.visionSenser.OverlapBoxAllEntity(realCenter, hitBoxDir, new Vector2(realCfg.Width, realCfg.Length), filter, hitHeight: hitHeight);
                candidates = iterList.ToList();
                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Rect, realCenter, new Vector2(realCfg.Width, realCfg.Length), Color.red, 1f, dir: hitBoxDir);
            }
            else if(realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
            {
                EntityFilterParam filter = new EntityFilterParam();
                filter.CampFilterType = realCfg.CampFilterType;
                filter.SelfCampId = ctx.SourceInfo.SrcFactionId;

                if(realCfg.CenterOffset != 0 && ctx.CtxType == EFightCtxType.Ability)
                {
                    if(srcEntity != null && srcEntity is BaseUnitLogicEntity unitEntity)
                    {
                        realCenter += unitEntity.FinalLook.normalized * realCfg.CenterOffset;
                    }
                }

                var iterList = ctx.Env.visionSenser.OverlapCircleAllEntity(realCenter, realCfg.Radius, filter, hitHeight);
                candidates = iterList.ToList();

                DebugHitBoxIndicator.Draw(DebugHitBoxIndicator.Shape.Circle, realCenter, new Vector2(realCfg.Radius, realCfg.Radius), Color.red, 1f);
            }
            
            if(candidates != null)
            {
                foreach (var candidate in candidates)
                {
                    if(candidate.CheckHasState(AttrIdConsts.NoSelect))
                    {
                        if (candidate is BaseUnitLogicEntity pdUnit
                            && pdUnit.TryResolvePerfectDodgeAgainstHit(ctx.SourceInfo.SrcEntityId, null))
                        {
                            continue;
                        }
                        continue; 
                    }

                    if(candidate is BaseUnitLogicEntity unitTarget && unitTarget.IsDead)
                    {
                        continue;
                    }

                    if (realCfg.TargetEntityType != EEntityType.None && candidate.Type != realCfg.TargetEntityType)
                    {
                        continue;
                    }

                    Debug.Log("AbilityEffectExecutor4HitBox find logic target " + candidate.Id);

                    Vector2 hitDir = Vector2.right;  // 逻辑碰撞方向 作为参数传入子事件
                    if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Direction)
                    {
                        hitDir = hitBoxDir;
                    }
                    else if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Square)
                    {
                        hitDir = candidate.Pos - realCenter;
                    }
                    else if (realCfg.Shape == MapAbilityEffectHitBoxCfg.EShape.Circle)
                    {
                        hitDir = candidate.Pos - realCenter;
                    }

                    if(realCfg.HitResult != null)
                    {
                        foreach (var e in realCfg.HitResult.OnHitEffects)
                        {
                            LogicFightEffectContext newCtx = new(ctx.Env, EFightCtxType.HitBox, ctx.SourceInfo);

                            //newCtx.TriggerPos = ctx.TriggerPos;
                            //newCtx.CastVec1 = ctx.CastVec1;

                            newCtx.TriggerPos = candidate.Pos;
                            newCtx.CastVec1 = hitDir; // 对于hitbox类型 施法方向为受击方向
                            newCtx.CastVec2 = Vector2.zero;

                            newCtx.TargetId = candidate.Id;
                            ctx.Env.HandleLogicFightEffect(e, newCtx);
                        }

                        if (!realCfg.HitResult.IgnoreHit)
                        {
                            if (candidate is BaseUnitLogicEntity unitEntity)
                            {
                                // 对目标执行一次hit result
                                unitEntity.ProcessHit(ctx.SourceInfo.SrcEntityId, hitDir);
                            }
                        }
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

            bool isTrue = true;
            switch(realCfg.CheckType)
            {
                case MapAbilityEffectIfBranchCfg.ECheckType.HasBuff:
                    {
                        var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
                        if (targetEntity == null)
                        {
                            Debug.LogError($"AbilityEffectExecutor4IfBranch target err :{ctx.TargetId}");
                            return;
                        }

                        if (targetEntity.BuffManager.CheckHasBuff(targetEntity.Id, realCfg.Param1))
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
                        var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
                        if (targetEntity == null)
                        {
                            Debug.LogError($"AbilityEffectExecutor4IfBranch target err :{ctx.TargetId}");
                            return;
                        }

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
                case MapAbilityEffectIfBranchCfg.ECheckType.HasTarget:
                    {
                        if(ctx.TargetId == 0)
                        {
                            isTrue = false;
                            break;
                        }

                        var entity = ctx.Env.GetLogicEntity(ctx.TargetId);
                        if (entity == null)
                        {
                            isTrue = false;
                            break;
                        }
                    }
                    break;
                case MapAbilityEffectIfBranchCfg.ECheckType.BodyVsWin:
                    {
                        if(ctx.SourceInfo.SrcEntityId == 0)
                        {
                            isTrue = false;
                            break;
                        }

                        var atkEntity = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
                        if(atkEntity == null)
                        {
                            isTrue = false;
                            break;
                        }
                        if (ctx.TargetId == 0)
                        {
                            isTrue = false;
                            break;
                        }
                        var targetEntity = ctx.Env.GetLogicEntity(ctx.TargetId);
                        if (targetEntity == null)
                        {
                            isTrue = false;
                            break;
                        }
                        var atkBody = atkEntity.GetAttr(AttrIdConsts.PhysicalPower);
                        var defBody = targetEntity.GetAttr(AttrIdConsts.PhysicalPower);

                        long atkBonus = realCfg.Param5 > 0 ? realCfg.Param5 : 10000;
                        long defBonus = realCfg.Param6 > 0 ? realCfg.Param6 : 10000;

                        atkBody = (long)(atkBody * (10000 + atkBonus) * 0.0001);
                        defBody = (long)(defBody * (10000 + defBonus) * 0.0001);
                        var rate10000 = PlayerGamePlayRule.CalcBodyVsRate(atkBody, defBody);
                        var rand = UnityEngine.Random.Range(0, 10000);
                        if(rand < rate10000)
                        {
                            isTrue = true;
                            break;
                        }
                        else
                        {
                            isTrue = false;
                            break;
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

            if (ctx.Env.viewer == null)
            {
                return;
            }

            if (realCfg.WindowType == "htangle")
            {
                ctx.Env.viewer.ShowHTangleCloseupWindow(ctx.SourceInfo.SrcEntityId);
                return;
            }

            if (realCfg.WindowType == "knockdown")
            {
                ctx.Env.viewer.ShowKnockdownCloseupWindow(ctx.SourceInfo.SrcEntityId, realCfg.Duration);
                return;
            }

            ctx.Env.viewer.ShowKaiYouCloseupWindow(ctx.SourceInfo.SrcEntityId, realCfg.WindowType, realCfg.Duration);
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

            long addValue = realCfg.AddValue;
            if (!string.IsNullOrEmpty(realCfg.AddValueFromAttrId))
            {
                var srcProvider = new CtxFightAttrProvider(ctx);
                if (!srcProvider.TryGetAttr(realCfg.AddValueFromAttrId, out addValue) || addValue <= 0)
                {
                    return;
                }
            }

            addValue = (long)(addValue * ctx.EffectOutputScale);
            target.ApplyResourceChange(realCfg.ResourceId, addValue, realCfg.IsEnmity, realCfg.Flags, ctx.SourceInfo.SrcEntityId, extraAttrs);
        }
    }

    public class AbilityEffectExecutor4ResourcePercentDamage : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectResourcePercentDamageCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ResourcePercentDamage err");
                return;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (target == null)
            {
                Debug.LogError("AbilityEffectExecutor4ResourcePercentDamage target null");
                return;
            }

            if (realCfg.RateBp <= 0)
            {
                return;
            }

            long maxVal = target.GetResourceMax(realCfg.ResourceId);
            long delta = -(maxVal * realCfg.RateBp / 10000);
            delta = (long)(delta * ctx.EffectOutputScale);
            target.ApplyResourceChange(
                realCfg.ResourceId,
                delta,
                realCfg.IsEnmity,
                realCfg.Flags,
                ctx.SourceInfo.SrcEntityId,
                null,
                realCfg.DamageCategory);
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
            var realCfg = effectConf as MapFightEffectApplyDamageCfg;

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

            var srcProvider = new CtxFightAttrProvider(ctx);
            var dmgVal = (long)(DamagePipeline.BuildRawDamage(realCfg, srcProvider) * ctx.EffectOutputScale);
            var hImpulse = DamagePipeline.ResolveHImpulseRate10000(realCfg, ctx.SourceInfo);
            var extraAttrs = DamagePipeline.BuildPipelineExtraAttrs(realCfg, srcProvider, hImpulse);

            Vector2? srcPos = null;
            if (srcProvider.TryGetWorldPos(out var sp))
            {
                srcPos = sp;
            }
            else if (ctx.TriggerPos != null)
            {
                srcPos = ctx.TriggerPos;
            }

            Vector2? hitDir = null;
            var diff = target.Pos - (srcPos ?? target.Pos);
            if (diff.sqrMagnitude > 1e-8f)
            {
                hitDir = diff.normalized;
            }

            long? srcId = ctx.SourceInfo.SrcEntityId != 0 ? ctx.SourceInfo.SrcEntityId : null;
            string resourceId = string.IsNullOrEmpty(realCfg.ResourceId) ? AttrIdConsts.HP : realCfg.ResourceId;
            target.ApplyResourceChange(resourceId, -dmgVal, realCfg.IsEnmity, EDmgFlag.None, srcId, extraAttrs, realCfg.DamageCategory, srcPos, hitDir);

            if (realCfg.KnockBackForce > 0 && srcPos != null && realCfg.TargetType == 0)
            {
                if (target is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.ApplyKnockBack(diff, realCfg.KnockBackForce);
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
            var ok = ctx.Env.globalThrowManager.TryLaunchThrow(throwLauncher, throwTarget, realCfg,
                ctx.SourceInfo.SrcAbilityId);
            if (!ok && realCfg.ThrowFailEffect != null)
                ctx.Env.HandleLogicFightEffect(realCfg.ThrowFailEffect, ctx);
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
                case EEntityType.Trap:
                    {
                        record = new LogicEntityRecord4Trap();
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

            if (ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false) is BaseUnitLogicEntity srcUnit)
            {
                record.FactionId = srcUnit.FactionId;
            }

            if (record is LogicEntityRecord4Npc npcRec)
            {
                var npcCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(realCfg.CfgId);
                if (npcCfg == null)
                {
                    Debug.LogError($"AbilityEffectExecutor4SpawnEntity unknown npc cfg: {realCfg.CfgId}");
                    return;
                }

                var cfgFaction = (EFactionId)npcCfg.FactionId;
                if (cfgFaction != EFactionId.None)
                {
                    record.FactionId = cfgFaction;
                }

                npcRec.EnmityConfId = npcCfg.EmnityCfgId;
                npcRec.IsPeace = npcCfg.IsPeace;
                // Param1 非 0：技能显式覆盖；否则用配表 idle_move_behave（与 EMoveBehaveType 数值一致）
                npcRec.MoveBehaveType = realCfg.Param1 != 0
                    ? (UnitMoveBehaveInfo.EMoveBehaveType)realCfg.Param1
                    : (UnitMoveBehaveInfo.EMoveBehaveType)npcCfg.IdleMoveBehave;
            }

            ctx.Env.AddNewEntityRecord(record);
        }
    }

    public class AbilityEffectExecutor4SpawnSkillProxy : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectSpawnSkillProxyCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnSkillProxy cfg error");
                return;
            }

            var spec = SkillProxySpecRuntimeMap.Get(realCfg.CfgId);
            if (spec == null)
            {
                Debug.LogError($"AbilityEffectExecutor4SpawnSkillProxy unknown cfg: {realCfg.CfgId}");
                return;
            }

            long ownerId = ctx.SourceInfo.SrcEntityId;
            var owner = ctx.Env.GetLogicEntity(ownerId, false) as BaseUnitLogicEntity;
            if (owner == null)
            {
                Debug.LogError("AbilityEffectExecutor4SpawnSkillProxy owner invalid");
                return;
            }

            var record = new LogicEntityRecord4SkillProxy
            {
                Id = GameLogicManager.LogicEntityIdInst++,
                CfgId = realCfg.CfgId,
                EntityType = EEntityType.SkillProxy,
                OwnerEntityId = ownerId,
                LifeBindEntityId = ownerId,
                LifeTime = realCfg.LifeTime > 0f ? realCfg.LifeTime : spec.DefaultLifetime,
                Position = owner.Pos,
                FactionId = owner.FactionId,
            };

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

            if (ctx.TriggerPos.HasValue)
            {
                ctx.Env.viewer.ShowFakeFxEffect(realCfg.EffectText, ctx.TriggerPos.Value);
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

            ctx.Env.playerDataManager.GiveItemToPlayer(realCfg.ItemId, realCfg.Count);
        }
    }

    public class AbilityEffectExecutor4FixExpose : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectFixExposeCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4FixExpose cfg error");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster is not PlayerLogicEntity playerEntity)
            {
                Debug.LogError("AbilityEffectExecutor4FixExpose player not found.");
                return;
            }

            playerEntity.TryFixClothesFromSkill(realCfg.RestoreValue);
        }
    }

    public class AbilityEffectExecutor4ReDisguise : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (effectConf is not MapFightEffectReDisguiseCfg realCfg)
            {
                Debug.LogError("AbilityEffectExecutor4ReDisguise cfg error");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster is not PlayerLogicEntity playerEntity)
            {
                Debug.LogError("AbilityEffectExecutor4ReDisguise player not found.");
                return;
            }

            playerEntity.TryReturnDisguiseFromSkill(realCfg.InitialClothes);
        }
    }

    public class AbilityEffectExecutor4EnterExpose : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (effectConf is not MapFightEffectEnterExposeCfg)
            {
                Debug.LogError("AbilityEffectExecutor4EnterExpose cfg error");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster is not PlayerLogicEntity playerEntity)
            {
                return;
            }

            playerEntity.TryEnterExposeFromSkill();
        }
    }

    public class AbilityEffectExecutor4CauseNoise : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectCauseNoise;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4FixExpose err");
                return;
            }

            if (ctx.TriggerPos != null)
            {
                ctx.Env.viewer.ShowNoiseEffect(0.5f, ctx.TriggerPos.Value);
            }
        }
    }
    public class AbilityEffectExecutor4CreateAreaEffect : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectCreateAreaEffectCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4FixExpose err");
                return;
            }

            EntityInitInfo initInfo = new EntityInitInfo4AreaEffect();
            initInfo.CfgId = realCfg.CfgId;
            initInfo.Position = ctx.TriggerPos.Value;

            var gcLiquidEntity = ctx.Env.AreaManager.CreateEntityRecordFromInitInfo(initInfo);
            gcLiquidEntity.LifeTime = realCfg.LifeTime;

            ctx.Env.AddNewEntityRecord(gcLiquidEntity);
        }
    }

    public class AbilityEffectExecutor4AddLiquid : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectAddLiquidCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4AddLiquid err");
                return;
            }

            var offsetOffset = Vector2.zero;
            if(realCfg.OffsetRange > 0)
            {
                float offsetRange = realCfg.OffsetRange < 0.5f ? 0.5f : realCfg.OffsetRange;
                offsetOffset = UnityEngine.Random.insideUnitCircle * offsetRange;
            }

            ctx.Env.GroundLiquidManager.AddElementCircle(ctx.TriggerPos.Value + offsetOffset, realCfg.Range, realCfg.ElementType, realCfg.Duration);
        }
    }

    public class AbilityEffectExecutor4AddMist : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectAddMistCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4AddMist err");
                return;
            }

            var offsetOffset = Vector2.zero;
            if (realCfg.OffsetRange > 0)
            {
                float offsetRange = realCfg.OffsetRange < 0.5f ? 0.5f : realCfg.OffsetRange;
                offsetOffset = UnityEngine.Random.insideUnitCircle * offsetRange;
            }

            ctx.Env.GroundMistManager.AddElementCircle(
                ctx.TriggerPos.Value + offsetOffset,
                realCfg.Range,
                realCfg.ElementType,
                realCfg.Duration);
        }
    }

    public class AbilityEffectExecutor4ApplyHImpulseCfg : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectApplyHImpulseCfg;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4ApplyHImpulseCfg err");
                return;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId);
            if (target is not NpcUnitLogicEntity npc)
            {
                Debug.LogError("AbilityEffectExecutor4ApplyHImpulseCfg err 222");
                return;
            }

            npc.ApplyNpcHImpulse(realCfg.BaseVal);
        }
    }

    


    public class AbilityEffectExecutor4WantedIncidentBroadcast : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectWantedIncidentBroadcastCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4WantedIncidentBroadcast: invalid cfg type");
                return;
            }

            if (ctx.Env?.WantedManager == null || ctx.Env.AreaManager == null)
            {
                Debug.LogWarning("AbilityEffectExecutor4WantedIncidentBroadcast: env missing");
                return;
            }

            Vector2? center = null;
            if (ctx.TriggerPos.HasValue)
            {
                center = ctx.TriggerPos.Value;
            }
            else if (ctx.TargetId != 0 && ctx.Env.GetLogicEntity(ctx.TargetId) is BaseUnitLogicEntity tpos)
            {
                center = tpos.Pos;
            }
            else if (ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId) is BaseUnitLogicEntity spos)
            {
                center = spos.Pos;
            }
            else
            {
                Debug.LogWarning("AbilityEffectExecutor4WantedIncidentBroadcast: no center position");
            }

            ctx.Env.AreaManager.OnWantedBehaviourHappend(realCfg.Behave, center);
        }
    }

    
    public class AbilityEffectExecutor4InterruptCaster : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectInterruptCaster;

            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4BroadcastAttract err");
                return;
            }

            var caster = ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId);
            if (caster == null || caster is not BaseUnitLogicEntity unitEntity)
            {
                Debug.LogError("AbilityEffectExecutor4BroadcastAttract unit not found.");
                return;
            }

            unitEntity.TryInterrupt(new InterruptRequest()
            {
                source = EInterruptSource.System,
            });
        }
    }
    


    public class AbilityEffectExecutor4MiniBlurt : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectMiniBlurtCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4MiniBlurt cfg err");
                return;
            }

            var target = ctx.Env.GetLogicEntity(ctx.TargetId) as NpcUnitLogicEntity;
            if (target == null)
            {
                return;
            }

            float sjAmount = realCfg.BaseSjAmount * ctx.EffectOutputScale;
            if (sjAmount <= 0f)
            {
                return;
            }

            long? srcId = ctx.SourceInfo.SrcEntityId != 0 ? ctx.SourceInfo.SrcEntityId : null;
            target.OnNpcMiniBlurt(sjAmount, realCfg.FixedSjDamage, srcId);
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

            ctx.RunningVariables.TryGetValue("SJ_Amount", out var p1);
            ctx.RunningVariables.TryGetValue("SJ_Damage", out var p2);

            float sjAmount = 1.0f;
            float sjDamage = 1.0f;

            if(!string.IsNullOrEmpty(p1))
            {
                float.TryParse(p1, out sjAmount);
            }
            if (!string.IsNullOrEmpty(p2))
            {
                float.TryParse(p1, out sjDamage);
            }

            npcUnit.OnNpcBlurt(sjAmount, sjDamage);


            if(!npcUnit.NpcConfig.NoRealJing)
            {
                // 进入该技能时  一定就是地喷射
                var diff = ctx.Env.playerLogicEntity.Pos - npcUnit.Pos;
                // 
                bool absorb = false;
                if (diff.magnitude < 0.1f)
                {
                    absorb = true;
                }
                else if (diff.magnitude < 1.0f)
                {
                    var signedAngle = Vector2.SignedAngle(diff, npcUnit.CurrentLook);
                    if (signedAngle < 45)
                    {
                        absorb = true;
                    }
                }

                // 特效
                {
                    var effectCtx = MapSceneEffectManager.Instance.ShowSceneEffect(npcUnit.Pos, 1.5f, "Hit/blurt_default", npcUnit.Id);
                    if (effectCtx != null)
                    {
                        effectCtx.BindingUnitVec = new Vector2(0, 0.05f);
                        var dir = npcUnit.FinalLook;
                        effectCtx.EffectGo.transform.right = -dir;
                    }
                }

                // 被主角吸收
                if (absorb)
                {
                    Debug.Log("AbilityEffectExecutor4HModeBlurt sj to player");
                    ctx.Env.viewer.ShowFakeFxEffect("精浴", ctx.Env.playerLogicEntity.Pos);

                    var goodVal = sjAmount * 0.5f;
                    ctx.Env.playerLogicEntity.OnAbsorbBlurtDirectly(goodVal, npcUnit);
                }
                else
                {
                    var dropPos = npcUnit.Pos + npcUnit.CurrentLook * 0.5f;
                    float perDropAmount = sjAmount / 4f;
                    var dropAmount = Mathf.Max(1, Mathf.RoundToInt(perDropAmount * 1000f));
                    var pickupItemId = npcUnit.BlurtDropItemId;

                    for (int i = 0; i < 4; i++)
                    {
                        ctx.Env.globalDropCollection.CreateDrop(
                            string.IsNullOrEmpty(pickupItemId) ? "j_drop_small" : pickupItemId,
                            dropAmount,
                            dropPos + UnityEngine.Random.insideUnitCircle * 0.5f,
                            true,
                            npcUnit.Pos);
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


    public class AbilityFightExecutor4XuLiStage : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapFightEffectXuLiStageCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityFightExecutor4XuLiStage cfg error");
                return;
            }
            string key = $"{realCfg.CheckPhaseName}.Timed";

            if (!ctx.RunningStorage.ContainsKey(key))
            {
                Debug.LogError($"AbilityFightExecutor4XuLiStage error no key :{key}");
                return;
            }

            ctx.RunningStorage.TryGetValue(key, out var xuliTime);
            float realTime = xuliTime * 0.001f;
            int stageIdx = -1;

            Debug.Log("AbilityFightExecutor4XuLiStage time" + realTime);

            for (int i=0;i<realCfg.StageInfos.Count;i++)
            {
                if (realTime >= realCfg.StageInfos[i].NeedTime)
                {
                    stageIdx = i;
                    break;
                }
            }

            if(stageIdx != -1)
            {
                Debug.Log($"AbilityFightExecutor4XuLiStage use stage {stageIdx} ");

                var stage = realCfg.StageInfos[stageIdx];

                foreach(var innerEffect in stage.StageEffects)
                {
                    ctx.Env.HandleLogicFightEffect(innerEffect, ctx);
                }
            }
        }
    }

    public class AbilityEffectExecutor4SneakBackstabResolve : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var realCfg = effectConf as MapAbilityEffectSneakBackstabResolveCfg;
            if (realCfg == null)
            {
                Debug.LogError("AbilityEffectExecutor4SneakBackstabResolve cfg error");
                return;
            }

            var player = ctx.Env.playerLogicEntity;
            var target = ctx.Env.GetLogicEntity(ctx.TargetId) as NpcUnitLogicEntity;
            if (player == null || target == null)
            {
                Debug.LogWarning("Sneak backstab: missing player or target");
                return;
            }

            if (!PlayerGamePlayRule.CanPlayerSneakThisNpc(player, target))
            {
                Debug.Log("Sneak backstab: conditions invalid at resolve");
                return;
            }

            long phy = target.GetAttr(AttrIdConsts.PhysicalPower);

            float p = Mathf.Clamp01(PlayerGamePlayRule.BaseSuccessChance - phy * PlayerGamePlayRule.PhysicalFormPenalty);
            bool success = UnityEngine.Random.value < p;

            if (success)
            {
                target.ForceUnitUnsensored(0, player.Id);

                MainGameManager.Instance?.ShowFakeFxEffect("偷袭成功", player.Pos);
                Debug.Log($"Sneak backstab SUCCESS vs entity {target.Id}, p={p:F3}, PhysicalForm={phy}");
            }
            else
            {
                target.EnmitySystem.AddTempEnmity(PlayerGamePlayRule.FailTempEnmity);
                MainGameManager.Instance?.ShowFakeFxEffect("偷袭失败", player.Pos);
                Debug.Log($"Sneak backstab FAIL vs entity {target.Id}, p={p:F3}, PhysicalForm={phy}");
            }
        }
    }

    public class AbilityEffectExecutor4ThrowTimedInput : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var cfg = effectConf as MapAbilityEffectThrowTimedInputCfg;
            if (cfg == null)
            {
                Debug.LogError("[ThrowTimedInput] cfg null");
                return;
            }

            if (!ctx.Env.globalThrowManager.TryGetThrowContextByTargetId(ctx.TargetId, out var tctx))
            {
                Debug.LogError("[ThrowTimedInput] no throw context for target " + ctx.TargetId);
                return;
            }

            if (tctx.ActiveHold != null && !tctx.ActiveHold.Resolved)
            {
                Debug.LogWarning("[ThrowTimedInput] timed input session already active");
                return;
            }

            float start = LogicTime.time;
            float timeout = start + Mathf.Max(0.05f, cfg.TimeoutSeconds);

            tctx.ActiveHold = new TimelineHoldSession
            {
                ResultVarKey = cfg.ResultVarKey,
                StartLogicTime = start,
                TimeoutAtLogicTime = timeout,
                HoldBlocksTimelineRowsAfterIndex = ctx.ThrowTimelineEventIndex,
            };

            Transform follow = null;
            if (My.MainGameManager.Instance != null && My.MainGameManager.Instance.playerScenePresenter != null)
            {
                follow = My.MainGameManager.Instance.playerScenePresenter.transform;
            }

            My.UI.PlayerHeadThrowQteHud.ShowSession(tctx, cfg.PromptText, follow, cfg.InputMode);
        }
    }

    public class AbilityEffectExecutor4ThrowTimedInputBranch : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            var cfg = effectConf as MapAbilityEffectThrowTimedInputBranchCfg;
            if (cfg == null)
            {
                Debug.LogError("[ThrowTimedInputBranch] cfg null");
                return;
            }

            if (!ctx.Env.globalThrowManager.TryGetThrowContextByTargetId(ctx.TargetId, out var tctx))
            {
                foreach (var e in cfg.FailBranchEffects)
                {
                    if (e != null)
                    {
                        ctx.Env.HandleLogicFightEffect(e, ctx);
                    }
                }

                return;
            }

            bool success = tctx.RunningVars.TryGetValue(cfg.ResultVarKey, out var val) && val == TimelineHoldSession.OutcomeSuccess;
            var list = success ? cfg.SuccessBranchEffects : cfg.FailBranchEffects;
            if (list == null)
            {
                return;
            }

            foreach (var e in list)
            {
                if (e != null)
                {
                    ctx.Env.HandleLogicFightEffect(e, ctx);
                }
            }
        }
    }

    public class AbilityEffectExecutor4ThrowBreakFree : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (effectConf is not MapAbilityEffectThrowBreakFreeCfg)
            {
                Debug.LogError("[ThrowBreakFree] cfg type");
                return;
            }

            if (!ctx.Env.globalThrowManager.TryGetThrowContextByTargetId(ctx.TargetId, out var tctx))
            {
                return;
            }

            ctx.Env.globalThrowManager.EndThrowAsPlayerBreakFree(tctx);
        }
    }

    public class AbilityEffectExecutor4EnqueueDetachedSkill : AbilityEffectExecutor
    {
        public override void Apply(MapFightEffectCfg effectConf, LogicFightEffectContext ctx)
        {
            if (effectConf is not MapFightEffectEnqueueDetachedSkill cfg
                || string.IsNullOrEmpty(cfg.SkillId))
            {
                return;
            }

            if (ctx.Env.GetLogicEntity(ctx.SourceInfo.SrcEntityId, false) is not BaseUnitLogicEntity caster)
            {
                return;
            }

            if (ctx.Env.GetLogicEntity(ctx.TargetId, false) is not BaseUnitLogicEntity skillTarget)
            {
                return;
            }

            if (caster.ablilityManager == null)
            {
                return;
            }

            Vector2 castPoint = skillTarget.Pos;
            var delta = castPoint - caster.Pos;
            if (delta.sqrMagnitude < 1e-6f)
            {
                if (ctx.CastVec1 != null && ctx.CastVec1.Value.sqrMagnitude > 1e-6f)
                {
                    castPoint = caster.Pos + ctx.CastVec1.Value.normalized;
                }
                else if (caster.FinalLook.sqrMagnitude > 1e-6f)
                {
                    castPoint = caster.Pos + caster.FinalLook.normalized;
                }
            }

             caster.ablilityManager.EnqueueDetachedSkill(cfg.SkillId, castVec: castPoint, target: skillTarget);
        }
    }
}