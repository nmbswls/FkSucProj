//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.InputSystem;

//[RequireComponent(typeof(Rigidbody2D))]
//public class QuickNpcController : MonoBehaviour
//{

//    #region movement controller

//    [Header("Base Move")]
//    public float moveSpeed = 5f;              // 基础行走速度
//    public float acceleration = 20f;          // 输入加速度（平滑）
//    public bool isAIControlled = false;       // 玩家或AI

//    [Header("External Effects")]
//    public float maxExternalSpeed = 10f;      // 外力叠加速度上限
//    public float externalDecay = 6f;          // 外力自然衰减（每秒）


//    [Header("Knockback")]
//    public float knockbackMinEndSpeed = 0.4f; // 低于此速度结束击退

//    [Header("Debug")]
//    public Vector2 currentVelocity;
//    public Vector2 baseMoveVel;
//    public Vector2 externalVel;
//    public LayerMask wallsLayer;

//    // 状态机（受控层）
//    public enum ControlState { Normal, Knockback, Dash, Stunned }
//    public ControlState controlState = ControlState.Normal;

//    private Vector2 facing = Vector2.down; // 初始面向下

//    #endregion


//    [Header("Animation")]
//    public Animator animator; // 2D动画机，含参数: Speed(float), DirX(float), DirY(float)
//    public SpriteRenderer spriteRenderer;


//    private Rigidbody2D rb;
//    private Collider2D col;

//    private Vector2 moveInput;
//    public Vector2 aiMoveDir;                 // AI提供的方向（单位向量）

//    // Knockback 计时
//    private float knockbackTimeLeft = 0f;

//    // 推挤缓存
//    private readonly Collider2D[] neighborBuffer = new Collider2D[32];



//    private void Awake()
//    {
//        rb = GetComponent<Rigidbody2D>();
//        col = GetComponent<Collider2D>();

//        rb.gravityScale = 0f;
//        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
//        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
//        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
//        rb.freezeRotation = true;
//    }

//    public void OnMove(Vector2 vec)
//    {
//        //moveInput = ctx.ReadValue<Vector2>();
//        //moveInput = Vector2.ClampMagnitude(moveInput, 1f);
//    }


//    private void Update()
//    {
//        float dt = Time.deltaTime;

//        switch (controlState)
//        {
//            case ControlState.Knockback:
//                knockbackTimeLeft -= dt;
//                // 由速度和时间共同决定结束（速度足够低或时间到）
//                if (knockbackTimeLeft <= 0f || externalVel.magnitude < knockbackMinEndSpeed)
//                    ExitKnockback();
//                break;
//        }

//        // 输入或AI转为 baseMoveVel（仅 Normal 状态下生效；Dash 可选允许转向）
//        Vector2 desiredDir;
//        if (!isAIControlled)
//        {
//            desiredDir = moveInput.normalized;
//        }
//        else
//        {
//            desiredDir = aiMoveDir.normalized;
//        }

//        bool controllable = controlState == ControlState.Normal;
//        Vector2 targetBase = controllable ? desiredDir * moveSpeed : Vector2.zero;
//        baseMoveVel = Vector2.MoveTowards(baseMoveVel, targetBase, acceleration * dt);

//        // 外力自然衰减（除非在Dash中保持常速）
//        if (controlState != ControlState.Dash)
//        {
//            externalVel = Vector2.MoveTowards(externalVel, Vector2.zero, externalDecay * dt);
//        }

//        currentVelocity = rb.velocity;
//    }

//    void FixedUpdate()
//    {
//        float dt = Time.fixedDeltaTime;

//        Vector2 desiredVel = baseMoveVel + externalVel;
//        desiredVel = Vector2.ClampMagnitude(desiredVel, 10f);
//        Vector2 delta = desiredVel * dt;

//        Vector2 startPos = rb.position;
//        Vector2 candidatePos = startPos + delta;


//        // 基于更新后的 externalVel 重新计算位移（更贴近“速度驱动”）
//        Vector2 newDesiredVel = baseMoveVel + externalVel;
//        Vector2 correctedDelta = newDesiredVel * dt;

//        // 位移插值，避免突变
//        Vector2 finalDelta = correctedDelta;
//        rb.MovePosition(startPos + finalDelta);

//        // 可选：速度滤波
//        Vector2 targetVel = finalDelta / dt;
//        rb.velocity = targetVel;
//    }


//    // 示例：攻击命中或撞墙事件可调用
//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        // 墙体撞击：在击退或冲刺中撞墙的处理
//        int layer = collision.collider.gameObject.layer;
//        bool isWall = (wallsLayer.value & (1 << layer)) != 0;

//        if (isWall)
//        {
//            Vector2 normal = collision.contacts[0].normal;
//            if (controlState == ControlState.Knockback)
//            {
//                // 撞墙后结束击退并可转眩晕
//                externalVel = Vector2.zero;
//                //ApplyStun(defaultStunDuration * 0.6f);
//            }
//            else if (controlState == ControlState.Dash)
//            {
//                // Dash撞墙立即结束
//            }
//        }
//    }

//    // 公开接口：触发击退
//    public void ApplyKnockback(Vector2 dir, float initialSpeed, float duration)
//    {
//        if (controlState == ControlState.Dash) return; // 冲刺霸体可忽略，按设计可改
//        controlState = ControlState.Knockback;
//        externalVel = dir.normalized * Mathf.Max(initialSpeed, 0f);
//        knockbackTimeLeft = Mathf.Max(duration, 0f);
//    }

//    private void ExitKnockback()
//    {
//        controlState = ControlState.Normal;
//        externalVel = Vector2.zero;
//    }
//}
