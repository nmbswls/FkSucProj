using DG.Tweening;
using Map.Entity;
using Map.Logic;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using static UnityEngine.GraphicsBuffer;


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
        private float acceleration = 99.0f;
        private float externalDecay = 30f;          // 外力自然衰减（每秒）

        public SimpleCharacterController CharacterController;

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

            if(!CharacterController)
            {
                CharacterController = GetComponent<SimpleCharacterController>();
                CharacterController.GetDisiredVelFunc = GetFixedDesiredVel;
            }
        }

        private float _tickMoveStateTimer;

        public override void Tick(float dt)
        {
            if (_logic == null) return;

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

            //if (UnitEntity.MarkDead || UnitEntity.GetAttr(AttrIdConsts.Unmovable) > 0
            //    || UnitEntity.dashIntent != null || UnitEntity.knockBackIntent != null)
            //{
            //    UnitEntity.activeMoveVec = Vector2.zero;
            //}
            //else
            //{
            //    float currMoveSpeed = GetCurrentMoveSpeed();
            //    Vector2 targetMoveVel;
            //    // 优先让受控移动生效
            //    if (UnitEntity.targettedMoveIntent != null && UnitEntity.targettedMoveIntent.targettedDesireDir != null)
            //    {
            //        if((UnitEntity.targettedMoveIntent.MoveTarget - UnitEntity.Pos).magnitude < 0.05f)
            //        {
            //            targetMoveVel = Vector2.zero;
            //        }
            //        else
            //        {
            //            targetMoveVel = UnitEntity.targettedMoveIntent.targettedDesireDir * currMoveSpeed;
            //        }
            //    }
            //    else
            //    {
            //        targetMoveVel = freeMoveDir * currMoveSpeed;
            //    }

            //    UnitEntity.activeMoveVec = Vector2.MoveTowards(UnitEntity.activeMoveVec, targetMoveVel, acceleration * dt);
            //}

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

        //private Vector2 smoothedTarget;

        /// <summary>
        /// 底层物理移动
        /// </summary>
        /// <returns></returns>
        public Vector2 GetFixedDesiredVel()
        {
            if(UnitEntity == null) return Vector2.zero;
            float arriveRaiuds = 0.12f;
            if (UnitEntity.MarkDead || UnitEntity.GetAttr(AttrIdConsts.Unmovable) > 0
                || UnitEntity.dashIntent != null || UnitEntity.knockBackIntent != null)
            {
                //UnitEntity.activeMoveVec = Vector2.zero;
                return UnitEntity.externalVel;
            }
            else
            {
                Vector2 pos = transform.position;
                float currMoveSpeed = GetCurrentMoveSpeed();
                Vector2 targetMoveVel;
                // 优先让受控移动生效
                if (UnitEntity.targetMoveIntent != null)
                {
                    float slowRadius = UnitEntity.targetMoveIntent.ArriveDistance * 1.2f;

                    Vector2 finishPos = UnitEntity.Pos;
                    if (UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FollowEntity)
                    {
                        finishPos = UnitEntity.targetMoveIntent.FollowEntity.Pos;
                    }
                    else if (UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FixPoint)
                    {
                        finishPos = UnitEntity.targetMoveIntent.FixedMoveTarget;
                    }

                    Vector2 toTarget = finishPos - UnitEntity.Pos;
                    float dist = toTarget.magnitude;

                    float maxSpeed = GetCurrentMoveSpeed();
                    if(UnitEntity.targetMoveIntent.SpeedType == BaseUnitLogicEntity.TargettedMoveIntent.ESpeedType.Slow)
                    {
                        maxSpeed = 1f;
                    }

                    float targetSpeed = (dist > slowRadius)
                        ? maxSpeed
                        : Mathf.Lerp(0f, maxSpeed, Mathf.InverseLerp(UnitEntity.targetMoveIntent.ArriveDistance, slowRadius, dist));


                    //UnitEntity.targetMoveIntent.targettedDesireDir


                    //Vector2 toReal = movePos - smoothedTarget;
                    //float maxStep = maxSpeed * Time.fixedDeltaTime;
                    //smoothedTarget += Vector2.ClampMagnitude(toReal, maxStep);

                    //Vector2 toTarget = smoothedTarget - UnitEntity.Pos;
                    //float dist = toTarget.magnitude;

                    

                    Vector3 desired = dist > 1e-3f ? toTarget.normalized * targetSpeed : Vector3.zero;

                    if (dist < UnitEntity.targetMoveIntent.ArriveDistance)
                    {
                        desired = Vector3.zero;
                    }
                    else
                    {
                        desired = targetSpeed * UnitEntity.targetMoveIntent.targettedDesireDir;
                    }
                    //float step = Time.fixedDeltaTime * currMoveSpeed;
                    //if(UnitEntity.targettedMoveIntent.FixedMoveTarget != null)
                    //{

                    //}
                    //else if (UnitEntity.targettedMoveIntent.FollowEntity != null)
                    //{
                    //    if ((UnitEntity.targettedMoveIntent.FollowEntity.Pos - pos).magnitude < step)
                    //    {
                    //        targetMoveVel = Vector2.zero;
                    //    }
                    //    else
                    //    {
                    //        targetMoveVel = UnitEntity.targettedMoveIntent.targettedDesireDir * currMoveSpeed;
                    //    }
                    //}
                    //else
                    //{
                    //    targetMoveVel = Vector2.zero;
                    //}
                    targetMoveVel = desired;
                }
                else
                {
                    targetMoveVel = freeMoveDir * currMoveSpeed;
                }
                //var clampedPos = WorldAreaManager.Instance.ClampPathToWalkable(transform.position, pos + targetMoveVel * Time.fixedDeltaTime);
                return targetMoveVel + UnitEntity.externalVel;
                //UnitEntity.activeMoveVec = Vector2.MoveTowards(UnitEntity.activeMoveVec, targetMoveVel, acceleration * dt);
            }


            //UnitEntity.activeMoveVec + UnitEntity.externalVel
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
            _updateVisibleTimer = 0.2f;
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
            //if (UnitEntity.MoveActMode == BaseUnitLogicEntity.EUnitMoveActMode.PatrolFollow)
            //{
            //    var followEntity = UnitEntity.LogicManager.AreaManager.GetLogicEntiy(this.UnitEntity.FollowPatrolId) as PatrolGroupLogicEntity;
            //    if (followEntity == null)
            //    {
            //        return UnitEntity.GetCurrSpeed();
            //    }
            //    return followEntity.MoveSpeed;
            //}
            //if (UnitEntity.targetMoveIntent != null && UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FollowEntity)
            //{
            //    if (UnitEntity.targetMoveIntent.FollowEntity is BaseUnitLogicEntity unit)
            //    {
            //        return unit.GetCurrSpeed();
            //    }
            //}

            return UnitEntity.GetCurrSpeed();
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
            if (UnitEntity.targetMoveIntent == null)
            {
                return;
            }
            Vector2 destNow = navAgent.destination;

            // 重算路径
            if (UnitEntity.targetMoveIntent.NeedRecalculatePath)
            {
                if (UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FollowEntity)
                {
                    navAgent.SetDestination(UnitEntity.targetMoveIntent.FollowEntity.Pos);
                    UnitEntity.targetMoveIntent.NeedRecalculatePath = false;
                }
                else if(UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FixPoint)
                {
                    navAgent.SetDestination(UnitEntity.targetMoveIntent.FixedMoveTarget);
                    UnitEntity.targetMoveIntent.NeedRecalculatePath = false;
                }
            }

            if(UnitEntity.targetMoveIntent.MoveType == BaseUnitLogicEntity.TargettedMoveIntent.ETargettedMoveType.FollowEntity)
            {
                if ((destNow - UnitEntity.targetMoveIntent.FollowEntity.Pos).magnitude > 0.1f)
                {
                    navAgent.SetDestination(UnitEntity.targetMoveIntent.FollowEntity.Pos);
                }
            }

            // pending中 等待寻找
            if (!navAgent.hasPath || navAgent.pathPending)
            {
                return;
            }

            Vector2 currPos = transform.position;

            UnitEntity.targetMoveIntent.targettedDesireDir = Vector2.zero;

            if ((currPos - destNow).magnitude < UnitEntity.targetMoveIntent.ArriveDistance)
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
            UnitEntity.targetMoveIntent.targettedDesireDir = desired;
        }


        protected void FixedUpdate()
        {
            
        }

        private float skin = 0.02f;
        private RaycastHit2D[] hits = new RaycastHit2D[8];

        protected bool IsWallLayer(int layer)
        {
            bool iswall = ((1 << layer) & (1 << LayerMask.NameToLayer("Wall"))) != 0;
            return iswall;
        }


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


