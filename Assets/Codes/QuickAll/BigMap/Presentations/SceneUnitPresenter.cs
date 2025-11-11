using DG.Tweening;
using Map.Entity;
using Map.Logic;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;


public interface IMapWeaponHolder
{
    void OnWeaponHitCallback(long hitId, long hitEntityId);
}

namespace My.Map.Scene
{
    /// <summary>
    /// 场景单位 基类
    /// </summary>
    public abstract class SceneUnitPresenter : ScenePresentationBase<BaseUnitLogicEntity>, IMapWeaponHolder
    {


        [SerializeField] protected SpriteRenderer icon;
        [SerializeField] protected GameObject highlightFx;
        public Transform ViewPoint;
        public GameObject faceIndicator;

        public Transform WeaponRoot;
        public MapUnitWeaponCtrl WeaponCtrl; // 武器控制器

        // 控制移动组件
        public NavMeshAgent navAgent;
        public Rigidbody2D rb;
        public Collider2D mainCol;

        public Vector2 freeMoveDir;
        private float acceleration = 20.0f;
        private float externalDecay = 30f;          // 外力自然衰减（每秒）


        public BaseUnitLogicEntity UnitEntity
        {
            get
            {
                return (BaseUnitLogicEntity)_logic;
            }
        }

        public void Update()
        {
            Tick(LogicTime.deltaTime);
        }

        protected override void Awake()
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            mainCol = GetComponent<Collider2D>();

            if (!navAgent)
            {
                navAgent = GetComponentInChildren<NavMeshAgent>();
            }
            if (navAgent != null)
            {
                navAgent.updatePosition = false;
                navAgent.updateRotation = false;
            }
        }

        private float _tickMoveStateTimer;

        public override void Tick(float dt)
        {
            if (navAgent != null)
            {
                navAgent.nextPosition = rb.position;
            }
            // 同步位置
            if (UnitEntity != null)
            {
                UnitEntity.SetPosition(MainGameManager.Instance.GetLogicPosFromWorldPos(transform.position));
            }

            UpdateTargettedMoveState();

            UpdateVisible(dt);

            if (UnitEntity.MarkDead || UnitEntity.GetAttr(AttrIdConsts.Unmovable) > 0
                || UnitEntity.dashIntent != null || UnitEntity.knockBackIntent != null)
            {
                UnitEntity.activeMoveVec = Vector2.zero;
            }
            else
            {
                float currMoveSpeed = GetCurrentMoveSpeed();
                Vector2 targetMoveVel;
                // 优先让受控移动生效
                if (UnitEntity.targettedMoveIntent != null && UnitEntity.targettedMoveIntent.targettedDesireDir != null)
                {
                    targetMoveVel = UnitEntity.targettedMoveIntent.targettedDesireDir * currMoveSpeed;
                }
                else
                {
                    targetMoveVel = freeMoveDir * currMoveSpeed;
                }

                UnitEntity.activeMoveVec = Vector2.MoveTowards(UnitEntity.activeMoveVec, targetMoveVel, acceleration * dt);
            }

            // 不锁面向时 调整
            if (UnitEntity.GetAttr(AttrIdConsts.LockFace) == 0)
            {
                if (WeaponRoot != null)
                {
                    float angle = Mathf.Atan2(UnitEntity.FaceDir.y, UnitEntity.FaceDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
                    WeaponRoot.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
                }
            }

            // 外力自然衰减（除非在Dash中保持常速）
            if (UnitEntity.dashIntent == null && UnitEntity.knockBackIntent == null)
            {
                UnitEntity.externalVel = Vector2.MoveTowards(UnitEntity.externalVel, Vector2.zero, externalDecay * dt);
            }

            //if (knockBackIntent.knockbackTimeLeft <= 0f || externalVel.magnitude < knockBackIntent.knockbackMinEndSpeed)
            //    ClearKnockbackIntent();

            if (icon != null)
            {
                if (UnitEntity.AnimOverrideList.Count > 0)
                {
                    icon.color = Color.cyan;
                }
                else
                {
                    icon.color = Color.white;
                }
            }

            UpdateFaceDirIndicator();
        }


        private bool _visibleNow = false;
        private float _updateVisibleTimer = 0;
        protected void UpdateVisible(float dt)
        {
            _updateVisibleTimer -= dt;
            if (_updateVisibleTimer > 0)
            {
                return;
            }
            _updateVisibleTimer = 0.4f;
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }
            bool visible = false;
            var diff = MainGameManager.Instance.playerScenePresenter.transform.position - transform.position;
            diff.z = 0;

            if (diff.magnitude < 1.0f)
            {
                visible = true;
            }
            if (!visible)
            {
                visible = MainGameManager.Instance.VisionSenser2D.CanSee(MainGameManager.Instance.playerScenePresenter.transform.position, MainGameManager.Instance.playerScenePresenter.UnitEntity.FaceDir,
                    transform.position,
                    MainGameManager.Instance.playerScenePresenter.UnitEntity.viewRadius, MainGameManager.Instance.playerScenePresenter.UnitEntity.fovDegrees);
            }

            if (visible)
            {
                icon.enabled = true;
                if (faceIndicator != null)
                {
                    faceIndicator?.gameObject.SetActive(true);
                }
            }
            else
            {
                icon.enabled = false;
                if (faceIndicator != null)
                    faceIndicator?.gameObject.SetActive(false);
            }
        }


