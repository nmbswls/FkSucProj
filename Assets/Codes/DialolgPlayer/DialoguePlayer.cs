using System;
using System.Collections.Generic;
using My.UI;
using UnityEngine;


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

    // 剧情跳转：供命令或 UI 回调触发跳转（可由外部绑定为 player.JumpToLabel）
    public Action<string> JumpTo;
}

public partial class DialoguePlayer : MonoBehaviour
{

    [Header("Refs")]
    public DialogueUI ui;
    public SimpleCameraDirector cam;
    //public AudioBus audioBus;
    public DialogueTimeDriver driver;

    [Header("Input")]
    public KeyCode confirmKey = KeyCode.Space;
    public bool AutoMode;
    public bool SkipMode;

    // 播放态
    private ScenarioData dataRef;
    private DialogueRuntime runtimeRef;
    private int stepIndex;
    private bool isPlaying;
    private int blockingCount;
    private bool stepWaitingForContinue;
    private readonly Dictionary<string, int> labelToStep = new Dictionary<string, int>();

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
        if (!stepWaitingForContinue) return;

        if (SkipMode)
        {
            ContinueNextStep();
            return;
        }

        if (AutoMode)
        {
            ui.autoTimer += Time.deltaTime;
            if (ui.autoTimer >= ui.autoDelay)
            {
                ui.autoTimer = 0f;
                ContinueNextStep();
            }
        }
        else
        {
            if (Input.GetKeyDown(confirmKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
            {
                ContinueNextStep();
            }
        }
    }

    public void Stop()
    {
        isPlaying = false;
        InputBlocker.Block(false);
    }

    private void BuildLabelIndex(ScenarioData data)
    {
        labelToStep.Clear();
        for (int i = 0; i < data.steps.Count; i++)
        {
            var lab = data.steps[i].label;
            if (!string.IsNullOrEmpty(lab) && !labelToStep.ContainsKey(lab))
            {
                labelToStep.Add(lab, i);
            }
            // 兼容命令里的 Label（如果也用）
            if (data.steps[i].commands != null)
            {
                foreach (var c in data.steps[i].commands)
                {
                    if (c.type == "Label" && c.s != null && c.s.TryGetValue("label", out var lbl))
                    {
                        if (!labelToStep.ContainsKey(lbl)) labelToStep.Add(lbl, i);
                    }
                }
            }
        }
    }

    // 从外部数据开始播放
    public void PlayFromData(ScenarioData data, DialogueRuntime runtime)
    {
        runtimeRef = runtime; // 你可以在类里存一个 runtimeRef
        dataRef = data;
        stepIndex = 0;
        isPlaying = true;
        InputBlocker.Block(true);
        BuildLabelIndex(dataRef);
        StartStepFromData();
    }

    public void JumpToLabel(string label)
    {
        if (labelToStep.TryGetValue(label, out int step))
        {
            stepIndex = step;
        }
        else
        {
            Debug.LogWarning($"Label not found: {label}");
        }
    }

    private void StartStepFromData()
    {
        if (dataRef == null || stepIndex >= dataRef.steps.Count)
        {
            isPlaying = false;
            InputBlocker.Block(false);
            return;
        }

        blockingCount = 0;
        stepWaitingForContinue = false;

        var step = dataRef.steps[stepIndex];
        var commands = step.commands ?? new List<CommandData>();

        // 并行执行
        foreach (var cd in commands)
        {
            if (cd.wait) blockingCount++;
            ExecuteDataCommand(cd);
        }
        if (blockingCount <= 0) EnterWaitForContinue();
    }

    private void ExecuteDataCommand(CommandData cd)
    {
        // 如果你使用 CommandFactory：
        // var cmd = CommandFactory.Create(cd, runtimeRef);
        // cmd.Execute(runtimeRef, () => CommandCompletedFromData(cd));

        // 这里给出与之前 ExecuteCommand 类似的直连实现
        switch (cd.type)
        {
            case "TypeText":
                {
                    string name = TryS(cd, "name");
                    string text = TryS(cd, "text");
                    string textKey = TryS(cd, "textKey");
                    if (!string.IsNullOrEmpty(textKey) && runtimeRef?.Localize != null)
                    {
                        text = runtimeRef.Localize(textKey);
                    }
                    string voice = TryS(cd, "voice");
                    ui.StartTypeText(name, text ?? "", voice, SkipMode, () => CommandCompletedFromData(cd));
                    break;
                }
            case "ShowPortrait":
                {
                    string slot = TryS(cd, "slot", "Left");
                    string charId = TryS(cd, "characterId");
                    string expr = TryS(cd, "expressionId", "default");
                    float fade = TryF(cd, "fade", 0.3f);
                    ui.portraits.Show(slot, charId, expr, fade, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "ChangeExpression":
                {
                    string slot = TryS(cd, "slot", "Left");
                    string expr = TryS(cd, "expressionId", "default");
                    float fade = TryF(cd, "fade", 0.2f);
                    ui.portraits.ChangeExpression(slot, expr, fade, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "HidePortrait":
                {
                    string slot = TryS(cd, "slot", "Left");
                    float fade = TryF(cd, "fade", 0.3f);
                    ui.portraits.Hide(slot, fade, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "CameraMove":
                {
                    // pos="x,y,z"
                    string posStr = TryS(cd, "pos", "0,0,-10");
                    Vector3 pos = ParseVec3(posStr);
                    float dur = TryF(cd, "duration", 0.5f);

                    // todo camera

                    //cam.MoveTo(pos, dur, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "CameraZoom":
                {
                    float fov = TryF(cd, "fov", 60f);
                    float dur = TryF(cd, "duration", 0.5f);

                    // todo camera

                    //cam.ZoomTo(fov, dur, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "CameraShake":
                {
                    float amp = TryF(cd, "amplitude", 1f);
                    float dur = TryF(cd, "duration", 0.2f);

                    // todo camera
                    //cam.Shake(amp, dur, driver, () => CommandCompletedFromData(cd));
                    break;
                }
            case "PlaySE":
                {
                    string name = TryS(cd, "name");
                    //audioBus.PlaySE(name);
                    CommandCompletedFromData(cd);
                    break;
                }
            case "Wait":
                {
                    float t = TryF(cd, "time", 0.3f);
                    driver.Run(t, _ => { }, () => CommandCompletedFromData(cd));
                    break;
                }
            case "Choice":
                {
                    // 选项 text 或 textKey
                    var options = new List<string>();
                    var jumpLabels = new List<string>();
                    if (cd.listS != null)
                    {
                        foreach (var row in cd.listS)
                        {
                            string txt = null;
                            if (row.TryGetValue("text", out var t1)) txt = t1;
                            if (row.TryGetValue("textKey", out var tk))
                            {
                                if (runtimeRef?.Localize != null) txt = runtimeRef.Localize(tk);
                                else txt ??= tk;
                            }
                            row.TryGetValue("jumpLabel", out var jl);
                            options.Add(txt ?? "");
                            jumpLabels.Add(jl);
                        }
                    }
                    ui.StartChoices(options, index => {
                        if (index >= 0 && index < jumpLabels.Count)
                        {
                            var label = jumpLabels[index];
                            if (!string.IsNullOrEmpty(label))
                            {
                                JumpToLabel(label);
                            }
                        }
                        CommandCompletedFromData(cd);
                    });
                    break;
                }
            case "Jump":
                {
                    string label = TryS(cd, "label");
                    if (!string.IsNullOrEmpty(label)) JumpToLabel(label);
                    CommandCompletedFromData(cd);
                    break;
                }
            case "Label":
                {
                    // 不执行，仅用于索引
                    CommandCompletedFromData(cd);
                    break;
                }
            default:
                {
                    Debug.LogWarning($"Unknown command type: {cd.type}");
                    CommandCompletedFromData(cd);
                    break;
                }
        }
    }

    private void CommandCompletedFromData(CommandData cd)
    {
        if (cd.wait)
        {
            blockingCount = Mathf.Max(0, blockingCount - 1);
            if (blockingCount == 0) EnterWaitForContinue();
        }
    }


    // 工具：取参数
    private static string TryS(CommandData c, string key, string def = "")
    {
        if (c.s != null && c.s.TryGetValue(key, out var v)) return v;
        return def;
    }
    private static float TryF(CommandData c, string key, float def = 0f)
    {
        if (c.f != null && c.f.TryGetValue(key, out var v)) return v;
        return def;
    }
    private static Vector3 ParseVec3(string s)
    {
        var p = s.Split(',');
        float x = p.Length > 0 && float.TryParse(p[0], out var vx) ? vx : 0;
        float y = p.Length > 1 && float.TryParse(p[1], out var vy) ? vy : 0;
        float z = p.Length > 2 && float.TryParse(p[2], out var vz) ? vz : 0;
        return new Vector3(x, y, z);
    }

    private void CommandCompleted(CommandData cd)
    {
        if (cd.wait)
        {
            blockingCount = Mathf.Max(0, blockingCount - 1);
            if (blockingCount == 0) EnterWaitForContinue();
        }
    }

    private void EnterWaitForContinue()
    {
        stepWaitingForContinue = true;
        ui.ShowNextIndicator(true);
        ui.autoTimer = 0f;
        if (SkipMode)
        {
            ContinueNextStep();
        }
    }

    private void ContinueNextStep()
    {
        stepWaitingForContinue = false;
        ui.ShowNextIndicator(false);
        stepIndex++;
        StartStepFromData();
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