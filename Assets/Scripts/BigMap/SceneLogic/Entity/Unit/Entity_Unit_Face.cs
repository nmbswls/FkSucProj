
using System;
using System.Collections.Generic;
using System.Linq;
using My.Map.Entity;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using static My.Map.BaseUnitLogicEntity;
using static Unity.VisualScripting.Member;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    public static class SceneAngleUtil
    {
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

        public bool DefaultControlledByVelocity { get; set; } = true;

        /// <summary>
        /// 受外部控制面向
        /// </summary>

        public float FaceTurnSpeed = 5f;        // 视线转向速度
        public float FaceResetDelay = 1.0f;     // 无目标后多久复位

        public enum EGazePriority
        {
            Idle = 0,       // 闲置/巡逻时的随意扫视
            Distraction = 5,// 环境噪音、吸引（扔石头、口哨）
            Suspicion = 10, // 警觉（看到残影、听到脚步）
            Interact = 15,
            Combat = 20,    // 战斗锁定（绝对优先）
            CastSkill = 21,
            Override = 99   // 剧情强制/死亡
        }

        // 注视intent
        public class FaceRequest
        {
            public string SourceTag;
            public long LockTargetId;
            public Vector2 TargetPos;
            public int Priority = 1;
            public float Weight;            // 权重 (0-1)，用于平滑过渡
            public float ExpirationTime;    // 过期时间戳，-1代表永久有效直到手动移除
        }

        // 存储所有活跃的视线请求
        private List<FaceRequest> _requests = new List<FaceRequest>();

        /// <summary>
        /// 强制更改面向
        /// </summary>
        /// <param name="faceDir"></param>
        /// <param name="immediately"></param>
        public void ForceSetFaceTarget(Vector2 faceDir, bool immediately)
        {
            _defaultFaceDir = faceDir;

            if (immediately)
            {
                ApplyGazeRotation(true, false);
            }
        }

        //protected float _currentAngle { get; set; }
        // 默认的向前看方向
        private Vector2 _defaultFaceDir; // 由外部实时更新
        private FaceRequest? _activeRequest = null;
        private Vector2 _currentLook;
        private Vector2 _targetLookDir;

        public Vector2 FinalLook { get { return _currentLook; } }
        public Vector2 CurrentLook { get { return _currentLook; } }

        private void InitGazeModule()
        {
            _defaultFaceDir = Vector2.right;
            _currentLook = _defaultFaceDir;
            _targetLookDir = _defaultFaceDir;
        }

        private void UpdateGazeModule()
        {
            if(DefaultControlledByVelocity && MotorSystem.DesiredVelocity.magnitude > 0.1f)
            {
                _defaultFaceDir = MotorSystem.DesiredVelocity.normalized;
            }

            var prevTopPriority = int.MinValue;
            if (_requests.Count > 0)
            {
                foreach (var r in _requests)
                {
                    if (r.Priority > prevTopPriority)
                    {
                        prevTopPriority = r.Priority;
                    }
                }
            }

            CleanUpExpiredRequests();
            EvaluateActiveRequest();
            var snapToDefault = _activeRequest == null && prevTopPriority >= (int)EGazePriority.Combat;
            ApplyGazeRotation(false, snapToDefault);
        }


        /// <summary>
        /// 添加或更新一个视线请求
        /// 由各模块调用
        ///   1.吸引点（声音、其他）
        ///   2.
        /// </summary>
        public void RegisterGaze(string sourceTag, long lockTargetId, Vector2 lockPosition, EGazePriority priority, float duration = 0f)
        {
            FaceRequest existing = null;
            if (lockTargetId != 0)
            {
                existing = _requests.FirstOrDefault(r => r.LockTargetId == lockTargetId && r.SourceTag == sourceTag);
            }
            else if (sourceTag == "Combat")
            {
                // 施法对点注视：避免 lockTargetId==0 时每条都堆积
                existing = _requests.FirstOrDefault(r => r.SourceTag == "Combat" && r.LockTargetId == 0);
            }

            var expireAt = duration > 0 ? LogicTime.time + duration : -1f;

            if (existing != null)
            {
                existing.Priority = (int)priority;
                existing.ExpirationTime = expireAt;
                existing.TargetPos = lockPosition;
                existing.LockTargetId = lockTargetId;
            }
            else
            {
                _requests.Add(new FaceRequest()
                {
                    SourceTag = sourceTag,
                    TargetPos = lockPosition,
                    LockTargetId = lockTargetId,
                    Priority = (int)priority,
                    ExpirationTime = expireAt,
                });
            }
        }

        /// <summary>
        /// 移除特定的视线请求（例如战斗结束，不再看玩家）
        /// </summary>
        public void UnregisterGaze(long? lockTargetId)
        {
            if (lockTargetId == null)
            {
                return;
            }
            _requests.RemoveAll(r => r.LockTargetId == lockTargetId);
        }

        /// <summary>
        /// 移除特定的视线请求（例如战斗结束，不再看玩家）
        /// </summary>
        public void UnregisterGazeBySourceTag(string sourceTag)
        {
            Debug.Log($"UnregisterGazeBySourceTag sourceTag={sourceTag}");
            _requests.RemoveAll(r => r.SourceTag == sourceTag);
        }

        // --- 2. 内部逻辑 ---

        private void CleanUpExpiredRequests()
        {
            // 移除过期请求
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (_requests[i].ExpirationTime > 0 && LogicTime.time > _requests[i].ExpirationTime)
                {
                    _requests.RemoveAt(i);
                }
            }
        }

        private void EvaluateActiveRequest()
        {
            if (_requests.Count == 0)
            {
                _activeRequest = null;
                return;
            }

            // 核心逻辑：选择优先级最高的请求
            // 如果优先级相同，可以选择最近添加的，或者距离最近的
            _activeRequest = _requests.OrderByDescending(r => r.Priority).First();


            if (_activeRequest.LockTargetId != 0)
            {
                var lockEntity = LogicManager.GetLogicEntity(_activeRequest.LockTargetId, false);
                if (lockEntity != null)
                {
                    _activeRequest.TargetPos = lockEntity.Pos;
                }
            }
        }

        private float GetFaceTurnSpeed()
        {
            if(CheckHasState(AttrIdConsts.FastTurn))
            {
                return FaceTurnSpeed * 3;
            }
            return FaceTurnSpeed;
        }


        /// <summary>
        /// 执行旋转
        /// </summary>
        private void ApplyGazeRotation(bool force = false, bool snapToDefault = false)
        {
            if (_activeRequest != null)
            {
                _targetLookDir = (_activeRequest.TargetPos - this.Pos);
            }
            else
            {
                _targetLookDir = _defaultFaceDir;
            }

            if (force)
            {
                _currentLook = _targetLookDir;
            }
            else if (snapToDefault && _activeRequest == null)
            {
                _currentLook = _targetLookDir.sqrMagnitude > 1e-8f ? _targetLookDir.normalized : _defaultFaceDir;
            }
            else
            {
                _currentLook = Vector2.Lerp(_currentLook, _targetLookDir, LogicTime.deltaTime * GetFaceTurnSpeed());
            }
        }
    }
}