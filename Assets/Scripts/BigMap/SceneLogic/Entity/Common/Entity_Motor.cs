


using System.Collections.Generic;
using My.Map.Entity;
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


    public interface IWithMotor
    {
        EMotorState MotorState { get; }

        bool CheckIsFollowTarget(long targetId);

        void TryMoveTo(Vector2 destination, float stopDistance = 0.35f, float moveSpeedRate = 1.0f);

        void TryMoveFollow(ILogicEntity target, float followPrediction, Vector2 offset, float stopDistance = 0.1f, float moveSpeedRate = 1.0f);

        void StopMove();

        Vector2 GetDesiredVelocity();

        Vector2 FreeMoveInput { get; set; }
    }

    public class EntityMotorSystem : IWithMotor
    {
        protected LogicEntityBase Owner { get; set; }
        public INavProvider navProvider;

        public EMotorState MotorState { get; private set; } = EMotorState.Free;

        // true：不走 Nav 建路/重规划，仅直线追 _currentGoal（飞行、穿障碍等）
        public bool IgnoreGround = false;

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

        public Vector2 FreeMoveInput
        {
            get => _freeMoveInput;
            set
            {
                if (HasSimulatedMoveInput)
                {
                    _freeMoveInput = Vector2.zero;
                    return;
                }

                _freeMoveInput = value;
            }
        }

        Vector2 _freeMoveInput = Vector2.zero;

        // Tier2：Buff 模拟摇杆输入，激活时拒收 TryMoveTo / TryMoveFollow
        public Vector2 SimulatedMoveInput { get; private set; }
        public float SimulatedMoveSpeedRate { get; private set; } = 1f;
        public bool HasSimulatedMoveInput { get; private set; }

        public EntityMotorSystem(LogicEntityBase entity, INavProvider navProvider)
        {
            this.Owner = entity;
            this.navProvider = navProvider;
        }

        public bool CheckIsFollowTarget(long targetId)
        {
            if (MotorState != EMotorState.Following) return false;
            if (_followTarget == null || _followTarget.Id != targetId) return false;
            return true;
        }

        public bool CheckIsMovingTo(Vector2 targetPos)
        {
            if (MotorState != EMotorState.Pathing) return false;
            if (_currentGoal == null || _currentGoal != targetPos) return false;
            return true;
        }

        public void TryMoveTo(Vector2 destination, float stopDistance = 0.35f, float moveSpeedRate = 1.0f)
        {
            if (HasSimulatedMoveInput)
            {
                return;
            }

            if(MotorState == EMotorState.Pathing)
            {
                if (Vector2.Distance(destination, _currentGoal) < ArriveTolerance)
                {
                    return;
                }
            }

            if (IgnoreGround)
            {
                _path = default;
                _pathIndex = -1;
                this._stopDistance = stopDistance;
                this._moveSpeedRate = moveSpeedRate;
                EnterPathing(destination);
                return;
            }

            if (navProvider.TryBuildPath(Owner.Pos, destination, out var newPath) && newPath.Length > 0)
            {
                TruncateNewPath(newPath);

                // 只有当路径发生剧烈变化时，才重置某些状态
                // 否则保持 State = EMotorState.Pathing 不变
                if (MotorState != EMotorState.Pathing)
                {
                    MotorState = EMotorState.Pathing;
                }

                _currentGoal = destination;
                _replanCooldownLeft = 0f;

                //EnterPathing(destination);
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
            if (HasSimulatedMoveInput)
            {
                return;
            }

            if(MotorState == EMotorState.Following && target == _followTarget)
            {
                _followPrediction = followPrediction;
                _followOffset = offset;
                this._stopDistance = stopDistance;
                this._moveSpeedRate = moveSpeedRate;
                return;
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
                if (IgnoreGround)
                {
                    _path = default;
                    _pathIndex = -1;
                    EnterFollowing();
                }
                else if (navProvider.TryBuildPath(Owner.Pos, goal, out var newPath) && newPath.Length > 0)
                {
                    TruncateNewPath(newPath);
                    EnterFollowing();
                }
                else
                {
                    _path = default;
                    _pathIndex = -1;
                    EnterFollowing();
                }
            }
            else
            {
                EnterFree();
            }
        }

        public void StopMove()
        {
            EnterFree();
        }

        public void SetSimulatedMoveInput(Vector2 direction, float speedRate)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                ClearSimulatedMoveInput();
                return;
            }

            if (!HasSimulatedMoveInput)
            {
                EnterFree();
            }

            SimulatedMoveInput = direction.normalized;
            SimulatedMoveSpeedRate = Mathf.Max(0.01f, speedRate);
            HasSimulatedMoveInput = true;
        }

        public void ClearSimulatedMoveInput()
        {
            SimulatedMoveInput = Vector2.zero;
            SimulatedMoveSpeedRate = 1f;
            HasSimulatedMoveInput = false;
        }




        public void Tick(float dt)
        {
            _replanCooldownLeft -= dt;
            // 记录卡住信息（仅逻辑判定）
            TrackStuck(dt);

            // 将期望旋转/速度转化为最终物理位姿（在FixedUpdate中执行）
            //DesiredRotation = ComputeDesiredRotationFromVelocity(DesiredVelocity, Rotation, _settings.AngularSpeedDeg, dt);
            if(MotorState == EMotorState.Pathing)
            {
                TickPathingState();
            }
            else if(MotorState == EMotorState.Following)
            {
                TickFollowingState();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Vector2 GetDesiredVelocity()
        {
            if (HasSimulatedMoveInput)
            {
                return SimulatedMoveInput * Owner.GetCurrSpeed() * SimulatedMoveSpeedRate;
            }

            switch (MotorState)
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
            var moved = (Owner.Pos - _lastPosition);
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
            DesiredVelocity = _freeMoveInput * Owner.GetCurrSpeed();
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
                if (Vector2.Distance(Owner.Pos, _currentGoal) <= ArriveTolerance)
                {
                    EnterFree();
                }
                return;
            }

            if (_pathIndex < 0 || _pathIndex >= _path.Length)
            {
                if (Vector2.Distance(Owner.Pos, _currentGoal) <= ArriveTolerance)
                {
                    EnterFree();
                }
                return;
            }

            // 2. 路径点切换判定 (Switch Logic)
            // 获取当前要去的路点
            Vector2 currentWaypoint = _path.Waypoints[_pathIndex];
            float dist = Vector2.Distance(Owner.Pos, currentWaypoint);

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
                    if (IgnoreGround)
                    {
                        _path = default;
                        _pathIndex = -1;
                    }
                    else if (navProvider.Linecast(Owner.Pos, goal, out var hit))
                    {
                        // 有阻挡：重新规划
                        if (navProvider.TryReplan(Owner.Pos, goal, out var newPath))
                        {
                            TruncateNewPath(newPath);
                        }
                        else
                        {
                            _path = default;
                            _pathIndex = -1;
                        }
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

            if (_path.Length > 0 && _pathIndex >= 0 && _pathIndex < _path.Length)
            {
                var wp = _path.Waypoints[_pathIndex];
                if (Arrived(Owner.Pos, wp, ArriveTolerance))
                {
                    _pathIndex++;
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

            // 有路径；索引越界时改追最终目标
            if (_pathIndex < 0 || _pathIndex >= _path.Length)
            {
                MoveToward(_currentGoal);
                return;
            }

            var waypoint = _path.Waypoints[_pathIndex];
            MoveToward(waypoint);

            //DesiredVelocity = ApplyAvoidanceToVelocity(DesiredVelocity);
        }

        private void UpdateFollowingVelocity()
        {
            float d = (Owner.Pos - _currentGoal).magnitude;

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
                if (_pathIndex >= _path.Length) dir = (_currentGoal - Owner.Pos).normalized;
                else dir = (_path.Waypoints[_pathIndex] - Owner.Pos).normalized;
            }
            else
            {
                dir = (_currentGoal - Owner.Pos).normalized; // 直线
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
                v_des = Mathf.Min(Owner.GetCurrSpeed(), targetSpeed + 1.5f);
            }
            else if (d > D_mid)
            {
                // 中距：部分同步 + 距离误差比例项
                float k_p = 0.5f;
                v_des = Mathf.Clamp(targetSpeed + k_p * (d - D_mid), 0f, Owner.GetCurrSpeed());
            }
            else
            {
                // 近距缓冲：线性衰减至停止距离
                float k_close = 2.0f;
                float distToStopEdge = Mathf.Max(0f, d - _stopDistance);
                float maxCloseSpeed = Mathf.Min(Owner.GetCurrSpeed(), distToStopEdge * k_close);
                v_des = maxCloseSpeed;
                // 当预计会反超或贴脸时，提高制动
                // 可临时提升Deceleration或将加速度目标设为0
            }

            if(v_des > Owner.GetCurrSpeed() * _moveSpeedRate)
            {
                v_des = Owner.GetCurrSpeed() * _moveSpeedRate;
            }

            // 5) 加减速限制与反超抑制
            float currentSpeed = Velocity.magnitude;
            float accel = Acceleration;
            float decel = Deceleration;

            // 若内积显示会反超，使用更高decel逼近
            var toGoal = (_currentGoal - Owner.Pos).normalized;
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
            var to = (target - Owner.Pos);
            var dir = to.normalized;
            var desiredSpeed = Owner.GetCurrSpeed() * _moveSpeedRate;

            // 加速度/减速度控制
            //var currentSpeed = target.magnitude;
            //var targetSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, 11);
            DesiredVelocity = dir * desiredSpeed;
        }

        private void TryReplan()
        {
            _replanCooldownLeft = ReplanCooldown;
            Vector3 goal = _currentGoal;
            if (MotorState == EMotorState.Following)
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

            if (IgnoreGround)
            {
                _path = default;
                _pathIndex = -1;
                _currentGoal = goal;
                return;
            }

            if (navProvider.TryReplan(Owner.Pos, goal, out var newPath) && newPath.Length > 0)
            {
                TruncateNewPath(newPath);
            }
            else
            {
                _path = default;
                _pathIndex = -1;
                _currentGoal = goal;
            }
        }

        /// <summary>
        /// 裁剪路径
        /// </summary>
        /// <param name="newPath"></param>
        /// <param name="startIndex"></param>
        private void TruncateNewPath(NavPath newPath)
        {
            if (newPath.Waypoints == null || newPath.Length <= 0)
            {
                _path = newPath;
                _pathIndex = -1;
                return;
            }

            int startIndex = 0;

            // 遍历新路径的前几个点
            for (int i = 0; i < newPath.Waypoints.Length; i++)
            {
                Vector2 wp = newPath.Waypoints[i];
                float distToWp = Vector2.Distance(Owner.Pos, wp);

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
        }

        private void EnterFree()
        {
            MotorState = EMotorState.Free;
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
            MotorState = EMotorState.Pathing;
            _pathIndex = 0;
            _currentGoal = destination;
            _replanCooldownLeft = 0f;
            //OnStateChanged?.Invoke(State);
        }

        private void EnterFollowing()
        {
            MotorState = EMotorState.Following;
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
    }


}


namespace My.Map
{
    public abstract partial class LogicEntityBase
    {
        protected EntityMotorSystem MotorSystem { get; set; }

        public EMotorState MotorState { get { return MotorSystem.MotorState; } }

        public bool CheckIsFollowTarget(long targetId)
        {
            return MotorSystem.CheckIsFollowTarget(targetId);
        }

        public void TryMoveTo(Vector2 destination, float stopDistance = 0.35f, float moveSpeedRate = 1.0f)
        {
            if (HasSimulatedMoveInput)
            {
                return;
            }

            MotorSystem.TryMoveTo(destination, stopDistance, moveSpeedRate);
        }

        public void TryMoveFollow(ILogicEntity target, float followPrediction, Vector2 offset, float stopDistance = 0.1f, float moveSpeedRate = 1.0f)
        {
            if (HasSimulatedMoveInput)
            {
                return;
            }

            MotorSystem.TryMoveFollow(target, followPrediction, offset, stopDistance, moveSpeedRate);
        }

        public void StopMove()
        {
            MotorSystem.StopMove();
        }

        public Vector2 GetDesiredVelocity()
        {
            return MotorSystem.GetDesiredVelocity();
        }

        public Vector2 FreeMoveInput { get { return MotorSystem.FreeMoveInput; } set { MotorSystem.FreeMoveInput = value; } }

        public bool HasSimulatedMoveInput => MotorSystem != null && MotorSystem.HasSimulatedMoveInput;

        public bool MotorIgnoreGround
        {
            get => MotorSystem != null && MotorSystem.IgnoreGround;
            set
            {
                if (MotorSystem != null)
                {
                    MotorSystem.IgnoreGround = value;
                }
            }
        }


        public float moveSpeed = 4.0f;
        protected virtual float GetBaseMoveSpeed()
        {
            return moveSpeed;
        }

        public float GetCurrSpeed()
        {
            var basicMove = GetAttr(AttrIdConsts.Basic_MoveSpeed);
            
            if(CheckHasState(AttrIdConsts.ImmuneJianSu))
            {
                if(basicMove < 0)
                {
                    basicMove = 0;
                }
            }
            long rate = 10000 + basicMove;

            if (rate > 50000)
            {
                rate = 50000;
            }

            if (rate < 5000)
            {
                rate = 5000;
            }

            return GetBaseMoveSpeed() * (rate) * 0.0001f;
        }
    }

}

