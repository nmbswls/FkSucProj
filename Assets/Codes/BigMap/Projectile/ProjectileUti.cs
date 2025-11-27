using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using static My.GameLogicManager;

public static class ProjectileUtil
{
    public static void ApplyDamage(SceneUnitPresenter unitPresenter, float damage, long entityId)
    {
        unitPresenter.UnitEntity.ApplyResourceChange(AttrIdConsts.HP, -40, true, new SourceKey() {type = SourceType.Bullet, entityId = entityId });
    }

    public static void HandleHitOutput(LogicProjectileInfo logicProjectile, Vector2 hitPosition, SceneUnitPresenter? hitOne)
    {
        foreach(var ef in logicProjectile.pData.OnHitEffects)
        {
            var efCtx = new LogicFightEffectContext(MainGameManager.Instance.gameLogicManager, new SourceKey() { type = SourceType.Bullet, entityId = logicProjectile.ownerEntity.Id });
            efCtx.Actor = logicProjectile.ownerEntity;
            efCtx.ActorFactionId = logicProjectile.ownerEntity.FactionId;
            efCtx.Position = hitPosition;
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