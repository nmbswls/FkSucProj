using Map.Entity;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMapProjectileMotion
{
    // 由运行时传入上下文，便于Motion访问共用数据
    void Initialize(MapProjectile owner);
    // 物理步
    void Tick(float dt);
    // 供Manager或Projectile查询：是否应结束（比如抛物落地）
    bool IsFinished { get; }
    // 提供当前位置与朝向（用于渲染与命中）
    Vector2 Position { get; }
    Vector2 Forward { get; }
}


public class LinearMotionData : MotionDataBase
{
    [Header("Linear")]
    public float speed = 18f;
    public float acceleration = 0f;
    public bool useCCD = true;
    public float radius = 0.1f;
}

public class ParabolaMotionData : MotionDataBase
{
    [Header("Horizontal")]
    public float horizontalSpeed = 7f;
    public float horizontalDrag = 0f;

    [Header("Vertical (Pseudo-Z)")]
    public float gravity = 20f;
    public float arcHeight = 2f;
    public bool overrideVzByFlightTime = false;
    public float flightTime = 0f;

    [Header("Visual")]
    public float lift = 0.6f;
    public AnimationCurve shadowScaleByZ = AnimationCurve.Linear(0, 1, 5, 0.5f);
    public AnimationCurve shadowAlphaByZ = AnimationCurve.Linear(0, 1, 5, 0.5f);

    [Header("Explode")]
    public float aoeRadius = 2f;
}

public class MapProjectileLinearMotion : IMapProjectileMotion
{
    private MapProjectile ownerProj;
    private Vector2 _pos;
    private Vector2 _dir;
    private float _speed;
    private float _lifetime;
    private int _penetrationLeft;
    private float _time;
    private bool _finished;

    private LinearMotionData D => (LinearMotionData)ownerProj.bindingProjInfo.pData.motionData;
    private ProjectileData PD => ownerProj.bindingProjInfo.pData;

    private Dictionary<long, float> _hitCD = new();

    public bool IsFinished => _finished;
    public Vector2 Position => _pos;
    public Vector2 Forward => _dir;

    public void Initialize(MapProjectile owner)
    {
        this.ownerProj = owner;
        _pos = ownerProj.bindingProjInfo.spawnPos;
        _dir = ownerProj.bindingProjInfo.initialDir;
        _speed = D.speed;
        _lifetime = PD.maxLifetime;
        _penetrationLeft = PD.maxPenetration;
        _time = 0f;
        _finished = false;
        _hitCD.Clear();

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
        owner.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
    }

