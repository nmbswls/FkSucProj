
using cfg;
using Map.Logic.Events;
using My.Config;
using My.Map;
using My.Player;
using My.Quest;
using My.Saving;
using SuperScrollView;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.Texture2D;

namespace My.Player
{

    // --- 步骤运行时 ---

    
    /// <summary>
    /// 单个目标
    /// 冗余装载配置中的数据
    /// </summary>
    public class QuestObjectiveRuntime
    {
        protected PlayerQuestSystem ctx { get; set; }
        public readonly cfg.demo.QuestStepObjective Data;
        public long ProgressVal = 0;

        public QuestObjectiveRuntime(cfg.demo.QuestStepObjective data, PlayerQuestSystem ctx)
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
            return Data.ObjProgress;
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

        public void OnLogicEvent(IMapLogicEvent evt)
        {

        }
    }


    public class StepRuntime
    {
        protected PlayerQuestSystem ctx { get; set; }
        public string CurrStepId = string.Empty;
        public readonly cfg.demo.QuestStepData CacheStepCfg;

        private readonly QuestObjectiveRuntime[] _objectiveRuntimes;
        public QuestObjectiveRuntime[] ObjectiveRuntimes { get { return _objectiveRuntimes; } }

        public Dictionary<string, QuestObjectiveRuntime> objectiveMap = new();

        public bool IsCompleted { get; set; }
        public string CompletedOutcomeId { get; private set; }
        public List<string> CompletedOptions { get; private set; } = null;

        private float _tickNextTimer;

        public StepRuntime(cfg.demo.QuestStepData data, PlayerQuestSystem ctx)
        {
            CurrStepId = data.StepId;
            CacheStepCfg = data;

            // 初始化 Outcomes
            _objectiveRuntimes = new QuestObjectiveRuntime[data.CfgObjectives.Count];
            for (int i = 0; i < _objectiveRuntimes.Length; i++)
            {
                _objectiveRuntimes[i] = new QuestObjectiveRuntime(data.CfgObjectives[i], ctx);

                objectiveMap[_objectiveRuntimes[i].Data.ObjId] = _objectiveRuntimes[i];
            }

            this.ctx = ctx;
        }

        public void Enter()
        {
            CompletedOutcomeId = string.Empty;
        }

        public void Exit()
        {
        }

        //public void Tick()
        //{
        //    if(!CacheStepCfg.AutoNext )
        //    {
        //        return;
        //    }

        //    if(LogicTime.time < _tickNextTimer + 0.5f)
        //    {
        //        return;
        //    }

        //    _tickNextTimer = LogicTime.time;

        //    if(CheckCompletion(out var outcomeId))
        //    {
        //        OnStepCompleted(outcomeId);
        //    }
        //}

        
        public bool CheckCompletion(out string outcomeId, out List<string> options)
        {
            outcomeId = null;
            options = null;
            for (int outcomeIdx = 0; outcomeIdx < CacheStepCfg.Outcomes.Count; outcomeIdx++)
            {
                var outcomeCfg = CacheStepCfg.CfgOutcomes[outcomeIdx];
                if(outcomeCfg == null)
                {
                    continue;
                }
                options = null;

                bool allFinish = true;
                foreach (var needObjId in outcomeCfg.NeedObjectiveIds)
                {
                    objectiveMap.TryGetValue(needObjId, out var objectRuntime);
                    if (objectRuntime == null)
                    {
                        allFinish = false;
                        break;
                    }

                    bool progressReach = objectRuntime.GetCurrProgress() >= objectRuntime.GetRequireProgress();

                    if(progressReach && objectRuntime.IsOptional())
                    {
                        if (options == null) options = new();
                        options.Add(needObjId);
                    }
                    
                    if (!objectRuntime.IsOptional() && !progressReach)
                    {
                        allFinish = false;
                        break;
                    }
                }

                // 检查是否完成
                if (allFinish)
                {
                    //_activeStep.OnStepCompleted(outcomeIdx);
                    outcomeId = outcomeCfg.OutcomeId;
                    return true;
                }
            }

            
            return false;
        }

        public void OnStepCompleted(string outcomeId, List<string> options = null)
        {
            IsCompleted = true;
            CompletedOutcomeId = outcomeId;
            CompletedOptions = options;

            ctx.RaiseQuestStepUpdateEvent(CacheStepCfg.QuestId);
        }
    }

    public class QuestInstance
    {

        protected PlayerQuestSystem ctx { get; set; }

        public cfg.demo.QuestData cacheCfg { get; private set; }
        public bool IsActive { get; private set; }

        
        // 当前活跃的步骤
        private StepRuntime _activeStep;
        public StepRuntime ActiveStep { get { return _activeStep; } }

        // --- 内部标签集 (Internal Tags) ---
        // 这是子系统交互的关键
        private HashSet<string> _internalTags = new HashSet<string>();

