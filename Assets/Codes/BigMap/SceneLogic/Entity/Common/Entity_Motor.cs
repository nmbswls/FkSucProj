


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Entity
{

    // 逻辑层接口：导航与物理服务由Unity层实现
    public interface INavProvider
    {
        // 请求生成或更新到目标位置的路径；返回是否成功
        bool TryBuildPath(Vector3 start, Vector3 destination, out NavPath path);

        // 针对移动目标（跟随）提供下一目标点（可做预测）
        bool TryGetFollowPoint(ILogicEntity target, float predictionSeconds, Vector2 offset, out Vector3 followPoint);

        // 动态重规划：在跟随或路径受阻时调用
        bool TryReplan(Vector3 current, Vector3 goal, out NavPath path);

        // 简单连通性/直线可达测试
        bool Linecast(Vector3 from, Vector3 to, out Vector3 hitPoint);


        Vector3? GetClosestValidPos(Vector3 pos);
    }

    public struct NavPath
    {
        public Vector2[] Waypoints; // 世界坐标
        public int Length => Waypoints?.Length ?? 0;
    }

    public enum EMotorState
    {
        Free,
        Pathing,
        Following,
    }

    public enum MoveCommandKind
    {
        Free,
        MoveTo,
        Follow,
        Stop,
    }

    public class MoveIntent
    {
    }


    public class EntityMotorComp
    {
        public BaseUnitLogicEntity UnitEntity;
        public INavProvider navProvider;

        public EMotorState State { get; private set; } = EMotorState.Free;

        private NavPath _path;
        private int _pathIndex;

        /// <summary>
        /// 
        /// </summary>
        private Vector2 _currentGoal;       // Pathing用的当前子目标

        private ILogicEntity? _followTarget;
        private float _followPrediction;
        private Vector2 _followOffset;

        private float _moveSpeedRate = 1.0f;
        private float _stopDistance = 0.1f;

        // 运行时辅助
        private float _stuckTimer;
        private Vector2 _lastPosition;
        private float _replanCooldownLeft;

        public Vector2 Velocity { get; private set; }

        public Vector2 DesiredVelocity { get; private set; }
        public Quaternion DesiredRotation { get; private set; }


        
        public float ArriveTolerance = 0.1f;
        public float SwitchRadius = 0.5f;

        public bool AllowReplan = true;
        public float ReplanCooldown = 0.2f;
        public float Acceleration = 99;
        public float Deceleration = 99;

        public Vector2 FreeMoveInput = Vector2.zero;


        public EntityMotorComp(BaseUnitLogicEntity unit, INavProvider navProvider)
        {
            this.UnitEntity = unit;
            this.navProvider = navProvider;
        }

        public bool CheckIsFollowTarget(long targetId)
        {
            if (State != EMotorState.Following) return false;
            if (_followTarget == null || _followTarget.Id != targetId) return false;
            return true;
        }

        public bool CheckIsMovingTo(Vector2 targetPos)
        {
            if (State != EMotorState.Pathing) return false;
            if (_currentGoal == null || _currentGoal != targetPos) return false;
            return true;
        }

        public void TryMoveTo(Vector2 destination, float stopDistance = 0.35f, float moveSpeedRate = 1.0f)
        {

            if(State == EMotorState.Pathing)
            {
                if (Vector2.Distance(destination, _currentGoal) < ArriveTolerance)
                {
                    return;
                }
            }

            if (navProvider.TryBuildPath(UnitEntity.Pos, destination, out var newPath) && newPath.Length > 0)
            {
                int startIndex = 0;

                // 遍历新路径的前几个点
                for (int i = 0; i < newPath.Waypoints.Length; i++)
                {
                    Vector2 wp = newPath.Waypoints[i];
                    float distToWp = Vector2.Distance(UnitEntity.Pos, wp);

                    // 如果这个路点离我非常近（比如小于切角半径 SwitchRadius），
                    // 或者比 SwitchRadius 稍大一点点（防止回头跑），
                    // 就认为这个点“我已经到了”或“可以直接忽略”。
                    // 这里建议使用稍大一点的容差，比如 SwitchRadius * 1.2f
                    if (distToWp < SwitchRadius)
                    {
                        startIndex = i + 1; // 跳过这个点，直接去下一个
                    }
                    else
                    {
                        // 遇到第一个“足够远”的点，停止修剪，以此为起点
                        break;
                    }
                }

                // 应用新路径
                _path = newPath;

                // 如果所有点都被剪掉了（说明离终点极近），就设为最后一个点
                _pathIndex = Mathf.Min(startIndex, _path.Length - 1);

                // 只有当路径发生剧烈变化时，才重置某些状态
                // 否则保持 State = EMotorState.Pathing 不变
                if (State != EMotorState.Pathing)
                {
                    State = EMotorState.Pathing;
                }

                _currentGoal = destination;
                _replanCooldownLeft = 0f;

                //EnterPathing(destination);
                //this._stopDistance = stopDistance;
                //this._moveSpeedRate = moveSpeedRate;
            }
            else
            {
                // 没有路径：尝试直线移动或立即失败
                _path = default;
                _pathIndex = -1;
                _currentGoal = destination;
                this._stopDistance = stopDistance;
                this._moveSpeedRate = moveSpeedRate;
                EnterPathing(destination); 
            }
        }

        /// <summary>
        /// 进行跟随
        /// </summary>
        /// <param name="target"></param>
        /// <param name="followPrediction"></param>
        /// <param name="offset"></param>
        /// <param name="stopDistance"></param>
        /// <param name="moveSpeedRate"></param>
        public void TryMoveFollow(ILogicEntity target, float followPrediction, Vector2 offset, float stopDistance = 0.1f, float moveSpeedRate = 1.0f)
        {
            _followTarget = target;
            _followPrediction = followPrediction;
            _followOffset = offset;
            this._stopDistance = stopDistance;
            this._moveSpeedRate = moveSpeedRate;

            // 首次取目标点并建路径（如果可）
            if (navProvider.TryGetFollowPoint(_followTarget, _followPrediction, _followOffset, out var goal))
            {
                _currentGoal = goal;
                navProvider.TryBuildPath(UnitEntity.Pos, goal,  out _path);
                EnterFollowing();
            }
            else
            {
                EnterFree();
                //OnLostTarget?.Invoke();
            }
        }

        public void StopMove()
        {
            EnterFree();
        }




        public void Tick(float dt)
        {
            _replanCooldownLeft -= dt;
            // 记录卡住信息（仅逻辑判定）
            TrackStuck(dt);

            // 将期望旋转/速度转化为最终物理位姿（在FixedUpdate中执行）
            //DesiredRotation = ComputeDesiredRotationFromVelocity(DesiredVelocity, Rotation, _settings.AngularSpeedDeg, dt);
            if(State == EMotorState.Pathing)
            {
                TickPathingState();
            }
            else if(State == EMotorState.Following)
            {
                TickFollowingState();
            }

            if (DesiredVelocity.sqrMagnitude < 0.01f)
            {
                _currentVelocityRef = Vector2.zero;
                _lastAvoidanceVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector2 GetDesiredVelocity()
        {
            switch (State)
            {
                case EMotorState.Free:
                    TickFree();
                    break;

                case EMotorState.Pathing:
                    UpdatePathingVelocity();
                    break;

                case EMotorState.Following:
                    UpdateFollowingVelocity();
                    break;
            }
            return DesiredVelocity;
        }


        private void TrackStuck(float dt)
        {
            var moved = (UnitEntity.Pos - _lastPosition);
            if (moved.magnitude < 0.01f && DesiredVelocity.magnitude > 0.1f)
            {
                _stuckTimer += dt;
                if (_stuckTimer >= 1)
                {
                    //OnPathBlocked?.Invoke();
                    TryReplan();
                    _stuckTimer = 0f;
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        private void TickFree()
        {
            DesiredVelocity = FreeMoveInput * UnitEntity.GetCurrSpeed();
            // 可渐停：从当前速度到0
            Velocity = Vector3.zero;
        }

        /// <summary>
        /// 
        /// </summary>
        private void TickPathingState()
        {
            // 1. 终点判定
            if (_path.Length == 0)
            {
                // 直线移动模式的终点判断
                if (Vector2.Distance(UnitEntity.Pos, _currentGoal) <= ArriveTolerance)
                {
                    EnterFree();
                }
                return;
            }

            // 2. 路径点切换判定 (Switch Logic)
            // 获取当前要去的路点
            Vector2 currentWaypoint = _path.Waypoints[_pathIndex];
            float dist = Vector2.Distance(UnitEntity.Pos, currentWaypoint);

            // 判断是否是最后一个点
            bool isFinalPoint = _pathIndex >= _path.Length - 1;

            // 切换条件：
            // A. 如果是中间点：使用宽松的 SwitchRadius (切角)
            // B. 如果是终点：使用严格的 ArriveTolerance
            float threshold = isFinalPoint ? ArriveTolerance : SwitchRadius;

            if (dist <= threshold)
            {
                if (isFinalPoint)
                {
                    // 真的到了终点
                    EnterFree();
                }
                else
                {
                    // 切换到下一个点
                    _pathIndex++;
                }
            }
        }


        private void TickFollowingState()
        {
            // 1) 更新目标点（带预测与重规划）
            if (AllowReplan && _replanCooldownLeft <= 0f)
            {
                if (navProvider.TryGetFollowPoint(_followTarget, _followPrediction, _followOffset, out var goal))
                {
                    _currentGoal = goal;
                    // 若直线可达且距离不大，禁用路径改用直线Steering
                    if (navProvider.Linecast(UnitEntity.Pos, goal, out var hit))
                    {
                        // 有阻挡：重新规划
                        navProvider.TryReplan(UnitEntity.Pos, goal, out _path);
                        _pathIndex = 0;
                    }
                    else
                    {
                        _path = default; // 直线模式
                        _pathIndex = -1;
                    }
                    _replanCooldownLeft = ReplanCooldown;
                }
                else
                {
                    //OnLostTarget?.Invoke(); 
                    EnterFree();
                    return;
                }
            }
        }

        private void UpdatePathingVelocity()
        {
            // 没路径点时直线前往_currentGoal
            if (_path.Length == 0)
            {
                MoveToward(_currentGoal);
                return;
            }

            // 有路径
            // 确定当前子路点
            var waypoint = _path.Waypoints[_pathIndex];
            // 朝子路点移动
            MoveToward(waypoint);

            //DesiredVelocity = ApplyAvoidanceToVelocity(DesiredVelocity);
        }

        private void UpdateFollowingVelocity()
        {
            float d = (UnitEntity.Pos - _currentGoal).magnitude;

            // 2) 牵引半径与停止距离
            if (d <= _stopDistance)
            {
                DesiredVelocity = Vector3.zero;
                return; // 保持Idle或微动
            }

            // 3) 计算目标方向（路径或直线）
            Vector3 dir;
            if (_path.Length > 0 && _pathIndex >= 0)
            {
                var wp = _path.Waypoints[_pathIndex];
                if (Arrived(UnitEntity.Pos, wp, ArriveTolerance))
                {
                    _pathIndex++;
                    if (_pathIndex >= _path.Length) dir = (_currentGoal - UnitEntity.Pos).normalized;
                    else dir = (_path.Waypoints[_pathIndex] - UnitEntity.Pos).normalized;
                }
                else dir = (wp - UnitEntity.Pos).normalized;
            }
            else
            {
                dir = (_currentGoal - UnitEntity.Pos).normalized; // 直线
            }

            // 4) 速度匹配逻辑
            //var targetVel = Vector2.zero;
            //if(_followTarget is BaseUnitLogicEntity unitEntity)
            //{
            //    _followTarget = unitEntity.GetCurrSpeed();
            //}

            float targetSpeed = 0;
            if (_followTarget is BaseUnitLogicEntity unitEntity)
            {
                targetSpeed = unitEntity.GetCurrSpeed();
            }
            else if(_followTarget is PatrolGroupLogicEntity patrolGroup)
            {
                targetSpeed = patrolGroup.MoveSpeed;
            }

            float v_des;
            const float D_far = 8f, D_mid = 3f, buffer = 0.75f;
            if (d > D_far)
            {
                // 远距：追赶，略高于目标速度，但不超过MaxSpeed
                v_des = Mathf.Min(UnitEntity.GetCurrSpeed(), targetSpeed + 1.5f);
            }
            else if (d > D_mid)
            {
                // 中距：部分同步 + 距离误差比例项
                float k_p = 0.5f;
                v_des = Mathf.Clamp(targetSpeed + k_p * (d - D_mid), 0f, UnitEntity.GetCurrSpeed());
            }
            else
            {
                // 近距缓冲：线性衰减至停止距离
                float k_close = 2.0f;
                float distToStopEdge = Mathf.Max(0f, d - _stopDistance);
                float maxCloseSpeed = Mathf.Min(UnitEntity.GetCurrSpeed(), distToStopEdge * k_close);
                v_des = maxCloseSpeed;
                // 当预计会反超或贴脸时，提高制动
                // 可临时提升Deceleration或将加速度目标设为0
            }

            // 5) 加减速限制与反超抑制
            float currentSpeed = Velocity.magnitude;
            float accel = Acceleration;
            float decel = Deceleration;

            // 若内积显示会反超，使用更高decel逼近
            var toGoal = (_currentGoal - UnitEntity.Pos).normalized;
            bool likelyOvershoot = Vector3.Dot(toGoal, (Velocity).normalized) < 0f || (currentSpeed > v_des && d < D_mid);
            float accelUsed = likelyOvershoot ? 0f : accel;
            float decelUsed = likelyOvershoot ? decel * 1.5f : decel;

            float nextSpeed = StepSpeed(currentSpeed, v_des, accelUsed, decelUsed, Time.deltaTime);
            DesiredVelocity = dir * nextSpeed;

            // 6) 侧向偏移与避障（可选）
            // DesiredVelocity = ApplyLocalAvoidance(DesiredVelocity);

            // 7) 旋转对齐
            //DesiredRotation = ComputeDesiredRotationFromVelocity(DesiredVelocity, Rotation, _settings.AngularSpeedDeg, dt);
        }

        float StepSpeed(float current, float target, float accel, float decel, float dt)
        {
            if (target > current) return Mathf.Min(target, current + accel * dt);
            else return Mathf.Max(target, current - decel * dt);
        }

        private void MoveToward(Vector2 target)
        {
            var to = (target - UnitEntity.Pos);
            var dir = to.normalized;
            var desiredSpeed = UnitEntity.GetCurrSpeed() * _moveSpeedRate;

            // 加速度/减速度控制
            //var currentSpeed = target.magnitude;
            //var targetSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 11);
            DesiredVelocity = dir * desiredSpeed;
        }

        private void TryReplan()
        {
            _replanCooldownLeft = ReplanCooldown;
            Vector3 goal = _currentGoal;
            if (State == EMotorState.Following)
            {
                if (navProvider.TryGetFollowPoint(_followTarget, _followPrediction, _followOffset, out var newGoal))
                {
                    goal = newGoal;
                }
                else
                {
                    //OnLostTarget?.Invoke();
                    //EnterFree();
                    return;
                }
            }

            if (navProvider.TryReplan(UnitEntity.Pos, goal, out var newPath))
            {
                _path = newPath;
                _pathIndex = 0;
                //OnReplanned?.Invoke();
            }
        }


        private void EnterFree()
        {
            State = EMotorState.Free;
            _path = default;
            _pathIndex = 0;
            //_knockbackTimeLeft = 0f;
            //_rootMotionTimeLeft = 0f;
            DesiredVelocity = Vector3.zero;
            //OnStateChanged?.Invoke(State);
        }


        /// <summary>
        /// 进入pathing
        /// </summary>
        /// <param name="destination"></param>
        private void EnterPathing(Vector3 destination)
        {
            State = EMotorState.Pathing;
            _pathIndex = 0;
            _currentGoal = destination;
            _replanCooldownLeft = 0f;
            //OnStateChanged?.Invoke(State);
        }

        private void EnterFollowing()
        {
            State = EMotorState.Following;
            _pathIndex = 0;
            _replanCooldownLeft = 0f;
            //OnStateChanged?.Invoke(State);
        }


        private static bool Arrived(Vector2 pos, Vector2 dst, float tol) => (pos - dst).magnitude <= tol;

        public float AvoidanceRadius = 0.2f;   // 侦测半径：多远开始避让
        public float AvoidanceWeight = 0.5f;   // 避障权重：越大躲得越狠
        protected List<(Vector2, Vector2)> avoidanceCache = new();
        public float lookAheadDistance = 0.3f;
        public float bodyRadius = 0.3f;
        public float sideRayAngle = 45f;
        public float avoidanceStrength = 1.0f;
        public float velocitySmoothTime = 0.1f;
        private Vector2 _currentVelocityRef;
        private Vector2 _lastAvoidanceVelocity;
        /// <summary>
        /// 计算避障速度
        /// </summary>
        /// <returns></returns>
        private Vector2 ApplyAvoidanceToVelocity(Vector2 desiredVelocity)
        {
            // 1. 停止判定与状态重置
            if (desiredVelocity.sqrMagnitude < 0.01f)
            {
                _currentVelocityRef = Vector2.zero;
                _lastAvoidanceVelocity = Vector2.zero;
                return Vector2.zero;
            }

            Vector2 moveDir = desiredVelocity.normalized;
            Vector2 origin = (Vector2)UnitEntity.Pos + (moveDir * bodyRadius * 0.5f);

            // 射线检测
            RaycastHit2D hit = Physics2D.Raycast(origin, moveDir, lookAheadDistance, 1 << LayerMask.NameToLayer("DynamicObs"));

#if UNITY_EDITOR
            if (hit.collider != null) Debug.DrawLine(origin, hit.point, Color.red);
            else Debug.DrawLine(origin, origin + moveDir * lookAheadDistance, Color.green);
#endif

            Vector2 targetVelocity = desiredVelocity;

            // 2. 避障逻辑 (切向滑动)
            if (hit.collider != null)
            {
                float dot = Vector2.Dot(moveDir, hit.normal);
                if (dot < 0)
                {
                    // 切向投影：消除撞墙分量
                    Vector2 slideVelocity = desiredVelocity - (hit.normal * dot * desiredVelocity.magnitude);

                    // 稍微推离墙壁
                    Vector2 pushOutVector = hit.normal * (1.0f - (hit.distance / lookAheadDistance)) * 2.0f;

                    targetVelocity = slideVelocity + pushOutVector;

                    // 死锁打破
                    if (dot < -0.9f)
                    {
                        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
                        targetVelocity += tangent * 1.5f;
                    }
                }
            }

            // 3. 保持速度大小 (建议开启，手感更好)
            targetVelocity = targetVelocity.normalized * desiredVelocity.magnitude;

            // 4. 平滑处理 (含冷启动修复)
            bool isColdStart = _lastAvoidanceVelocity.sqrMagnitude < 0.01f;
            Vector2 finalVelocity;

            if (isColdStart)
            {
                // 冷启动：直接赋值，跳过平滑
                finalVelocity = targetVelocity;
                _currentVelocityRef = Vector2.zero;
            }
            else
            {
                // 运行中：应用平滑，防止突变抖动
                finalVelocity = Vector2.SmoothDamp(
                    _lastAvoidanceVelocity,
                    targetVelocity,
                    ref _currentVelocityRef,
                    velocitySmoothTime
                );
            }

            _lastAvoidanceVelocity = finalVelocity;
            return finalVelocity;
        }

        private Vector2 CastRay(Vector2 origin, Vector2 dir, float distance)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, distance, 1 << LayerMask.NameToLayer("DynamicObs"));

            // 调试绘制
#if UNITY_EDITOR
            if (hit.collider != null) Debug.DrawLine(origin, hit.point, Color.red);
            else Debug.DrawLine(origin, origin + dir * distance, Color.green);
#endif

            if (hit.collider != null)
            {
                // 计算斥力：
                // 1. 越近斥力越大 (1.0 - fraction)
                // 2. 方向是 碰撞法线 (hit.normal) 或者 简单的反向 (-dir)
                // 这里推荐使用 hit.normal，因为它是滑墙的关键，它会指引你沿着墙面切线走
                float repulsionStrength = 1.0f - (hit.distance / distance);

                // 为了防止正对墙面时法线完全反向导致停止，我们可以混合一点反射向量
                return hit.normal * repulsionStrength;
            }

            return Vector2.zero;
        }
    }


}