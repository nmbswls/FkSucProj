
using cfg;
using Map.Logic.Events;
using My.Cfg_Ex;
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

    public class QuestFulfillOption
    {
        public int QuestId;
        public string ObjId;
        public string DisplayName;
        public string ItemId;
        public long NeedCount;
    }

    public class QuestAcceptOption
    {
        public int QuestId;
        public string QuestName;
    }

    // --- 步骤运行时 ---

    
    /// <summary>
    /// 单个目标
    /// 冗余装载配置中的数据
    /// </summary>
    public class QuestObjectiveRuntime
    {
        protected PlayerQuestSystem ctx { get; set; }
        public QuestInstance OwnerQuest { get; private set; }
        public readonly cfg.demo.QuestStepObjective Data;
        public long ProgressVal = 0;

        public QuestObjectiveRuntime(cfg.demo.QuestStepObjective data, PlayerQuestSystem ctx, QuestInstance ownerQuest)
        {
            Data = data;
            this.ctx = ctx;
            OwnerQuest = ownerQuest;
        }

        // IsOption：可选目标。不挡出口完成；达成时记入 CompletedOptions，供 UI / completion_tag。
        // IsHidden：对玩家隐藏的条件目标（如暗失败条件）；不参与任务板展示。二者正交，勿混用。
        public bool IsOptional()
        {
            return Data.IsOption;
        }

        public bool IsHidden()
        {
            return Data.IsHidden;
        }

        public long GetRequireProgress()
        {
            return Data.ObjProgress;
        }

        public long GetCurrProgress()
        {
            if (Data.ObjType == cfg.demo.EQuestObjectiveType.CharacterDream)
            {
                return GetCharacterDreamProgress();
            }

            switch (Data.ObjType)
            {
                case cfg.demo.EQuestObjectiveType.Talk:
                    return GetTalkProgressFromTriggeredDialogs();
                case cfg.demo.EQuestObjectiveType.OwnItem:
                    {
                        var itemId = Data.ObjP4;
                        if (string.IsNullOrEmpty(itemId) || ctx?.Ctx?.playerDataManager?.InventorySystem == null)
                        {
                            return ProgressVal;
                        }

                        var owned = ctx.Ctx.playerDataManager.InventorySystem.GetCarriedItemTotal(itemId);
                        return Math.Min(owned, Data.ObjProgress);
                    }
                case cfg.demo.EQuestObjectiveType.SubmitItem:
                    return ProgressVal;
                default:
                    return ProgressVal;
            }
        }

        // Talk：只读任务实例上按 obj_id 记的触发次数（Mark 时写入，不查 Dialog 表）
        long GetTalkProgressFromTriggeredDialogs()
        {
            if (OwnerQuest == null || string.IsNullOrEmpty(Data.ObjId))
            {
                return 0;
            }

            return Math.Min(OwnerQuest.GetTalkDialogProgress(Data.ObjId), Data.ObjProgress);
        }

        long GetCharacterDreamProgress()
        {
            var psm = ctx?.Ctx?.playerDataManager;
            if (psm == null || string.IsNullOrEmpty(Data.ObjP5) || Data.ObjP0 <= 0)
            {
                return 0;
            }

            if (!Enum.IsDefined(typeof(My.MiniGame.Dream.DreamEntryResultRequirement), Data.ObjP1))
            {
                return 0;
            }

            var requirement = (My.MiniGame.Dream.DreamEntryResultRequirement)Data.ObjP1;
            var count = psm.GetDreamEntryResultCount(Data.ObjP5, Data.ObjP0, requirement);
            return Math.Min(count, Data.ObjProgress);
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

        public StepRuntime(cfg.demo.QuestStepData data, PlayerQuestSystem ctx, QuestInstance ownerQuest)
        {
            CurrStepId = data.StepId;
            CacheStepCfg = data;

            // 初始化 Outcomes
            _objectiveRuntimes = new QuestObjectiveRuntime[data.CfgObjectives.Count];
            for (int i = 0; i < _objectiveRuntimes.Length; i++)
            {
                _objectiveRuntimes[i] = new QuestObjectiveRuntime(data.CfgObjectives[i], ctx, ownerQuest);

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

        
        // 出口匹配：按 Outcomes 表序取第一个「所需非可选目标均达成」的 outcome。
        // IsOption 目标：未达成不挡出口；已达成则收集进 options。IsHidden 不影响本匹配（仅展示/失败叙事用）。
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

        // 已触发的 ObjectiveDialog 行 id（once / Remind 门闩）
        private HashSet<int> _triggeredObjectiveDialogIds = new HashSet<int>();
        // Talk 进度：obj_id → 已计入的触发次数（仅 Mark 时增加）
        private Dictionary<string, int> _talkDialogProgressByObjId = new Dictionary<string, int>();

        public IReadOnlyCollection<string> InternalTags => _internalTags;

        public bool ErrFlag;
        public bool SuccessFlag;
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

            var runtime = new StepRuntime(initStep, ctx, this);
            _activeStep = runtime;

            IsActive = true;

            RefreshEventListener();
        }

        // 从存档恢复进行中任务（步骤 + 目标进度 + 内部标签）
        public QuestInstance(cfg.demo.QuestData data, PlayerQuestSystem ctx, ActiveQuestPersist persist)
        {
            this.ctx = ctx;
            cacheCfg = data;

            var stepId = persist != null && !string.IsNullOrEmpty(persist.StepId)
                ? persist.StepId
                : data.InitStepId;
            var stepCfg = cacheCfg.GetStep(stepId);
            if (stepCfg == null)
            {
                Debug.LogError($"[Quest] Restore failed: step not found quest={data.QuestId} step={stepId}");
                ErrFlag = true;
                IsActive = false;
                return;
            }

            _activeStep = new StepRuntime(stepCfg, ctx, this);
            if (persist?.Objectives != null)
            {
                foreach (var objPersist in persist.Objectives)
                {
                    if (objPersist == null || string.IsNullOrEmpty(objPersist.ObjId))
                    {
                        continue;
                    }

                    if (_activeStep.objectiveMap.TryGetValue(objPersist.ObjId, out var runtime))
                    {
                        runtime.ProgressVal = objPersist.ProgressVal;
                    }
                }
            }

            if (persist?.InternalTags != null)
            {
                foreach (var tag in persist.InternalTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        _internalTags.Add(tag);
                    }
                }
            }

            if (persist?.TriggeredObjectiveDialogIds != null)
            {
                foreach (var dialogId in persist.TriggeredObjectiveDialogIds)
                {
                    if (dialogId > 0)
                    {
                        _triggeredObjectiveDialogIds.Add(dialogId);
                    }
                }
            }

            if (persist?.TalkDialogProgress != null)
            {
                foreach (var entry in persist.TalkDialogProgress)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.ObjId) || entry.Count <= 0)
                    {
                        continue;
                    }

                    _talkDialogProgressByObjId[entry.ObjId] = entry.Count;
                }
            }

            // 旧存档只有 Triggered ids、没有 Talk 记账时，按表重建一次（仅加载迁移）
            if (_talkDialogProgressByObjId.Count == 0 && _triggeredObjectiveDialogIds.Count > 0)
            {
                RebuildTalkDialogProgressFromTriggeredIds();
            }

            IsActive = true;
            RefreshEventListener();
        }


        public void ForceComplete()
        {
            _activeStep = null;
            SuccessFlag = true;
            OnQuestComplete();
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

            if(_activeStep == null)
            {
                Debug.Log("Quest Fully Completed or err!");
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

            // 步骤出口副作用（变量等）。Outcome.FinishReward 为步骤出口时机奖励，尚未在此兑现。
            ApplyOutcomeEffects(outcomeCfg);

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
                else
                {
                    SuccessFlag = true;
                }
                OnQuestComplete();
            }
            else
            {
                var nextStep = cacheCfg.GetStep(outcomeCfg.NextStepId);
                if (nextStep != null)
                {
                    var nextRuntime = new StepRuntime(nextStep, ctx, this);
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

        private void ApplyOutcomeEffects(cfg.demo.QuestStepOutcome outcomeCfg)
        {
            if (outcomeCfg?.SetVariables == null || outcomeCfg.SetVariables.Count == 0)
            {
                return;
            }

            var playerData = ctx?.Ctx?.playerDataManager;
            if (playerData == null)
            {
                return;
            }

            var changed = false;
            foreach (var variable in outcomeCfg.SetVariables)
            {
                if (string.IsNullOrWhiteSpace(variable))
                {
                    continue;
                }

                playerData.SetVariable(variable.Trim());
                changed = true;
            }

            if (changed)
            {
                ctx.Ctx.AreaManager?.ForceCheckAllRefreshInfos();
            }
        }

        // 任务终局奖励（QuestData.FinishReward）。与 Outcome.FinishReward（步骤出口时机）是两个发放点。
        public void OnQuestComplete()
        {
            if(cacheCfg.FinishReward != null)
            {
                foreach (var pair in cacheCfg.FinishReward)
                {
                    ctx.Ctx.playerDataManager.GiveItemToPlayer(pair.Key, pair.Value);
                }
            }

            PlayerEventBus.Publish(new PlayerQuestCompleteEvent
            {
                QuestId = cacheCfg.QuestId,
            });
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
                        case cfg.demo.EQuestObjectiveType.InteractEntity:
                            {
                                eType = EPlayerEventType.EntityInteractionCompleted;
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
                    if (!_currentListernTypes.Contains(eType))
                    {
                        _currentListernTypes.Add(eType);
                    }
                }
            }
            
        }

        public void ClearEventListeners()
        {
            if (_currentListernTypes.Count == 0)
            {
                return;
            }

            foreach (var e in _currentListernTypes)
            {
                if (ctx.EventRouter.TryGetValue(e, out var map))
                {
                    map.Remove(cacheCfg.QuestId);
                }
            }

            _currentListernTypes.Clear();
        }

        public bool IsObjectiveDialogTriggered(int dialogId)
        {
            return dialogId > 0 && _triggeredObjectiveDialogIds.Contains(dialogId);
        }

        public int GetTalkDialogProgress(string objId)
        {
            if (string.IsNullOrEmpty(objId))
            {
                return 0;
            }

            return _talkDialogProgressByObjId.TryGetValue(objId, out var count) ? count : 0;
        }

        // dialog 触发作账：记 once，并按 obj_id 增加 Talk 进度（同一 dialogId 只计一次）
        public void MarkObjectiveDialogTriggered(int dialogId, string objId)
        {
            if (dialogId <= 0)
            {
                return;
            }

            if (!_triggeredObjectiveDialogIds.Add(dialogId))
            {
                return;
            }

            if (string.IsNullOrEmpty(objId))
            {
                return;
            }

            _talkDialogProgressByObjId.TryGetValue(objId, out var count);
            _talkDialogProgressByObjId[objId] = count + 1;
        }

        void RebuildTalkDialogProgressFromTriggeredIds()
        {
            var table = CfgMgr.Cfgs?.TbQuestObjectiveDialog?.DataList;
            if (table == null || cacheCfg == null)
            {
                return;
            }

            var questId = cacheCfg.QuestId;
            for (int i = 0; i < table.Count; i++)
            {
                var row = table[i];
                if (row == null || row.QuestId != questId || string.IsNullOrEmpty(row.ObjId))
                {
                    continue;
                }

                if (!_triggeredObjectiveDialogIds.Contains(row.Id))
                {
                    continue;
                }

                _talkDialogProgressByObjId.TryGetValue(row.ObjId, out var count);
                _talkDialogProgressByObjId[row.ObjId] = count + 1;
            }
        }

        public ActiveQuestPersist ToPersist()
        {
            var persist = new ActiveQuestPersist
            {
                QuestId = cacheCfg.QuestId,
                StepId = _activeStep != null ? _activeStep.CurrStepId : string.Empty,
                Objectives = new List<QuestObjectivePersist>(),
                InternalTags = new List<string>(_internalTags),
                TriggeredObjectiveDialogIds = new List<int>(_triggeredObjectiveDialogIds),
                TalkDialogProgress = new List<QuestTalkDialogProgressPersist>(),
            };

            foreach (var pair in _talkDialogProgressByObjId)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0)
                {
                    continue;
                }

                persist.TalkDialogProgress.Add(new QuestTalkDialogProgressPersist
                {
                    ObjId = pair.Key,
                    Count = pair.Value,
                });
            }

            if (_activeStep?.ObjectiveRuntimes != null)
            {
                foreach (var obj in _activeStep.ObjectiveRuntimes)
                {
                    if (obj?.Data == null)
                    {
                        continue;
                    }

                    persist.Objectives.Add(new QuestObjectivePersist
                    {
                        ObjId = obj.Data.ObjId,
                        ProgressVal = obj.ProgressVal,
                    });
                }
            }

            return persist;
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
                    if (!string.IsNullOrEmpty(obj.Data.ObjP4)
                        && !string.Equals(obj.Data.ObjP4, e.KilledCfgId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (obj.Data.ObjP0 != 0 && !e.KilledByPlayer)
                    {
                        continue;
                    }

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
            if (_activeStep == null)
            {
                return;
            }

            bool updated = false;
            foreach (var obj in _activeStep.objectiveMap.Values)
            {
                if (obj.Data.ObjType != cfg.demo.EQuestObjectiveType.OwnItem)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(e.ItemId)
                    && !string.Equals(obj.Data.ObjP4, e.ItemId, StringComparison.Ordinal))
                {
                    continue;
                }

                updated = true;
            }

            if (!updated)
            {
                return;
            }

            ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);

            if (_activeStep.CacheStepCfg.AutoNext)
            {
                if (_activeStep.CheckCompletion(out string outcomeId, out var options))
                {
                    _activeStep.OnStepCompleted(outcomeId, options);
                }
            }
        }

        public bool TryFulfillObjective(string characterKey, string objId, out string failReason)
        {
            failReason = null;
            if (_activeStep == null || string.IsNullOrEmpty(characterKey) || string.IsNullOrEmpty(objId))
            {
                failReason = "invalid_args";
                return false;
            }

            if (!_activeStep.objectiveMap.TryGetValue(objId, out var objRuntime))
            {
                failReason = "no_objective";
                return false;
            }

            if (!QuestObjectiveFulfillUtil.SupportsDialogFulfill(objRuntime.Data.ObjType))
            {
                failReason = "not_fulfillable_type";
                return false;
            }

            switch (objRuntime.Data.ObjType)
            {
                case cfg.demo.EQuestObjectiveType.SubmitItem:
                    if (objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
                    {
                        failReason = "already_done";
                        return false;
                    }

                    return TryFulfillSubmitItem(objRuntime, out failReason);
                case cfg.demo.EQuestObjectiveType.Talk:
                    // Talk：此处只校验 NPC；进度与步骤推进在 Mark 之后由 TryAdvanceAfterObjectiveDialog 处理
                    return TryValidateTalkNpc(characterKey, objRuntime, out failReason);
                default:
                    failReason = "not_fulfillable_type";
                    return false;
            }
        }

        public void OnEntityInteractionCompleted(PlayerEntityInteractionCompletedEvent e)
        {
            if (_activeStep == null)
            {
                return;
            }

            bool updated = false;
            foreach (var obj in _activeStep.objectiveMap.Values)
            {
                if (obj.Data.ObjType != cfg.demo.EQuestObjectiveType.InteractEntity)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(obj.Data.ObjP4)
                    && !string.Equals(obj.Data.ObjP4, e.CfgId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(obj.Data.ObjP5)
                    && !string.Equals(obj.Data.ObjP5, e.UniqName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (obj.Data.ObjP0 > 0 && obj.Data.ObjP0 != e.InteractId)
                {
                    continue;
                }

                obj.ProgressVal += 1;
                updated = true;
            }

            if (!updated)
            {
                return;
            }

            ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);
            if (_activeStep.CacheStepCfg.AutoNext
                && _activeStep.CheckCompletion(out string outcomeId, out var options))
            {
                _activeStep.OnStepCompleted(outcomeId, options);
            }
        }

        private bool TryValidateTalkNpc(string characterKey, QuestObjectiveRuntime objRuntime, out string failReason)
        {
            failReason = null;
            var needCharacterKey = objRuntime.Data.ObjP5;
            if (!string.IsNullOrEmpty(needCharacterKey)
                && !string.Equals(needCharacterKey, characterKey, StringComparison.Ordinal))
            {
                failReason = "wrong_npc";
                return false;
            }

            return true;
        }

        // Mark Talk 进度之后调用：刷 UI 并尝试 AutoNext
        public void TryAdvanceAfterObjectiveDialog()
        {
            if (_activeStep == null)
            {
                return;
            }

            ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);

            if (_activeStep.CacheStepCfg.AutoNext
                && _activeStep.CheckCompletion(out string outcomeId, out var options))
            {
                _activeStep.OnStepCompleted(outcomeId, options);
            }
        }

        private bool TryFulfillSubmitItem(QuestObjectiveRuntime objRuntime, out string failReason)
        {
            failReason = null;
            var itemId = objRuntime.Data.ObjP4;
            var needCount = objRuntime.GetRequireProgress();
            var pdm = ctx.Ctx?.playerDataManager;
            if (pdm == null || string.IsNullOrEmpty(itemId) || needCount <= 0)
            {
                failReason = "bad_cfg";
                return false;
            }

            if (!pdm.CheckHaveItem(itemId, needCount))
            {
                failReason = "not_enough_" + itemId;
                return false;
            }

            if (pdm.CostItem(itemId, needCount) < needCount)
            {
                failReason = "cost_failed";
                return false;
            }

            objRuntime.ProgressVal = needCount;
            ctx.RaiseQuestObjUpdateEvent(cacheCfg.QuestId);

            if (_activeStep.CacheStepCfg.AutoNext)
            {
                if (_activeStep.CheckCompletion(out string outcomeId, out var options))
                {
                    _activeStep.OnStepCompleted(outcomeId, options);
                }
            }

            return true;
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
            _questInfoMap.Clear();
            _finishQuestSet.Clear();
            _autoAcceptQuests.Clear();
            _eventRouter.Clear();
            MarkQuestId = 0;

            LoadFromSave(savingData?.PlayerData);

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

        void LoadFromSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            if (pd.FinishedQuestIds != null)
            {
                foreach (var id in pd.FinishedQuestIds)
                {
                    _finishQuestSet.Add(id);
                }
            }

            if (pd.ActiveQuests != null)
            {
                foreach (var persist in pd.ActiveQuests)
                {
                    if (persist == null || persist.QuestId <= 0)
                    {
                        continue;
                    }

                    if (_finishQuestSet.Contains(persist.QuestId) || _questInfoMap.ContainsKey(persist.QuestId))
                    {
                        continue;
                    }

                    var cfg = CfgMgr.Cfgs?.TbQuestData?.GetOrDefault(persist.QuestId);
                    if (cfg == null)
                    {
                        Debug.LogWarning($"[Quest] Skip restore missing cfg questId={persist.QuestId}");
                        continue;
                    }

                    var inst = new QuestInstance(cfg, this, persist);
                    if (!inst.IsActive)
                    {
                        continue;
                    }

                    _questInfoMap[cfg.QuestId] = inst;
                }
            }

            if (pd.MarkQuestId != 0 && _questInfoMap.ContainsKey(pd.MarkQuestId))
            {
                MarkQuestId = pd.MarkQuestId;
            }
        }

        public void WriteToSave(PlayerData pd)
        {
            if (pd == null)
            {
                return;
            }

            pd.FinishedQuestIds ??= new List<int>();
            pd.FinishedQuestIds.Clear();
            foreach (var id in _finishQuestSet)
            {
                pd.FinishedQuestIds.Add(id);
            }

            pd.ActiveQuests ??= new List<ActiveQuestPersist>();
            pd.ActiveQuests.Clear();
            foreach (var quest in _questInfoMap.Values)
            {
                if (quest == null || !quest.IsActive || quest.cacheCfg == null)
                {
                    continue;
                }

                pd.ActiveQuests.Add(quest.ToPersist());
            }

            pd.MarkQuestId = MarkQuestId;
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        /// <summary>
        /// 注册player事件
        /// </summary>
        public void RegisterPlayerEvents()
        {
            PlayerEventBus.Subscribe<PlayerKillUnitEvent>(OnPlayerKillUnit);
            PlayerEventBus.Subscribe<PlayerKilledEvent>(OnPlayerKilled);
            PlayerEventBus.Subscribe<PlayerItemChangeEvent>(OnPlayerItemChange);
            PlayerEventBus.Subscribe<PlayerEntityInteractionCompletedEvent>(OnEntityInteractionCompleted);
        }

        private List<int> _removedQuests = new();

        /// <summary>
        /// 强制完成
        /// </summary>
        /// <param name="questId"></param>
        public void ForceFinishQuest(int questId)
        {
            if(_questInfoMap.TryGetValue(questId, out var questInst))
            {
                questInst.ForceComplete();
            }
            else
            {
                _finishQuestSet.Add(questId);
            }
        }
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
                if (_questInfoMap.TryGetValue(removedId, out var dying))
                {
                    dying.ClearEventListeners();
                }

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

        // 接取入口：由 TbQuestAcceptDialog.character_key 决定谁能谈；不校验 QuestData.StartNpcId。
        public bool TryAcceptQuestFromNpc(string characterKey, int questId, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(characterKey))
            {
                failReason = "invalid_npc";
                return false;
            }

            var questCfg = CfgMgr.Cfgs.TbQuestData.GetOrDefault(questId);
            if (questCfg == null)
            {
                failReason = "no_cfg";
                return false;
            }

            if (questCfg.IsAutoAccept)
            {
                failReason = "auto_accept_only";
                return false;
            }

            if (_finishQuestSet.Contains(questId) || _questInfoMap.ContainsKey(questId))
            {
                failReason = "already_taken";
                return false;
            }

            foreach (var cond in questCfg.AcceeptCond)
            {
                if (!Ctx.CheckCommonCond(cond))
                {
                    failReason = "accept_cond";
                    return false;
                }
            }

            AcceptQuest(questCfg);
            if (MarkQuestId == 0)
            {
                MarkQuestId = questId;
            }

            return true;
        }

        // 可接列表只扫 TbQuestAcceptDialog × characterKey；QuestData.StartNpcId 不参与。
        public List<QuestAcceptOption> GetAvailableAcceptOptions(string characterKey)
        {
            var result = new List<QuestAcceptOption>();
            if (string.IsNullOrEmpty(characterKey))
            {
                return result;
            }

            foreach (var row in My.Cfg_Ex.QuestDialogResolver.ListAcceptByCharacter(characterKey))
            {
                if (row == null)
                {
                    continue;
                }

                var questCfg = CfgMgr.Cfgs.TbQuestData.GetOrDefault(row.QuestId);
                if (questCfg == null || questCfg.IsAutoAccept)
                {
                    continue;
                }

                if (_finishQuestSet.Contains(questCfg.QuestId) || _questInfoMap.ContainsKey(questCfg.QuestId))
                {
                    continue;
                }

                if (row.OpenCond != null)
                {
                    bool openOk = true;
                    foreach (var cond in row.OpenCond)
                    {
                        if (cond != null && !Ctx.CheckCommonCond(cond))
                        {
                            openOk = false;
                            break;
                        }
                    }

                    if (!openOk)
                    {
                        continue;
                    }
                }

                bool allPassed = true;
                foreach (var cond in questCfg.AcceeptCond)
                {
                    if (!Ctx.CheckCommonCond(cond))
                    {
                        allPassed = false;
                        break;
                    }
                }

                if (!allPassed)
                {
                    continue;
                }

                result.Add(new QuestAcceptOption
                {
                    QuestId = questCfg.QuestId,
                    QuestName = questCfg.Name,
                });
            }

            return result;
        }

        public void MarkObjectiveDialogTriggered(int questId, int objectiveDialogId, string objId)
        {
            if (!_questInfoMap.TryGetValue(questId, out var quest) || quest == null)
            {
                return;
            }

            quest.MarkObjectiveDialogTriggered(objectiveDialogId, objId);
        }

        public void TryAdvanceAfterObjectiveDialog(int questId)
        {
            if (!_questInfoMap.TryGetValue(questId, out var quest) || quest == null)
            {
                return;
            }

            quest.TryAdvanceAfterObjectiveDialog();
        }

        public List<QuestFulfillOption> GetPendingFulfillOptions(string characterKey)
        {
            var result = new List<QuestFulfillOption>();
            if (string.IsNullOrEmpty(characterKey))
            {
                return result;
            }

            foreach (var row in My.Cfg_Ex.QuestDialogResolver.ListObjectiveByCharacter(characterKey))
            {
                if (row == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(row.ObjId))
                {
                    continue;
                }

                if (!_questInfoMap.TryGetValue(row.QuestId, out var quest))
                {
                    continue;
                }

                var step = quest.ActiveStep;
                if (step == null || step.IsCompleted)
                {
                    continue;
                }

                if (!My.Cfg_Ex.QuestDialogShowCondUtil.Passes(row.ShowCond, quest))
                {
                    continue;
                }

                if (!step.objectiveMap.TryGetValue(row.ObjId, out var obj))
                {
                    continue;
                }

                if (row.Once && quest.IsObjectiveDialogTriggered(row.Id))
                {
                    continue;
                }

                if (!QuestObjectiveFulfillUtil.CanPresentDialogFulfill(obj, this))
                {
                    continue;
                }

                var itemId = obj.Data.ObjP4;
                var itemDef = My.Config.ItemCatalog.GetItemDef(itemId);
                var itemName = itemDef?.DisplayName ?? itemId;
                var displayName = obj.Data.ObjType == cfg.demo.EQuestObjectiveType.SubmitItem
                    ? itemName
                    : QuestObjectiveFulfillUtil.GetFulfillOptionFallbackText(obj.Data);
                result.Add(new QuestFulfillOption
                {
                    QuestId = row.QuestId,
                    ObjId = row.ObjId,
                    DisplayName = displayName,
                    ItemId = itemId,
                    NeedCount = obj.GetRequireProgress(),
                });
            }

            return result;
        }

        public bool TryFulfillObjective(string characterKey, int questId, string objId, out string failReason)
        {
            failReason = null;
            if (!_questInfoMap.TryGetValue(questId, out var quest))
            {
                failReason = "quest_not_running";
                return false;
            }

            return quest.TryFulfillObjective(characterKey, objId, out failReason);
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

        private void OnPlayerItemChange(PlayerItemChangeEvent e)
        {
            var eType = EPlayerEventType.ItemChange;

            if (!EventRouter.TryGetValue(eType, out var listeners))
            {
                return;
            }

            foreach (var q in listeners.Values)
            {
                q.OnPlayerItemChange(e);
            }
        }

        private void OnEntityInteractionCompleted(PlayerEntityInteractionCompletedEvent e)
        {
            if (!EventRouter.TryGetValue(e.EventType, out var listeners))
            {
                return;
            }

            foreach (var q in listeners.Values)
            {
                q.OnEntityInteractionCompleted(e);
            }
        }

        #endregion

    }

}
