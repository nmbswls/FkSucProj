using System.Collections;
using System.Collections.Generic;
using Animancer;
using My.Map.Entity;
using My.Map.Scene;
using UnityEngine;
using UnityEngine.AI;
using static My.UI.UISceneInteractMenu4Choose;


namespace My.Map
{
    public class HomeSimpleNpc : MonoBehaviour, ISceneInteractable
    {
        public enum MobState
        {
            Idle,           // 闲置/决策中
            MovingToSpot,   // 正在走向目标点
            Working,        // 到达点位，正在播放工作动画
            Interacting     // 被玩家暂停（对话中）
        }

        public Transform HintPivot;

        [Header("状态监控")]
        public MobState CurrentState = MobState.Idle;

        [Header("配置参数")]
        public float WalkSpeed = 1.5f;
        public float DecisionInterval = 2.0f; // 闲置多久后做决策

        // --- 内部组件 ---
        //private NavMeshAgent _agent;
        //private Animator _anim;
        private AnimancerComponent _anim;

        // --- 运行时数据 ---
        private float _timer = 0f;          // 通用计时器
        private float _workDuration = 0f;   // 当前任务需要工作多久
        private MobState _previousState;    // 交互前的状态备份

        // --- 当前任务数据 ---
        private HomeActionSpot _targetSpot; // 目标点
        private int _targetSlotIndex = -1;   // 目标槽位
        private Vector3 _targetPos;          // 目标具体坐标

        public LogicEntityMpbNpc FakeInnerEntity;

        public EntityMotorSystem MotorSystem;

        public long Id => gameObject.GetInstanceID();

        public string ShowName => "人";

        public Vector2 Pos => transform.position;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => true;

        public SimpleCharacterController CharacterController;

        void Start()
        {

            FakeInnerEntity = new(MainGameManager.Instance.gameLogicManager, 0, string.Empty, transform.position, new Logic.LogicEntityRecord());

            FakeInnerEntity.Initialize();
            FakeInnerEntity.moveSpeed = 1.0f;
            //_agent = GetComponent<NavMeshAgent>();
            _anim = GetComponent<AnimancerComponent>();
            //_agent.speed = WalkSpeed;

            // 初始状态
            SwitchState(MobState.Idle);

            if (!CharacterController)
            {
                CharacterController = GetComponent<SimpleCharacterController>();
                CharacterController.GetDisiredVelFunc = GetFixedDesiredVel;
                CharacterController.ClampValidPos = (desired) =>
                {
                    return WorldAreaManager.Instance.ClampPathToWalkable(transform.position, desired);
                };
                CharacterController.IsGhoseMove = () =>
                {
                    return false;
                };
                CharacterController.SyncPos = (pos) =>
                {
                    FakeInnerEntity.SetPosition(pos);
                };

            }
        }

        public Vector2 GetFixedDesiredVel()
        {
            if (FakeInnerEntity == null) return Vector2.zero;

            Vector2 targetMoveVel = FakeInnerEntity.GetDesiredVelocity();
            return targetMoveVel;
        }

        void Update()
        {
            // 1. 优先处理交互状态 (Interacting)
            if (CurrentState == MobState.Interacting)
            {
                HandleInteractionLogic();
                return; // 交互时阻断其他逻辑
            }

            // 2. 状态机分发
            switch (CurrentState)
            {
                case MobState.Idle:
                    TickIdle();
                    break;
                case MobState.MovingToSpot:
                    TickMoving();
                    break;
                case MobState.Working:
                    TickWorking();
                    break;
            }

            // 3. 动画参数更新
            UpdateAnimationParams();

            FakeInnerEntity?.Tick(LogicTime.deltaTime);


        }

        // --- 状态逻辑 Tick ---

        private void TickIdle()
        {
            _timer += Time.deltaTime;

            // 还没到决策时间，继续发呆
            if (_timer < DecisionInterval) return;

            // --- 决策时刻 ---
            _timer = 0; // 重置计时器

            // 简单的概率决策：70% 去工作，30% 去休息
            if (Random.value < 0.7f)
            {
                TryFindTask(HomeFacility.FacilityType.LumberMill, HomeActionSpot.SpotType.Work, 15f);
            }
            else
            {
                TryFindGlobalTask(HomeActionSpot.SpotType.Social, 10f);
            }
        }

        private void TickMoving()
        {
            // 检查目标点是否还在（防止设施被销毁）
            if (_targetSpot == null)
            {
                StopTaskAndIdle();
                return;
            }

            do
            {
                if (MainGameManager.Instance.playerScenePresenter == null)
                {
                    break;
                }

                var diff = MainGameManager.Instance.playerScenePresenter.Pos - this.Pos;
                if (diff.sqrMagnitude > 1.0f)
                {
                    break;
                }

                FakeInnerEntity?.StopMove();
                return;
            }
            while (false);

            FakeInnerEntity?.TryMoveTo(_targetSpot.transform.position);

            var targetDiff = _targetSpot.transform.position - this.transform.position;
            if (targetDiff.magnitude < 0.2f)
            {
                StartWorking();
            }
        }

