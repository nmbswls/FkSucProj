
using My.Def.Quest;
using My.Player;
using My.Saving;
using SuperScrollView;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Quest
{

    



    // --- 步骤运行时 ---

    
    /// <summary>
    /// 单个目标
    /// 冗余装载配置中的数据
    /// </summary>
    public class ObjectiveRuntime
    {
        protected GameLogicManager ctx { get; set; }
        public readonly ObjectiveData Data;
        public long ProgressVal = 0;

        public ObjectiveRuntime(ObjectiveData data, GameLogicManager ctx)
        {
            Data = data;
            this.ctx = ctx;
        }

        public bool IsOptional()
        {
            return Data.isOption;
        }

        public long GetRequireProgress()
        {
            return 1;
        }

        public long GetCurrProgress()
        {
            switch (Data.condition.ConditionCfg)
            {
                case QuestConditionHasItem hasItemCond:
                    {
                        return ProgressVal;
                    }
                    break;
            }

            return 0;
        }
    }


    public class StepRuntime
    {
        protected GameLogicManager ctx { get; set; }
        public int CurrStepId = 0;
        public readonly QuestStepData Data;

        private readonly ObjectiveRuntime[] _objectives;
        private readonly ObjectiveRuntime[] _failObjectives;

        private Dictionary<int, ObjectiveRuntime> objectiveMap = new();

        public bool IsCompleted { get; set; }
        public int CompletedOutcome { get; private set; }

        public StepRuntime(QuestStepData data, GameLogicManager ctx)
        {
            CurrStepId = data.stepId;
            Data = data;

            // 初始化 Outcomes
            _objectives = new ObjectiveRuntime[data.objectives?.Length ?? 0];
            for (int i = 0; i < _objectives.Length; i++)
            {
                _objectives[i] = new ObjectiveRuntime(data.objectives[i], ctx);

                objectiveMap[data.objectives[i].objectiveId] = _objectives[i];
            }

            this.ctx = ctx;
        }

        public void Enter()
        {
            CompletedOutcome = 0;
            // Subscribe logic...
        }

        public void Exit()
        {
        }

        public void Tick()
        {
            if(Data.isAuto && !IsCompleted)
            {
                for(int ii = 0; ii< Data.outcomes.Length; ii++)
                {
                    var complete = CheckCompletion(ii);

                    if(complete)
                    {
                        OnStepCompleted(ii);
                        break;
                    }
                }
            }
        }

        public bool CheckFailure()
        {
            return false;
        }

        public bool CheckCompletion(int index)
        {
            var outcome = Data.outcomes[index];
            bool allFinish = true;
            foreach (var needObj in outcome.NeedObjectiveIds)
            {
                objectiveMap.TryGetValue(needObj, out var objectRuntime);
                if(objectRuntime == null)
                {
                    allFinish = false;
                    break;
                }

                if(objectRuntime.GetCurrProgress() < objectRuntime.GetRequireProgress())
                {
                    allFinish = false;
                    break;
                }

            }
            return allFinish;
        }

        public void OnStepCompleted(int index)
        {
            IsCompleted = true;
            CompletedOutcome = index;
        }
    }

    public class QuestInstance
    {

        protected GameLogicManager ctx { get; set; }

        public QuestData Data { get; private set; }
        public bool IsActive { get; private set; }

        
        // 当前活跃的步骤
        private StepRuntime _activeStep;

        // 步骤查找表
        private Dictionary<int, QuestStepData> _stepMap = new();

        // --- 内部标签集 (Internal Tags) ---
        // 这是子系统交互的关键
        private HashSet<string> _internalTags = new HashSet<string>();

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ctx"></param>
        public QuestInstance(QuestData data, GameLogicManager ctx)
        {
            this.ctx = ctx;

            Data = data;
            _stepMap = data.BuildStepMap();


            foreach(var step in Data.steps)
            {
                _stepMap[step.stepId] = step;
            }

            _stepMap.TryGetValue(Data.InitStepId, out var initStep);
            if(initStep == null)
            {
                Debug.LogError($"QuestInstance init fail no init step found {data.QuestId} {Data.InitStepId}");
                return;
            }

            var runtime = new StepRuntime(initStep, ctx);
            _activeStep = runtime;

            IsActive = true;

        }

        public void CheckComplete()
        {
            if(_activeStep != null)
            {
                for (int outcomeIdx = 0; outcomeIdx < _activeStep.Data.outcomes.Length; outcomeIdx++)
                {
                    var complete = _activeStep.CheckCompletion(outcomeIdx);

                    if (complete)
                    {
                        _activeStep.OnStepCompleted(outcomeIdx);
                        break;
                    }
                }
            }
        }

        // 标签操作 API
        private void AddInternalTag(string tag)
        {
            if (!_internalTags.Contains(tag))
            {
                _internalTags.Add(tag);
                Debug.Log($"[QuestInstance] Tag Added: {tag}");
            }
        }

        public bool HasTag(string tag) => _internalTags.Contains(tag);

        // 主循环
        public void LateTick()
        {
            if (!IsActive) return;


            // 2. 处理失败
            if(_activeStep != null && _activeStep.IsCompleted)
            {
                ResolveNextSteps();
            }

            //if (_activeSteps.Count == 0)
            //{
            //    Debug.Log("Quest Fully Completed!");
            //    IsActive = false;
            //}
        }

        private void ResolveNextSteps()
        {
            var outcomeIdx = _activeStep.CompletedOutcome;
            var outcome = _activeStep.Data.outcomes[outcomeIdx];

            if (outcome == null || outcome.nextStepId == 0)
            {
                Debug.LogError($"ResolveNextSteps not outcome found quest invalid");
                return;
            }

            if (_stepMap.TryGetValue(outcome.nextStepId, out var nextData))
            {
                var nextRuntime = new StepRuntime(nextData, ctx);
                _activeStep = nextRuntime;
            }
        }

        /// <summary>
        /// 这里维护监听结构
        /// </summary>
        public void OnLogicEvent()
        {
            if (_activeStep != null && !_activeStep.IsCompleted)
            {
                //for (int ii = 0; ii < Data.outcomes.Length; ii++)
                //{
                //    var complete = CheckCompletion(ii);

                //    if (complete)
                //    {
                //        OnStepCompleted(ii);
                //        break;
                //    }
                //}
            }
        }
    }


    public class PlayerQuestSystem : IPlayerSystem
    {
        protected GameLogicManager Ctx { get; private set; }

        private Dictionary<int, QuestInstance> _questInfoMap = new();
        private HashSet<int> _finishQuestSet;

        public void InitQuestSystem(SaveData savingData)
        {
            TryRefreshQuest();
        }

        /// <summary>
        /// 清理过期
        /// 
        /// </summary>
        public void TryRefreshQuest()
        {
            //
            //_questInfoMap.Add
        }

        public QuestInstance GetQuest(int questId)
        {
            _questInfoMap.TryGetValue(questId, out var result);
            return result;
        }

        private QuestInstance CreateQuestInstanceFromCfg(QuestData cfg)
        {
            return new QuestInstance(cfg, Ctx);
        }

        public bool CheckQuestFinish(int questId)
        {
            return false;
        }

        public bool CheckQuestRunning(int questId)
        {
            return false;
        }
    }

}