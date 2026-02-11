using Animancer;
using DG.Tweening;
using Map.Entity;
using Map.Logic;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Properties;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using static My.GameLogicManager;
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
    public abstract partial class SceneUnitPresenter : ScenePresentationBase<BaseUnitLogicEntity>, IMapWeaponHolder, ISceneTargettable
    {
        [SerializeField] protected GameObject highlightFx;

        public Transform ViewRoot;
        public Transform ShadowView;
        public Transform AgentView;
        private SpriteRenderer[] srs;

        //public SpriteRenderer MainView;

        public Transform ViewPoint;
        public GameObject faceIndicator;

        public Transform HitPivot;

        //public Transform WeaponRoot;
        public MapUnitWeaponCtrl WeaponCtrl; // 武器控制器

        // 控制移动组件
        public NavMeshAgent navAgent;
        public Rigidbody2D rb;
        public Collider2D mainCol;

        //public Vector2 freeMoveDir;
        private float acceleration = 99.0f;

        public SimpleCharacterController CharacterController;

        public Collider2D targetCol;

        public BaseUnitLogicEntity UnitEntity
        {
            get
            {
                return (BaseUnitLogicEntity)_logic;
            }
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
                CharacterController.ClampValidPos = (desired) =>
                {
                    return WorldAreaManager.Instance.ClampPathToWalkable(transform.position, desired);
                };
                CharacterController.IsGhoseMove = () =>
                {
                    if (UnitEntity == null) return false;
                    return UnitEntity.CheckHasState(AttrIdConsts.Ghost);
                };
                CharacterController.SyncPos = (pos) =>
                {
                    UnitEntity.SetPosition(pos);
                };
                
            }

            if(AgentView != null)
            {
                srs = AgentView.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            }

            MainFlasher = GetComponentInChildren<SpriteWhiteFlasher>();

            InitAnimComps();
        }

        private float _tickMoveStateTimer;

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if (navAgent != null)
            {
                navAgent.nextPosition = rb.position;
            }
            // 同步位置
            if (UnitEntity != null)
            {
                UnitEntity.SetPosition(MainGameManager.Instance.GetLogicPosFromWorldPos(transform.position));
            }

            //UpdateTargettedMoveState();

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
            //if (UnitEntity.GetAttr(AttrIdConsts.LockFace) == 0)
            {
                if(AgentView != null)
                {
                    if (UnitEntity.CurrentLook.x >= 0)
                    {
                        AgentView.localScale = new Vector3(Mathf.Abs(AgentView.localScale.x), AgentView.localScale.y, AgentView.localScale.z);
                    }
                    else
                    {
                        AgentView.localScale = new Vector3(-Mathf.Abs(AgentView.localScale.x), AgentView.localScale.y, AgentView.localScale.z);
                    }
                }
            }
            //if (knockBackIntent.knockbackTimeLeft <= 0f || externalVel.magnitude < knockBackIntent.knockbackMinEndSpeed)
            //    ClearKnockbackIntent();

            if (srs != null)
            {
                if (UnitEntity.AnimOverrideList.Count > 0)
                {
                    //icon.color = Color.cyan;
                }
                else
                {
                    //icon.color = Color.white;
                }
            }

            UpdateFaceDirIndicator();


            UpdateOffsetZView();
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
            if (!CheckCanActiveMove())
            {
                //UnitEntity.activeMoveVec = Vector2.zero;
                return UnitEntity.externalVel;
            }
            else
            {
                Vector2 targetMoveVel = UnitEntity.GetDesiredVelocity();
                return targetMoveVel + UnitEntity.externalVel;
            }

        }

        public virtual bool CheckCanActiveMove()
        {
            if(UnitEntity.MarkDestroyed)
            {
                return false;
            }

            if(UnitEntity.MarkDespawn)
            {
                return false;
            }

            if(UnitEntity.IsDead)
            {
                return false;
            }

            if(UnitEntity.CheckHasState(AttrIdConsts.Unmovable))
            {
                return false;
            }

            if(UnitEntity.controlledMoveCtx != null)
            {
                return false;
            }

            return true;
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
            _updateVisibleTimer = 0.3f;
            if (MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }


            bool visible = false;
            if(MainGameManager.Instance.playerScenePresenter.PlayerEntity.IsQueenMode)
            {
                visible = true;
            }
            else
            {
                var diff = MainGameManager.Instance.playerScenePresenter.transform.position - transform.position;
                diff.z = 0;

                if (diff.magnitude < 0.6f)
                {
                    visible = true;
                }
                if (!visible)
                {
                    visible = MainGameManager.Instance.VisionSenser2D.SimpleCanSee(MainGameManager.Instance.playerScenePresenter.transform.position, MainGameManager.Instance.playerScenePresenter.UnitEntity.CurrentLook,
                        transform.position,
                        8f, 120f);
                }
            }
            

            if (visible && !UnitEntity.MarkDestroyed)
            {
                if(ViewRoot != null)
                {
                    ViewRoot.gameObject.SetActive(true);
                }

                if (faceIndicator != null)
                {
                    faceIndicator?.gameObject.SetActive(true);
                }
            }
            else
            {
                if(ViewRoot != null)
                {
                    ViewRoot.gameObject.SetActive(false);
                }
                
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
            float targetAngle = Mathf.Atan2(UnitEntity.CurrentLook.y, UnitEntity.CurrentLook.x) * Mathf.Rad2Deg;
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

            OnEventGhostChange(this.Id);
            OnEventInvisibleChange(this.Id);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            UnitEntity.EventOnDie += OnEventUnitDie;
            UnitEntity.EventOnHit += OnEventUnitHit;
            UnitEntity.EventOnStartStealth += OnEventStartStealth;
            UnitEntity.EventOnEndStealth += OnEventEndStealth;
            UnitEntity.EventOnAttachStatusChanged += OnEventConvertAttach;
            UnitEntity.EventOnGhostChange += OnEventGhostChange;
            UnitEntity.EventOnInvisibleChange += OnEventInvisibleChange;
            UnitEntity.EventOnBuffRegister += OnEventBuffRegister;
            UnitEntity.EventOnBuffUnregister += OnEventBuffUnregister;

            UnitEntity.EventOnAnimPlay += OnEventAnimPlay;
            

            //UnitEntity.onNewDashIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.dashDir.normalized * intent.dashSpeed;
            //};

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            UnitEntity.EventOnDie -= OnEventUnitDie;
            UnitEntity.EventOnHit -= OnEventUnitHit;
            UnitEntity.EventOnStartStealth -= OnEventStartStealth;
            UnitEntity.EventOnEndStealth -= OnEventEndStealth;
            UnitEntity.EventOnAttachStatusChanged -= OnEventConvertAttach;
            UnitEntity.EventOnGhostChange -= OnEventGhostChange;
            UnitEntity.EventOnInvisibleChange -= OnEventInvisibleChange;
            UnitEntity.EventOnBuffRegister -= OnEventBuffRegister;
            UnitEntity.EventOnBuffUnregister -= OnEventBuffUnregister;

            UnitEntity.EventOnAnimPlay -= OnEventAnimPlay;
        }



        protected virtual void InitWeaponInfo()
        {
            UnitEntity.abilityController.EventOnApplyUseWeapon += (hitId, weaponName, duration) =>
            {
                WeaponCtrl.ApplyUseWeapon(weaponName, hitId, duration);
            };

            UnitEntity.abilityController.EventOnCloseHitWindow += (weaponName, hitId) =>
            {
                WeaponCtrl.OnHitWindowClear(weaponName, hitId);
            };
        }

        #region 监听

        protected virtual void OnEventUnitDie(long entityId)
        {
            MainGameManager.Instance.ShowFakeFxEffect("我死了", this.transform.position);

            if(ViewRoot != null)
            {
                ViewRoot.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            }
        }

        protected virtual void OnEventUnitHit(long entityId, long? srcId)
        {
            PresenterOnHit(srcId);
        }

        protected virtual void OnEventStartStealth(long entityId)
        {
            //if (mainCol != null)
            //{
            //    mainCol.enabled = false;
            //}

            var pres = SceneAOIManager.Instance.GetActivePresentation(entityId);
            if (pres != null)
            {
                pres.SetFadeAlpha(0.35f);
            }
        }


        protected virtual void OnEventEndStealth(long entityId)
        {
            //if (mainCol != null)
            //{
            //    mainCol.enabled = true;
            //}

            var pres = SceneAOIManager.Instance.GetActivePresentation(entityId);
            if (pres != null)
            {
                pres.SetFadeAlpha(1);
            }
        }

        protected override void OnEventEntityDestroyed(long entityId)
        {
            base.OnEventEntityDestroyed(entityId);

            if (ViewRoot != null)
            {
                ViewRoot.gameObject.SetActive(false);
                //ViewRoot.DOColor(new Color(1, 1, 1, 0), 0.5f);
            }
        }

        private Dictionary<long, int> _bindingEffectsCtxId = new();

        protected virtual void OnEventBuffRegister(BuffInstance buffInst)
        {
            if(!string.IsNullOrEmpty(buffInst.Def.EffectId))
            {
                var effectCtx = MapSceneEffectManager.Instance.ShowSceneEffect(this.transform.position, -1, buffInst.Def.EffectId, UnitEntity.Id);
                if(effectCtx != null)
                {
                    _bindingEffectsCtxId[buffInst.InstanceId] = effectCtx.UniqId;
                }
            }
            
        }
        protected virtual void OnEventBuffUnregister(long buffInstId)
        {
            if (_bindingEffectsCtxId.TryGetValue(buffInstId, out var ctxId))
            {
                MapSceneEffectManager.Instance.ForceDestroy(ctxId);
                _bindingEffectsCtxId.Remove(buffInstId);
            }
        }


        protected virtual void OnEventConvertAttach(long entityId)
        {
            if (mainCol != null)
            {
                mainCol.enabled = false;
            }

            if(UnitEntity.IsAttaching)
            {
                SetFadeAlpha(0);
            }
            else
            {
                SetFadeAlpha(1);
            }
        }

        protected virtual void OnEventGhostChange(long entityId)
        {
            if(UnitEntity.CheckHasState(AttrIdConsts.Ghost))
            {
                //mainCol.enabled = false;
            }
            else
            { 
                mainCol.enabled = true;
                Vector3 diff = UnityEngine.Random.insideUnitCircle * 0.05f;
                this.transform.position = this.transform.position + diff;
            }
        }

        protected virtual void OnEventInvisibleChange(long entityId)
        {
            if (UnitEntity.CheckHasState(AttrIdConsts.Invisible))
            {
                if(UnitEntity.FactionId == EFactionId.Player)
                {
                    SetFadeAlpha(0.5f);
                }
                else
                {
                    SetFadeAlpha(0);
                }
            }
            else
            {
                SetFadeAlpha(1);
            }
        }

        #endregion

        #region 受控移动



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
                if (UnitEntity.controlledMoveCtx != null)
                {
                    if(UnitEntity.controlledMoveCtx.EndOnCollideWall)
                    {
                        UnitEntity.EndControlledMove(2);
                    }
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

            // tood
            // 临时写在这里 后面统一到hitbox manage
            if(UnitEntity.controlledMoveCtx != null && UnitEntity.controlledMoveCtx.HitWindow != null)
            {
                if(UnitEntity.controlledMoveCtx.HitWindow.hitId == hitId)
                {
                    var window = UnitEntity.controlledMoveCtx.HitWindow;
                    //  todo 多次命中
                    if (!window.HitRecord.Contains(hitEntityId))
                    {
                        window.HitRecord.Add(hitEntityId);
                        //Debug.Log("OnWeaponHitCallback " + "hittttttttttttttttttttttttttttttttttttttttttttttttttttttt " + hitEntityId);

                        if (window.OnHitEffects != null)
                        {
                            var hitEntity = UnitEntity.LogicManager.AreaManager.GetLogicEntiy(hitEntityId);
                            //MainGameManager.Instance.gameLogicManager.logicEntityDict.TryGetValue(hitEntityId, out var hitEntity);
                            if (hitEntity != null && !hitEntity.MarkDestroyed)
                            {
                                var srcInfo = new EffectSourceInfo()
                                {
                                    SrcType = ESourceType.Ability,
                                    SrcEntityId = UnitEntity.Id,
                                };

                                foreach (var hitEffect in window.OnHitEffects)
                                {
                                    GameLogicManager.LogicFightEffectContext newCtx = new(UnitEntity.LogicManager, srcInfo);

                                    newCtx.TargetId = hitEntity.Id;
                                    newCtx.TriggerPos = UnitEntity.Pos;
                                    //newCtx.CastVec1 = hitEntity.Pos - EntityOwner.Pos;
                                    newCtx.CastVec1 = UnitEntity.FinalLook;

                                    UnitEntity.LogicManager.HandleLogicFightEffect(hitEffect, newCtx);
                                }

                                MainGameManager.Instance.StartHitStop(0.04f);
                            }
                            else
                            {
                                Debug.Log($"OnWeaponHitCallback hit target not found or dead {hitEntityId}");
                            }
                        }
                    }
                    return;
                }
            }
            UnitEntity.abilityController.OnUseWeaponHitCallback(hitId, hitEntityId);
        }

        /// <summary>
        /// 更新view透明度 
        /// 根据优先级和状态 决定最终显示效果
        /// </summary>
        public void UpdateViewAlpha()
        {
            if (UnitEntity.GetAttr(AttrIdConsts.HideView) > 0)
            {
                //icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0.6f);
            }
            else
            {
                //icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1);
            }
        }

        public Collider2D GetTargetCol()
        {
            return targetCol;
        }

    }
}