        protected float GetCurrentMoveSpeed()
        {
            if (UnitEntity.MoveActMode == BaseUnitLogicEntity.EUnitMoveActMode.PatrolFollow)
            {
                var followEntity = UnitEntity.LogicManager.AreaManager.GetLogicEntiy(this.UnitEntity.FollowPatrolId) as PatrolGroupLogicEntity;
                if (followEntity == null)
                {
                    return UnitEntity.moveSpeed;
                }
                return followEntity.MoveSpeed;
            }
            return UnitEntity.moveSpeed;
        }

        protected override void LateUpdate()
        {
            // 同步位置
            if (UnitEntity != null)
            {
                //transform.position = MainGameManager.Instance.GetWorldPosFromLogicPos(UnitEntity.Pos);
            }


        }


        public float maxTurnSpeedDegPerSec = 720f; // 可选最大角速度限制
        public float smoothTime = 0.12f;           // 越小越跟手
        private float angularVel;                  // SmoothDampAngle 内部速度缓存

        protected void UpdateFaceDirIndicator()
        {
            if (faceIndicator == null) return;
            float targetAngle = Mathf.Atan2(UnitEntity.FaceDir.y, UnitEntity.FaceDir.x) * Mathf.Rad2Deg;
            float currentAngle = faceIndicator.transform.eulerAngles.z;

            float desired = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref angularVel, smoothTime);

            // 可选：限制每帧角度变化不超过最大角速度
            float maxStep = maxTurnSpeedDegPerSec * LogicTime.deltaTime;
            float delta = Mathf.DeltaAngle(currentAngle, desired);
            delta = Mathf.Clamp(delta, -maxStep, maxStep);

            float newAngle = currentAngle + delta;

            faceIndicator.transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            InitWeaponInfo();

            UnitEntity.EventOnDeath += () =>
            {
                icon.DOColor(new Color(1, 1, 1, 0), 0.5f);
                //.OnComplete(() =>
                //{
                //    SceneAOIManager.Instance.UnregisterEntity(logic, transform.position);
                //})
                //.OnKill(() =>
                //{
                //    //icon.color = new Color(1, 1, 1, 0);
                //    SceneAOIManager.Instance.UnregisterEntity(logic, transform.position);
                //});
            };