        private void TickWorking()
        {
            if (_targetSpot == null)
            {
                StopTaskAndIdle();
                return;
            }

            // 确保面对正确方向
            if (_targetSpot != null)
            {
                // 简单的插值旋转
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetSpot.transform.rotation, Time.deltaTime * 5f);
            }

            // 计时
            _timer += Time.deltaTime;

            // 工作时间结束
            if (_timer >= _workDuration)
            {
                StopTaskAndIdle();
            }
        }

        private void HandleInteractionLogic()
        {
            // 交互时的逻辑，比如始终看向玩家
            if(MainGameManager.Instance != null && MainGameManager.Instance.playerScenePresenter != null)
            {
                Vector3 dir = MainGameManager.Instance.playerScenePresenter.transform.position - transform.position;
                dir.y = 0;
                //if (dir != Vector3.zero)
                //{
                //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                //}
            }
        }

        // --- 行为控制方法 ---

        // 尝试找设施任务
        private void TryFindTask(HomeFacility.FacilityType fType, HomeActionSpot.SpotType sType, float duration)
        {
            HomeFacility facility = HomeSceneManager.Instance.GetRandomFacility(fType);
            if (facility == null) return; // 没找到设施，下一帧继续 Idle

            HomeActionSpot spot;
            int slotIndex;
            Vector3 pos;

            if (facility.TryGetSpot(sType, out spot, out slotIndex, out pos))
            {
                AcceptTask(spot, slotIndex, pos, duration);
            }
        }

        // 尝试找全局任务
        private void TryFindGlobalTask(HomeActionSpot.SpotType sType, float duration)
        {
            HomeActionSpot spot = HomeSceneManager.Instance.GetRandomGlobalSpot(sType);
            if (spot == null) return;

            int slotIndex = spot.TryGetFreeSlotIndex();
            if (slotIndex != -1)
            {
                // 计算坐标偏移
                Vector3 offset = spot.IsQueueMode ? -spot.transform.forward * (slotIndex * spot.Spacing) : Vector3.zero;
                Vector3 pos = spot.transform.position + spot.transform.rotation * offset;

                AcceptTask(spot, slotIndex, pos, duration);
            }
        }

        // 接受任务并开始移动
        private void AcceptTask(HomeActionSpot spot, int slotIndex, Vector3 pos, float duration)
        {
            // 1. 占坑
            spot.OccupySlot(slotIndex, this);
            _targetSpot = spot;
            _targetSlotIndex = slotIndex;
            _targetPos = pos;
            _workDuration = duration;

            // 3. 切换状态
            SwitchState(MobState.MovingToSpot);
        }

        // 到达后开始工作
        private void StartWorking()
        {
            _timer = 0f; // 重置计时器用于工作倒计时

            // 播放特定动画
            if (_targetSpot != null && !string.IsNullOrEmpty(_targetSpot.AnimationTrigger))
            {
                //_anim?.SetTrigger(_targetSpot.AnimationTrigger);
            }

            SwitchState(MobState.Working);
        }

        // 结束任务并回到空闲
        private void StopTaskAndIdle()
        {
            // 1. 释放坑位
            ReleaseCurrentSpot();

            // 2. 动画复位
            //_anim?.SetTrigger("StopWork");

            // 3. 回到 Idle
            SwitchState(MobState.Idle);
        }

        // --- 交互系统接口 ---

        public void StartInteraction()
        {
            if (CurrentState == MobState.Interacting) return;

            _previousState = CurrentState;

            // 动画重置为站立
            //_anim?.SetTrigger("StopWork");

            CurrentState = MobState.Interacting;
        }

        public void EndInteraction()
        {
            if (CurrentState != MobState.Interacting) return;

            // 恢复之前的状态
            CurrentState = _previousState;

            // 根据恢复的状态做额外处理
            if (CurrentState == MobState.MovingToSpot)
            {
                // 确保动画是移动状态（UpdateAnimationParams 会处理，这里通常不需要手动 Trigger）
            }
            else if (CurrentState == MobState.Working)
            {
                // 恢复动作
                //if (_targetSpot != null) _anim.SetTrigger(_targetSpot.AnimationTrigger);
            }
        }

        // --- 辅助方法 ---

        private void SwitchState(MobState newState)
        {
            CurrentState = newState;

            // 进入 Idle 时重置计时器，防止立即触发决策
            if (newState == MobState.Idle)
            {
                _timer = 0f;
            }
        }

        private void ReleaseCurrentSpot()
        {
            if (_targetSpot != null && _targetSlotIndex != -1)
            {
                _targetSpot.ReleaseSlot(_targetSlotIndex);
                _targetSpot = null;
                _targetSlotIndex = -1;
            }
        }

        private void UpdateAnimationParams()
        {
            //float speed = _agent.velocity.magnitude;
            //_anim.SetFloat("Speed", speed);
        }

        private void OnDestroy()
        {
            ReleaseCurrentSpot();
        }

        public bool CanInteractEnable()
        {
            return true;
        }

        public bool TriggerInteract(int selectionId, int playerId)
        {
            StartInteraction();
            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            if(HintPivot != null)
            {
                return HintPivot.transform.position;
            }
            return transform.position;
        }

        public float GetHintOffsetInfos()
        {
            return 0;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "互动",
            });
            return ret;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

