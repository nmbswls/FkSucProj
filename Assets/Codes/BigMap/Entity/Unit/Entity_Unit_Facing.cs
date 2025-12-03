
using My.Map.Entity;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{
    public partial class BaseUnitLogicEntity
    {
        //public enum OrientationMode { MoveFacing, LookAtTarget, BlendFacing, FixedHeading }

        public bool ControlledFacing = false;
        //public OrientationMode faceMode = OrientationMode.MoveFacing;
        public float blendWeight = 0.6f;

        public ILogicEntity? lookTarget;      // 固定目标
        public Vector2? lootTargetPos;        // 固定目标
        public Vector2 fixedHeading = Vector2.right; // 固定面向

        public float speedThreshold = 0.05f; // 速度太小不更新面向
        public float deadzoneAngle = 3f;     // 小角度不转动（度）
        public float maxAngularSpeed = 360f; // 每秒最大旋转角度（度/s）
        public float smoothTime = 0.05f;     // 平滑时间（秒）

        private Vector2 faceDir = Vector2.zero;
        public Vector2 FaceDir 
        {
            get { return faceDir; }
            set 
            {
                faceDir = value;
                _currentAngle = AngleFromDir(faceDir);
                _targetAngle = _currentAngle;
            }
        }
        // 内部状态
        float _currentAngle;     // 当前朝向角度（度，0=向右）

        public Vector2 DesiredFaceDir { get { return DirFromAngle(_targetAngle); } }
        float _targetAngle;

        float _angularVel;       // SmoothDampAngle 用

        public void InitFacing()
        {
        }

        /// <summary>
        /// 更新朝向
        /// </summary>
        protected virtual void UpdateFaceDir()
        {

            if (attributeStore.CheckHasState(AttrIdConsts.LockFace))
            {
                return;
            }

            // 外部控制 不处理
            if (ControlledFacing)
            {
                return;
            }

            UpdateFacing();

            {
                // 死区：小角差直接保持，减少抖动
                float angleDelta = Mathf.DeltaAngle(_currentAngle, _targetAngle);
                if (Mathf.Abs(angleDelta) < deadzoneAngle)
                    _currentAngle = _targetAngle;

                // 单次最大角步长（限速）
                //float maxStep = maxAngularSpeed * Time.deltaTime;
                float maxStep = 10000;
                float clampedTarget = MoveTowardsAngle(_currentAngle, _targetAngle, maxStep);

                // 仅保留一次平滑：对限速后的目标做 SmoothDampAngle
                float newAngle = SmoothDampAngle(_currentAngle, clampedTarget, ref _angularVel, smoothTime);

                // 更新状态与朝向向量
                _currentAngle = newAngle;
                faceDir = DirFromAngle(_currentAngle);
            }
        }

        public void UpdateFacing()
        {
            // 检查是否需要锁定目标朝向
            Vector2? lootTarget = null;
           
            if (combatStateComp != null && combatStateComp.CombatState == EntityCombatStateComp.ECombatState.InCombat && combatStateComp.PrimaryTargetId != 0)
            {
                var targt = LogicManager.GetLogicEntity(combatStateComp.PrimaryTargetId, false);
                if (targt != null)
                {
                    lootTarget = targt.Pos;
                }
            }
            else if(attractInfo != null)
            {
                if(attractInfo.AttractSource != null)
                {
                    lootTarget = attractInfo.AttractSource.Pos;
                }
                else
                {
                    lootTarget = attractInfo.Pos;
                }
            }
            //else if(IsWatchingPlayer)
            //{
            //    lootTarget = LogicManager.playerLogicEntity.Pos;
            //}

            
            Vector2 lookDir = Vector2.zero;
            if (lootTarget != null)
            {
                lookDir = (Vector2)(lootTarget - this.Pos);
            }
            else if (entityMotorComp.DesiredVelocity.magnitude > 1e-2)
            {
                lookDir = entityMotorComp.DesiredVelocity;
            }


            if (lookDir == Vector2.zero || lookDir.sqrMagnitude < 1e-8f)
            {
                return;
            }

            lookDir.Normalize();
            // FaceDir = lookDir;

            _targetAngle = AngleFromDir(lookDir);

            
        }




        // 工具函数
        static float AngleFromDir(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        static Vector2 DirFromAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        static Vector2 NormalizeSafe(Vector2 v)
        {
            float m = v.magnitude;
            return m > 1e-8f ? v / m : Vector2.zero;
        }

        static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = Mathf.DeltaAngle(current, target);
            delta = Mathf.Clamp(delta, -maxDelta, maxDelta);
            return current + delta;
        }

        static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime);
        }
    }

}
