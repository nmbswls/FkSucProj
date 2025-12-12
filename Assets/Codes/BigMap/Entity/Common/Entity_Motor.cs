


using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static My.Map.BaseUnitLogicEntity;
using static UnityEditor.PlayerSettings;
using static UnityEngine.CullingGroup;

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

        public void MoveTo(Vector2 destination, float stopDistance = 0.35f, float moveSpeedRate = 1.0f)
        {
            if (navProvider.TryBuildPath(UnitEntity.Pos, destination, out _path) && _path.Length > 0)
            {
                EnterPathing(destination);
                this._stopDistance = stopDistance;
                this._moveSpeedRate = moveSpeedRate;
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


        public void MoveFollow(ILogicEntity target, float followPrediction, Vector2 offset, float stopDistance = 0.1f, float moveSpeedRate = 1.0f)
        {
            if(target == null)
            {
                ;
            }
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector2 GetDesiredVelocity()
        {
            // 根据状态机计算 DesiredVelocity / DesiredRotation
            switch (State)
            {
                case EMotorState.Free:
                    TickFree();
                    break;

                case EMotorState.Pathing:
                    TickPathing();
                    break;

                case EMotorState.Following:
                    TickFollowing();
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
            DesiredVelocity = Vector3.zero;
            DesiredVelocity = FreeMoveInput;
            // 可渐停：从当前速度到0
            Velocity = Vector3.zero;
        }


        private void TickPathing()
        {
            // 没路径点时直线前往_currentGoal
            if (_path.Length == 0)
            {
                if (Arrived(UnitEntity.Pos, _currentGoal, ArriveTolerance))
                {
                    DesiredVelocity = Vector3.zero;
                    //OnReachedDestination?.Invoke();
                    EnterFree();
                    return;
                }
                MoveToward(_currentGoal);
                return;
            }

            // 有路径
            // 确定当前子路点
            var waypoint = _path.Waypoints[_pathIndex];
            // 若到达子路点，推进索引
            if (Arrived(UnitEntity.Pos, waypoint, ArriveTolerance))
            {
                _pathIndex++;
                if (_pathIndex >= _path.Length)
                {
                    //OnReachedDestination?.Invoke();
                    EnterFree();
                    return;
                }
                waypoint = _path.Waypoints[_pathIndex];
            }

            // 朝子路点移动
            MoveToward(waypoint);
        }

        private void TickFollowing()
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

    }


}