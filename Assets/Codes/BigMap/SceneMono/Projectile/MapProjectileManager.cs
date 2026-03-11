using System;
using System.Collections.Generic;
using My.Map.Entity;
using My.Map.Fight;
using UnityEngine;


/// <summary>
/// 子弹配置数据
/// </summary>
public class ProjectileData
{
    public string id;
    public float maxLifetime = 6f;

    public bool friendlyFire = false;
    public int maxPenetration = 0;       // 直线可穿透数

    public bool rotateBodyToVelocity = true;

    //[Header("FX")]
    //public GameObject impactFX;          // 直线命中点FX
    //public GameObject explodeFX;         // 抛物/终止FX
    public float fxAutoDestroy = 3f;

    public bool showRangeWarn = false;

    public EMotionType motiontype;
    public MotionDataBase motionData;    // 指向具体运动SO（Linear/Parabola/Homing）

    public FightStruct.Shape ProjShape;

    public bool isHoming;
    public float homingTime = 999; // 制导时间

    public bool TriggerOnLifeEnd;
    public bool TriggerOnCollide;

    // 击中单位效果
    public FightStruct.HitResult EntityHitResult = null;

    // 爆炸效果
    public List<MapFightEffectCfg> ExplodeEffects = null;

    public bool lockAngle = false;
}


public enum EMotionType
{
    Invalid, 
    Linear,
    Parabola,
    Homing,
}

[Serializable]
public abstract class MotionDataBase
{
    //public abstract IMapProjectileMotion CreateMotionInstance();

}
 

public class MapProjectileManager : MonoBehaviour
{
    public static MapProjectileManager Instance { get; private set; }
    void Awake() { if (Instance != null && Instance != this) Destroy(gameObject); else Instance = this; }

    public List<GameObject> PrefabList = new();
    private Dictionary<string, GameObject> a = new();

    public MapProjectile Spawn(LogicProjectileInfo logicProjectile, Transform homingTarget = null)
    {
        var projectilePrefab = Resources.Load<GameObject>($"Prefab/Projectile/{logicProjectile.pData.id}");
        // var firstPrefab = PrefabList.FirstOrDefault();
        var newGo = GameObject.Instantiate(projectilePrefab, transform);
        //newGo.name = "Projectile";
        //var go = new GameObject($"Projectile_{logicProjectile.pData.id}");
        var p = newGo.AddComponent<MapProjectile>();

        p.Launch(logicProjectile, homingTarget);
        return p;
    }
}
