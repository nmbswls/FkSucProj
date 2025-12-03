using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using My.Map.Entity;
using UnityEngine;


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

    public EMotionType motiontype;
    public MotionDataBase motionData;    // 指向具体运动SO（Linear/Parabola/Homing）

    public bool TriggerOnLifeEnd;
    public bool TriggerOnCollide;
    public List<MapFightEffectCfg> OnHitEffects = new() ;
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
        var firstPrefab = PrefabList.FirstOrDefault();
        var newGo = GameObject.Instantiate(firstPrefab, transform);
        //newGo.name = "Projectile";
        //var go = new GameObject($"Projectile_{logicProjectile.pData.id}");
        var p = newGo.AddComponent<MapProjectile>();

        p.Launch(logicProjectile, homingTarget);
        return p;
    }
}