    public void Tick(float dt)
    {
        if (_finished) return;
        _time += dt;
        _lifetime -= dt;
        if (_lifetime <= 0f) { _finished = true; return; }

        _speed += D.acceleration * dt;
        if (_speed < 0f) _speed = 0f;

        Vector2 start = _pos;
        Vector2 delta = _dir * (_speed * dt);
        Vector2 end = start + delta;

        Collider2D hit = default;
        bool hitSomething = false;

        if (D.useCCD)
        {
            var hitResult = Physics2D.CircleCast(start, D.radius, _dir, delta.magnitude, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Units"));
            hit = hitResult.collider;
            hitSomething = hitResult.collider != null;
        }
        else
        {
            _pos = end;
            var col = Physics2D.OverlapCircle(_pos, D.radius, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Units"));
            if (col != null)
            {
                hitSomething = true;
                hit = col;
            }
        }

        if (hitSomething)
        {
            if (HandleHit(hit))
            {
                // 终止
                _finished = true;
                //ProjectileUtil.PlayFX(PD.impactFX, hit.point, hit.normal);
                return;
            }
            else
            {
                _pos = end;
                // 穿透继续，位置推进至命中点
                //_pos = hit.point + _dir * (D.radius * 0.5f);
            }
        }
        else
        {
            _pos = end;
        }
    }

    private bool HandleHit(Collider2D col)
    {
        if (col == null) return false;

        GameObject tgt = col.gameObject;

        if (col.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            return true;
        }
        else if(col.gameObject.layer == LayerMask.NameToLayer("Units"))
        {
            var unitPresent = tgt.GetComponentInParent<SceneUnitPresenter>();
            if (unitPresent == null || !unitPresent.CheckValid()) return false;
            if(unitPresent.Id == ownerProj.bindingProjInfo.ownerEntity.Id)
            {
                return false;
            }
            if(unitPresent.UnitEntity.MarkDead)
            {
                return false;
            }
            //if (!PD.friendlyFire && _ctx.owner != null && tgt.transform.root == _ctx.owner.root)
            //    return false;
            long entityId = unitPresent.Id;
            if (_hitCD.TryGetValue(entityId, out float next) && _time < next) return false;
            //_hitCD[id] = _time + PD.hitCooldown;

            ProjectileUtil.ApplyDamage(unitPresent, PD.damage, ownerProj.bindingProjInfo.ownerEntity.Id);

            _penetrationLeft--;
            if (_penetrationLeft <= 0) return true;
            return false;
        }
        return false;
    }
}


public class MapProjectileParabolaMotion : IMapProjectileMotion
{
    public MapProjectile Owner;

    private ParabolaMotionData D => (ParabolaMotionData)Owner.bindingProjInfo.pData.motionData;
    private ProjectileData PD => Owner.bindingProjInfo.pData;

    private Vector2 _pos;
    private Vector2 _vxy;
    private float _z;
    private float _vz;
    private float _time;
    private float _lifetime;
    private bool _finished;

    public bool IsFinished => _finished;
    public Vector2 Position => _pos;           // 地面位置（阴影位置）
    public Vector2 Forward => _vxy.sqrMagnitude > 0.0001f ? _vxy.normalized : Vector2.right;

    public void Initialize(MapProjectile owner)
    {
        this.Owner = owner;
        //_ctx = ctx;
        //_pos = ctx.spawnPos;
        var dir = Owner.bindingProjInfo.initialDir.sqrMagnitude > 0.0001f ? Owner.bindingProjInfo.initialDir.normalized : Vector2.right;
        _vxy = dir * Mathf.Max(0.01f, D.horizontalSpeed);

        if (D.overrideVzByFlightTime && D.flightTime > 0.02f)
            _vz = D.gravity * (D.flightTime * 0.5f);
        else
            _vz = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * D.gravity * Mathf.Max(0f, D.arcHeight)));

        _z = 0.01f;
        _time = 0f;
        _lifetime = PD.maxLifetime;
        _finished = false;

        // 设置视觉（阴影/抬升）由外层Projectile负责（根据Motion类型）
        Owner.ConfigureParabolaVisual(D);
    }

    public void Tick(float dt)
    {
        if (_finished) return;
        _time += dt;
        _lifetime -= dt;
        if (_lifetime <= 0f) { Explode(); return; }

        if (D.horizontalDrag > 0f)
        {
            float k = Mathf.Clamp01(D.horizontalDrag * dt);
            _vxy *= (1f - k);
        }

        _pos += _vxy * dt;
        _vz -= D.gravity * dt;
        _z += _vz * dt;

        // 视觉更新
        Owner.UpdateParabolaVisual(_pos, _z, Forward);

        if (_z <= 0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        _finished = true;
        //if (PD.explodeFX) ProjectileUtil.PlayFX(PD.explodeFX, _pos, Vector2.up);

        if (D.aoeRadius > 0.01f)
        {
            var cols = Physics2D.OverlapCircleAll(_pos, D.aoeRadius);
            foreach (var c in cols)
            {
                if (c == null) continue;
                var unitPresent = c.GetComponentInParent<SceneUnitPresenter>();
                if (unitPresent == null) continue;
                //if (!PD.friendlyFire && _ctx.owner != null && tgt.transform.root == _ctx.owner.root)
                //    return false;
                long entityId = unitPresent.Id;
                //_hitCD[id] = _time + PD.hitCooldown;
                ProjectileUtil.ApplyDamage(unitPresent, PD.damage, Owner.bindingProjInfo.ownerEntity.Id);

                //if (!PD.friendlyFire && _ctx.owner != null && c.transform.root == _ctx.owner.root)
                //    continue;
            }
        }
    }
}

/// <summary>
/// 逻辑层信息
/// </summary>
public class LogicProjectileInfo
{
    public long instId;
    //public Projectile projectile;      // 运行时容器（可用于回调）
    public ILogicEntity ownerEntity;
    public ProjectileData pData;        // 统一数据

    //public MotionDataBase motionData;  // 具体运动数据SO

    public Vector2 spawnPos;
    public Vector2 initialDir;
    public Transform homingTarget;     // 追踪用（可空）
}