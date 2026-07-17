
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;
using static My.GameLogicManager;

namespace My

{
    public class ProjectileHolder
    {
        public Dictionary<long, LogicProjectileInfo> ProjectileInfos = new();
        public static long IdInstCounter = 10000;

        public event Action<LogicProjectileInfo> EventOnLogicProjectileSpawn;

        public LogicProjectileInfo CreateLogicProjectile(
            ProjectileData pData,
            ILogicEntity caster,
            Vector2 bornPos,
            Vector2 dir,
            long? homingTarget = null,
            Vector2? homingTargetPos = null,
            float parabolaLaunchZ = 0f)
        {
            var projectilInfo = new LogicProjectileInfo
            {
                instId = ++IdInstCounter,
                ownerEntity = caster,
                pData = pData,
                spawnPos = bornPos,
                initialDir = dir,
                parabolaLaunchZ = parabolaLaunchZ,
            };

            projectilInfo.homingTargetId = homingTarget;
            projectilInfo.homingTargetPos = homingTargetPos;
            ProjectileInfos.Add(projectilInfo.instId, projectilInfo);
            EventOnLogicProjectileSpawn?.Invoke(projectilInfo);
            return projectilInfo;
        }

        public void TickLogicProjectile()
        {

        }

        public void OnProjectileExplode(long projectileId, Vector2 explodePos)
        {
            if (!ProjectileInfos.TryGetValue(projectileId, out var pInfo) || pInfo == null)
            {
                return;
            }

            if (pInfo.pData.ExplodeEffects != null)
            {
                foreach (var ef in pInfo.pData.ExplodeEffects)
                {
                    var srcInfo = new GameLogicManager.EffectSourceInfo()
                    {
                        SrcType = GameLogicManager.ESourceType.Bullet,
                        SrcInstId = pInfo.instId,
                        SrcEntityId = pInfo.ownerEntity.Id,
                        SrcFactionId = pInfo.ownerEntity.FactionId,
                    };


                    var efCtx = new GameLogicManager.LogicFightEffectContext(MainGameManager.Instance.gameLogicManager, EFightCtxType.Bullet, srcInfo);
                    efCtx.TriggerPos = explodePos;
                    efCtx.CastVec1 = explodePos;

                    MainGameManager.Instance.gameLogicManager.HandleLogicFightEffect(ef, efCtx);
                }
            }

            ProjectileInfos.Remove(projectileId);
        }

    }


    public partial class GameLogicManager
    {

        
    }
}