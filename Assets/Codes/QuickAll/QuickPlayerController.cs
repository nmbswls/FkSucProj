using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class QuickPlayerController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator; // 2D动画机，含参数: Speed(float), DirX(float), DirY(float)
    public SpriteRenderer spriteRenderer;

    [Header("Collision")]
    public LayerMask obstacleMask; // 地形/墙体碰撞层

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 moveInput;
    private bool sprinting;
    private Vector2 velocity;

    [Header("Push (reaction)")]
    public float selfRadius = 0.5f;
    public float queryRadius = 1.2f;
    public int maxNeighbors = 12;


    [Header("Smoothing")]
    public float blendDelta = 0.6f;       // 候选位移到最终位移的插值比例
    public float velFilter = 0.5f;        // 最终速度低通滤波
    public float pushPriority = 1f;       // 优先级(越大越优先推动别人，自己少动)
    public bool unpushable = false;       // 霸体：几乎不被推（仍可推别人）

    // 分离控制（基于目标分离速度）
    public float kp = 10f;                // 穿透 -> 目标分离速度 比例
    public float kd = 5f;                 // 相对法向速度阻尼
    public float maxSepSpeedPerPair = 6f; // 单对最大分离速度
    public float maxImpulsePerPair = 3.5f;// 单对最大冲量(速度增量)
    public float maxTotalImpulse = 6f;    // 单帧累计最大冲量


    // Dash 计时与冷却
    private float dashTimeLeft = 0f;
    private float dashCooldownLeft = 0f;
    private bool dashIFrameActive = false;
    private float dashIFrameLeft = 0f;


    public float separationK = 14f;        // 分离弹簧系数 k
    public float separationC = 3f;         // 相对速度阻尼 c
    public float maxCorrectionPerStep = 0.5f;  // 单步最大修正系数，防止过度修正

    // Knockback 计时
    private float knockbackTimeLeft = 0f;

    // 推挤缓存
    private readonly Collider2D[] neighborBuffer = new Collider2D[32];


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    //public void OnMove(InputAction.CallbackContext ctx)
    //{
    //    moveInput = ctx.ReadValue<Vector2>();
    //    moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    //}
    
    //public void OnDash(InputAction.CallbackContext ctx)
    //{
    //    if(ctx.performed)
    //    {
    //        TryDash();
    //    }
    //}


    //public void OnInteract(InputAction.CallbackContext ctx)
    //{
    //    if (ctx.performed)
    //    {
    //        // TODO: 交互检测（射线/触发器）
    //    }
    //}

    //private Vector2 dir4;
    //private float speed = 4f;

    //private void UpdateDashInfo()
    //{
    //    // 冷却/时长计时
    //    float dt = Time.deltaTime;
    //    if (dashCooldownLeft > 0f) dashCooldownLeft -= dt;
    //    if (dashIFrameActive)
    //    {
    //        dashIFrameLeft -= dt;
    //        if (dashIFrameLeft <= 0f) dashIFrameActive = false;
    //    }
    //}

    //private void Update()
    //{
    //    float dt = Time.deltaTime;

    //    UpdateDashInfo();

    //    switch (controlState)
    //    {
    //        case ControlState.Dash:
    //            dashTimeLeft -= dt;
    //            if (dashTimeLeft <= 0f) EndDash();
    //            break;
    //        case ControlState.Knockback:
    //            knockbackTimeLeft -= dt;
    //            // 由速度和时间共同决定结束（速度足够低或时间到）
    //            if (knockbackTimeLeft <= 0f || externalVel.magnitude < knockbackMinEndSpeed)
    //                ExitKnockback();
    //            break;
    //    }


    //    //// 面向方向：当有输入时更新
    //    //if (moveInput.sqrMagnitude > 0.0001f)
    //    //{
    //    //    facing = moveInput.normalized;
    //    //}

    //    //// 动画参数
    //    //float speedParam = velocity.magnitude;
    //    //animator.SetFloat("Speed", speedParam);
    //    //animator.SetFloat("DirX", facing.x);
    //    //animator.SetFloat("DirY", facing.y);

    //    //// 可选：左右翻转（若你用侧视动画）
    //    //if (spriteRenderer) spriteRenderer.flipX = facing.x < 0f;


    //    // 输入或AI转为 baseMoveVel（仅 Normal 状态下生效；Dash 可选允许转向）
    //    Vector2 desiredDir = Vector2.zero;
    //    if (!isAIControlled && MainGameManager.Instance.gameLogicManager.playerLogicEntity.LockMovementState.Count == 0)
    //    {
    //        desiredDir = moveInput.normalized;
    //    }
    //    else
    //    {
    //        desiredDir = aiMoveDir.normalized;
    //    }

    //    bool controllable = controlState == ControlState.Normal;
    //    Vector2 targetBase = controllable ? desiredDir * moveSpeed : Vector2.zero;
    //    baseMoveVel = Vector2.MoveTowards(baseMoveVel, targetBase, acceleration * dt);

    //    // 外力自然衰减（除非在Dash中保持常速）
    //    if (controlState != ControlState.Dash)
    //    {
    //        externalVel = Vector2.MoveTowards(externalVel, Vector2.zero, externalDecay * dt);
    //    }

    //    // Debug：可在 Animator 参数中使用
    //    currentVelocity = rb.velocity;

    //    if(Input.GetKeyDown(KeyCode.K))
    //    {
    //        ApplyKnockback(Vector2.left, 4,0.3f);
    //    }
    //}

    //void FixedUpdate()
    //{
    //    float dt = Time.fixedDeltaTime;

    //    Vector2 desiredVel = baseMoveVel + externalVel;
    //    desiredVel = Vector2.ClampMagnitude(desiredVel, 10f);
    //    Vector2 delta = desiredVel * dt;

    //    Vector2 startPos = rb.position;
    //    Vector2 candidatePos = startPos + delta;


    //    // 推挤：单位间分离力，写入 externalVel（仅非Dash或穿人关闭时）
    //    if (controlState != ControlState.Dash)
    //    {
    //        Vector2 totalSelfImpulse = ComputeReactiveImpulses(startPos, candidatePos, dt);
    //        // 将冲量叠加为外力速度
    //        externalVel += totalSelfImpulse;

    //        // 上限与滤波
    //        if (externalVel.magnitude > maxExternalSpeed)
    //            externalVel = externalVel.normalized * maxExternalSpeed;

    //        //candidatePos = ApplySeparationForces(startPos, candidatePos, dt);
    //    }

    //    // 基于更新后的 externalVel 重新计算位移（更贴近“速度驱动”）
    //    Vector2 newDesiredVel = baseMoveVel + externalVel;
    //    Vector2 correctedDelta = newDesiredVel * dt;

    //    // 位移插值，避免突变
    //    Vector2 finalDelta = Vector2.Lerp(delta, correctedDelta, blendDelta);
    //    rb.MovePosition(startPos + finalDelta);

    //    // 可选：速度滤波
    //    Vector2 targetVel = finalDelta / dt;
    //    rb.velocity = Vector2.Lerp(rb.velocity, targetVel, velFilter);


    //    //// 组合速度
    //    //Vector2 finalVel = baseMoveVel + externalVel;
    //    //// 限制外力的上限，防止无穷增速
    //    //if (externalVel.magnitude > maxExternalSpeed)
    //    //{
    //    //    externalVel = externalVel.normalized * maxExternalSpeed;
    //    //    finalVel = baseMoveVel + externalVel;
    //    //}

    //    //// 冲刺期间可选择忽略单位碰撞（切换层或禁用与单位的接触）
    //    //if (controlState == ControlState.Dash && dashPassThroughUnits)
    //    //{
    //    //    // 仍保持与墙体碰撞：这里不改物理层，碰撞仍由 Rigidbody2D 处理
    //    //    // 如果需要彻底改层，请在项目中设置 Layer 碰撞矩阵，并在 EnterDash/EndDash 里切换 gameObject.layer
    //    //}

    //    //rb.MovePosition(candidatePos); 
    //}


    //// 返回施加在“自己”身上的总冲量（速度增量），并尽可能把反向冲量写到对方
    //private Vector2 ComputeReactiveImpulses(Vector2 startPos, Vector2 candidatePos, float dt)
    //{
    //    int count = Physics2D.OverlapCircleNonAlloc(candidatePos, queryRadius, neighborBuffer, unitLayer);
    //    float remainingBudget = maxTotalImpulse;

    //    List<(Vector3, float)> pairs = new List<(Vector3, float)>(16);

    //    for (int i = 0; i < count && remainingBudget > 0f; i++)
    //    {
    //        var other = neighborBuffer[i];
    //        if (other == null || other == col) continue;

    //        Vector2 oPos = other.transform.position;
    //        float otherR = EstimateRadius(other);
    //        float minDist = selfRadius + otherR;

    //        Vector2 toSelf = candidatePos - oPos;
    //        float dist = toSelf.magnitude;
    //        if (dist <= 0.0001f) continue;

    //        if (dist < minDist)
    //        {
    //            // 法线：从对方指向自己
    //            Vector2 n = toSelf / dist;
    //            float penetration = (minDist - dist);

    //            float penSoft = 0.4f * selfRadius;
    //            float penEff = penetration / (1f + penetration / Mathf.Max(1e-4f, penSoft)); // 软饱和
    //            // 相对法向速度
    //            Vector2 vSelf = (candidatePos - startPos) / dt;
    //            Vector2 vOther = GetBodyVel(other);
    //            float vRelN = Vector2.Dot(vSelf - vOther, n);

    //            // 目标分离速度（上限）
    //            float vSep = kp * penEff - kd * vRelN;
    //            vSep = Mathf.Clamp(vSep, 0f, maxSepSpeedPerPair);

    //            // 本对冲量（速度增量）
    //            float pairImpulseMag = Mathf.Min(vSep, maxImpulsePerPair);
    //            pairs.Add((n, pairImpulseMag));



    //            //Vector2 pairImpulse = n * pairImpulseMag;

    //            //// 质量与优先级分配
    //            //float selfShare, otherShare;
    //            //GetImpulseShares(other, out selfShare, out otherShare);

    //            //Vector2 selfImp = pairImpulse * selfShare;
    //            //Vector2 otherImp = -pairImpulse * otherShare;

    //            //selfImpulseSum += selfImp;
    //            //remainingBudget -= pairImpulseMag;

    //            //// 尝试把反作用力施加给对方
    //            //var otherCtrl = other.GetComponent<ReactivePushController>();
    //            //if (otherCtrl != null)
    //            //{
    //            //    otherCtrl.ApplyExternalImpulse(otherImp);
    //            //}
    //        }
    //    }


    //    // 2) 统计并计算缩放
    //    float sumMag = 0f;
    //    for (int i = 0; i < pairs.Count; i++) sumMag += pairs[i].Item2;

    //    // 基于预算的全局缩放
    //    float scaleBudget = (sumMag > 1e-4f) ? Mathf.Min(1f, maxTotalImpulse / sumMag) : 1f;

    //    // 基于邻居数量的附加衰减（避免高密度叠加）
    //    float scaleNeighbors = 1f;
    //    if (pairs.Count > 1)
    //    {
    //        // 每多一个邻居，乘以 perNeighborAttenuation
    //        int extra = pairs.Count - 1;
    //        scaleNeighbors = Mathf.Pow(0.5f, extra);
    //    }

    //    float scale = scaleBudget * scaleNeighbors;

    //    // 3) 施加缩放后的作用-反作用冲量
    //    Vector2 selfImpulseSum = Vector2.zero;

    //    for (int i = 0; i < pairs.Count; i++)
    //    {
    //        var p = pairs[i];
    //        float mag = p.Item2 * scale;
    //        if (mag <= 1e-6f) continue;

    //        Vector2 pairImpulse = p.Item1 * mag;

    //        float selfShare, otherShare;
    //        GetImpulseShares(null, out selfShare, out otherShare);

    //        Vector2 selfImp = pairImpulse * selfShare;
    //        Vector2 otherImp = -pairImpulse * otherShare;

    //        selfImpulseSum += selfImp;
            
    //    }

    //    // 4) 最终安全限幅（极端保护）
    //    float m = selfImpulseSum.magnitude;
    //    if (m > maxTotalImpulse)
    //        selfImpulseSum = selfImpulseSum.normalized * maxTotalImpulse;

    //    return selfImpulseSum;
    //}


    //// 根据质量与优先级决定冲量分配比例
    //private void GetImpulseShares(Collider2D other, out float selfShare, out float otherShare)
    //{
    //    //float mSelf = Mathf.Max(0.001f, effectiveMass);
    //    float mSelf = 0.001f;
    //    float pSelf = Mathf.Max(0.001f, pushPriority);

    //    float mOther = 1f;
    //    float pOther = 1f;
    //    bool otherUnpushable = false;

    //    // 对方没有脚本，视为较重较硬物体（但仍不与其碰撞）
    //    mOther = 2f;
    //    pOther = 1.5f;

    //    // 计算“抵抗系数” r = mass * priority，r 越大越不动
    //    float rSelf = mSelf * pSelf;
    //    float rOther = mOther * pOther;

    //    // 分配份额：谁更“重/优先”，谁承担更少
    //    float denom = rSelf + rOther;
    //    if (denom < 1e-5f) { selfShare = 0.5f; otherShare = 0.5f; return; }

    //    // 正常分配（对自己施加的正向冲量比例）
    //    selfShare = rOther / denom;
    //    otherShare = rSelf / denom;

    //    // 霸体修正
    //    //if (unpushable) selfShare = Mathf.Min(selfShare, unpushableShare);
    //    if (otherUnpushable) otherShare = Mathf.Min(otherShare, 0.1f);

    //    // 归一，防止极端
    //    float sum = selfShare + otherShare;
    //    if (sum > 1e-5f) { selfShare /= sum; otherShare /= sum; }
    //}



    ////private Vector2 ApplySeparationForces(Vector2 startPos, Vector2 candidatePos, float dt)
    ////{
    ////    int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationQueryRadius, neighborBuffer, unitLayer);
    ////    int used = 0;

    ////    Vector2 corrected = candidatePos;

    ////    // 单次迭代：叠加所有邻居的修正向量
    ////    Vector2 totalCorrection = Vector2.zero;
    ////    for (int i = 0; i < count && used < separationMaxNeighbors; i++)
    ////    {
    ////        var other = neighborBuffer[i];
    ////        if (other == null || other == col) continue;

    ////        Vector2 oPos = other.transform.position;
    ////        float otherR = EstimateRadius(other);
    ////        float minDist = selfRadius + otherR;

    ////        Vector2 toSelf = (Vector2)corrected - oPos;
    ////        float dist = toSelf.magnitude;
    ////        if (dist < 0.0001f) continue;

    ////        if (dist < minDist)
    ////        {
    ////            float penetration = (minDist - dist);
    ////            Vector2 n = toSelf / dist; // 修正法线：从邻居指向自己

    ////            // 相对速度阻尼（避免抖动）
    ////            Vector2 relVel = (candidatePos - startPos) / dt - GetBodyVel(other);
    ////            float vN = Vector2.Dot(relVel, n);

    ////            // 软约束修正量：k*pen - c*vN
    ////            float corrMag = separationK * penetration - separationC * vN;
    ////            corrMag = Mathf.Max(0f, corrMag);
    ////            totalCorrection += n * corrMag * dt;
    ////            used++;
    ////        }
    ////    }

    ////    // 限制最大修正，避免一次推太远
    ////    if (totalCorrection.magnitude > maxCorrectionPerStep)
    ////        totalCorrection = totalCorrection.normalized * maxCorrectionPerStep;

    ////    corrected += totalCorrection;

    ////    return corrected;
    ////}

    //private float EstimateRadius(Collider2D c)
    //{
    //    if (c is CircleCollider2D cc) return cc.radius * Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.y);
    //    if (c is CapsuleCollider2D cap)
    //    {
    //        var size = cap.size;
    //        float r = Mathf.Max(size.x, size.y) * 0.5f;
    //        return r * Mathf.Max(cap.transform.lossyScale.x, cap.transform.lossyScale.y);
    //    }
    //    if (c is BoxCollider2D bc)
    //    {
    //        var size = bc.size;
    //        float r = Mathf.Max(size.x, size.y) * 0.5f;
    //        return r * Mathf.Max(bc.transform.lossyScale.x, bc.transform.lossyScale.y);
    //    }
    //    return separationRadius; // fallback
    //}

    //private Vector2 GetBodyVel(Collider2D c)
    //{
    //    var rb2 = c.attachedRigidbody;
    //    return rb2 ? rb2.velocity : Vector2.zero;
    //}

    //public bool TryDash()
    //{
    //    if (dashCooldownLeft > 0f) return false;
    //    if (controlState == ControlState.Stunned) return false;

    //    Vector2 dir = Vector2.one;
    //    if (moveInput.magnitude < 0.01f)
    //    {
    //        dir = facing;
    //    }
    //    else
    //    {
    //        dir = moveInput;
    //    }

    //    //Vector2 dashVec = dir * 
    //    //if (dir.sqrMagnitude < 0.0001f)
    //    //    dir = baseMoveVel.sqrMagnitude > 0.001f ? baseMoveVel.normalized : Vector2.right;

    //    EnterDash(dir.normalized);
    //    return true;
    //}


    //// 示例：攻击命中或撞墙事件可调用
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    // 墙体撞击：在击退或冲刺中撞墙的处理
    //    int layer = collision.collider.gameObject.layer;
    //    bool isWall = (wallsLayer.value & (1 << layer)) != 0;

    //    if (isWall)
    //    {
    //        Vector2 normal = collision.contacts[0].normal;
    //        if (controlState == ControlState.Knockback)
    //        {
    //            // 撞墙后结束击退并可转眩晕
    //            externalVel = Vector2.zero;
    //            //ApplyStun(defaultStunDuration * 0.6f);
    //        }
    //        else if (controlState == ControlState.Dash)
    //        {
    //            // Dash撞墙立即结束
    //            EndDash();
    //        }
    //    }
    //}

    //// 公开接口：触发击退
    //public void ApplyKnockback(Vector2 dir, float initialSpeed, float duration)
    //{
    //    if (controlState == ControlState.Dash) return; // 冲刺霸体可忽略，按设计可改
    //    controlState = ControlState.Knockback;
    //    externalVel = dir.normalized * Mathf.Max(initialSpeed, 0f);
    //    knockbackTimeLeft = Mathf.Max(duration, 0f);
    //}



    //#region movement controller

    //[Header("Base Move")]
    //public float moveSpeed = 5f;              // 基础行走速度
    //public float acceleration = 20f;          // 输入加速度（平滑）
    //public bool isAIControlled = false;       // 玩家或AI
    //public Vector2 aiMoveDir;                 // AI提供的方向（单位向量）

    //[Header("External Effects")]
    //public float maxExternalSpeed = 10f;      // 外力叠加速度上限
    //public float externalDecay = 6f;          // 外力自然衰减（每秒）
    //public bool enableUnitSeparation = true;  // 是否启用单位间推挤
    //public float separationRadius = 0.5f;     // 自身碰撞半径（与CircleCollider2D一致或略大）
    //public float separationStrength = 12f;    // 推挤力度系数k
    //public float separationDamping = 3f;      // 推挤阻尼c
    //public LayerMask unitLayer;               // 可被推挤的单位层


    //public int separationMaxNeighbors = 8;    // 推挤计算的最大邻居数
    //public float separationQueryRadius = 1.2f;// 推挤采样半径

    //[Header("Dash")]
    //public float dashSpeed = 12f;
    //public float dashDuration = 0.2f;
    //public float dashCooldown = 0.6f;
    //public bool dashPassThroughUnits = true;  // 冲刺是否穿过单位
    //public LayerMask wallsLayer;              // 墙体层（冲刺仍需碰撞）
    //public bool dashInvulnerable = true;      // 冲刺无敌帧
    //public float dashIFrameTime = 0.15f;

    //[Header("Knockback")]
    //public float knockbackDrag = 4f;          // 击退速度指数衰减
    //public float knockbackMinEndSpeed = 0.4f; // 低于此速度结束击退

    //[Header("Debug")]
    //public Vector2 currentVelocity;
    //public Vector2 baseMoveVel;
    //public Vector2 externalVel;


    //// 状态机（受控层）
    //public enum ControlState { Normal, Knockback, Dash, Stunned }
    //public ControlState controlState = ControlState.Normal;

    //private Vector2 facing = Vector2.down; // 初始面向下


    //#endregion

    //private void EnterDash(Vector2 dir)
    //{
    //    controlState = ControlState.Dash;
    //    dashTimeLeft = dashDuration;
    //    externalVel = dir * dashSpeed;
    //    dashCooldownLeft = dashCooldown;

    //    if (dashInvulnerable)
    //    {
    //        dashIFrameActive = true;
    //        dashIFrameLeft = dashIFrameTime;
    //    }

    //    // 如需穿人：可在此切换 layer 到一个与“单位层”不碰撞的层
    //    // int oldLayer = gameObject.layer; // 需要缓存以便恢复
    //    // gameObject.layer = LayerMask.NameToLayer("DashLayer");
    //}


    //private void EndDash()
    //{
    //    controlState = ControlState.Normal;
    //    // 平滑收尾：保留少量速度并快速衰减
    //    externalVel *= 0.1f;
    //    dashIFrameActive = false;

    //    // 恢复层
    //    // gameObject.layer = previousLayer;
    //}

    //private void ExitKnockback()
    //{
    //    controlState = ControlState.Normal;
    //    externalVel = Vector2.zero;
    //}
}
