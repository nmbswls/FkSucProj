
using cfg;
using My.Config;
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
        public readonly cfg.demo.QuestStepObjective Data;
        public long ProgressVal = 0;

        public ObjectiveRuntime(cfg.demo.QuestStepObjective data, GameLogicManager ctx)
        {
            Data = data;
            this.ctx = ctx;
        }

        public bool IsOptional()
        {
            return Data.IsOption;
        }

        public long GetRequireProgress()
        {
            return 1;
        }

        public long GetCurrProgress()
        {
            //switch (Data.condition.ConditionCfg)
            //{
            //    case QuestConditionHasItem hasItemCond:
            //        {
            //            return ProgressVal;
            //        }
            //        break;
            //}

            return ProgressVal;
        }
    }


    public class StepRuntime
    {
        protected GameLogicManager ctx { get; set; }
        public string CurrStepId = string.Empty;
        public readonly cfg.demo.QuestStepData CacheStepCfg;

        private readonly ObjectiveRuntime[] _objectiveRuntimes;
        private Dictionary<string, ObjectiveRuntime> objectiveMap = new();

        public bool IsCompleted { get; set; }
        public int CompletedOutcome { get; private set; }

        public StepRuntime(cfg.demo.QuestStepData data, GameLogicManager ctx)
        {
            CurrStepId = data.StepId;
            CacheStepCfg = data;

            // 初始化 Outcomes
            _objectiveRuntimes = new ObjectiveRuntime[data.CfgObjectives.Count];
            for (int i = 0; i < _objectiveRuntimes.Length; i++)
            {
                _objectiveRuntimes[i] = new ObjectiveRuntime(data.CfgObjectives[i], ctx);

                objectiveMap[_objectiveRuntimes[i].Data.ObjId] = _objectiveRuntimes[i];
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
            if(CacheStepCfg.AutoNext && !IsCompleted)
            {
                for(int ii = 0; ii< CacheStepCfg.CfgOutcomes.Count; ii++)
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
            if(index < 0 || index >= CacheStepCfg.CfgOutcomes.Count)
            {
                return false;
            }
            var outcome = CacheStepCfg.CfgOutcomes[index];
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

        public cfg.demo.QuestData cacheCfg { get; private set; }
        public bool IsActive { get; private set; }

        
        // 当前活跃的步骤
        private StepRuntime _activeStep;

        // --- 内部标签集 (Internal Tags) ---
        // 这是子系统交互的关键
        private HashSet<string> _internalTags = new HashSet<string>();

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ctx"></param>
        public QuestInstance(cfg.demo.QuestData data, GameLogicManager ctx)
        {
            this.ctx = ctx;

            cacheCfg = data;

            var initStep = cacheCfg.GetStep(cacheCfg.InitStepId);
            if(initStep == null)
            {
                Debug.LogError($"QuestInstance init fail no init step found quest:{data.QuestId} {cacheCfg.InitStepId}");
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
                for (int outcomeIdx = 0; outcomeIdx < _activeStep.CacheStepCfg.Outcomes.Count; outcomeIdx++)
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
                _activeStep.Tick();
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
            var outcome = _activeStep.CacheStepCfg.CfgOutcomes[outcomeIdx];

            if (outcome == null || string.IsNullOrEmpty(outcome.NextStepId))
            {
                Debug.LogError($"ResolveNextSteps not outcome found quest invalid");
                return;
            }
            var nextStep = cacheCfg.GetStep(outcome.NextStepId);
            if (nextStep != null)
            {
                var nextRuntime = new StepRuntime(nextStep, ctx);
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

        private List<cfg.demo.QuestData> _autoAcceptQuests = new();


        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.Ctx = ctx;

            TryRefreshQuest();

            foreach(var q in CfgMgr.Cfgs.TbQuestData.DataList)
            {
                if(!q.IsAutoAccept)
                {
                    continue;
                }

                if(_finishQuestSet.Contains(q.QuestId))
                {
                    continue;
                }

                if(_questInfoMap.ContainsKey(q.QuestId))
                {
                    continue;
                }
                _autoAcceptQuests.Add(q);
            }
        }

        public void Tick(float dt)
        {
            TickAutoAccept();

            foreach(var quest in _questInfoMap.Values)
            {
                quest.LateTick();
            }
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

        public void TickAutoAccept()
        {
            for(int i = _autoAcceptQuests.Count; i>=0; i--)
            {
                var quest = _autoAcceptQuests[i];
                bool allPassed = true;
                foreach (var cond in quest.AcceeptCond)
                {
                    if (!Ctx.CheckCommonCond(cond))
                    {
                        allPassed = false;
                        break;
                    }
                }
                if (allPassed)
                {
                    AcceptQuest(quest);
                    _autoAcceptQuests.Remove(quest);
                }
            }
        }

        public QuestInstance GetQuest(int questId)
        {
            _questInfoMap.TryGetValue(questId, out var result);
            return result;
        }

        public void AcceptQuest(cfg.demo.QuestData cfg)
        {
            var questInst = new QuestInstance(cfg, Ctx);
            _questInfoMap[cfg.QuestId] = questInst;
        }


        public bool CheckQuestFinish(int questId)
        {
            return _finishQuestSet.Contains(questId);
        }

        public bool CheckQuestRunning(int questId)
        {
            return _questInfoMap.ContainsKey(questId);
        }

        public void InitSystem(GameLogicManager ctx)
        {
            throw new NotImplementedException();
        }
    }

}