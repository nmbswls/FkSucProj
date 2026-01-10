
using System;
using My.Map;
using System.Collections.Generic;
using UnityEngine;

namespace My

{
    public class ProjectileHolder
    {
        public Dictionary<long, LogicProjectileInfo> ProjectileInfos = new();
        public static long IdInstCounter = 10000;

        public event Action<LogicProjectileInfo> EventOnLogicProjectileSpawn;

        public LogicProjectileInfo CreateLogicProjectile(ProjectileData pData, ILogicEntity caster, Vector2 bornPos, Vector2 dir, long? homingTarget = null)
        {
            var projectilInfo = new LogicProjectileInfo
            {
                instId = ++IdInstCounter,
                ownerEntity = caster,
                pData = pData,
                spawnPos = bornPos,
                initialDir = dir,
            };

            projectilInfo.homingTargetId = homingTarget;
            ProjectileInfos.Add(projectilInfo.instId, projectilInfo);
            EventOnLogicProjectileSpawn?.Invoke(projectilInfo);
            return projectilInfo;
        }

        public void TickLogicProjectile()
        {

        }

        public void OnProjectileTriggered(long projectileId)
        {
            ProjectileInfos.TryGetValue(projectileId, out var pInfo);
            if (pInfo != null)
            {
                // give effect
                //pInfo.
            }
        }

    }


    public partial class GameLogicManager
    {

        
    }
}