using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using cfg.demo;
using Cinemachine;
using My;
using My.Dialog;
using My.Map;
using My.Map.Logic;
using My.MapExport;
using My.UI;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;


[Serializable]
public class DialogueRuntime
{
    // 对话运行期上下文：由 DialoguePlayer 填充 UI/镜头/驱动等引用，供数据驱动流程读取
    public DialogueUI ui;
    public PortraitManager portraits;
    public SimpleCameraDirector cam;
    //public AudioBus audio;
    public DialogueTimeDriver driver;

    // 文案本地化：将表 key 转为显示文本；为 null 时直接使用 key
    public Func<string, string> Localize;

    public Action<string> JumpTo;

    public long? SrcEntityId;

    public List<long> ControlledEntityList = new();

    public DialogueSessionContext SessionContext;

}

public partial class DialoguePlayer : MonoBehaviour
{

    public const string NpcDialogHubId = "npc_generic_entry";


    [Header("Refs")]
    public DialogueUI ui;
    public SimpleCameraDirector cam;
    public DialogueTimeDriver driver;

    [Header("Input")]
    public bool AutoMode;
    public bool SkipMode;

    // 播放结束回调（可选）
    private Action? OnPlayEnd;

    private DialogMetaInfo MetaInfo;

    private DialogueData dataRef;
    private DialogueRuntime runtimeRef;

    private int stepIndex;
    private bool isPlaying;

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    // 当前 Step 内正在执行的命令下标
    private int currentCmdIndex;

    // 当前命令是否仍在异步执行（如移动未结束）
    private bool commandRunning;

    // 是否在等待玩家确认继续，或 TypeText 播完后的停顿
    private bool waitingForContinue;

    // 本 Step 的命令已全部跑完，等待进入下一 Step
    private bool waitingForNextStep;

    // 标签到 Step 索引（Jump 用，当前未启用）
    //private readonly Dictionary<string, int> labelToStep = new Dictionary<string, int>();

    // Step 内发生 Jump 后，下一帧重入 StartStepFromData
    private bool pendingJump;

    private string currCutsceneName;
    private GameObject cutsceneRootGo;
    private readonly Dictionary<string, long> dynamicDialogActorIds = new(StringComparer.Ordinal);
    private readonly HashSet<long> dynamicDialogActorDestroyOnEnd = new();

    public PlayableDirector activeDirector; // 当前 Timeline / Cutscene 的 PlayableDirector
    private string waitingSignalName;       // WaitTimelineSignal：等待 Timeline 发出的信号名
    private Action onSignalReceivedCallback;// 收到上述信号后执行的收尾回调

    private void Awake()
    {
        if (!driver)
        {
            driver = gameObject.AddComponent<DialogueTimeDriver>();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        CheckPendingCommandEnd();
        
        // 仅在等待继续或等待下一 Step 时处理自动/跳过推进，避免打断异步命令
        if (!waitingForContinue && !waitingForNextStep) return;

        if (SkipMode)
        {
            DoContinue();
            return;
        }

        if (AutoMode)
        {
            ui.autoTimer += Time.deltaTime;
            if (ui.autoTimer >= ui.autoDelay)
            {
                ui.autoTimer = 0f;
                DoContinue();
            }
            return;
        }
    }


    private void CheckPendingCommandEnd()
    {
        if (dataRef == null)
        {
            return;
        }
        if(!commandRunning)
        {
            return;
        }

        if (stepIndex < 0 || stepIndex >= dataRef.Steps.Count)
        {
            return;
        }
        var step = dataRef.Steps[stepIndex];
        var commands = step.Commands ?? new List<DialogCommandData>();
        
        if(currentCmdIndex >= commands.Count)
        {
            return;
        }
        var cd = commands[currentCmdIndex];
        bool passed = false;
        switch(cd)
        {
            case DialogCommandData4MoveEntity cmd4MoveEntity:
                {
                    var dialogActor = GetDialogueActorByStaticName(cmd4MoveEntity.StaticName);
                    if(dialogActor == null)
                    {
                        passed = true;
                        break;
                    }

                    if(dialogActor.DialogMoveFinished)
                    {
                        passed = true;
                    }
                }
                break;
        }

        if(passed)
        {
            commandRunning = false;
            waitingForContinue = true;
            ui.ShowNextIndicator(true);
            ui.autoTimer = 0f;

            // 异步条件满足后直接推进
            DoContinue();
        }
    }

    private IDialogueActor GetDialogueActorByStaticName(string staticName)
    {
        if (!string.IsNullOrEmpty(staticName) &&
            dynamicDialogActorIds.TryGetValue(staticName, out var dynamicActorId))
        {
            var dynamicEntity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(dynamicActorId, true);
            return dynamicEntity as IDialogueActor;
        }

        var staticId = MainGameManager.Instance.gameLogicManager.AreaManager.GetStaticIdByUniqName(staticName);
        MainGameManager.Instance.gameLogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var refreshInfo);
        if (refreshInfo == null)
        {
            return null;
        }

        var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(refreshInfo.EntityInstId, true);
        if (entity is not IDialogueActor dialogActor)
        {
            return null;
        }

        return dialogActor;
    }

