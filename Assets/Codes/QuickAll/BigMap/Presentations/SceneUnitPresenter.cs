using DG.Tweening;
using Map.Entity;
using Map.Entity.Attr;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using static QuickNpcController;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;


public interface IMapWeaponHolder
{
    void OnWeaponHitCallback(long hitId, long hitEntityId);
}

/// <summary>
/// 场景单位 基类
/// </summary>
public abstract class SceneUnitPresenter : ScenePresentationBase<BaseUnitLogicEntity>, IMapWeaponHolder
{
    

    [SerializeField] protected SpriteRenderer icon;
    [SerializeField] protected GameObject highlightFx;

    [SerializeField] protected GameObject faceIndicator;

    public Transform WeaponRoot;
    public MapUnitWeaponCtrl WeaponCtrl; // 武器控制器

    // 控制移动组件
    public NavMeshAgent navAgent;
    public Rigidbody2D rb;
    public Collider2D mainCol;

    public Vector2 freeMoveDir;
    private float acceleration = 20.0f;
    private float externalDecay = 30f;          // 外力自然衰减（每秒）


    public BaseUnitLogicEntity UnitEntity { get
        {
            return (BaseUnitLogicEntity)_logic;
        } }


    protected override void Awake()
    {
        if(!rb)
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

    public override void Tick(float dt)
    {
        // 同步位置
        if(UnitEntity != null)
        {
            UnitEntity.SetPosition(MainGameManager.Instance.GetLogicPosFromWorldPos(transform.position));
        }

        UpdateTargettedMoveState();

        
        if(UnitEntity.GetAttr(AttrIdConsts.Unmovable) > 0)
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
            if(WeaponRoot != null)
            {
                float angle = Mathf.Atan2(UnitEntity.FaceDir.y, UnitEntity.FaceDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
                WeaponRoot.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
            }
        }

        // 外力自然衰减（除非在Dash中保持常速）
        if (UnitEntity.dashIntent == null)
        {
            UnitEntity.externalVel = Vector2.MoveTowards(UnitEntity.externalVel, Vector2.zero, externalDecay * dt);
        }

        //if (knockBackIntent.knockbackTimeLeft <= 0f || externalVel.magnitude < knockBackIntent.knockbackMinEndSpeed)
        //    ClearKnockbackIntent();

        if(icon != null)
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

    protected float GetCurrentMoveSpeed()
    {
        if(UnitEntity.MoveActMode == BaseUnitLogicEntity.EUnitMoveActMode.PatrolFollow)
        {
            var followEntity = UnitEntity.LogicManager.AreaManager.GetLogicEntiy(this.UnitEntity.FollowPatrolId) as PatrolGroupLogicEntity;
            if(followEntity == null)
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

        if (navAgent != null)
        {
            navAgent.nextPosition = rb.position;
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
        float maxStep = maxTurnSpeedDegPerSec * Time.deltaTime;
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
        UnitEntity.targettedMoveIntent.targettedDesireDir = Vector2.zero;
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

        if ((currPos - UnitEntity.targettedMoveIntent.MoveTarget).magnitude < UnitEntity.targettedMoveIntent.StopDistance)
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
        var v = rb.velocity;
        Vector2 startPos = rb.position;
        //Vector2 candidatePos;
        var dt = Time.fixedDeltaTime;
        Vector2 posDelta;

        Vector2 baseVel;

        FixDynamicBlock();

        //// 当前目标速度
        //if (UnitEntity.targettedMoveIntent != null && UnitEntity.targettedMoveIntent.targettedDesireVec != null)
        //{
        //    // 目标速度
        //    baseVel = UnitEntity.targettedMoveIntent.targettedDesireVec;
        //}
        //else
        //{
        //    baseVel = UnitEntity.baseMoveVel;
        //}

        //Vector2 desiredVel = baseVel + UnitEntity.externalVel;
        //desiredVel = Vector2.ClampMagnitude(desiredVel, 10f);
        //posDelta = desiredVel * dt;

        //var candidatePos = startPos + posDelta;

        //// 推挤：单位间分离力，写入 externalVel（仅非Dash或穿人关闭时）
        //if (UnitEntity.dashIntent == null)
        //{
        //    Vector2 totalSelfImpulse = ComputeReactiveImpulses(startPos, candidatePos, dt);
        //    // 将冲量叠加为外力速度
        //    UnitEntity.externalVel += totalSelfImpulse;

        //    // 上限与滤波
        //    if (UnitEntity.externalVel.magnitude > maxExternalSpeed)
        //        UnitEntity.externalVel = UnitEntity.externalVel.normalized * maxExternalSpeed;

        //    //candidatePos = ApplySeparationForces(startPos, candidatePos, dt);
        //}

        //// 基于更新后的 externalVel 重新计算位移（更贴近“速度驱动”）
        //Vector2 newDesiredVel = UnitEntity.baseMoveVel + UnitEntity.externalVel;
        //Vector2 correctedDelta = newDesiredVel * dt;

        //// 位移插值，避免突变
        //Vector2 finalDelta = Vector2.Lerp(posDelta, correctedDelta, blendDelta);
        //rb.MovePosition(startPos + finalDelta);

        //// 可选：速度滤波
        //Vector2 targetVel = finalDelta / dt;
        //rb.velocity = targetVel;
    }

    private float skin = 0.02f;
    private RaycastHit2D[] hits = new RaycastHit2D[8];
    protected void FixDynamicBlock()
    {
        Vector2 totalVel = UnitEntity.activeMoveVec + UnitEntity.externalVel;
        Vector2 delta = totalVel * Time.fixedDeltaTime;
        if (delta == Vector2.zero) return;

        // 2) 主方向 Cast
        float allowed = delta.magnitude;
        //int count = mainCol.Cast(delta.normalized, hits, allowed, true);
        //for (int i = 0; i < count; i++)
        //{
        //    if (((1 << hits[i].collider.gameObject.layer) & (1 << LayerMask.NameToLayer("Units"))) == 0) continue;
        //    allowed = Mathf.Min(allowed, Mathf.Max(0f, hits[i].distance - skin));
        //}
        Vector2 move = delta.normalized * allowed;

        // 3) 切向滑沿（命中时）
        if (allowed + 1e-5f < delta.magnitude)
        {
            // 最近命中法线（示例取第一个；可遍历选最限制你的那一个）
            Vector2 n = hits[0].normal;
            // 切向分量
            Vector2 tangent = delta - Vector2.Dot(delta, n) * n;
            if (tangent.sqrMagnitude > 1e-6f)
            {
                float tangentLen = tangent.magnitude;
                int tCount = mainCol.Cast(tangent.normalized, hits, tangentLen, true);
                float tAllowed = tangentLen;
                for (int i = 0; i < tCount; i++)
                {
                    if (((1 << hits[i].collider.gameObject.layer) & (1 << LayerMask.NameToLayer("Units"))) == 0) continue;
                    tAllowed = Mathf.Min(tAllowed, Mathf.Max(0f, hits[i].distance - skin));
                }
                move += tangent.normalized * tAllowed;
            }
        }

        // 4) 应用位移
        rb.MovePosition(rb.position + move);
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

    // 返回施加在“自己”身上的总冲量（速度增量），并尽可能把反向冲量写到对方
    private Vector2 ComputeReactiveImpulses(Vector2 startPos, Vector2 candidatePos, float dt)
    {
        int count = Physics2D.OverlapCircleNonAlloc(candidatePos, queryRadius, neighborBuffer, 1 << LayerMask.NameToLayer("Units"));
        float remainingBudget = maxTotalImpulse;

        List<(Vector3, float)> pairs = new List<(Vector3, float)>(16);

        for (int i = 0; i < count && remainingBudget > 0f; i++)
        {
            var other = neighborBuffer[i];
            if (other == null || other == mainCol) continue;

            Vector2 oPos = other.transform.position;
            float otherR = EstimateRadius(other);
            float minDist = selfRadius + otherR;

            Vector2 toSelf = candidatePos - oPos;
            float dist = toSelf.magnitude;
            if (dist <= 0.0001f) continue;

            if (dist < minDist)
            {
                // 法线：从对方指向自己
                Vector2 n = toSelf / dist;
                float penetration = (minDist - dist);

                float penSoft = 0.4f * selfRadius;
                float penEff = penetration / (1f + penetration / Mathf.Max(1e-4f, penSoft)); // 软饱和
                // 相对法向速度
                Vector2 vSelf = (candidatePos - startPos) / dt;
                Vector2 vOther = GetBodyVel(other);
                float vRelN = Vector2.Dot(vSelf - vOther, n);

                // 目标分离速度（上限）
                float vSep = kp * penEff - kd * vRelN;
                vSep = Mathf.Clamp(vSep, 0f, maxSepSpeedPerPair);

                // 本对冲量（速度增量）
                float pairImpulseMag = Mathf.Min(vSep, maxImpulsePerPair);
                pairs.Add((n, pairImpulseMag));
            }
        }

        // 2) 统计并计算缩放
        float sumMag = 0f;
        for (int i = 0; i < pairs.Count; i++) sumMag += pairs[i].Item2;

        // 基于预算的全局缩放
        float scaleBudget = (sumMag > 1e-4f) ? Mathf.Min(1f, maxTotalImpulse / sumMag) : 1f;

        // 基于邻居数量的附加衰减（避免高密度叠加）
        float scaleNeighbors = 1f;
        if (pairs.Count > 1)
        {
            // 每多一个邻居，乘以 perNeighborAttenuation
            int extra = pairs.Count - 1;
            scaleNeighbors = Mathf.Pow(0.5f, extra);
        }

        float scale = scaleBudget * scaleNeighbors;

        // 3) 施加缩放后的作用-反作用冲量
        Vector2 selfImpulseSum = Vector2.zero;

        for (int i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i];
            float mag = p.Item2 * scale;
            if (mag <= 1e-6f) continue;

            Vector2 pairImpulse = p.Item1 * mag;

            float selfShare, otherShare;
            GetImpulseShares(null, out selfShare, out otherShare);

            Vector2 selfImp = pairImpulse * selfShare;
            Vector2 otherImp = -pairImpulse * otherShare;

            selfImpulseSum += selfImp;

        }

        // 4) 最终安全限幅（极端保护）
        float m = selfImpulseSum.magnitude;
        if (m > maxTotalImpulse)
            selfImpulseSum = selfImpulseSum.normalized * maxTotalImpulse;

        return selfImpulseSum;
    }

    private Vector2 GetBodyVel(Collider2D c)
    {
        var rb2 = c.attachedRigidbody;
        return rb2 ? rb2.velocity : Vector2.zero;
    }

    private float EstimateRadius(Collider2D c)
    {
        if (c is CircleCollider2D cc) return cc.radius * Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.y);
        if (c is CapsuleCollider2D cap)
        {
            var size = cap.size;
            float r = Mathf.Max(size.x, size.y) * 0.5f;
            return r * Mathf.Max(cap.transform.lossyScale.x, cap.transform.lossyScale.y);
        }
        if (c is BoxCollider2D bc)
        {
            var size = bc.size;
            float r = Mathf.Max(size.x, size.y) * 0.5f;
            return r * Mathf.Max(bc.transform.lossyScale.x, bc.transform.lossyScale.y);
        }
        return separationRadius; // fallback
    }

    // 根据质量与优先级决定冲量分配比例
    private void GetImpulseShares(Collider2D other, out float selfShare, out float otherShare)
    {
        //float mSelf = Mathf.Max(0.001f, effectiveMass);
        float mSelf = 0.001f;
        float pSelf = Mathf.Max(0.001f, pushPriority);

        float mOther = 1f;
        float pOther = 1f;
        bool otherUnpushable = false;

        // 对方没有脚本，视为较重较硬物体（但仍不与其碰撞）
        mOther = 2f;
        pOther = 1.5f;

        // 计算“抵抗系数” r = mass * priority，r 越大越不动
        float rSelf = mSelf * pSelf;
        float rOther = mOther * pOther;

        // 分配份额：谁更“重/优先”，谁承担更少
        float denom = rSelf + rOther;
        if (denom < 1e-5f) { selfShare = 0.5f; otherShare = 0.5f; return; }

        // 正常分配（对自己施加的正向冲量比例）
        selfShare = rOther / denom;
        otherShare = rSelf / denom;

        // 霸体修正
        //if (unpushable) selfShare = Mathf.Min(selfShare, unpushableShare);
        if (otherUnpushable) otherShare = Mathf.Min(otherShare, 0.1f);

        // 归一，防止极端
        float sum = selfShare + otherShare;
        if (sum > 1e-5f) { selfShare /= sum; otherShare /= sum; }
    }


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
        if(UnitEntity.GetAttr(AttrIdConsts.HidingMask) > 0)
        {
            icon.color = new Color(1, 1, 1, 0.6f);
        }
        else
        {
            icon.color = new Color(1, 1, 1, 1);
        }
    }
}
