using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements;

public enum QuickAIState { Idle, Patrol, Chase, Attack, Search, Stunned, Dead }

public class QuickAIController : MonoBehaviour
{
    public QuickAIConfig config;
    public Transform target; // 外部赋值（玩家）
    private QuickAIBlackboard bb;


    private QuickAIMotor motor;
    private QuickAIUseSkill useSkillComp;
    private QuickAISensor sensor;
    private QuickAIState state;
    private Coroutine attackRoutine;

    private int health;

    public NavMeshAgent navAgent;


    void Awake()
    {
        motor = GetComponent<QuickAIMotor>();
        sensor = GetComponent<QuickAISensor>();
        useSkillComp = GetComponent<QuickAIUseSkill>();

        bb = new QuickAIBlackboard();
        sensor.bb = bb;


        //attack.bb = bb;

        health = config.maxHealth;

        if (!navAgent) { 
            navAgent = GetComponent<NavMeshAgent>();
        }

        if(navAgent  != null)
        {
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
        }
    }

    void Start()
    {
        bb.target = target;
        TransitionTo(config.waypoints != null && config.waypoints.Length > 0 ? QuickAIState.Patrol : QuickAIState.Idle);
    }

    void Update()
    {
        if (state == QuickAIState.Dead) return;

        // 感知更新
        bool see = sensor.CanSeeTarget();
        bool hear = sensor.CanHearTarget();

        if (see)
        {
            bb.timeSinceSeen = 0f;
            bb.lastKnownTargetPos = bb.target.position;
        }
        else
        {
            bb.timeSinceSeen += Time.deltaTime;
        }

        // 通用死亡检测（示例）
        if (health <= 0 && state != QuickAIState.Dead)
        {
            TransitionTo(QuickAIState.Dead);
            return;
        }

        // 状态驱动
        switch (state)
        {
            case QuickAIState.Idle: UpdateIdle(see, hear); break;
            case QuickAIState.Patrol: UpdatePatrol(see, hear); break;
            case QuickAIState.Chase: UpdateChase(see); break;
            case QuickAIState.Attack: UpdateAttack(see); break;
            case QuickAIState.Search: UpdateSearch(see); break;
            case QuickAIState.Stunned: UpdateStunned(); break;
        }
    }

    void TransitionTo(QuickAIState next)
    {
        OnExit(state);
        state = next;
        bb.stateTimer = 0f;
        OnEnter(state);
    }

    void OnEnter(QuickAIState s)
    {
        switch (s)
        {
            case QuickAIState.Idle:
                motor.Stop();
                break;
            case QuickAIState.Patrol:
                motor.Stop();
                break;
            case QuickAIState.Chase:
                break;
            case QuickAIState.Attack:
                motor.Stop();
                //TryStartAttack();
                break;
            case QuickAIState.Search:
                motor.Stop();
                break;
            case QuickAIState.Stunned:
                motor.Stop();
                break;
            case QuickAIState.Dead:
                OnDead();
                break;
        }
    }

