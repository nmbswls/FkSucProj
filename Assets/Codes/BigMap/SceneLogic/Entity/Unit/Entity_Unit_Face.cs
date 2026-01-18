
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

    public partial class BaseUnitLogicEntity
    {

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
            Combat = 20,    // 战斗锁定（绝对优先）
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
            var angle = SceneAngleUtil.AngleFromDir(faceDir);
            //_targetAngle = angle;

            if (immediately)
            {
                _currentAngle = angle;
                this._defaultFaceDir = faceDir;
            }
        }

        protected float _currentAngle { get; set; }
        // 默认的向前看方向
        private Vector2 _defaultFaceDir; // 由外部实时更新
        private FaceRequest? _activeRequest = null;
        private Vector2 _currentLook;
        private Vector2 _targetLookDir;

        public Vector2 FinalLook { get { return _targetLookDir; } }
        public Vector2 CurrentLook { get { return _currentLook; } }

        private void InitGazeModule()
        {
            _defaultFaceDir = Vector2.right;
            _currentLook = _defaultFaceDir;
            _targetLookDir = _defaultFaceDir;
        }

        private void UpdateGazeModule()
        {
            if(entityMotorComp.DesiredVelocity.magnitude > 0.1f)
            {
                _defaultFaceDir = entityMotorComp.DesiredVelocity.normalized;
            }

            CleanUpExpiredRequests();
            EvaluateActiveRequest();
            ApplyGazeRotation();
        }


        /// <summary>
        /// 添加或更新一个视线请求
        /// 由各模块调用
        ///   1.吸引点（声音、其他）
        ///   2.
        /// </summary>
        public void RegisterGaze(string sourceTag, long lockTargetId, Vector2 lockPosition, EGazePriority priority, float duration = 2f)
        {
            FaceRequest existing = null;
            // 检查所有锁定目标的视线类型
            if (lockTargetId != 0)
            {
                existing = _requests.FirstOrDefault(r => r.LockTargetId == lockTargetId && r.SourceTag == sourceTag);
            }

            if (existing != null)
            {
                // 更新现有请求
                existing.Priority = (int)priority;
                existing.ExpirationTime = duration > 0 ? Time.time + duration : -1;
            }
            else
            {
                // 新增请求
                _requests.Add(new FaceRequest() 
                {
                    SourceTag = sourceTag,
                    TargetPos = lockPosition,
                    LockTargetId = lockTargetId,
                    Priority = (int)priority,
                    ExpirationTime = duration > 0 ? Time.time + duration : -1,
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
            _requests.RemoveAll(r => r.SourceTag == sourceTag);
        }

        // --- 2. 内部逻辑 ---

        private void CleanUpExpiredRequests()
        {
            // 移除过期请求
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (_requests[i].ExpirationTime > 0 && Time.time > _requests[i].ExpirationTime)
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

        /// <summary>
        /// 执行旋转
        /// </summary>
        private void ApplyGazeRotation()
        {
            if (_activeRequest != null)
            {
                _targetLookDir = (_activeRequest.TargetPos - this.Pos);
            }
            else
            {
                // 如果没有请求，默认看身体前方
                _targetLookDir = _defaultFaceDir;
            }

            // 平滑插值计算当前看向的点
            _currentLook = Vector2.Lerp(_currentLook, _targetLookDir, Time.deltaTime * FaceTurnSpeed);
        }
    }
}