using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using My;
using My.Dialog;
using My.Map;
using My.Map.Logic;
using My.UI;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


[Serializable]
public class DialogueRuntime
{
    // 可选：这些引用也可以直接用 DialoguePlayer 的 public 字段，但封装到 Runtime 便于测试与复用
    public DialogueUI ui;
    public PortraitManager portraits;
    public SimpleCameraDirector cam;
    //public AudioBus audio;
    public DialogueTimeDriver driver;

    // 本地化函数：传入 key 返回文本；如果不用本地化，可置空或返回 key
    public Func<string, string> Localize;

    public Action<string> JumpTo;

    public long? SrcEntityId;

    public List<long> ControlledEntityList = new();

}

public partial class DialoguePlayer : MonoBehaviour
{

    [Header("Refs")]
    public DialogueUI ui;
    public SimpleCameraDirector cam;
    public DialogueTimeDriver driver;

    [Header("Input")]
    public bool AutoMode;
    public bool SkipMode;

    // 播放态
    private Action? OnPlayEnd;

    private DialogueData dataRef;
    private DialogueRuntime runtimeRef;

    private int stepIndex;
    private bool isPlaying;

    public bool IsPlaying
    {
        get { return isPlaying; }
    }

    // 顺序执行游标
    private int currentCmdIndex;

    // 当前命令是否在执行中
    private bool commandRunning;

    // 等待用户继续到下一条“对白/停顿”的状态
    private bool waitingForContinue;

    // Step 完成后等待继续到下一 Step 的状态
    private bool waitingForNextStep;

    // label 索引
    //private readonly Dictionary<string, int> labelToStep = new Dictionary<string, int>();

    // 跳转标记（用于在命令回调后切 Step）
    private bool pendingJump;

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
        
        // 每句对白/等待点（waitingForContinue）或 Step 末尾（waitingForNextStep）时才响应输入/自动
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

        //if (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        //{
        //    DoContinue();
        //}

        
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