    void OnExit(QuickAIState s)
    {
        if (s == QuickAIState.Attack && attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    // 状态更新实现
    void UpdateIdle(bool see, bool hear)
    {
        bb.stateTimer += Time.deltaTime;

        if (see || hear)
        {
            TransitionTo(QuickAIState.Chase);
            return;
        }

        // 简单等待后转巡逻（如有路径）
        if (config.waypoints != null && config.waypoints.Length > 0 && bb.stateTimer > 1.0f)
        {
            TransitionTo(QuickAIState.Patrol);
        }
    }

    void UpdatePatrol(bool see, bool hear)
    {
        if (see || hear)
        {
            TransitionTo(QuickAIState.Chase);
            return;
        }

        // 配置异常
        if (config.waypoints == null || config.waypoints.Length == 0)
        {
            TransitionTo(QuickAIState.Idle);
            return;
        }

        Transform wp = config.waypoints[bb.currentWaypointIndex];
        float dist = Vector2.Distance(transform.position, wp.position);

        if (dist < 0.15f)
        {
            motor.Stop();
            bb.stateTimer += Time.deltaTime;
            if (bb.stateTimer >= config.patrolWaitTime)
            {
                bb.stateTimer = 0f;
                AdvanceWaypoint();
            }
        }
        else
        {
            if(!navAgent.hasPath)
            {
                navAgent.SetDestination(wp.position);
            }
            else if(navAgent.pathPending)
            {
                return;
            }
            else
            {

                // 从Agent获取期望速度，投影到XY
                Vector3 desired3 = navAgent.desiredVelocity;
                Vector2 desired = new Vector2(desired3.x, desired3.y);

                //// 按自定义加速度逼近目标速度
                //Vector2 targetVel = Vector2.ClampMagnitude(desired, maxSpeed);
                //currentVel = Vector2.MoveTowards(currentVel, targetVel, acceleration * Time.fixedDeltaTime);
                //rb.velocity = currentVel;

                //// 面向移动方向（可选）
                //if (currentVel.sqrMagnitude > 0.0001f)
                //{
                //    float targetAngle = Mathf.Atan2(currentVel.y, currentVel.x) * Mathf.Rad2Deg - 90f; // 以角色up指向前
                //    float newAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, turnSpeedDegPerSec * Time.fixedDeltaTime);
                //    rb.MoveRotation(newAngle);
                //}

                //// 同步Agent内部位置
                //navAgent.nextPosition = transform.position;

                //var corners = agent.path.corners;
                //if (corners != null && corners.Length > 1)
                //{
                //    Vector3 nextCorner = corners[1]; // [0]是当前位置
                //    Vector3 dir = (nextCorner - transform.position);
                //    dir.z = 0f;
                //    Vector3 move = dir.normalized * moveSpeed;
                //    controller.Move(move * Time.deltaTime);
                //    agent.nextPosition = transform.position;
                //}

                motor.MoveToFixPoint(wp.position);
            }
        }
    }

    void AdvanceWaypoint()
    {
        switch (config.patrolType)
        {
            case QuickAIConfig.PatrolType.Loop:
                bb.currentWaypointIndex = (bb.currentWaypointIndex + 1) % config.waypoints.Length;
                break;
            case QuickAIConfig.PatrolType.PingPong:
                if (bb.currentWaypointIndex == 0) bb.patrolDirection = 1;
                else if (bb.currentWaypointIndex == config.waypoints.Length - 1) bb.patrolDirection = -1;
                bb.currentWaypointIndex = Mathf.Clamp(bb.currentWaypointIndex + bb.patrolDirection, 0, config.waypoints.Length - 1);
                break;
            case QuickAIConfig.PatrolType.Random:
                bb.currentWaypointIndex = Random.Range(0, config.waypoints.Length);
                break;
        }
    }

    void UpdateChase(bool see)
    {
        Transform t = bb.target;
        if (t == null)
        {
            TransitionTo(QuickAIState.Idle);
            return;
        }

        //Vector2 dest = see ? (Vector2)t.position : bb.lastKnownTargetPos;
        //motor.MoveTowards(dest, config.chaseSpeed);

        //// 进入攻击
        //if (Vector2.Distance(transform.position, t.position) <= config.attackRange && see)
        //{
        //    TransitionTo(QuickAIState.Attack);
        //    return;
        //}

        //// 追击超时转搜索
        //if (!see && bb.timeSinceSeen >= config.loseTargetTime)
        //{
        //    TransitionTo(QuickAIState.Search);
        //}
    }

    void UpdateAttack(bool see)
    {
        Transform t = bb.target;
        if (t == null)
        {
            TransitionTo(QuickAIState.Idle);
            return;
        }

        // 保持面向
        Vector2 dir = (t.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GetComponent<Rigidbody2D>().rotation = angle;

        // 超出范围或看不到 -> 追击/搜索
        float dist = Vector2.Distance(transform.position, t.position);
        if (dist > config.attackRange * 1.05f)
        {
            TransitionTo(see ? QuickAIState.Chase : QuickAIState.Search);
            return;
        }

        // 冷却结束则继续攻击
        if (!bb.inAttackCooldown && attackRoutine == null)
        {
            //TryStartAttack();
        }
    }

    //void TryStartAttack()
    //{
    //    if (bb.inAttackCooldown) return;
    //    attackRoutine = StartCoroutine(AttackFlow());
    //}

    //IEnumerator AttackFlow()
    //{
    //    yield return attack.DoAttackCoroutine();
    //    bb.inAttackCooldown = true;
    //    yield return new WaitForSeconds(config.attackCooldown);
    //    bb.inAttackCooldown = false;
    //    attackRoutine = null;
    //}

    void UpdateSearch(bool see)
    {
        if (see)
        {
            TransitionTo(QuickAIState.Chase);
            return;
        }

        bb.stateTimer += Time.deltaTime;

        // 在最后目击点附近随机点巡查
        Vector2 center = bb.lastKnownTargetPos;
        Vector2 targetPos = center + Random.insideUnitCircle * config.searchRadius;

        //motor.MoveTowards(targetPos, config.moveSpeed);

        //if (bb.stateTimer >= config.searchDuration)
        //{
        //    // 搜索失败，回到巡逻或待机
        //    TransitionTo((config.waypoints != null && config.waypoints.Length > 0) ? QuickAIState.Patrol : QuickAIState.Idle);
        //}
    }

    void UpdateStunned()
    {
        bb.stateTimer += Time.deltaTime;
        if (bb.stateTimer >= config.stunnedDuration)
        {
            TransitionTo(QuickAIState.Idle);
        }
    }

    // 示例受伤/死亡入口
    public void TakeDamage(int amount)
    {
        if (state == QuickAIState.Dead) return;
        health -= amount;
        if (health <= 0)
        {
            TransitionTo(QuickAIState.Dead);
        }
        else
        {
            TransitionTo(QuickAIState.Stunned);
        }
    }

    void OnDead()
    {
        motor.Stop();
        // 播放死亡动画/掉落/禁用碰撞
        GetComponent<Collider2D>().enabled = false;
        // 可延迟销毁或对象池回收
        Destroy(gameObject, 2f);
    }
}
