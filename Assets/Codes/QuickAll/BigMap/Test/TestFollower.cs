using System.Collections;
using System.Collections.Generic;
using Map.Entity;
using Map.Scene;
using My.Map.Scene;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.PlayerSettings;

public class TestFollower : MonoBehaviour
{
    public class TargettedMoveIntent
    {
        public Vector2 MoveTarget;
        public float lastUpdateNavTime;
        public bool NeedRecalculatePath;
        public Vector2 targettedDesireDir;
        public float StopDistance = 1f; // 停止距离
    }

    public TargettedMoveIntent? targettedMoveIntent;

    public NavMeshAgent navAgent;
    public Transform Followed;

    public Rigidbody2D rb;

    public Vector2 offSet;

    public SimpleCharacterController CharacterController;
    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();

        navAgent.updatePosition = false;
        navAgent.updateRotation = false;

        CharacterController = GetComponent<SimpleCharacterController>();
    }

    private void Update()
    {
        navAgent.nextPosition = transform.position;
        Vector3 dv3 = navAgent.desiredVelocity;
        Vector3 dir3 = dv3.sqrMagnitude > 1e-6f ? dv3.normalized : Vector3.zero;

        UpdateNav();

        UpdateTargettedMoveState();

        //if(targettedMoveIntent == null)
        //{
        //    CharacterController.DesiredVel = Vector2.zero;
        //}
        //else
        //{
        //    CharacterController.DesiredVel = targettedMoveIntent.targettedDesireDir * 0.2f;
        //}
    }

    private Vector2? _lastPos = null;
    private float _lastNavTimer = 0;
    public void UpdateNav()
    {
        _lastNavTimer -= Time.deltaTime;
        if (_lastNavTimer > 0)
        {
            return;
        }

        _lastNavTimer = 0.2f;
        if(targettedMoveIntent != null)
        {
            Vector2 vec = Followed.position + new Vector3(offSet.x, offSet.y, 0);
            if ((targettedMoveIntent.MoveTarget - vec).magnitude < 0.2f)
            {
                return;
            }
        }
        var followPos = Followed.position + new Vector3(offSet.x, offSet.y, 0);

        StartTargettedMove(followPos, 0.3f);
    }

    public void StartTargettedMove(Vector2 moveToPos, float stopDistance)
    {
        if (targettedMoveIntent != null && moveToPos == targettedMoveIntent.MoveTarget)
        {
            return;
        }

        if (targettedMoveIntent == null)
        {
            targettedMoveIntent = new();
        }

        targettedMoveIntent.StopDistance = stopDistance;
        targettedMoveIntent.MoveTarget = moveToPos;
        targettedMoveIntent.NeedRecalculatePath = true;
    }

    public void UpdateTargettedMoveState()
    {
        if (targettedMoveIntent == null)
        {
            return;
        }
        
        // 重算路径
        if (targettedMoveIntent.NeedRecalculatePath)
        {
            navAgent.SetDestination(targettedMoveIntent.MoveTarget);
            targettedMoveIntent.NeedRecalculatePath = false;
        }

        // pending中 等待寻找
        if (!navAgent.hasPath || navAgent.pathPending)
        {
            return;
        }

        targettedMoveIntent.targettedDesireDir = Vector2.zero;

        Vector2 currPos = transform.position;

        if ((currPos - targettedMoveIntent.MoveTarget).magnitude < 1e-2)
        {
            return;
        }

        // 从Agent获取期望速度，投影到XY
        Vector3 desired3 = navAgent.desiredVelocity;
        Vector2 desired = new Vector2(desired3.x, desired3.y);
        desired = desired.normalized;
        targettedMoveIntent.targettedDesireDir = desired;


    }

    private void FixedUpdate()
    {
        //if (targettedMoveIntent == null) return;

        //// 你的移动速度（单位：米/秒）
        //float moveSpeed = 0.2f;
        //float dt = Time.fixedDeltaTime;

        //// 当前位置与目标位置
        //Vector2 pos = rb.position;
        //Vector2 target = targettedMoveIntent.MoveTarget; // 需要有目标点
        //Vector2 dir = targettedMoveIntent.targettedDesireDir; // 已归一化的方向向量（长度≈1）

        //// 距离与本帧位移
        //float dist = Vector2.Distance(pos, target);
        //float step = moveSpeed * dt;

        //// 停止/到达判定阈值，避免抖动
        //float arriveThreshold = 0.02f; // 可根据角色体型/速度调整

        //if (dist <= arriveThreshold)
        //{
        //    // 到达目标：停止或切状态
        //    // 可将速度清零、发事件、更新意图等
        //    // rb.MovePosition(target); // 可直接贴到目标点（谨慎使用，避免穿透）
        //    return;
        //}

        //// 裁剪步长：不要超过剩余距离
        //float clampedStep = Mathf.Min(step, dist);

        //// 也可以用指向目标的真实方向，避免旧方向与目标不一致
        //Vector2 realDir = (dist > 1e-6f) ? (target - pos).normalized : dir;

        //rb.MovePosition(pos + realDir * clampedStep);
    }
}