        public bool ErrFlag;
        public bool FailFlag;

        private List<EPlayerEventType> _currentListernTypes = new();

        /// <summary>
        /// 创建任务
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ctx"></param>
        public QuestInstance(cfg.demo.QuestData data, PlayerQuestSystem ctx)
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

            RefreshEventListener();
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

        private float _autoStepTimer = 0;

        // 主循环
        public void LateTick()
        {
            if (!IsActive) return;

            if(_activeStep != null)
            {
                if(LogicTime.time > _autoStepTimer + 0.5f)
                {
                    if (_activeStep.CacheStepCfg.AutoNext)
                    {
                        if (_activeStep.CheckCompletion(out string outcomeId, out var options))
                        {
                            _activeStep.OnStepCompleted(outcomeId, options);
                        }
                    }
                    _autoStepTimer = LogicTime.time;
                }
            }

            if(_activeStep != null)
            {
                if(_activeStep.IsCompleted)
                {
                    ResolveNextSteps();
                    RefreshEventListener();
                }
            }
            else
            {
                Debug.Log("Quest Fully Completed!");
                IsActive = false;
            }
        }

        /// <summary>
        /// 处理下一步
        /// </summary>
        private void ResolveNextSteps()
        {
            var outcomeId = _activeStep.CompletedOutcomeId;
            if(string.IsNullOrEmpty(outcomeId))
            {
                _activeStep = null;
                ErrFlag = true;
                Debug.Log("ResolveNextSteps no outcome confirmed!");
                return;
            }

            var outcomeCfg = _activeStep.CacheStepCfg.CfgOutcomes.Find(item=>item.OutcomeId == outcomeId);
            if (outcomeCfg == null)
            {
                _activeStep = null;
                ErrFlag = true;
                Debug.Log("ResolveNextSteps no outcome confirmed!");
                return;
            }

            if(_activeStep.CompletedOptions != null)
            {
                foreach(var oneOptionId in _activeStep.CompletedOptions)
                {
                    _activeStep.objectiveMap.TryGetValue(oneOptionId, out var objRuntime);
                    if(objRuntime != null && !string.IsNullOrEmpty(objRuntime.Data.CompletionTag))
                    {
                        AddInternalTag(objRuntime.Data.CompletionTag);
                    }
                }
            }

            if(outcomeCfg.IsFinal)
            {
                Debug.Log($"ResolveNextSteps final step");
                _activeStep = null;
                if(outcomeCfg.IsFail)
                {
                    FailFlag = true;
                }
                OnQuestComplete();
            }
            else
            {
                var nextStep = cacheCfg.GetStep(outcomeCfg.NextStepId);
                if (nextStep != null)
                {
                    var nextRuntime = new StepRuntime(nextStep, ctx);
                    _activeStep = nextRuntime;
                }
                else
                {
                    _activeStep = null;
                    ErrFlag = true;
                    Debug.Log($"ResolveNextSteps no outcome next step:{outcomeCfg.NextStepId}!");
                }
            }
        }

        /// <summary>
        /// 处理任务完成
        /// </summary>
        public void OnQuestComplete()
        {
            if(cacheCfg.FinishReward != null)
            {
                foreach (var pair in cacheCfg.FinishReward)
                {
                    ctx.Ctx.playerDataManager.TryGiveItem(pair.Key, pair.Value, 0);
                }
            }
        }
        

        public void RefreshEventListener()
        {
            if(_currentListernTypes.Count != 0)
            {
                foreach(var e in _currentListernTypes)
                {
                    ctx.EventRouter.TryGetValue(e, out var map);
                    map.Remove(cacheCfg.QuestId);
                }

                _currentListernTypes.Clear();
            }

            if(_activeStep != null)
            {
                foreach(var obj in _activeStep.objectiveMap.Values)
                {
                    EPlayerEventType eType = EPlayerEventType.Inlivad;
                    switch (obj.Data.ObjType)
                    {
                        case cfg.demo.EQuestObjectiveType.KillMonster:
                            {
                                eType = EPlayerEventType.PlayerKillUnit;
                            }
                            break;
                        case cfg.demo.EQuestObjectiveType.OwnItem:
                            {
                                eType = EPlayerEventType.ItemChange;
                            }
                            break;
                        case cfg.demo.EQuestObjectiveType.PlayerKilled:
                            {
                                eType = EPlayerEventType.PlayerKilled;
                            }
                            break;
                    }

                    if(eType == EPlayerEventType.Inlivad)
                    {
                        continue;
                    }

                    if(!ctx.EventRouter.TryGetValue(eType, out var map))
                    {
                        map = new();
                        ctx.EventRouter[eType] = map;
                    }

                    map[cacheCfg.QuestId] = this;
                }
            }
            
        }

