using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unit.Ability.Effect;
using UnityEngine;


public class ProjectileData
{
    public string id;
    public float maxLifetime = 6f;
    public float damage = 10f;
    //public LayerMask hitMask;            // 直线碰撞/命中层
    //public LayerMask aoeMask;            // 爆炸AOE层（抛物）
    public bool friendlyFire = false;
    public int maxPenetration = 0;       // 直线可穿透数
    //public float hitCooldown = 0.05f;

    //public GameObject bodyPrefab;        // 本体可视
    //public GameObject shadowPrefab;      // 抛物用阴影，可为空
    public bool rotateBodyToVelocity = true;

    //[Header("FX")]
    //public GameObject impactFX;          // 直线命中点FX
    //public GameObject explodeFX;         // 抛物/终止FX
    public float fxAutoDestroy = 3f;

    public EMotionType motiontype;
    public MotionDataBase motionData;    // 指向具体运动SO（Linear/Parabola/Homing）

    public List<MapFightEffectCfg> OnHitEffects;
}


public enum EMotionType
{
    Invalid,
    Linear,
    Parabola,
    Homing,
}

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
