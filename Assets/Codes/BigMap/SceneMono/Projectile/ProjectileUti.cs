using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using static My.GameLogicManager;

public static class ProjectileUtil
{
    public static void HandleExplodeEffect(MapProjectile mapProjecilt, Vector2 hitPosition)
    {
        //_dir

        //if (mapProjecilt.bindingProjInfo.pData.PassHitResult != null)
        //{
        //    foreach (var ef in logicProjectile.pData.PassHitResult.OnHitEffects)
        //    {
        //        var srcInfo = new EffectSourceInfo()
        //        {
        //            SrcType = ESourceType.Bullet,
        //            SrcInstId = logicProjectile.instId,
        //            SrcEntityId = logicProjectile.ownerEntity.Id,
        //            SrcFactionId = logicProjectile.ownerEntity.FactionId,
        //        };


        //        var efCtx = new LogicFightEffectContext(MainGameManager.Instance.gameLogicManager, srcInfo);
        //        efCtx.TriggerPos = hitPosition;
        //        efCtx.CastVec1 = hitPosition - logicProjectile.spawnPos;

        //        MainGameManager.Instance.gameLogicManager.HandleLogicFightEffect(ef, efCtx);
        //    }

        //    if (!logicProjectile.pData.PassHitResult.IgnoreHit)
        //    {
        //        if (hitOne != null && hitOne.UnitEntity != null)
        //        {
        //            // 对目标执行一次hit result
        //            hitOne.UnitEntity.ProcessHit(logicProjectile.ownerEntity?.Id ?? 0, hitDir);
        //        }
        //    }
        //}
    }


    public static void HandleHitOutput(LogicProjectileInfo logicProjectile, Vector2 hitPosition, Vector2 hitDir, SceneUnitPresenter? hitOne)
    {
        if(logicProjectile.pData.PassHitResult != null)
        {
            foreach (var ef in logicProjectile.pData.PassHitResult.OnHitEffects)
            {
                var srcInfo = new EffectSourceInfo()
                {
                    SrcType = ESourceType.Bullet,
                    SrcInstId = logicProjectile.instId,
                    SrcEntityId = logicProjectile.ownerEntity.Id,
                    SrcFactionId = logicProjectile.ownerEntity.FactionId,
                };


                var efCtx = new LogicFightEffectContext(MainGameManager.Instance.gameLogicManager, srcInfo);
                efCtx.TriggerPos = hitPosition;
                efCtx.CastVec1 = hitPosition - logicProjectile.spawnPos;

                MainGameManager.Instance.gameLogicManager.HandleLogicFightEffect(ef, efCtx);
            }

            if(!logicProjectile.pData.PassHitResult.IgnoreHit)
            {
                if (hitOne != null && hitOne.UnitEntity != null)
                {
                    // 对目标执行一次hit result
                    hitOne.UnitEntity.ProcessHit(logicProjectile.ownerEntity?.Id ?? 0, hitDir);
                }
            }
        }
        
    }

    public static void PlayFX(SceneUnitPresenter unitPresenter, Vector2 pos, Vector2 normal)
    {
        //if (unitPresenter == null) return;
        //var go = Object.Instantiate(fx, pos, Quaternion.identity);
        //go.transform.right = normal.sqrMagnitude > 0.0001f ? (Vector3)normal : Vector3.right;
        //Object.Destroy(go, 3f);
    }
}