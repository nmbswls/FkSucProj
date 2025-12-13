
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map
{

    public partial class BaseUnitLogicEntity
    {

        /// <summary>
        /// 受外部控制面向
        /// </summary>
        public bool ControlledFacing { get; set; } = false;

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
                playerEntity.OnGazeLeave();
            }
            this.lastestLookIntent = intent;
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
            // 当前有intent
            if(lastestLookIntent != null)
            {
                if (interruptedFaceDir == null)
                {
                    interruptedFaceDir = FaceDir;
                }

                if(lastestLookIntent.LockEntityId != null)
                {
                    var srcEntity = LogicManager.GetLogicEntity(lastestLookIntent.LockEntityId.Value);

                }
                else if(lastestLookIntent.LockPos != null)
                {

                }

            }
            else
            {
                interruptedFaceDir = null;
            }

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

            if (attributeStore.CheckHasState(AttrIdConsts.LockFace))
            {
                return;
            }

            // 外部控制 不处理
            if (ControlledFacing)
            {
                return;
            }
        }



        /// <summary>
        /// 更新朝向
        /// </summary>
        protected override void UpdateFaceDir()
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


        }


        // 工具函数
        public static float AngleFromDir(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-8f) return 0f;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        public static Vector2 DirFromAngle(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        #endregion
    }
}