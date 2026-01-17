
using System;
using System.Collections.Generic;
using System.Linq;
using My.Map.Entity;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    public static class SceneAngleUtil
    {
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

        public static Vector2 NormalizeSafe(Vector2 v)
        {
            float m = v.magnitude;
            return m > 1e-8f ? v / m : Vector2.zero;
        }

        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = Mathf.DeltaAngle(current, target);
            delta = Mathf.Clamp(delta, -maxDelta, maxDelta);
            return current + delta;
        }

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime);
        }
    }


    public partial class BaseUnitLogicEntity
    {

        public float deadzoneAngle = 3f;     // 小角度不转动（度）
        public float smoothTime = 0.05f;     // 平滑时间（秒）
        protected float _angularVel;       // SmoothDampAngle 用

        /// <summary>
        /// 面向到底怎么调 
        ///   1 default面向和速度保持？
        /// </summary>
        protected Vector2 faceDir = Vector2.zero;
        public Vector2 FaceDir
        {
            get { return faceDir; }
            set
            {
                faceDir = value;
                _currentAngle = SceneAngleUtil.AngleFromDir(faceDir);
                _targetAngle = _currentAngle;
            }
        }

        // 内部状态
        protected float _currentAngle { get; set; }     // 当前朝向角度（度，0=向右）
        protected float _targetAngle;

        public Vector2 DesiredFaceDir { get { return SceneAngleUtil.DirFromAngle(_targetAngle); } }


        /// <summary>
        /// 强制更改面向
        /// </summary>
        /// <param name="faceDir"></param>
        /// <param name="immediately"></param>
        public void ForceSetFaceTarget(Vector2 faceDir, bool immediately)
        {
            var angle = SceneAngleUtil.AngleFromDir(faceDir);
            _targetAngle = angle;

            if (immediately)
            {
                _currentAngle = angle;
                this.faceDir = faceDir;
            }
        }

        /// <summary>
        /// 更新面向
        /// </summary>
        protected void UpdateFaceDir()
        {
            // 死区：小角差直接保持，减少抖动
            float angleDelta = Mathf.DeltaAngle(_currentAngle, _targetAngle);
            if (Mathf.Abs(angleDelta) < deadzoneAngle)
                _currentAngle = _targetAngle;

            // 单次最大角步长（限速）
            //float maxStep = maxAngularSpeed * Time.deltaTime;
            float maxStep = 10000;
            float clampedTarget = SceneAngleUtil.MoveTowardsAngle(_currentAngle, _targetAngle, maxStep);

            // 仅保留一次平滑：对限速后的目标做 SmoothDampAngle
            float newAngle = SceneAngleUtil.SmoothDampAngle(_currentAngle, clampedTarget, ref _angularVel, smoothTime);

            // 更新状态与朝向向量
            _currentAngle = newAngle;

            faceDir = SceneAngleUtil.DirFromAngle(_currentAngle);

            _defaultLookDir = faceDir;
        }
    }
}