        #region 监听

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnPlayerKillUnit(PlayerKillUnitEvent e)
        {
            if(_activeStep == null)
            {
                return;
            }
            bool updated = false;
            foreach(var obj in _activeStep.objectiveMap.Values)
            {
                if(obj.Data.ObjType == cfg.demo.EQuestObjectiveType.KillMonster)
                {
                    obj.ProgressVal += 1;
                    updated = true;
                }
            }

            if(updated)
            {
                ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);

                if (_activeStep.CacheStepCfg.AutoNext)
                {
                    if (_activeStep.CheckCompletion(out string outcomeId, out var options))
                    {
                        _activeStep.OnStepCompleted(outcomeId, options);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnPlayerKilled(PlayerKilledEvent e)
        {
            if (_activeStep == null)
            {
                return;
            }

            bool updated = false;

            foreach (var obj in _activeStep.objectiveMap.Values)
            {
                if (obj.Data.ObjType == cfg.demo.EQuestObjectiveType.PlayerKilled)
                {
                    obj.ProgressVal += 1;
                    updated = true;
                }
            }

            if (updated)
            {
                ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);

                if (_activeStep.CacheStepCfg.AutoNext)
                {
                    if (_activeStep.CheckCompletion(out string outcomeId, out var options))
                    {
                        _activeStep.OnStepCompleted(outcomeId, options);
                    }
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        public void OnPlayerItemChange(PlayerItemChangeEvent e)
        {

        }



        #endregion
    }


    public class PlayerQuestSystem : IPlayerSystem
    {
        public GameLogicManager Ctx { get; private set; }

        private Dictionary<int, QuestInstance> _questInfoMap = new();
        private HashSet<int> _finishQuestSet = new();

        private List<cfg.demo.QuestData> _autoAcceptQuests = new();

        public int MarkQuestId = 0;

        private Dictionary<EPlayerEventType, Dictionary<int, QuestInstance>> _eventRouter = new();
        public Dictionary<EPlayerEventType, Dictionary<int, QuestInstance>> EventRouter { get { return _eventRouter; } }


        public event Action<int> EventOnQuestObjUpdate;
        public event Action<int> EventOnQuestStepUpdate;
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

            RegisterPlayerEvents();

            if(MarkQuestId == 0)
            {
                if(_questInfoMap.Count > 0)
                {
                    var firstQuest = _questInfoMap.First().Value;
                    MarkQuestId = firstQuest.cacheCfg.QuestId;
                }
            }
        }

        /// <summary>
        /// 注册player事件
        /// </summary>
        public void RegisterPlayerEvents()
        {
            PlayerEventBus.Subscribe<PlayerKillUnitEvent>(OnPlayerKillUnit);
            PlayerEventBus.Subscribe<PlayerKilledEvent>(OnPlayerKilled);
        }

        private List<int> _removedQuests = new();

        public void Tick(float dt)
        {
            TickAutoAccept();

            _removedQuests.Clear();
            foreach (var quest in _questInfoMap.Values)
            {
                quest.LateTick();
                if(!quest.IsActive)
                {
                    _finishQuestSet.Add(quest.cacheCfg.QuestId);
                    _removedQuests.Add(quest.cacheCfg.QuestId);
                }
            }

            foreach(var removedId in _removedQuests)
            {
                _questInfoMap.Remove(removedId);

                if(MarkQuestId == removedId)
                {
                    MarkQuestId = 0;
                }
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
            for(int i = _autoAcceptQuests.Count-1; i>=0; i--)
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
            var questInst = new QuestInstance(cfg, this);
            _questInfoMap[cfg.QuestId] = questInst;

            if (MarkQuestId == 0)
            {
                if (_questInfoMap.Count > 0)
                {
                    var firstQuest = _questInfoMap.First().Value;
                    MarkQuestId = firstQuest.cacheCfg.QuestId;
                }
            }
        }


        public bool CheckQuestFinish(int questId)
        {
            return _finishQuestSet.Contains(questId);
        }

        public bool CheckQuestRunning(int questId)
        {
            return _questInfoMap.ContainsKey(questId);
        }

        public void RaiseQuestObjUpdateEvent(int questId)
        {
            EventOnQuestObjUpdate?.Invoke(questId);
        }
        public void RaiseQuestStepUpdateEvent(int questId)
        {
            EventOnQuestStepUpdate?.Invoke(questId);
        }

        #region 监听

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void OnPlayerKillUnit(PlayerKillUnitEvent e)
        {
            var eType = e.EventType;

            if(!EventRouter.TryGetValue(eType, out var listeners))
            {
                return;
            }
            foreach(var q in listeners.Values)
            {
                q.OnPlayerKillUnit(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        private void OnPlayerKilled(PlayerKilledEvent e)
        {
            var eType = e.EventType;

            if (!EventRouter.TryGetValue(eType, out var listeners))
            {
                return;
            }
            foreach (var q in listeners.Values)
            {
                q.OnPlayerKilled(e);
            }
        }

        #endregion

    }

}