using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsoleGM : MonoBehaviour
{
    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.BackQuote; // ~ 或 `
    public bool visible = false;

    [Header("UI")]
    public float height = 260f;         // 控制台窗口高度
    public int fontSize = 14;
    public int maxLogLines = 200;
    public Color bgColor = new Color(0, 0, 0, 0.8f);
    public Color inputBgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color hintColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color paramColor = new Color(0.6f, 0.9f, 0.6f, 1f);
    public Color errorColor = new Color(1f, 0.5f, 0.5f, 1f);

    private string input = "";
    private Vector2 scroll;
    private GUIStyle logStyle;
    private GUIStyle inputStyle;
    private GUIStyle hintStyle;

    private readonly List<string> logs = new();
    private readonly List<string> history = new();
    private int historyIndex = -1;

    // 命令注册
    private readonly Dictionary<string, Command> commands = new(StringComparer.OrdinalIgnoreCase);

    // 自动完成候选
    private List<string> candidates = new();
    private int candidateIndex = 0;

    // 参数提示缓存
    private string paramHint = "";

    void Awake()
    {
        Application.logMessageReceived += OnUnityLog;

        // 样例命令注册
        Register("help", "显示所有命令或查看某命令帮助",
            new[] { new CmdParam("cmd", "可选，命令名") },
            args =>
            {
                if (args.Count == 0)
                {
                    Log("可用命令：");
                    foreach (var kv in commands.OrderBy(k => k.Key))
                        Log($"  {kv.Key} - {kv.Value.Description}");
                }
                else
                {
                    var name = args[0];
                    if (commands.TryGetValue(name, out var cmd))
                        Log($"{name} {cmd.ParamUsage()} - {cmd.Description}");
                    else
                        LogError($"未知命令 {name}");
                }
            });

        Register("set_time_scale", "设置时间缩放",
            new[] { new CmdParam("scale", "float，时间缩放，例如 0.5") },
            args =>
            {
                if (args.Count < 1) { LogError("用法：set_time_scale <scale>"); return; }
                if (float.TryParse(args[0], out var s)) { Time.timeScale = s; Log($"已设置 Time.timeScale = {s}"); }
                else LogError("参数格式错误，需 float");
            });

        Register("tp", "传送到坐标",
            new[] { new CmdParam("x", "float"), new CmdParam("y", "float") },
            args =>
            {
                if (args.Count < 2) { LogError("用法：tp <x> <y>"); return; }
                if (Camera.main == null) { LogError("无主相机"); return; }
                var player = FindAnyObjectByType<Rigidbody2D>();
                if (player == null) { LogError("未找到 Rigidbody2D 作为玩家示例"); return; }
                if (float.TryParse(args[0], out var x) && float.TryParse(args[1], out var y))
                {
                    player.position = new Vector2(x, y);
                    Log($"玩家传送至 ({x}, {y})");
                }
                else LogError("参数需为 float");
            });

        Register("give_item", "给予物品（示例）",
            new[] { new CmdParam("id", "string，物品ID"), new CmdParam("count", "int，数量", true) },
            args =>
            {
                if (args.Count < 1) { LogError("用法：give_item <id> [count]"); return; }
                var id = args[0];
                int count = 1;
                if (args.Count >= 2 && !int.TryParse(args[1], out count))
                { LogError("count 需为 int"); return; }
                Log($"给予物品 id={id}, count={count}");
            });
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnUnityLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
            if (visible) EnableInput();
            else DisableInput();
        }

        if (!visible) return;

        // 基础键盘交互（不依赖 IMGUI focus）
        if (Input.GetKeyDown(KeyCode.UpArrow)) BrowseHistory(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) BrowseHistory(+1);

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DoAutoComplete();
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        EnsureStyles();

        var rect = new Rect(0, 0, Screen.width, height);
        // 背景
        DrawRect(rect, bgColor);

        GUILayout.BeginArea(rect);
        GUILayout.Space(6);

        // 日志区域
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
        foreach (var line in logs)
        {
            GUILayout.Label(line, logStyle);
        }
        GUILayout.EndScrollView();

        // 输入区域背景
        var inputRect = new Rect(8, height - 36, Screen.width - 16, 28);
        DrawRect(inputRect, inputBgColor);

        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUI.SetNextControlName("ConsoleInput");
        input = GUILayout.TextField(input, inputStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
        GUI.FocusControl("ConsoleInput");

        if (GUILayout.Button("执行", GUILayout.Width(60), GUILayout.Height(24)))
            ExecuteInput();
        GUILayout.Space(8);
        GUILayout.EndHorizontal();

        // 参数提示
        if (!string.IsNullOrEmpty(paramHint))
        {
            GUILayout.Space(2);
            var c = hintStyle.normal.textColor;
            hintStyle.normal.textColor = paramColor;
            GUILayout.Label(paramHint, hintStyle);
            hintStyle.normal.textColor = c;
        }

        // 自动完成候选
        if (candidates.Count > 0)
        {
            GUILayout.Space(2);
            var c = hintStyle.normal.textColor;
            hintStyle.normal.textColor = hintColor;
            GUILayout.Label("候选：" + string.Join("  |  ", candidates.Select((s, i) => i == candidateIndex ? $"[{s}]" : s)), hintStyle);
            hintStyle.normal.textColor = c;
        }

        // 处理回车
        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        {
            ExecuteInput();
            e.Use();
        }
        GUILayout.EndArea();

        // 根据当前输入更新候选和参数提示
        UpdateHints();
    }

    // =============== 命令系统 ===============

    private void Register(string name, string desc, CmdParam[] parameters, Action<List<string>> handler)
    {
        commands[name] = new Command(name, desc, parameters?.ToList() ?? new List<CmdParam>(), handler);
    }

    private void ExecuteInput()
    {
        var line = input.Trim();
        if (string.IsNullOrEmpty(line)) return;

        Log($"> {line}");
        history.Add(line);
        historyIndex = history.Count;
        input = "";
        candidates.Clear();
        paramHint = "";

        // 解析命令
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return;

        var cmdName = tokens[0];
        tokens.RemoveAt(0);

        if (!commands.TryGetValue(cmdName, out var cmd))
        {
            LogError($"未知命令：{cmdName}（输入 help 查看）");
            return;
        }

        try
        {
            cmd.Handler(tokens);
        }
        catch (Exception ex)
        {
            LogError($"命令执行错误：{ex.Message}");
        }
    }

    private List<string> Tokenize(string line)
    {
        // 简单分词：支持双引号包裹的参数
        var list = new List<string>();
        bool inQuote = false;
        var cur = new System.Text.StringBuilder();
        foreach (char ch in line)
        {
            if (ch == '"') { inQuote = !inQuote; continue; }
            if (!inQuote && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0) { list.Add(cur.ToString()); cur.Clear(); }
            }
            else cur.Append(ch);
        }
        if (cur.Length > 0) list.Add(cur.ToString());
        return list;
    }

    private void UpdateHints()
    {
        var prefix = input;
        // 构建命令提示和参数提示
        var tokens = Tokenize(prefix);
        if (tokens.Count == 0)
        {
            candidates = commands.Keys.OrderBy(k => k).ToList();
            candidateIndex = 0;
            paramHint = "";
            return;
        }

        if (input.EndsWith(" "))
        {
            // 用户正在输入下一个参数
            tokens.Add("");
        }

        if (tokens.Count == 1)
        {
            // 命令名自动完成
            string head = tokens[0];
            candidates = commands.Keys.Where(k => k.StartsWith(head, StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(k => k).ToList();
            candidateIndex = 0;

            // 参数提示：显示该命令的参数签名
            if (commands.TryGetValue(head, out var cmdExact))
            {
                paramHint = $"{cmdExact.Name} {cmdExact.ParamUsage()} - {cmdExact.Description}";
            }
            else
            {
                // 最近的公共前缀命中显示第一个候选的签名
                if (candidates.Count > 0 && commands.TryGetValue(candidates[0], out var cmd))
                    paramHint = $"{cmd.Name} {cmd.ParamUsage()} - {cmd.Description}";
                else
                    paramHint = "";
            }
        }
        else
        {
            // 已确定命令名，给参数提示
            string cmdName = tokens[0];
            if (commands.TryGetValue(cmdName, out var cmd))
            {
                paramHint = $"{cmd.Name} {cmd.ParamUsage()}";
            }
            else paramHint = "";

            // 不对参数做自动补全（也可在此做自定义补全）
            candidates.Clear();
        }
    }

    private void DoAutoComplete()
    {
        var tokens = Tokenize(input);
        bool addingSpace = input.EndsWith(" ");

        if (tokens.Count == 0 || (tokens.Count == 1 && !addingSpace))
        {
            // 在命令名阶段进行补全
            if (candidates.Count == 0)
            {
                candidates = commands.Keys.OrderBy(k => k).ToList();
                candidateIndex = 0;
            }
            else
            {
                // 循环选择候选
                candidateIndex = (candidateIndex + 1) % candidates.Count;
            }

            if (candidates.Count > 0)
            {
                input = candidates[candidateIndex] + " ";
                // 将光标放到末尾
            }
        }
        else
        {
            // 参数补全：此处留空，若需要可根据命令自定义
        }
    }

    private void BrowseHistory(int delta)
    {
        if (history.Count == 0) return;
        historyIndex = Mathf.Clamp(historyIndex + delta, 0, history.Count);
        if (historyIndex >= 0 && historyIndex < history.Count) input = history[historyIndex];
        else input = "";
        // 更新提示
        UpdateHints();
    }

    // =============== UI/日志 ===============

    private void EnsureStyles()
    {
        if (logStyle == null)
        {
            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.fontSize = fontSize;
            logStyle.normal.textColor = Color.white;

            inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.fontSize = fontSize;

            hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = fontSize - 1;
            hintStyle.normal.textColor = hintColor;
        }
    }

    private void DrawRect(Rect r, Color c)
    {
        var prev = GUI.color;
        GUI.color = c;
        GUI.Box(r, GUIContent.none);
        GUI.color = prev;
    }

    private void Log(string msg)
    {
        logs.Add(msg);
        TrimLogs();
        scroll.y = float.MaxValue;
    }

    private void LogError(string msg)
    {
        logs.Add(Colorize(msg, errorColor));
        TrimLogs();
        scroll.y = float.MaxValue;
    }

    private void TrimLogs()
    {
        if (logs.Count > maxLogLines)
            logs.RemoveRange(0, logs.Count - maxLogLines);
    }

    private string Colorize(string text, Color c)
    {
        var col = ColorUtility.ToHtmlStringRGBA(c);
        return $"<color=#{col}>{text}</color>";
    }

    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            LogError(condition);
        else if (type == LogType.Warning)
            Log(Colorize(condition, new Color(1f, 0.9f, 0.6f)));
        else
            Log(condition);
    }

    private void EnableInput()
    {
        // 可选：暂停游戏输入/锁定鼠标等
    }

    private void DisableInput()
    {
        // 可选：恢复输入
    }

    // =============== 数据结构 ===============
    private class Command
    {
        public string Name;
        public string Description;
        public List<CmdParam> Params;
        public Action<List<string>> Handler;

        public Command(string name, string desc, List<CmdParam> @params, Action<List<string>> handler)
        {
            Name = name;
            Description = desc;
            Params = @params ?? new List<CmdParam>();
            Handler = handler;
        }

        public string ParamUsage()
        {
            if (Params == null || Params.Count == 0) return "";
            return string.Join(" ", Params.Select(p => p.Optional ? $"[{p.Name}]" : $"<{p.Name}>"));
        }
    }

    private class CmdParam
    {
        public string Name;
        public string Hint;
        public bool Optional;

        public CmdParam(string name, string hint = "", bool optional = false)
        {
            Name = name;
            Hint = hint;
            Optional = optional;
        }
    }
}