            // 直接下一步
            DoContinue();
        }
    }

    private IDialogueActor GetDialogueActorByStaticName(string staticName)
    {
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
            // 推进下一条命令
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

        this.OnPlayEnd?.Invoke();
        this.OnPlayEnd = null;
        LogicTime.ClearPauseSource("Dialog");
    }

    private void BuildLabelIndex(DialogueData data)
    {
        //labelToStep.Clear();
        //for (int i = 0; i < data.Steps.Count; i++)
        //{
        //    var lab = data.Steps[i].Id;
        //    if (!string.IsNullOrEmpty(lab))
        //    {
        //        labelToStep[lab] = i; // 后者覆盖前者
        //    }
        //    if (data.Steps[i].Commands != null)
        //    {
        //        foreach (var c in data.Steps[i].Commands)
        //        {
        //            //if (c.type == "Label" && c.s != null && c.s.TryGetValue("label", out var lbl))
        //            //{
        //            //    labelToStep[lbl] = i;
        //            //}
        //        }
        //    }
        //}
    }

    public void PlayFromData(DialogueData data, DialogueRuntime runtime, Action? onPlayEnd)
    {
        runtimeRef = runtime;
        dataRef = data;
        stepIndex = 0;
        isPlaying = true;
        pendingJump = false;
        InputBlocker.Block(true);
        BuildLabelIndex(dataRef);
        StartStepFromData();

        this.OnPlayEnd = onPlayEnd;

        OnDialogStart();
    }

    /// <summary>
    /// 触发回调
    /// </summary>
    private void OnDialogStart()
    {
        List<int> staticIds = new();
        foreach(var actorName in dataRef.ControlledEntityNames)
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
                    Debug.LogError("??");
                    continue;
                }

                dialogActor.OnDialogEnd();
            }
        }
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
            // 切换到 Jump 目标 Step
            StartStepFromData();
            return;
        }

        var step = dataRef.Steps[stepIndex];
        var commands = step.Commands ?? new List<DialogCommandData>();

        // 所有命令执行完毕，进入“等待下一 Step”
        if (currentCmdIndex >= commands.Count)
        {
            EnterWaitForNextStep();
            return;
        }

        var cd = commands[currentCmdIndex];

        // 每次执行前复位状态
        commandRunning = true;
        waitingForContinue = false;
        waitingForNextStep = false;

        // 执行命令
        ExecuteDataCommand(cd);
    }

    private void ExecuteDataCommand(DialogCommandData cd)
    {
        // 为了防止“瞬间回调连锁推进”，我们在每个完成回调前引入最小一帧延迟
        void SafeComplete()
        {
            if (!isActiveAndEnabled)
            {
                CommandCompletedFromData(cd);
                return;
            }
            // 最小一帧延迟再推进，避免 UI 等立即回调造成瞬间连锁
            driver.Run(0f, _ => { }, () => CommandCompletedFromData(cd));
        }

        switch (cd)
        {
            case DialogCommandData4Text cd4Text:
                {
                    //string name = cd4Text.Speaker;
                    //string text = cd4Text.Content;
                    var textLines = cd4Text.TextLines;
                    // 让 TypeText 播完后“不直接推进下一条”，而是进入“等待继续”阶段
                    ui.StartTypeTextBatch(textLines, () =>
                    {
                        commandRunning = false;
                        waitingForContinue = true;
                        ui.ShowNextIndicator(true);
                        ui.autoTimer = 0f;

                        // 直接下一步
                        DoContinue();
                    });
                    break;
                }
            case DialogCommandData4BranchText cd4BranchText:
                {
                    // Choice 阻塞，等待用户选择；选择后推进并可能 Jump
                    ui.StartChoices(cd4BranchText.SimpleBranch, index =>
                    {
                        if (index >= 0 && index < cd4BranchText.SimpleTextLines.Count)
                        {
                            var textLines = cd4BranchText.SimpleTextLines[index];
                            // 让 TypeText 播完后“不直接推进下一条”，而是进入“等待继续”阶段
                            ui.StartTypeTextBatch(textLines, () =>
                            {
                                commandRunning = false;
                                waitingForContinue = true;
                                ui.ShowNextIndicator(true);
                                ui.autoTimer = 0f;

                                // 直接下一步
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
            //        // 如果你没有实现摄像机动画，仍然用短延迟完成，避免瞬间连锁推进
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

            //case "Wait":
            //    {
            //        float t = TryF(cd, "time", 0.3f);
            //        driver.Run(t, _ => { }, SafeComplete);
            //        break;
            //    }

            case DialogCommandData4MoveEntity cd4MoveEntity:
                {
                    string staticName = cd4MoveEntity.StaticName;
                    var staticId = MainGameManager.Instance.gameLogicManager.AreaManager.GetStaticIdByUniqName(staticName);
                    MainGameManager.Instance.gameLogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var refreshInfo);
                    if(refreshInfo == null)
                    {
                        break;
                    }

                    var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(refreshInfo.EntityInstId, true);
                    if(entity is not IDialogueActor dialogActor)
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

            case DialogCommandData4SimpleFunc cd4Func:
                {
                    switch(cd4Func.SimpleFuncType)
                    {
                        case EDialogSimpleFuncType.SetGlobalSwitch:
                            {
                                MainGameManager.Instance.gameLogicManager.playerDataManager.VariableDict[cd4Func.Param5] = true;
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
                                MainGameManager.Instance.gameLogicManager.WantedManager.CurrentWantedVal = 0;
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
                            
                    }
                    SafeComplete();
                }
                break;

            case DialogCommandData4Choice cd4Choice:
                {
                    var options = new List<string>();
                    var jumpLabels = new List<string>();
                    if (cd4Choice.Options != null)
                    {
                        foreach (var choice in cd4Choice.Options)
                        {
                            options.Add(choice.Text ?? "");
                            jumpLabels.Add(choice.TargetStepId ?? "");
                        }
                    }

                    // Choice 阻塞，等待用户选择；选择后推进并可能 Jump
                    ui.StartChoices(options, index =>
                    {
                        if (index >= 0 && index < jumpLabels.Count)
                        {
                            var label = jumpLabels[index];
                            if (!string.IsNullOrEmpty(label))
                            {
                                JumpToStep(label);
                            }
                        }
                        SafeComplete();
                    });
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
                    // 不阻塞
                    SafeComplete();
                    break;
                }
        }
    }

    private void CommandCompletedFromData(DialogCommandData cd)
    {
        if (!commandRunning)
        {
            // 如果这条命令是对白（我们在对白里把推进权交给“等待继续”逻辑），此处不再推进
            if (waitingForContinue) return;
        }

        commandRunning = false;

        if (pendingJump)
        {
            StartStepFromData();
            return;
        }

        // 非对白类命令执行完毕后，直接推进到下一条命令
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
}

// 简单输入屏蔽（保持原逻辑）
public static class InputBlocker
{
    private static int counter = 0;
    public static void Block(bool block)
    {
        counter += block ? 1 : -1;
        counter = Mathf.Max(0, counter);
        // 项目里具体处理：禁用玩家控制器/交互等
    }
}