    private void DoContinue()
    {
        if (waitingForContinue)
        {
            waitingForContinue = false;
            ui.ShowNextIndicator(false);
            // 玩家确认后继续执行下一条命令
            currentCmdIndex++;
            ExecuteNextCommandInStep();
        }
        else if (waitingForNextStep)
        {
            waitingForNextStep = false;
            ui.ShowNextIndicator(false);
            stepIndex++;
            StartStepFromData();
        }
    }

    public void Stop()
    {
        if (runtimeRef != null && runtimeRef.SrcEntityId != null)
        {
            var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(runtimeRef.SrcEntityId.Value);
            if (entity != null && entity is BaseUnitLogicEntity unitEntity)
            {
                unitEntity.UnregisterGazeBySourceTag("Dialog");
            }
        }

        OnDialogEnd();

        isPlaying = false;
        waitingForContinue = false;
        waitingForNextStep = false;
        commandRunning = false;
        InputBlocker.Block(false);
        ui.ShowNextIndicator(false);

        waitingSignalName = null;
        onSignalReceivedCallback = null;


        this.OnPlayEnd?.Invoke();
        this.OnPlayEnd = null;
        LogicTime.ClearPauseSource("Dialog");

        MainGameManager.Instance.gameLogicManager.IsDialogPlayering = false;

        // 结束对话：若加载过过场则异步卸载该 Additive 场景
        // 清理 Timeline 引用与信号等待状态
        if (!string.IsNullOrEmpty(currCutsceneName))
        {
            StartCoroutine(UnloadCutsceneSceneRoutine(null));
        }
    }


    public void PlayFromData(DialogMetaInfo metaInfo, DialogueData data, DialogueRuntime runtime, Action? onPlayEnd)
    {
        this.MetaInfo = metaInfo;
        this.dataRef = data;

        runtimeRef = runtime;
        dataRef = data;
        stepIndex = 0;
        isPlaying = true;
        pendingJump = false;
        dynamicDialogActorIds.Clear();
        dynamicDialogActorDestroyOnEnd.Clear();
        //InputBlocker.Block(true);
        StartStepFromData();

        this.OnPlayEnd = onPlayEnd;

        OnDialogStart();
    }

    /// <summary>
    /// 对话开始：注册受控静态实体、强制刷新、可选加载过场
    /// </summary>
    private void OnDialogStart()
    {

        List<int> staticIds = new();
        foreach(var actorName in MetaInfo.ControlledEntityList)
        {
            var staticId = MainGameManager.Instance.gameLogicManager.AreaManager.GetStaticIdByUniqName(actorName);
            MainGameManager.Instance.gameLogicManager.AreaManager.DialogForceStaticIds.Add(staticId);

            staticIds.Add(staticId);
        }

        MainGameManager.Instance.gameLogicManager.AreaManager.ForceCheckRefreshInfos();

        foreach (var staticId in staticIds)
        {
            MainGameManager.Instance.gameLogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var refreshRuntime);
            if(refreshRuntime == null)
            {
                Debug.LogError($"Ensure dialog actor fail. {staticId}");
                continue;
            }

            var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(refreshRuntime.EntityInstId, true);
            if (entity is not IDialogueActor dialogActor)
            {
                Debug.LogError($"Ensure dialog actor not valid. {entity.Id} {entity.Type}");
                continue;
            }

            dialogActor.OnDialogStart();

            runtimeRef.ControlledEntityList.Add(entity.Id);
        }

        MainGameManager.Instance.gameLogicManager.IsDialogPlayering = true;

