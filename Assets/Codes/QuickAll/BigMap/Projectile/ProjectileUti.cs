using UnityEngine;

public static class ProjectileUtil
{
    public static void ApplyDamage(SceneUnitPresenter unitPresenter, float damage)
    {
        unitPresenter.UnitEntity.OnHit(10, null);
    }

    public static void PlayFX(SceneUnitPresenter unitPresenter, Vector2 pos, Vector2 normal)
    {
        //if (unitPresenter == null) return;
        //var go = Object.Instantiate(fx, pos, Quaternion.identity);
        //go.transform.right = normal.sqrMagnitude > 0.0001f ? (Vector3)normal : Vector3.right;
        //Object.Destroy(go, 3f);
    }
}