            //UnitEntity.onNewDashIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.dashDir.normalized * intent.dashSpeed;
            //};

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};
        }

        protected virtual void InitWeaponInfo()
        {
            UnitEntity.abilityController.EventOnApplyUseWeapon += (hitId, weaponName, duration) =>
            {
                WeaponCtrl.ApplyUseWeapon(weaponName, hitId, duration);
            };

            UnitEntity.abilityController.EventOnCloseHitWindow += (hitId) =>
            {
                WeaponCtrl.OnHitWindowClear(hitId);
            };
        }


        #region 受控移动


        public void UpdateTargettedMoveState()
        {
            if (UnitEntity.targettedMoveIntent == null)
            {
                return;
            }
            // 重算路径
            if (UnitEntity.targettedMoveIntent.NeedRecalculatePath)
            {
                navAgent.SetDestination(UnitEntity.targettedMoveIntent.MoveTarget);
                UnitEntity.targettedMoveIntent.NeedRecalculatePath = false;
            }

            // pending中 等待寻找
            if (!navAgent.hasPath || navAgent.pathPending)
            {
                return;
            }

            Vector2 currPos = transform.position;

            UnitEntity.targettedMoveIntent.targettedDesireDir = Vector2.zero;

            if ((currPos - UnitEntity.targettedMoveIntent.MoveTarget).magnitude < 0.1f)
            {
                return;
            }

            if (UnitEntity.GetAttr(AttrIdConsts.Unmovable) > 0)
            {
                return;
            }

            // 从Agent获取期望速度，投影到XY
            Vector3 desired3 = navAgent.desiredVelocity;
            Vector2 desired = new Vector2(desired3.x, desired3.y);
            desired = desired.normalized;
            UnitEntity.targettedMoveIntent.targettedDesireDir = desired;
        }


        protected void FixedUpdate()
        {
            if (UnitEntity == null)
            {
                return;
            }
            //var clamped = GetClampedMoveVelocity();
            var delta = UnitEntity.activeMoveVec * Time.deltaTime;
            Vector2 pos = rb.position;
            rb.MovePosition(pos + delta);

            //FixDynamicBlock();
        }

        //public Vector2 GetClampedMoveVelocity()
        //{
        //    // 你的移动速度（单位：米/秒）
        //    float moveSpeed = 0.2f;
        //    float dt = Time.fixedDeltaTime;

        //    // 当前位置与目标位置
        //    Vector2 pos = rb.position;
        //    Vector2 target = UnitEntity.targettedMoveIntent.MoveTarget; // 需要有目标点
        //    Vector2 dir = UnitEntity.targettedMoveIntent.targettedDesireDir; // 已归一化的方向向量（长度≈1）

        //    // 距离与本帧位移
        //    float dist = Vector2.Distance(pos, target);
        //    float step = moveSpeed * dt;

        //    // 停止/到达判定阈值，避免抖动
        //    float arriveThreshold = 0.02f; // 可根据角色体型/速度调整

        //    if (dist <= arriveThreshold)
        //    {
        //        return Vector2.zero;
        //    }

        //    // 裁剪步长：不要超过剩余距离
        //    float clampedStep = Mathf.Min(step, dist);

        //    // 也可以用指向目标的真实方向，避免旧方向与目标不一致
        //    Vector2 realDir = (dist > 1e-6f) ? (target - pos).normalized : dir;

        //    return realDir * clampedStep;
        //}

        private float skin = 0.02f;
        private RaycastHit2D[] hits = new RaycastHit2D[8];

        protected bool IsWallLayer(int layer)
        {
            bool iswall = ((1 << layer) & (1 << LayerMask.NameToLayer("Wall"))) != 0;
            return iswall;
        }

        protected void FixDynamicBlock()
        {
            Vector2 v = UnitEntity.activeMoveVec + UnitEntity.externalVel;
            float dt = Time.fixedDeltaTime;
            if (v.sqrMagnitude < 1e-8f) return;

            Vector2 pos = rb.position;
            const int MaxSlides = 3;
            float skin = this.skin; // 例如 0.02f
            pos += v * dt;
            //for (int iter = 0; iter < MaxSlides; iter++)
            //{
            //    float dist = v.magnitude * dt;
            //    if (dist <= 1e-6f) break;

            //    // Cast沿当前速度方向
            //    int count = mainCol.Cast(v.normalized, hits, dist + skin, false);
            //    // 选择最近“墙层”命中
            //    int hitIndex = -1;
            //    float bestDist = float.MaxValue;
            //    for (int i = 0; i < count; i++)
            //    {
            //        int layer = hits[i].collider.gameObject.layer;
            //        if (!IsWallLayer(layer)) continue; // 只对墙阻挡
            //        if (hits[i].distance < bestDist) { bestDist = hits[i].distance; hitIndex = i; }
            //    }

            //    if (hitIndex < 0)
            //    {
            //        // 没有命中墙：直接位移
            //        pos += v * dt;
            //        break;
            //    }
            //    else
            //    {
            //        // 推到命中点前（减skin）
            //        float allowed = Mathf.Max(0f, bestDist - skin);
            //        pos += v.normalized * allowed;

            //        // 对速度做“滑动投影”
            //        Vector2 n = hits[hitIndex].normal; // 法线指向我们（Unity的2D/3D法线方向均为朝向碰撞体外侧）
            //        float vn = Vector2.Dot(v, n);
            //        if (vn < 0f)
            //        {
            //            v = v - vn * n; // 去除法线分量，保留切向速度
            //        }

            //        // 如果投影后速度很小，结束
            //        if (v.sqrMagnitude < 1e-6f)
            //            break;

            //        // 继续下一轮（可能撞到另一面墙，迭代滑动）
            //        // 注意：不要再对“单位层”进行阻挡，否则贴墙滑动会被其他单位卡住
            //    }
            //}

            rb.MovePosition(pos);
        }

        //void ApplySoftInteration(ref Vector2 v, float dt)
        //{
        //    var neighbors = Physics2D.OverlapCircleAll(rb.position, neighborRadius, unitsLayerMask);
        //    foreach (var col in neighbors)
        //    {
        //        if (col.attachedRigidbody == rb) continue;
        //        Vector2 pij = (Vector2)col.attachedRigidbody.position - rb.position;
        //        float dist = pij.magnitude;
        //        if (dist < 1e-4f) continue;

        //        Vector2 n = pij / dist;
        //        Vector2 vj = GetNeighborVelocity(col); // 需要你维护
        //        Vector2 vr = v - vj;

        //        // 相向度：正值表示逼近
        //        float approach = Vector2.Dot(vr, n);
        //        if (approach > 0f)
        //        {
        //            // 法向阻尼
        //            float k = softBlockK; // 0.4f
        //            v -= k * approach * n;

        //            // 侧向偏移（打破对冲）
        //            float side = sideGain; // 拥挤时增大
        //            Vector2 t = new Vector2(-n.y, n.x);
        //            v += side * t;
        //        }
        //    }

        //    // 拥挤时限速
        //    int density = neighbors.Length;
        //    float vmax = Mathf.Lerp(vMaxCrowd, vMaxNormal, Mathf.Clamp01(1f - (density - crowdStart) / (crowdPeak - crowdStart)));
        //    v = Vector2.ClampMagnitude(v, vmax);
        //}


        // 分离控制（基于目标分离速度）
        public float selfRadius = 0.5f;
        public float queryRadius = 1.2f;
        public int maxNeighbors = 12;
        public float kp = 10f;                // 穿透 -> 目标分离速度 比例
        public float kd = 5f;                 // 相对法向速度阻尼
        public float maxSepSpeedPerPair = 6f; // 单对最大分离速度
        public float maxImpulsePerPair = 3.5f;// 单对最大冲量(速度增量)
        public float maxTotalImpulse = 6f;    // 单帧累计最大冲量
        public bool enableUnitSeparation = true;  // 是否启用单位间推挤
        public float separationRadius = 0.5f;     // 自身碰撞半径（与CircleCollider2D一致或略大）
        public float separationStrength = 12f;    // 推挤力度系数k
        public float separationDamping = 3f;      // 推挤阻尼c
        public float pushPriority = 1f;       // 优先级(越大越优先推动别人，自己少动)
        public float maxExternalSpeed = 20f;
        public float blendDelta = 0.6f;       // 候选位移到最终位移的插值比例

        // 推挤缓存
        private readonly Collider2D[] neighborBuffer = new Collider2D[32];


        #endregion


        #region 冲刺/击退处理

        public LayerMask wallsLayer;




        // 示例：攻击命中或撞墙事件可调用
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            // 墙体撞击：在击退或冲刺中撞墙的处理
            int layer = collision.collider.gameObject.layer;
            bool isWall = (wallsLayer.value & (1 << layer)) != 0;

            if (isWall)
            {
                Vector2 normal = collision.contacts[0].normal;
                if (UnitEntity.knockBackIntent != null)
                {
                    // 撞墙后结束击退并可转眩晕
                    UnitEntity.externalVel = Vector2.zero;
                    //ApplyStun(defaultStunDuration * 0.6f);
                }

                if (UnitEntity.dashIntent != null)
                {
                    // Dash撞墙立即结束
                    UnitEntity.ClearDashIntent();
                }
            }
        }


        #endregion

        /// <summary>
        /// 武器回调
        /// </summary>
        /// <param name="hitId"></param>
        /// <param name="hitEntityId"></param>
        public void OnWeaponHitCallback(long hitId, long hitEntityId)
        {
            UnitEntity.abilityController.OnUseWeaponHitCallback(hitId, hitEntityId);
        }

        /// <summary>
        /// 更新view透明度 
        /// 根据优先级和状态 决定最终显示效果
        /// </summary>
        public void UpdateViewAlpha()
        {
            if (UnitEntity.GetAttr(AttrIdConsts.HidingMask) > 0)
            {
                icon.color = new Color(1, 1, 1, 0.6f);
            }
            else
            {
                icon.color = new Color(1, 1, 1, 1);
            }
        }
    }
}