        if(!string.IsNullOrEmpty(MetaInfo.CutsceneId))
        {

            StartCoroutine(LoadCutsceneSceneRoutine(MetaInfo.CutsceneId, true));
        }
    }

    private void OnDialogEnd()
    {
        if(runtimeRef != null)
        {
            foreach(var eId in runtimeRef.ControlledEntityList)
            {
                var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(eId, true);

                if(entity is not IDialogueActor dialogActor)
                {
                    Debug.LogError("Invalid dialogue actor type.");
                    continue;
                }

                dialogActor.OnDialogEnd();
            }
        }

        foreach (var eId in dynamicDialogActorDestroyOnEnd)
        {
            MainGameManager.Instance.gameLogicManager.AreaManager.ForceDestroyEntityNow(eId, "DialogActorEnd");
        }
        dynamicDialogActorDestroyOnEnd.Clear();
        dynamicDialogActorIds.Clear();

        MainGameManager.Instance.gameLogicManager.playerDataManager.DialogTriggerSystem.AddTriggerCount(MetaInfo.DialogId);
    }

    public void JumpToStep(string stepId)
    {
        var stepIdx = dataRef.Steps.FindIndex((item) => item.Id == stepId);
        if(stepIdx == -1)
        {
            //Debug.LogWarning($"JumpToStep fail: {stepId}");
            this.stepIndex = -1;
            this.pendingJump = true;
            return;
        }

        this.stepIndex = stepIdx;
        this.pendingJump = true;
    }

    // 解析选项要切换到的对话 id：TargetDialogId 字面量 / 占位符
    private string ResolveChoiceTargetDialogId(DialogChoiceOption choice)
    {
        if (choice == null)
            return null;

        var raw = choice.TargetDialogId != null ? choice.TargetDialogId.Trim() : "";
        if (raw.Length == 0)
            return null;

        if (string.Equals(raw, DialogChoicePlaceholders.NpcResolvedPeace, StringComparison.OrdinalIgnoreCase))
            return ResolveNpcPeaceDialogIdForSrcNpc();
        return raw;
    }

    private string ResolveNpcPeaceDialogIdForSrcNpc()
    {
        var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
        if (glm == null || runtimeRef?.SrcEntityId == null)
        {
            Debug.LogWarning("[Dialog] ResolveNpcPeaceDialogId: missing manager or SrcEntityId.");
            return null;
        }

        var ent = glm.GetLogicEntity(runtimeRef.SrcEntityId.Value, false);
        if (ent is not NpcUnitLogicEntity npc)
        {
            Debug.LogWarning("[Dialog] ResolveNpcPeaceDialogId: SrcEntity is not NPC.");
            return null;
        }
        
        return NpcDialogHubId;
    }

    // 选项要求切换对话资源时：结束当前对话并 PlayDialog
    private bool TryPlayChoiceTargetDialog(DialogChoiceOption choice)
    {
        var targetDialogId = ResolveChoiceTargetDialogId(choice);
        if (string.IsNullOrEmpty(targetDialogId))
            return false;

        if (MetaInfo != null && string.Equals(targetDialogId, MetaInfo.DialogId, StringComparison.Ordinal))
        {
            Debug.LogWarning("[Dialog] TryPlayChoiceTargetDialog: target equals current dialog, skip.");
            return false;
        }

        var mgr = MainGameManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[Dialog] TryPlayChoiceTargetDialog: MainGameManager missing.");
            return false;
        }

        var srcId = runtimeRef?.SrcEntityId;
        ui?.PrepareForDialogSegmentSwitch();
        Stop();
        mgr.PlayDialog(targetDialogId, srcId, pause: false, onDialogEnd: null, sessionContext: runtimeRef?.SessionContext);
        return true;
    }

    private DialogueSessionContext BuildQuestActionSession(DialogCommandData4QuestAction cmd)
    {
        if (cmd == null)
        {
            return null;
        }

        var session = runtimeRef?.SessionContext;
        if (session != null)
        {
            return session;
        }

        var characterKey = ResolveSrcNpcCharacterKey();
        if (string.IsNullOrEmpty(characterKey))
        {
            return null;
        }

        return cmd.QuestAction switch
        {
            EDialogueQuestAction.Accept => QuestDialogSession.CreateAccept(
                cmd.QuestId > 0 ? cmd.QuestId : 0,
                characterKey,
                ""),
            EDialogueQuestAction.Fulfill => QuestDialogSession.CreateFulfill(
                cmd.QuestId > 0 ? cmd.QuestId : 0,
                cmd.ObjId,
                characterKey,
                ""),
            _ => null,
        };
    }

    private string ResolveSrcNpcCharacterKey()
    {
        var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
        if (glm == null || runtimeRef?.SrcEntityId == null)
        {
            return null;
        }

        var ent = glm.GetLogicEntity(runtimeRef.SrcEntityId.Value, false);
        if (ent is not NpcUnitLogicEntity npc)
        {
            return null;
        }

        return npc.NpcRecord?.CharacterKey;
    }

    private void StartStepFromData()
    {
        if (dataRef == null || stepIndex < 0 || stepIndex >= dataRef.Steps.Count)
        {
            Stop();
            return;
        }

        pendingJump = false;
        waitingForContinue = false;
        waitingForNextStep = false;
        commandRunning = false;
        ui.ShowNextIndicator(false);
        ui.autoTimer = 0f;

        currentCmdIndex = 0;
        ExecuteNextCommandInStep();
    }

    private void ExecuteNextCommandInStep()
    {
        if (pendingJump)
        {
            // 存在待处理 Jump：从目标 Step 重新开始
            StartStepFromData();
            return;
        }

        var step = dataRef.Steps[stepIndex];
        var commands = step.Commands ?? new List<DialogCommandData>();

        // 本 Step 内命令已全部执行完，进入「等待下一 Step」
        if (currentCmdIndex >= commands.Count)
        {
            EnterWaitForNextStep();
            return;
        }

        var cd = commands[currentCmdIndex];

        // 开始执行当前下标对应的命令
        commandRunning = true;
        waitingForContinue = false;
        waitingForNextStep = false;

        // 按命令类型分发执行
        ExecuteDataCommand(cd);
    }

    private void ExecuteDataCommand(DialogCommandData cd)
    {
        // 统一收尾：禁用时立即回调；否则经时间驱动延迟一帧，避免与 UI 协程抢同一帧
        void SafeComplete()
        {
            if (!isActiveAndEnabled)
            {
                CommandCompletedFromData(cd);
                return;
            }
            // 通过 DialogueTimeDriver 延迟执行 CommandCompletedFromData，给 UI 一帧机会
            driver.Run(0f, _ => { }, () => CommandCompletedFromData(cd));
        }

        switch (cd)
        {
            case DialogCommandData4Text cd4Text:
                {
                    //string name = cd4Text.Speaker;
                    //string text = cd4Text.Content;
                    var textLines = cd4Text.TextLines;
                    // TypeText 播完后进入等待继续，由 Update 里 DoContinue 再推进
                    ui.StartTypeTextBatch(textLines, () =>
                    {
                        commandRunning = false;
                        waitingForContinue = true;
                        ui.ShowNextIndicator(true);
                        ui.autoTimer = 0f;

                        // 同上：进入等待继续
                        DoContinue();
                    });
                    break;
                }
            case DialogCommandData4BranchText cd4BranchText:
                {
                    // 分支文本：选项确定后播对应行，目标 Step 由 JumpToStep 处理
                    ui.StartChoices(cd4BranchText.SimpleBranch, index =>
                    {
                        if (index >= 0 && index < cd4BranchText.SimpleTextLines.Count)
                        {
                            var textLines = cd4BranchText.SimpleTextLines[index];
                            // 分支内 TypeText 结束同样进入等待继续
                            ui.StartTypeTextBatch(textLines, () =>
                            {
                                commandRunning = false;
                                waitingForContinue = true;
                                ui.ShowNextIndicator(true);
                                ui.autoTimer = 0f;

                                // 同上：进入等待继续
                                DoContinue();
                            });
                        }
                        else
                        {
                            SafeComplete();
                        }
                    });

                    break;
                }

            //case "ShowPortrait":
            //    {
            //        string slot = TryS(cd, "slot", "Left");
            //        string charId = TryS(cd, "characterId");
            //        string expr = TryS(cd, "expressionId", "default");
            //        float fade = TryF(cd, "fade", 0.3f);
            //        ui.portraits.Show(slot, charId, expr, fade, driver, SafeComplete);
            //        break;
            //    }

            //case "ChangeExpression":
            //    {
            //        string slot = TryS(cd, "slot", "Left");
            //        string expr = TryS(cd, "expressionId", "default");
            //        float fade = TryF(cd, "fade", 0.2f);
            //        ui.portraits.ChangeExpression(slot, expr, fade, driver, SafeComplete);
            //        break;
            //    }

            //case "HidePortrait":
            //    {
            //        string slot = TryS(cd, "slot", "Left");
            //        float fade = TryF(cd, "fade", 0.3f);
            //        ui.portraits.Hide(slot, fade, driver, SafeComplete);
            //        break;
            //    }

            //case "CameraMove":
            //    {
            //        // 镜头位移等可在此接入 cam.MoveTo，经 driver 延迟后 SafeComplete
            //        // cam?.MoveTo(..., () => SafeComplete());
            //        driver.Run(0.01f, _ => { }, SafeComplete);
            //        break;
            //    }

            //case "CameraZoom":
            //    {
            //        driver.Run(0.01f, _ => { }, SafeComplete);
            //        break;
            //    }

            //case "CameraShake":
            //    {
            //        driver.Run(0.01f, _ => { }, SafeComplete);
            //        break;
            //    }

            //case "PlaySE":
            //    {
            //        string name = TryS(cd, "name");
            //        // audioBus?.PlaySE(name);
            //        driver.Run(0f, _ => { }, SafeComplete);
            //        break;
            //    }

            case DialogCommandData4SetImage setImage:
                {
                    ApplySetImage(setImage);
                    SafeComplete();
                    break;
                }
            case DialogCommandData4ShowImage showImage:
                {
                    ApplyShowImage(showImage);
                    SafeComplete();
                    break;
                }

            case DialogCommandData4Wait cd4Wait:
                {
                    driver.Run(cd4Wait.WaitTime, _ => { }, SafeComplete);
                    break;
                }
            case DialogCommandData4WaitTimelineSignal cdWaitSignal:
                {
                    // 暂停剧情直到 Timeline 发出指定 Signal（由 ReceiveTimelineSignal 解除）
                    waitingSignalName = cdWaitSignal.SignalName;
                    onSignalReceivedCallback = SafeComplete;
                    break;
                }

            case DialogCommandData4ResumeTimeline cdResume:
                {
                    if (activeDirector != null && activeDirector.state == PlayState.Paused)
                    {
                        activeDirector.Play(); // 从暂停点继续播放
                    }
                    SafeComplete(); // 无暂停或已恢复：本帧内完成命令收尾
                    break;
                }

            case DialogCommandData4PlayTimeline cd4Timeline:
                {
                    // PlayTimeline 内根据 waitFinished 决定是否订阅 stopped 再回调
                    PlayTimeline(cd4Timeline.TimelineId, cd4Timeline.WaitUntilFinished, SafeComplete);
                    break;
                }




            case DialogCommandData4MoveEntity cd4MoveEntity:
                {
                    string staticName = cd4MoveEntity.StaticName;
                    var dialogActor = GetDialogueActorByStaticName(staticName);
                    if(dialogActor == null)
                    {
                        break;
                    }

                    Vector2? forcedStartPos = null;
                    if(cd4MoveEntity.ForceStartPos)
                    {
                        forcedStartPos = cd4MoveEntity.StartPos;
                    }
                    dialogActor.DoDialogMove(cd4MoveEntity.MovePos, cd4MoveEntity.MoveDuration, forcedStartPos);
                }
                break;

            case DialogCommandData4SpawnDialogActor cd4SpawnActor:
                {
                    SpawnDialogActor(cd4SpawnActor);
                    SafeComplete();
                }
                break;

            case DialogCommandData4ShowCameraOverride cd4Camera:
                {
                    ShowDialogCameraOverride(cd4Camera, SafeComplete);
                }
                break;

            case DialogCommandData4SimpleFunc cd4Func:
                {
                    switch(cd4Func.SimpleFuncType)
                    {
                        case EDialogSimpleFuncType.SetGlobalSwitch:
                            {
                                MainGameManager.Instance.gameLogicManager.playerDataManager?.SetVariable(cd4Func.Param5);
                            }
                            break;

                        case EDialogSimpleFuncType.SrcLocalSwitch:
                            {
                                var srcId = runtimeRef.SrcEntityId;
                                if(srcId == null || srcId == 0)
                                {
                                    break;
                                }

                                var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(srcId.Value);
                                if (entity == null) break;

                                string switchName = cd4Func.Param5;
                                entity.SetLocalSwitch(switchName, true);

                            }
                            break;
                        case EDialogSimpleFuncType.AddTmpEnmity:
                            {
                                var srcId = runtimeRef.SrcEntityId;
                                if (srcId == null || srcId == 0)
                                {
                                    break;
                                }

                                var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(srcId.Value);
                                if (entity == null || entity is not BaseUnitLogicEntity unitEntity) break;

                                unitEntity?.EnmitySystem.AddTempEnmity(srcId.Value);
                            }
                            break;
                        case EDialogSimpleFuncType.ClearWanted:
                            {
                                MainGameManager.Instance.gameLogicManager.WantedManager.ClearAllWanted();
                            }
                            break;
                        case EDialogSimpleFuncType.Charmed:
                            {
                                var srcId = runtimeRef.SrcEntityId;
                                if (srcId == null || srcId == 0)
                                {
                                    break;
                                }

                                var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(srcId.Value);
                                if (entity == null || entity is not NpcUnitLogicEntity npcEntity) break;

                                npcEntity.ApplySocialCharmed(MainGameManager.Instance.gameLogicManager.playerLogicEntity);
                            }
                            break;
                        case EDialogSimpleFuncType.Teleport:
                            {
                                string mapName = cd4Func.Param5;
                                string targetPoint = cd4Func.Param6;

                                MainGameManager.Instance.gameLogicManager.PreparePlayerSwitchArea(mapName, false, targetPoint, silent : true);
                            }
                            break;
                        case EDialogSimpleFuncType.OpenFunc:
                            {
                                if (Enum.IsDefined(typeof(EFuncOpenType), (int)cd4Func.Param1))
                                {
                                    MainGameManager.Instance.gameLogicManager.playerDataManager?.FuncOpenSystem?.TryOpenFunc((EFuncOpenType)cd4Func.Param1);
                                }
                            }
                            break;

                    }
                    SafeComplete();
                }
                break;

            case DialogCommandData4Choice cd4Choice:
                {
                    var options = new List<string>();
                    var jumpLabels = new List<string>();
                    var choiceRefs = new List<DialogChoiceOption>();
                    if (cd4Choice.Options != null)
                    {
                        foreach (var choice in cd4Choice.Options)
                        {
                            if (choice == null) continue;
                            options.Add(choice.Text ?? "");
                            jumpLabels.Add(choice.TargetStepId ?? "");
                            choiceRefs.Add(choice);
                        }
                    }

                    // 纯选项：选择后可选切到 NPC peace 对话资源，或 JumpToStep，再 SafeComplete
                    ui.StartChoices(options, index =>
                    {
                        if (index >= 0 && index < choiceRefs.Count)
                        {
                            var picked = choiceRefs[index];
                            if (TryPlayChoiceTargetDialog(picked))
                                return;
                            var label = jumpLabels[index];
                            if (!string.IsNullOrEmpty(label))
                                JumpToStep(label);
                        }
                        SafeComplete();
                    });
                    break;
                }

            case DialogCommandData4SwitchDialogSegment sw:
                {
                    if (sw.CancelTypingState && ui != null)
                        ui.PrepareForDialogSegmentSwitch();
                    if (!string.IsNullOrEmpty(sw.TargetStepId))
                        JumpToStep(sw.TargetStepId);
                    SafeComplete();
                    break;
                }

            case DialogCommandData4DynamicNpcChoice dyn:
                {
                    var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
                    LogicEntityBase srcEntity = null;
                    if (glm != null && runtimeRef != null && runtimeRef.SrcEntityId != null)
                    {
                        var ent = glm.GetLogicEntity(runtimeRef.SrcEntityId.Value, false);
                        srcEntity = ent as LogicEntityBase;
                    }

                    var jumpLabels = new List<string>();
                    var options = new List<string>();
                    var pickedEntries = new List<NpcHubChoiceEntry>();
                    var sourceEntries = DynamicNpcChoiceRuntime.BuildEntries(srcEntity, glm);
                    foreach (var entry in sourceEntries)
                    {
                        if (entry?.Option == null) continue;
                        if (!DialogConditionRuntime.AllPass(entry.Option.Conditions1, srcEntity, glm))
                            continue;
                        options.Add(entry.Option.Text ?? "");
                        jumpLabels.Add(entry.Option.TargetStepId ?? "");
                        pickedEntries.Add(entry);
                    }

                    if (options.Count == 0)
                    {
                        Debug.LogWarning("[Dialog] DialogCommandData4DynamicNpcChoice: no option passed condition filters.");
                        SafeComplete();
                        break;
                    }


                    ui.StartChoices(
                        options,
                        index =>
                        {
                            if (index >= 0 && index < pickedEntries.Count)
                            {
                                var entry = pickedEntries[index];
                                var picked = entry.Option;
                                if (entry.Session != null)
                                {
                                    ui?.PrepareForDialogSegmentSwitch();
                                    Stop();
                                    QuestDialogFlowRunner.Start(entry.Session, runtimeRef?.SrcEntityId);
                                    return;
                                }

                                if (TryPlayChoiceTargetDialog(picked))
                                    return;
                                var label = jumpLabels[index];
                                if (!string.IsNullOrEmpty(label))
                                    JumpToStep(label);
                            }
                            SafeComplete();
                        },
                        dyn.TimeLimit, overrideText:"entry");
                    break;
                }

            case DialogCommandData4QuestAction questAction:
                {
                    var session = BuildQuestActionSession(questAction);
                    if (session != null)
                    {
                        QuestDialogFlowRunner.DispatchQuestAction(session, runtimeRef?.SrcEntityId);
                    }
                    SafeComplete();
                    break;
                }

            case DialogCommandData4JumpTo cd4JumpTo:
                {
                    if (!string.IsNullOrEmpty(cd4JumpTo.TargetStepId)) JumpToStep(cd4JumpTo.TargetStepId);
                    SafeComplete();
                    break;
                }
            //case "GiveItem":
            //    {
            //        string itemId = TryS(cd, "itemId");
            //        int amount = cd.i != null && cd.i.TryGetValue("amount", out var iv) ? iv : (int)TryF(cd, "amount", 1f);
            //        Debug.Log("give item from dialog " + dataRef.id + " " + itemId + " " + amount);

            //        SafeComplete();
            //        break;
            //    }
            //case "EnterEncounter":
            //    {
            //        Debug.Log("EnterEncounter item from dialog ");

            //        string id = TryS(cd, "id");
            //        string defeat = TryS(cd, "defeat");

            //        bool defeated = false;
            //        if(defeat == "True")
            //        {
            //            defeated = true;
            //        }
            //        MainGameManager.Instance.EnterEncounter(0, "dialog", defeated);

            //        SafeComplete();
            //        break;
            //    }
            //case "BlackMask":
            //    {
            //        ui.BlackMask.gameObject.SetActive(true);
            //        SafeComplete();
            //        break;
            //    }
            //case "Label":
            default:
                {
                    // 未识别命令类型：直接完成避免卡死
                    SafeComplete();
                    break;
            }
        }
    }

    private bool TryResolveDialogStaticEntity(string staticName, out LogicEntityBase entity)
    {
        entity = null;
        if (string.IsNullOrEmpty(staticName))
        {
            return false;
        }

        var glm = MainGameManager.Instance?.gameLogicManager;
        if (glm?.AreaManager == null)
        {
            return false;
        }

        long entityId = 0;
        if (!dynamicDialogActorIds.TryGetValue(staticName, out entityId))
        {
            var staticId = glm.AreaManager.GetStaticIdByUniqName(staticName);
            if (staticId == 0 ||
                !glm.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var refreshInfo))
            {
                return false;
            }

            entityId = refreshInfo.EntityInstId;
        }

        entity = glm.GetLogicEntity(entityId, true) as LogicEntityBase;
        return entity != null;
    }

    private void ShowDialogCameraOverride(DialogCommandData4ShowCameraOverride cmd, Action onComplete)
    {
        if (cmd == null)
        {
            onComplete?.Invoke();
            return;
        }

        var focusPos = cmd.Position;
        long pinEntityId = 0;
        if (TryResolveDialogStaticEntity(cmd.StaticName, out var focusEntity))
        {
            focusPos = focusEntity.Pos;
            if (cmd.PinTarget)
            {
                pinEntityId = focusEntity.Id;
            }
        }

        var duration = cmd.Duration > 0f
            ? cmd.Duration
            : MainGameManager.DefaultCameraOverrideDuration;
        var visualRadius = cmd.VisualRadius > 0f
            ? cmd.VisualRadius
            : MainGameManager.DefaultCameraOverrideVisualRadius;

        MainGameManager.Instance?.ShowCameraOverrideFix(
            focusPos,
            duration,
            pinEntityId,
            visualRadius,
            cmd.BlockInput);

        if (cmd.WaitUntilFinished)
        {
            driver.Run(duration, _ => { }, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void SpawnDialogActor(DialogCommandData4SpawnDialogActor cmd)
    {
        if (cmd == null || string.IsNullOrEmpty(cmd.CfgId))
        {
            Debug.LogWarning("[Dialog] SpawnDialogActor skipped: missing cfg id.");
            return;
        }

        var glm = MainGameManager.Instance?.gameLogicManager;
        if (glm?.AreaManager == null)
        {
            Debug.LogWarning("[Dialog] SpawnDialogActor skipped: game logic manager missing.");
            return;
        }

        if (!string.IsNullOrEmpty(cmd.StaticName) &&
            dynamicDialogActorIds.TryGetValue(cmd.StaticName, out var oldId))
        {
            glm.AreaManager.ForceDestroyEntityNow(oldId, "DialogActorReplace");
            dynamicDialogActorIds.Remove(cmd.StaticName);
            dynamicDialogActorDestroyOnEnd.Remove(oldId);
        }

        var faceDir = cmd.FaceDir.sqrMagnitude > 1e-8f ? cmd.FaceDir.normalized : Vector2.right;
        var initInfo = new EntityInitInfo4Npc
        {
            CfgId = cmd.CfgId,
            Position = cmd.Position,
            FaceDir = faceDir,
            IsPeace = cmd.IsPeace,
            MoveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
            CharacterKey = cmd.CharacterKey ?? string.Empty,
        };

        var record = glm.AreaManager.CreateEntityRecordFromInitInfo(initInfo);
        if (record == null)
        {
            Debug.LogWarning($"[Dialog] SpawnDialogActor failed: invalid cfg id {cmd.CfgId}.");
            return;
        }

        glm.AreaManager.RegisterEntityRecord(record, true);
        var entity = glm.GetLogicEntity(record.Id, true);
        if (entity is IDialogueActor dialogActor)
        {
            dialogActor.OnDialogStart();
            runtimeRef?.ControlledEntityList.Add(record.Id);
        }

        if (!string.IsNullOrEmpty(cmd.StaticName))
        {
            dynamicDialogActorIds[cmd.StaticName] = record.Id;
        }

        if (cmd.DestroyOnDialogEnd)
        {
            dynamicDialogActorDestroyOnEnd.Add(record.Id);
        }
    }

    private void CommandCompletedFromData(DialogCommandData cd)
    {
        if (!commandRunning)
        {
            // 若已处于「等待继续」且本回调来自 TypeText 完成，则不再重复推进
            if (waitingForContinue) return;
        }

        commandRunning = false;

        if (pendingJump)
        {
            StartStepFromData();
            return;
        }

        // 本条命令结束：推进下标并继续队列
        currentCmdIndex++;
        ExecuteNextCommandInStep();
    }

    private void EnterWaitForNextStep()
    {
        waitingForNextStep = true;
        ui.ShowNextIndicator(true);
        ui.autoTimer = 0f;

        //if (SkipMode)
        {
            DoContinue();
        }
    }


    #region 过场与 Timeline

    private IEnumerator LoadCutsceneSceneRoutine(string sceneName, bool hideMainScene)
    {
        UIManager.Instance.FadeShowBlack(0.05f);

        //var oldBlend = MainGameManager.Instance.CineBrain.m_DefaultBlend;
        //MainGameManager.Instance.CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
        //    CinemachineBlendDefinition.Style.Cut, 0f);

        // 若已有 Additive 过场则先卸载，避免重复叠加
        if (!string.IsNullOrEmpty(currCutsceneName))
        {
            yield return SceneManager.UnloadSceneAsync(currCutsceneName);
        }

        currCutsceneName = sceneName;

        // Additive 加载过场场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 可选：隐藏主场景渲染（当前注释保留）
        if (hideMainScene)
        {
            //MainGameManager.Instance.gameLogicManager.SetMainWorldVisible(false);
        }

        // 对话期间暂停逻辑时间
        LogicTime.RequestPause("Dialog");


        // 将活动场景切到过场，便于 Find/Timeline 解析到正确实例
        Scene cutscene = SceneManager.GetSceneByName(sceneName);
        if (cutscene.IsValid())
        {
            SceneManager.SetActiveScene(cutscene);
        }

        var rootGo = cutscene.GetRootGameObjects()[0];
        cutsceneRootGo = rootGo;

        // 提升过场内主虚拟相机优先级，确保镜头切换生效
        var mainVcam = cutsceneRootGo.transform.Find("VCam").GetComponent<CinemachineVirtualCamera>();
        mainVcam.Priority = 100;

        mainVcam.PreviousStateIsValid = false;

        yield return new WaitForSeconds(2.0f);

        UIManager.Instance.FadeHideBlack(1.0f);
    }

    private IEnumerator UnloadCutsceneSceneRoutine(Action onComplete)
    {
        UIManager.Instance.FadeShowBlack(0.01f);

        if (!string.IsNullOrEmpty(currCutsceneName))
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currCutsceneName);
            while (asyncUnload != null && !asyncUnload.isDone)
            {
                yield return null;
            }
            currCutsceneName = null;
        }

        // 卸载过场后可在此恢复主场景可见性
        //MainGameManager.Instance.gameLogicManager.SetMainWorldVisible(true);

        // 将 ActiveScene 切回主场景（路径需与工程一致）
        //SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainGameScene")); // 示例：主场景名请按项目配置

        onComplete?.Invoke();

        yield return new WaitForSeconds(2.0f);

        UIManager.Instance.FadeHideBlack(1.0f);
    }

    /// <summary>
    /// 按资源名查找场景物体上的 PlayableDirector 并播放
    /// </summary>
    /// <param name="timelineId"></param>
    /// <param name="waitFinished"></param>
    /// <param name="onComplete"></param>
    private void PlayTimeline(string timelineId, bool waitFinished, Action onComplete)
    {
        // timelineId 对应场景中物体名，其挂 PlayableDirector
        // 若要求等待播完：订阅 stopped，在回调里清空 activeDirector 再 onComplete
        GameObject timelineObj = GameObject.Find(timelineId);
        if (timelineObj != null)
        {
            activeDirector = timelineObj.GetComponent<PlayableDirector>();
            if (activeDirector != null)
            {
                activeDirector.Play();

                if (waitFinished)
                {
                    // 播完自然停止时触发收尾
                    activeDirector.stopped += OnTimelineStopped;

                    void OnTimelineStopped(PlayableDirector director)
                    {
                        if (director == activeDirector)
                        {
                            activeDirector.stopped -= OnTimelineStopped;
                            activeDirector = null;
                            onComplete?.Invoke();
                        }
                    }
                    return; // 等待 stopped，不在此处立即 onComplete
                }
            }
            else
            {
                Debug.LogWarning($"TimelineId {timelineId} does not have a PlayableDirector.");
            }
        }
        else
        {
            Debug.LogWarning($"TimelineId {timelineId} not found in scene.");
        }

        // 不等待播完或未找到 Director：立即视为命令完成
        onComplete?.Invoke();
    }

    /// <summary>
    /// 供 Timeline SignalReceiver 转发：匹配等待中的信号名则恢复对话命令流
    /// </summary>
    public void ReceiveTimelineSignal(string signalName)
    {
        // 收到信号后暂停 Timeline，便于接对话 UI
        if (activeDirector != null && activeDirector.state == PlayState.Playing)
        {
            activeDirector.Pause();
        }

        // 与 DialoguePlayer 内 WaitTimelineSignal 命令配对
        if (!string.IsNullOrEmpty(waitingSignalName) && waitingSignalName == signalName)
        {
            waitingSignalName = null;
            var callback = onSignalReceivedCallback;
            onSignalReceivedCallback = null;

            // 执行注册在对话命令里的回调，继续 ExecuteNextCommandInStep 流程
            callback?.Invoke();
        }
    }

    // 由 PlayTimeline 流程或外部把场景里的 Director 赋给 activeDirector
    public void SetActiveDirector(PlayableDirector director)
    {
        activeDirector = director;
    }

    private void ApplySetImage(DialogCommandData4SetImage setImage)
    {
        var pm = ui != null && ui.portraits != null ? ui.portraits : runtimeRef?.portraits;
        if (pm == null)
            return;

        string slotName = setImage.Position switch
        {
            DialogCommandData4SetImage.ImgPos.Left => "Left",
            DialogCommandData4SetImage.ImgPos.Center => "Center",
            DialogCommandData4SetImage.ImgPos.Right => "Right",
            DialogCommandData4SetImage.ImgPos.Background => "Background",
            _ => "Center"
        };

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(setImage.ImageName))
            sprite = LoadDialogueSprite(setImage.ImageName);

        string speakerId = DialoguePortraitCatalog.ResolveSpeaker(setImage.ImageName);
        pm.ShowSlotSprite(slotName, sprite, speakerId);
    }

    private void ApplyShowImage(DialogCommandData4ShowImage showImage)
    {
        if (ui == null || showImage == null)
            return;

        Sprite sprite = null;
        if (showImage.Visible && !string.IsNullOrEmpty(showImage.ImageName))
            sprite = LoadDialogueSprite(showImage.ImageName);

        ui.ShowCgImage(sprite, showImage.Visible, showImage.Alpha);
    }

    private static Sprite LoadDialogueSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

        assetPath = assetPath.Replace('\\', '/');
        if (!assetPath.StartsWith("Assets/Resources/"))
            return null;

        string resPath = assetPath.Substring("Assets/Resources/".Length);
        int dot = resPath.LastIndexOf('.');
        if (dot > 0)
            resPath = resPath.Substring(0, dot);

        return Resources.Load<Sprite>(resPath);
    }


    #endregion

}

// 简单引用计数：阻塞/解除玩家输入（与对话等系统协作）
public static class InputBlocker
{
    private static int counter = 0;
    public static void Block(bool block)
    {
        counter += block ? 1 : -1;
        counter = Mathf.Max(0, counter);
        // 此处可接入：counter>0 时禁用玩家操作或显示遮罩
    }
}
