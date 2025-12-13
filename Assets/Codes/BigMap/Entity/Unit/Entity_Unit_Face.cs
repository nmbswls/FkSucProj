
using System.Collections.Generic;
using My.Map.Entity;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

namespace My.Map
{

    public partial class BaseUnitLogicEntity
    {

        /// <summary>
        /// 受外部控制面向
        /// </summary>
        public bool ControlledFacing { get; set; } = false;

        public float deadzoneAngle = 3f;     // 小角度不转动（度）
        public float smoothTime = 0.05f;     // 平滑时间（秒）

        // 注视intent
        public class UnitLookIntent
        {
            public long? LockEntityId;
            public Vector2? LockPos;
            public int Priority = 1;
            public float HappenTime;
            public float Duration;
        }

        public UnitLookIntent? lastestLookIntent;

        private Vector2? interruptedFaceDir { get; set; }

        /// <summary>
        /// 更新视线intent
        /// </summary>
        /// <param name="intent"></param>
        public void UpdateLookIntent(UnitLookIntent intent)
        {
            if(this.lastestLookIntent.LockEntityId != null && this.lastestLookIntent.LockEntityId == intent.LockEntityId)
            {
                this.lastestLookIntent.HappenTime = LogicTime.time;
                return;
            }

            var oldIntent = this.lastestLookIntent;
            if(oldIntent != null && oldIntent.LockEntityId != null)
            {
                var playerEntity = LogicManager.GetLogicEntity(oldIntent.LockEntityId.Value) as PlayerLogicEntity;
                playerEntity.OnGazeLeave(this.Id);
            }
            this.lastestLookIntent = intent;

            if(intent.LockEntityId != null)
            {
                var playerEntity = LogicManager.GetLogicEntity(intent.LockEntityId.Value) as PlayerLogicEntity;
                playerEntity.OnGazeEnter(this.Id);
            }
        }

        #region face

        protected Vector2 faceDir = Vector2.zero;
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
        protected float _currentAngle;     // 当前朝向角度（度，0=向右）

        public Vector2 DesiredFaceDir { get { return DirFromAngle(_targetAngle); } }
        protected float _targetAngle;

        protected float _angularVel;       // SmoothDampAngle 用


        public virtual void InitFacing()
        {

        }


        protected virtual void UpdateFaceDir()
        {
            // 检查状态
            if (attributeStore.CheckHasState(AttrIdConsts.LockFace))
            {
                return;
            }

            // 外部控制 不处理
            if (ControlledFacing)
            {
                return;
            }

            if(lastestLookIntent == null)
            {
                interruptedFaceDir = null;
                return;
            }

            // 当前有intent
            if (interruptedFaceDir == null)
            {
                interruptedFaceDir = FaceDir;
            }
            Vector2? lookDir = null;

            if (lastestLookIntent.LockEntityId != null)
            {
                var srcEntity = LogicManager.GetLogicEntity(lastestLookIntent.LockEntityId.Value);
                lookDir = srcEntity.Pos - this.Pos;
            }
            else if (lastestLookIntent.LockPos != null)
            {
                lookDir = lastestLookIntent.LockPos - this.Pos;
            }

            if(lookDir == null)
            {
                return;
            }

            lookDir = lookDir.Value.normalized;

            _targetAngle = AngleFromDir(lookDir.Value);

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

        // 工具函数
        protected static float AngleFromDir(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        protected static Vector2 DirFromAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        protected static Vector2 NormalizeSafe(Vector2 v)
        {
            float m = v.magnitude;
            return m > 1e-8f ? v / m : Vector2.zero;
        }

        protected static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = Mathf.DeltaAngle(current, target);
            delta = Mathf.Clamp(delta, -maxDelta, maxDelta);
            return current + delta;
        }

        protected static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime);
        }


        #endregion
    }
}