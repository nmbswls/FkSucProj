using Map.Entity;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My
{
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

    [Serializable]
    public class NoneMotionData : MotionDataBase
    {

    }


    [Serializable]
    public class InstanceMotionData : MotionDataBase
    {
        // 准备时间
        public float prepareTime = 1.0f;
        public float speed = 12;

        public float homingSterRate = 1.0f;  // 转向保留原速度
        public bool homingConstantSpeed = false;
        public float homingOverrideSpeed = 12.0f;
    }

    [Serializable]
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
        // 仅非制导抛物使用；制导时水平速率由「目标距离 / 飞行时间」推出，不看本字段
        public float horizontalSpeed = 7f;
        public float gravity = 20f;
        public float arcHeight = 2f;
        public float hitRadius = 0.55f;
        public float lift = 0.6f;
        // 制导：竖直估算落地时间若短于此值，则抬到该时间并重算 vz，使与水平 dist/T 同一条轨迹自洽
        public float minFlightTime = 0.3f;
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
            if (owner.ViewRoot != null)
                owner.ViewRoot.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
        }

        public void Tick(float dt)
        {
            if (_finished) return;
            _time += dt;
            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                _finished = true;
                if (ownerProj.bindingProjInfo.pData.TriggerOnLifeEnd)
                {
                    MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(ownerProj.bindingProjInfo.instId, ownerProj.transform.position);
                    //ProjectileUtil.HandleExplodeEffect(ownerProj.bindingProjInfo, ownerProj.transform.position, null);
                }
                return;
            }

            _speed += D.acceleration * dt;
            if (_speed < 0f) _speed = 0f;

            Vector2 start = _pos;
            Vector2 delta = _dir * (_speed * dt);
            Vector2 end = start + delta;

            Collider2D hit = default;
            bool hitSomething = false;

            if (D.useCCD)
            {
                var hitResult = Physics2D.CircleCast(start, D.radius, _dir, delta.magnitude, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("MapTarget"));
                hit = hitResult.collider;
                hitSomething = hitResult.collider != null;
            }
            else
            {
                _pos = end;
                var col = Physics2D.OverlapCircle(_pos, D.radius, 1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("MapTarget"));
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
                if (ownerProj.bindingProjInfo.pData.TriggerOnCollide)
                {
                    MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(ownerProj.bindingProjInfo.instId, ownerProj.transform.position);
                    //ProjectileUtil.HandleHitOutput(ownerProj.bindingProjInfo, _pos, null);
                    Debug.Log("Handle Hit _pos " + _pos);
                }
                return true;
            }
            else if (col.gameObject.layer == LayerMask.NameToLayer("MapTarget"))
            {
                var unitPresent = tgt.GetComponentInParent<SceneUnitPresenter>();
                if (unitPresent == null || !unitPresent.CheckValid()) return false;
                if (unitPresent.Id == ownerProj.bindingProjInfo.ownerEntity.Id)
                {
                    return false;
                }
                if (unitPresent.UnitEntity.MarkDestroyed)
                {
                    return false;
                }
                //if (!PD.friendlyFire && _ctx.owner != null && tgt.transform.root == _ctx.owner.root)
                //    return false;
                long entityId = unitPresent.Id;
                if (_hitCD.TryGetValue(entityId, out float next) && _time < next) return false;
                //_hitCD[id] = _time + PD.hitCooldown;

                ProjectileUtil.HandleHitOutput(ownerProj.bindingProjInfo, ownerProj.transform.position, hitDir: _dir, unitPresent);

                _penetrationLeft--;
                if (_penetrationLeft <= 0) return true;
                return false;
            }
            return false;
        }
    }

    /// <summary>
    /// instance motion
    /// 立即触发的子弹
    /// </summary>
    public class MapProjectileInstanceMotion : IMapProjectileMotion
    {
        private MapProjectile ownerProj;

        private Vector2 _pos;
        private Vector2 _dir;

        private float _lifetime;
        private float _time;
        private bool _finished;

        private InstanceMotionData D => (InstanceMotionData)ownerProj.bindingProjInfo.pData.motionData;
        private ProjectileData PD => ownerProj.bindingProjInfo.pData;

        private Dictionary<long, float> _hitCD = new();

        public bool IsFinished => _finished;
        public Vector2 Position => _pos;
        public Vector2 Forward => _dir;

        /// <summary>
        /// 转向时间
        /// </summary>
        public float turnResponseTime = 0.3f;

        public void Initialize(MapProjectile owner)
        {
            this.ownerProj = owner;
            _pos = ownerProj.bindingProjInfo.spawnPos;
            _dir = ownerProj.bindingProjInfo.initialDir;

            _lifetime = PD.maxLifetime;
            _time = 0f;
            _finished = false;
            _hitCD.Clear();

            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
            if (!ownerProj.bindingProjInfo.pData.lockAngle)
            {
                if (owner.ViewRoot != null)
                    owner.ViewRoot.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
            }
        }

        float angleVel;


        public void Tick(float dt)
        {
            if (_finished) return;
            _time += dt;
            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                _finished = true;
                if (ownerProj.bindingProjInfo.pData.TriggerOnLifeEnd)
                {
                    MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(ownerProj.bindingProjInfo.instId, ownerProj.transform.position);
                }
                return;
            }

            // 未到准备阶段
            if (_time >= D.prepareTime)
            {
                _finished = true;
                //ProjectileUtil.HandleHitOutput(ownerProj.bindingProjInfo, ownerProj.transform.position, null);
                MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(ownerProj.bindingProjInfo.instId, ownerProj.transform.position);
                return;
            }

            TickHoming();

        }

        private void TickHoming()
        {

            if (!PD.isHoming) return;
            if (_time > PD.homingTime) return;

            // 导航
            var targetId = ownerProj.bindingProjInfo.homingTargetId;
            if (targetId == null || targetId == 0)
            {
                _pos = _pos + _dir * D.speed * LogicTime.deltaTime;
                Debug.Log(ownerProj.gameObject.name + " update pos " + ownerProj.transform.position);
                return;
            }
            var target = MainGameManager.Instance.gameLogicManager.GetLogicEntity(targetId.Value);
            if (target == null) return;

            Vector2 toTarget = ((Vector2)target.Pos - _pos);
            float desired = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            float current = ownerProj.transform.eulerAngles.z;

            float newAngle = Mathf.SmoothDampAngle(current, desired, ref angleVel, turnResponseTime, Mathf.Infinity, LogicTime.deltaTime);
            ownerProj.transform.rotation = Quaternion.Euler(0, 0, newAngle);

            Vector2 forward = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));

            // 固定速度 可能跑过
            if (D.homingConstantSpeed)
            {
                Vector2 vel = forward * D.speed;
                _pos = _pos + vel * LogicTime.deltaTime;
            }
            else
            {
                if (toTarget.sqrMagnitude < 1e-6f) return;
                if (toTarget.sqrMagnitude < LogicTime.deltaTime * D.speed)
                {
                    _pos = toTarget;
                }
                else
                {
                    Vector2 vel = forward * D.speed;
                    _pos = _pos + vel * LogicTime.deltaTime;
                }
            }

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

        private int _hitsRemaining;
        private readonly Dictionary<long, float> _perTargetNextHitTime = new();
        private bool _emptyGroundExplodeDone;
        private float _peakZ;
        private float _zLaunch;

        private const float PerTargetHitCooldown = 0.18f;
        private const float SinkZ = -0.45f;
        // 首帧 z 很小、vz 经一步积分后易同时满足「落地」，且 Overlap 与出生点重叠 → 需延迟再判命中/落地
        private const float ArmedDelay = 0.08f;

        public bool IsFinished => _finished;
        public Vector2 Position => _pos;
        public Vector2 Forward => _vxy.sqrMagnitude > 0.0001f ? _vxy.normalized : Vector2.right;

        public void Initialize(MapProjectile owner)
        {
            Owner = owner;
            _pos = owner.bindingProjInfo.spawnPos;

            Vector2 dir;
            ILogicEntity homingTgt = null;
            if (PD.isHoming)
            {
                long homingId = owner.bindingProjInfo.homingTargetId ?? 0;
                homingTgt = MainGameManager.Instance.gameLogicManager.GetLogicEntity(homingId, false);
                if (homingTgt != null)
                {
                    Vector2 toT = (Vector2)homingTgt.Pos - _pos;
                    dir = toT.sqrMagnitude > 1e-8f ? toT.normalized : Vector2.right;
                }
                else
                {
                    dir = InitialDirNormalized();
                }
            }
            else
            {
                dir = InitialDirNormalized();
            }

            float g = Mathf.Max(0.01f, D.gravity);
            float vzFromArc = Mathf.Sqrt(Mathf.Max(0.0001f, 2f * g * Mathf.Max(0f, D.arcHeight)));
            _zLaunch = 0.12f;
            _z = _zLaunch;
            _peakZ = _z;

            float horizScalar;
            if (PD.isHoming && homingTgt != null)
            {
                float dist = Vector2.Distance(_pos, (Vector2)homingTgt.Pos);
                dist = Mathf.Max(0.02f, dist);
                _vz = vzFromArc;
                float tNat = EstimateTimeToPseudoGround(_z, _vz, g);
                float tUse = tNat;
                if (D.minFlightTime > 0.02f)
                {
                    tUse = Mathf.Max(tNat, D.minFlightTime);
                }

                if (tUse > tNat + 1e-4f)
                {
                    // z0 + vz*T - g*T^2/2 = 0 => vz = g*T/2 - z0/T，与水平 dist/T 共用同一 T
                    _vz = g * 0.5f * tUse - _z / tUse;
                    _vz = Mathf.Max(0.08f, _vz);
                }

                horizScalar = Mathf.Clamp(dist / Mathf.Max(0.08f, tUse), 0.15f, 80f);
            }
            else
            {
                _vz = vzFromArc;
                horizScalar = Mathf.Max(0.01f, D.horizontalSpeed);
            }

            _vxy = dir * horizScalar;
            _time = 0f;
            _lifetime = PD.maxLifetime;
            _finished = false;
            _perTargetNextHitTime.Clear();
            _hitsRemaining = PD.maxPenetration > 0 ? PD.maxPenetration : 1;
            _emptyGroundExplodeDone = false;
            Owner.ConfigureParabolaVisual(D);
        }

        // z(t)=z0+vz*t-g/2*t^2=0 的正根：水平射程 ≈ |vxy|*t 时应接近目标距离
        private static float EstimateTimeToPseudoGround(float z0, float vz, float g)
        {
            float disc = vz * vz + 2f * g * z0;
            if (disc < 0f)
            {
                return Mathf.Max(0.2f, 2f * Mathf.Abs(vz) / g);
            }

            float t = (vz + Mathf.Sqrt(disc)) / g;
            return Mathf.Clamp(t, 0.12f, 40f);
        }

        private Vector2 InitialDirNormalized()
        {
            Vector2 d = Owner.bindingProjInfo.initialDir;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right;
        }

        public void Tick(float dt)
        {
            if (_finished) return;
            _time += dt;
            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                DoTimeoutFinishWithoutGroundHit();
                return;
            }

            _pos += _vxy * dt;
            _vz -= D.gravity * dt;
            _z += _vz * dt;
            _peakZ = Mathf.Max(_peakZ, _z);
            Owner.UpdateParabolaVisual(_pos, _z, Forward);

            if (_time >= ArmedDelay)
            {
                TryFlightHits();
                if (_finished)
                {
                    return;
                }
            }

            if (!_emptyGroundExplodeDone
                && _time >= ArmedDelay
                && _vz <= 0f
                && _z <= 0f
                && (_peakZ >= _zLaunch + 0.02f || _time >= 0.5f))
            {
                _emptyGroundExplodeDone = true;
                DoTimeoutFinishWithoutGroundHit();
                return;
            }

            if (_time >= ArmedDelay && _z < SinkZ)
            {
                DoTimeoutFinishWithoutGroundHit();
                return;
            }
        }

        private void DoTimeoutFinishWithoutGroundHit()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(
                Owner.bindingProjInfo.instId,
                _pos);
        }

        private void TryFlightHits()
        {
            if (PD.EntityHitResult == null || _hitsRemaining <= 0)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.visionSenser == null)
            {
                return;
            }

            float hitR = Mathf.Max(0.05f, D.hitRadius);

            var filter = new EntityFilterParam
            {
                FilterType = EEntityType.None,
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = Owner.bindingProjInfo.ownerEntity.FactionId,
            };

            Vector2 hitDir = Forward;
            float atkHeight = _z;

            foreach (var ent in glm.visionSenser.OverlapCircleAllEntity(_pos, hitR, filter, atkHeight))
            {
                if (_hitsRemaining <= 0)
                {
                    break;
                }

                if (ent is not BaseUnitLogicEntity unit)
                {
                    continue;
                }

                if (unit.MarkDestroyed || unit.IsDead)
                {
                    continue;
                }

                if (_perTargetNextHitTime.TryGetValue(unit.Id, out var nextT) && _time < nextT)
                {
                    continue;
                }

                var pres = SceneAOIManager.Instance != null
                    ? SceneAOIManager.Instance.GetActivePresentation(unit.Id) as SceneUnitPresenter
                    : null;
                if (pres == null || !pres.CheckValid())
                {
                    continue;
                }

                ProjectileUtil.HandleHitOutput(Owner.bindingProjInfo, _pos, hitDir, pres);
                _hitsRemaining--;
                float cd = Mathf.Max(0.02f, PerTargetHitCooldown);
                _perTargetNextHitTime[unit.Id] = _time + cd;

                if (_hitsRemaining <= 0)
                {
                    _finished = true;
                    MainGameManager.Instance.gameLogicManager.projectileHolder.OnProjectileExplode(
                        Owner.bindingProjInfo.instId,
                        _pos);
                    return;
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
        public long? homingTargetId;     // 追踪

    }
}
