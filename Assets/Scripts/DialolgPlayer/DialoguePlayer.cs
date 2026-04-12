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
using My.UI;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using static UnityEngine.EventSystems.EventTrigger;


[Serializable]
public class DialogueRuntime
{
    // ??????????????????????? DialoguePlayer ?? public ???????????? Runtime ???????????
    public DialogueUI ui;
    public PortraitManager portraits;
    public SimpleCameraDirector cam;
    //public AudioBus audio;
    public DialogueTimeDriver driver;

    // ??????????????? key ????????????????????????????? key
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

    // ?????
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

    // ??????????
    private int currentCmdIndex;

    // ?????????????????
    private bool commandRunning;

    // ??????????????????????/????????
    private bool waitingForContinue;

    // Step ??????????????? Step ????
    private bool waitingForNextStep;

    // label ????
    //private readonly Dictionary<string, int> labelToStep = new Dictionary<string, int>();

    // ??????????????????????? Step??
    private bool pendingJump;

    private string currCutsceneName;
    private GameObject cutsceneRootGo;

    public PlayableDirector activeDirector; // ??????????? Timeline ???cutscene????????
    private string waitingSignalName;       // ??????????? Timeline ???
    private Action onSignalReceivedCallback;// ??????????

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
        
        // ?????/?????waitingForContinue???? Step ?????waitingForNextStep????????????/???
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

            // ????????
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
            // ????????????
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

        // ?????????????????????????????????????
        // ???????????
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
        //InputBlocker.Block(true);
        StartStepFromData();

        this.OnPlayEnd = onPlayEnd;

