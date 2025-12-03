using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using static My.GameLogicManager;

public static class ProjectileUtil
{
    public static void HandleHitOutput(LogicProjectileInfo logicProjectile, Vector2 hitPosition, SceneUnitPresenter? hitOne)
    {
        foreach(var ef in logicProjectile.pData.OnHitEffects)
        {
            var srcInfo = new EffectSourceInfo()
            {
                SrcType = ESourceType.Bullet,
                SrcInstId = logicProjectile.instId,
                SrcEntityId = logicProjectile.ownerEntity.Id,
                SrcFactionId = logicProjectile.ownerEntity.FactionId,
            };


            var efCtx = new LogicFightEffectContext(MainGameManager.Instance.gameLogicManager, srcInfo);
            efCtx.CastPos = hitPosition;
            efCtx.CastDir = hitPosition - logicProjectile.spawnPos;

            MainGameManager.Instance.gameLogicManager.HandleLogicFightEffect(ef, efCtx);
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