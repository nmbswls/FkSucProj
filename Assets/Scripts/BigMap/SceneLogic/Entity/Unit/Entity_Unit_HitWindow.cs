

using System;
using System.Collections.Generic;
using System.Security.Principal;
using My.Map.Fight;
using My.Map.Entity;
using My.Map.Fight;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static My.GameLogicManager;

namespace My.Map
{

    public partial class BaseUnitLogicEntity
    {
        public UnitHitWindowRegistry HitWindowRegistry { get; set; }

        public event Action<long, string, float, string?> EventOnUseWeapon; // 使用武器回调 由
        public event Action<string, long> EventOnHideWeapon;

        public long ApplyUseWeapon(
            string weaponName,
            string animName,
            float duration,
            List<MapFightEffectCfg> hitCfgs,
            int maxHit = 0,
            Dictionary<string, long> castAttrSnapshot = null)
        {
            var hitWindow = HitWindowRegistry.OpenHitWindow(
                duration,
                false,
                hitEffects: hitCfgs,
                srcWeaponName: weaponName,
                castAttrSnapshot: castAttrSnapshot);
            hitWindow.MaxHitCount = maxHit;
            EventOnUseWeapon?.Invoke(hitWindow.HitId, weaponName, duration, animName);
            return hitWindow.HitId;
        }

        public void ClearWeapon(string weaponName)
        {
            EventOnHideWeapon?.Invoke(weaponName, 0);
        }
    }

    /// <summary>
    /// 单位hit窗口
    /// </summary>
    public class ActiveHitWindow
    {
        public long HitId;
        public float openTime;
        public float durationTime;
        public List<long> HitRecord = new();

        public int HitParam0;
        public int HitParam1;

        public List<MapFightEffectCfg> OnHitEffects; // 原始数据 还是生成hitwindow专用数据放入？

        public bool IsSilentHit; // 是否是静默hit（不产生碰撞特效等）

        public int MaxHitCount;

        public Dictionary<string, long> CastAttrSnapshot;
    }

    public class UnitHitWindowRegistry
    {
        public long HitWiindowIdCounter = 10000;

        
        public Dictionary<long, ActiveHitWindow> activeHitWindows = new();
        public BaseUnitLogicEntity Owner { get; private set; }
        public UnitHitWindowRegistry(BaseUnitLogicEntity owner)
        {
            this.Owner = owner;
        }

        public ActiveHitWindow OpenHitWindow(
            float duration,
            bool isSilentHit,
            List<MapFightEffectCfg> hitEffects = null,
            string? srcWeaponName = null,
            Dictionary<string, long> castAttrSnapshot = null)
        {
            long hitId = ++HitWiindowIdCounter;

            var hitWin = new ActiveHitWindow()
            {
                HitId = hitId,
                openTime = LogicTime.time,
                durationTime = duration,
                IsSilentHit = isSilentHit,
                OnHitEffects = hitEffects,
                CastAttrSnapshot = FightCastAttrUtil.CopyCacheAttrs(castAttrSnapshot),
            };

            activeHitWindows[hitId] = hitWin;
            return hitWin;
        }


        public void CloseHitWindow(long hitId)
        {
            if(activeHitWindows.TryGetValue(hitId, out var hitWindow))
            {
                activeHitWindows.Remove(hitId);
            }
        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="hitId"></param>
        /// <param name="hitEntityId"></param>
        public void OnMapHitboxCallback(long hitId, long hitEntityId)
        {
            activeHitWindows.TryGetValue(hitId, out var window);

            if(window == null)
            {
                Debug.LogError("OnMapHitTriggerCallback");
                return;
            }

            var hitEntity = Owner.LogicManager.GetLogicEntity(hitEntityId, false);
            if(hitEntity == null || hitEntity.MarkDestroyed)
            {
                Debug.LogError("OnMapHitTriggerCallback");
                return;
            }

            if (window.HitRecord.Contains(hitEntityId))
            {
                return;
            }

            //  todo 多次命中
            window.HitRecord.Add(hitEntityId);

            if (hitEntity is BaseUnitLogicEntity peaceTarget
                && FightEffectInterceptors.ShouldBlockHit(Owner, peaceTarget))
            {
                return;
            }

            if (!window.IsSilentHit && hitEntity is BaseUnitLogicEntity unitHitTarget)
            {
                // 对目标执行一次hit result
                unitHitTarget.ProcessHit(Owner.Id, Owner.FinalLook);
            }

            if (window.OnHitEffects != null)
            {
                var srcInfo = new EffectSourceInfo()
                {
                    SrcType = ESourceType.Ability,
                    SrcEntityId = Owner.Id,
                };

                foreach (var hitEffect in window.OnHitEffects)
                {
                    GameLogicManager.LogicFightEffectContext newCtx = new(Owner.LogicManager, EFightCtxType.HitBox, srcInfo);

                    newCtx.TargetId = hitEntity.Id;
                    newCtx.TriggerPos = Owner.Pos;
                    //newCtx.CastVec1 = hitEntity.Pos - EntityOwner.Pos;
                    newCtx.CastVec1 = Owner.FinalLook;

                    if (window.CastAttrSnapshot != null)
                    {
                        FightCastAttrUtil.CopyInto(window.CastAttrSnapshot, newCtx.CacheAttrVal);
                    }

                    Owner.LogicManager.HandleLogicFightEffect(hitEffect, newCtx);
                }

                MainGameManager.Instance.StartHitStop(0.04f);

            }
            
        }
    }




}