        OnDialogStart();
    }

    /// <summary>
    /// ???????
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
                    Debug.LogError("??");
                    continue;
                }

                dialogActor.OnDialogEnd();
            }
        }

        MainGameManager.Instance.gameLogicManager.playerDataManager.DialogTriggerSystem.AddTriggerCount(MetaInfo.DialogId);

        MainGameManager.Instance.MainMapVCam.PreviousStateIsValid = false;
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
            // ?????? Jump ??? Step
            StartStepFromData();
            return;
        }

        var step = dataRef.Steps[stepIndex];
        var commands = step.Commands ?? new List<DialogCommandData>();

        // ????????????????????????? Step??
        if (currentCmdIndex >= commands.Count)
        {
            EnterWaitForNextStep();
            return;
        }

        var cd = commands[currentCmdIndex];

        // ?????????????
        commandRunning = true;
        waitingForContinue = false;
        waitingForNextStep = false;

        // ???????
        ExecuteDataCommand(cd);
    }

    private void ExecuteDataCommand(DialogCommandData cd)
    {
        // ???????????????????????????????????????????????????
        void SafeComplete()
        {
            if (!isActiveAndEnabled)
            {
                CommandCompletedFromData(cd);
                return;
            }
            // ???????????????????? UI ???????????????????
            driver.Run(0f, _ => { }, () => CommandCompletedFromData(cd));
        }

        switch (cd)
        {
            case DialogCommandData4Text cd4Text:
                {
                    //string name = cd4Text.Speaker;
                    //string text = cd4Text.Content;
                    var textLines = cd4Text.TextLines;
                    // ?? TypeText ????????????????????????????????????????
                    ui.StartTypeTextBatch(textLines, () =>
                    {
                        commandRunning = false;
                        waitingForContinue = true;
                        ui.ShowNextIndicator(true);
                        ui.autoTimer = 0f;

                        // ????????
                        DoContinue();
                    });
                    break;
                }
            case DialogCommandData4BranchText cd4BranchText:
                {
                    // Choice ???????????????????????????? Jump
                    ui.StartChoices(cd4BranchText.SimpleBranch, index =>
                    {
                        if (index >= 0 && index < cd4BranchText.SimpleTextLines.Count)
                        {
                            var textLines = cd4BranchText.SimpleTextLines[index];
                            // ?? TypeText ????????????????????????????????????????
                            ui.StartTypeTextBatch(textLines, () =>
                            {
                                commandRunning = false;
                                waitingForContinue = true;
                                ui.ShowNextIndicator(true);
                                ui.autoTimer = 0f;

                                // ????????
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
            //        // ?????????????????????????????????????????????????
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

            case DialogCommandData4Wait cd4Wait:
                {
                    driver.Run(cd4Wait.WaitTime, _ => { }, SafeComplete);
                    break;
                }
            case DialogCommandData4WaitTimelineSignal cdWaitSignal:
                {
                    // ???????????????????????????????? Timeline ?????????
                    waitingSignalName = cdWaitSignal.SignalName;
                    onSignalReceivedCallback = SafeComplete;
                    break;
                }

            case DialogCommandData4ResumeTimeline cdResume:
                {
                    if (activeDirector != null && activeDirector.state == PlayState.Paused)
                    {
                        activeDirector.Play(); // ???????
                    }
                    SafeComplete(); // ???????????????????????????????????????????
                    break;
                }

            case DialogCommandData4PlayTimeline cd4Timeline:
                {
                    // ???????????????????Timeline??????
                    PlayTimeline(cd4Timeline.TimelineId, cd4Timeline.WaitUntilFinished, SafeComplete);
                    break;
                }




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
                        case EDialogSimpleFuncType.Teleport:
                            {
                                string mapName = cd4Func.Param5;
                                string targetPoint = cd4Func.Param6;

                                MainGameManager.Instance.gameLogicManager.PreparePlayerSwitchArea(mapName, false, targetPoint, silent : true);
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

                    // Choice ???????????????????????????? Jump
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
                    // ??????
                    SafeComplete();
                    break;
                }
        }
    }

    private void CommandCompletedFromData(DialogCommandData cd)
    {
        if (!commandRunning)
        {
            // ???????????????????????????????????????????????????????????????
            if (waitingForContinue) return;
        }

        commandRunning = false;

        if (pendingJump)
        {
            StartStepFromData();
            return;
        }

        // ??????????????????????????????????
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


    #region ????????

    private IEnumerator LoadCutsceneSceneRoutine(string sceneName, bool hideMainScene)
    {
        UIManager.Instance.FadeShowBlack(0.05f);

        //var oldBlend = MainGameManager.Instance.CineBrain.m_DefaultBlend;
        //MainGameManager.Instance.CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(
        //    CinemachineBlendDefinition.Style.Cut, 0f);

        // ????????????????????
        if (!string.IsNullOrEmpty(currCutsceneName))
        {
            yield return SceneManager.UnloadSceneAsync(currCutsceneName);
        }

        currCutsceneName = sceneName;

        // ??? Additive ????????????????
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // ????????????????????????????????????????????
        if (hideMainScene)
        {
            //MainGameManager.Instance.gameLogicManager.SetMainWorldVisible(false);
        }

        // ????????
        LogicTime.RequestPause("Dialog");


        // ???????????????????????????????????????
        Scene cutscene = SceneManager.GetSceneByName(sceneName);
        if (cutscene.IsValid())
        {
            SceneManager.SetActiveScene(cutscene);
        }

        var rootGo = cutscene.GetRootGameObjects()[0];
        cutsceneRootGo = rootGo;

        // 
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

        // ?????????????
        //MainGameManager.Instance.gameLogicManager.SetMainWorldVisible(true);

        // ?????????? Active
        //SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainGameScene")); // ???????????????????????

        onComplete?.Invoke();

        yield return new WaitForSeconds(2.0f);

        UIManager.Instance.FadeHideBlack(1.0f);
    }

    /// <summary>
    /// ????timeline
    /// </summary>
    /// <param name="timelineId"></param>
    /// <param name="waitFinished"></param>
    /// <param name="onComplete"></param>
    private void PlayTimeline(string timelineId, bool waitFinished, Action onComplete)
    {
        // ????????????????????????? PlayableDirector
        // ??????????????????????????Director??????????????????????
        GameObject timelineObj = GameObject.Find(timelineId);
        if (timelineObj != null)
        {
            activeDirector = timelineObj.GetComponent<PlayableDirector>();
            if (activeDirector != null)
            {
                activeDirector.Play();

                if (waitFinished)
                {
                    // ????Timeline??????
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
                    return; // ??????? onComplete????? stopped ???
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

        // ????????????????????????????????
        onComplete?.Invoke();
    }

    /// <summary>
    /// ?? Timeline ?? SignalReceiver ????????????
    /// </summary>
    public void ReceiveTimelineSignal(string signalName)
    {
        // ???????? Timeline
        if (activeDirector != null && activeDirector.state == PlayState.Playing)
        {
            activeDirector.Pause();
        }

        // ??? DialoguePlayer ???????????????
        if (!string.IsNullOrEmpty(waitingSignalName) && waitingSignalName == signalName)
        {
            waitingSignalName = null;
            var callback = onSignalReceivedCallback;
            onSignalReceivedCallback = null;

            // ??????????????????? DialoguePlayer ????????????????????????
            callback?.Invoke();
        }
    }

    // ???????? PlayTimeline ??????? director ????? activeDirector
    public void SetActiveDirector(PlayableDirector director)
    {
        activeDirector = director;
    }


    #endregion

}

// ??????????????????????
public static class InputBlocker
{
    private static int counter = 0;
    public static void Block(bool block)
    {
        counter += block ? 1 : -1;
        counter = Mathf.Max(0, counter);
        // ?????????????????????????/??????
    }
}