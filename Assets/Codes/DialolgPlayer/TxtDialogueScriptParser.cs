
namespace My
{
//    注释：以 # 或 // 开头的行忽略；空行忽略。
//Step 开始与可选标签：
//[Step]
//    开始无标签的 Step
//[Step labelName] 开始带标签的 Step（用于 Jump/Choice 跳转）
//对白行：角色名: 文本
//会转换为 TypeText 命令（name=角色名，text=文本；你也可以习惯写 textKey = xxx，此解析器会优先识别 textKey 标记）
//内联命令块：在对白或单独行中使用[Command key = value … wait = 0 / 1]
//多个命令块可出现在同一行，对应“并行命令”。默认 wait = 1；若想非阻塞，写 wait = 0。
//支持的命令与之前一致：ShowPortrait、ChangeExpression、HidePortrait、CameraMove、CameraZoom、CameraShake、PlaySE、Wait、Jump 等。
//Choice 区块：
//[Choice] 开始，随后若干选项行
//选项文本 -> jumpLabel
//光标“- ”后是显示给玩家的文本，-> 右侧是跳转到的标签名
//结束条件：遇到空行、下一个[Step]、或文件结束
//生成一条 Choice 命令，wait=1
//允许在对白行后附加命令块作为并行命令，例如摄像机/立绘效果与说话同步

    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    // 将“对话导向剧本语法”的 txt 文本解析为 ScenarioData
    public static class TxtDialogueScriptParser
    {
        // 入口：传入完整文本，返回 ScenarioData
        public static ScenarioData Parse(string txt, string scenarioId = "script_txt")
        {
            var sc = new ScenarioData { id = scenarioId };
            var lines = SplitLines(txt);
            int idx = 0;

            StepData currentStep = null;

            while (idx < lines.Count)
            {
                string raw = lines[idx];
                idx++;

                string line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (IsComment(line)) continue;

                // Step 块
                if (IsStepHeader(line, out string stepLabel))
                {
                    currentStep = new StepData { label = string.IsNullOrEmpty(stepLabel) ? null : stepLabel };
                    sc.steps.Add(currentStep);
                    continue;
                }

                // Choice 块
                if (IsChoiceHeader(line))
                {
                    EnsureStep(ref currentStep, sc);
                    var choiceCmd = ParseChoiceBlock(lines, ref idx);
                    currentStep.commands.Add(choiceCmd);
                    continue;
                }

                // 对白行或纯命令行
                EnsureStep(ref currentStep, sc);
                ParseDialogueOrCommandLine(line, currentStep);
            }

            if (sc.steps.Count == 0)
            {
                // 自动创建空 Step，避免后续空引用
                sc.steps.Add(new StepData());
            }
            return sc;
        }

        // 将一行解析为对白+内联命令 或 单行命令们
        private static void ParseDialogueOrCommandLine(string line, StepData step)
        {
            // 提取行中的内联命令块 [Cmd ...]
            var commandBlocks = ExtractCommandBlocks(line, out string lineWithoutBlocks);

            // 尝试对白格式：Name: content
            string speaker = null;
            string content = null;

            int colon = IndexOfSpeakerColon(lineWithoutBlocks);
            if (colon > 0)
            {
                speaker = lineWithoutBlocks.Substring(0, colon).Trim();
                content = lineWithoutBlocks.Substring(colon + 1).Trim();

                if (!string.IsNullOrEmpty(speaker) && !string.IsNullOrEmpty(content))
                {
                    var say = NewCommand("TypeText", true);
                    say.s.Add("name", speaker);

                    // 支持 “textKey: xxx” 的写法（例如 content="textKey:intro.line1"）
                    if (content.StartsWith("textKey:", StringComparison.OrdinalIgnoreCase))
                    {
                        string tk = content.Substring("textKey:".Length).Trim();
                        say.s.Add("textKey", tk);
                    }
                    else
                    {
                        say.s.Add("text", content);
                    }
                    step.commands.Add(say);
                }
            }
            else
            {
                // 非对白行：可能是纯命令行，语法形如： [Cmd ...] [Cmd ...] 或 CameraZoom fov=55 duration=0.5 等
                // 如果没有方括号，会尝试解析为一条“命令行（首 token 为 type）”
                if (commandBlocks.Count == 0)
                {
                    var tokens = SplitTokens(lineWithoutBlocks);
                    if (tokens.Count > 0)
                    {
                        var cmd = ParseCommandFromTokens(tokens, defaultWait: true);
                        if (cmd != null) step.commands.Add(cmd);
                    }
                }
            }

            // 把内联命令块并行加入
            foreach (var block in commandBlocks)
            {
                var cmd = ParseCommandBlock(block);
                if (cmd != null) step.commands.Add(cmd);
            }
        }

        // 解析 Choice 区块
        private static CommandData ParseChoiceBlock(List<string> lines, ref int idx)
        {
            var cmd = NewCommand("Choice", true);
            cmd.listS = new List<SerializableDict<string, string>>();

            // 读取直到空行/下一 Step/下一 Choice/文件结束
            while (idx < lines.Count)
            {
                string raw = lines[idx];
                string l = raw.Trim();
                if (string.IsNullOrEmpty(l) || IsComment(l) || IsStepHeader(l, out _) || IsChoiceHeader(l)) break;

                // 选项行语法：- 文本 -> jumpLabel
                // 允许写成：- textKey:choice.left -> go_left
                if (l.StartsWith("-"))
                {
                    string body = l.Substring(1).Trim();
                    string textPart = body;
                    string jumpPart = null;

                    int arrow = body.IndexOf("->", StringComparison.Ordinal);
                    if (arrow >= 0)
                    {
                        textPart = body.Substring(0, arrow).Trim();
                        jumpPart = body.Substring(arrow + 2).Trim();
                    }

                    var row = new SerializableDict<string, string>();
                    if (!string.IsNullOrEmpty(textPart))
                    {
                        if (textPart.StartsWith("textKey:", StringComparison.OrdinalIgnoreCase))
                        {
                            row.Add("textKey", textPart.Substring("textKey:".Length).Trim());
                        }
                        else
                        {
                            row.Add("text", textPart);
                        }
                    }
                    if (!string.IsNullOrEmpty(jumpPart))
                    {
                        row.Add("jumpLabel", jumpPart);
                    }
                    cmd.listS.Add(row);
                }
                else
                {
                    // 非法或结束，跳出
                    break;
                }

                idx++;
            }
            return cmd;
        }

        // 解析形如 [Command key=value ...] 的块
        private static CommandData ParseCommandBlock(string block)
        {
            // 去掉两端方括号
            string inner = block.Trim();
            if (inner.StartsWith("[")) inner = inner.Substring(1);
            if (inner.EndsWith("]")) inner = inner.Substring(0, inner.Length - 1);
            var tokens = SplitTokens(inner);
            if (tokens.Count == 0) return null;

            return ParseCommandFromTokens(tokens, defaultWait: true);
        }

        // 将一串 tokens（第一个为 type，其后是 key=value）解析为 CommandData
        private static CommandData ParseCommandFromTokens(List<string> tokens, bool defaultWait)
        {
            string type = tokens[0];
            var cmd = NewCommand(type, defaultWait);

            for (int i = 1; i < tokens.Count; i++)
            {
                var kv = tokens[i];
                int eq = kv.IndexOf('=');
                if (eq <= 0) continue;
                string key = kv.Substring(0, eq).Trim();
                string valRaw = kv.Substring(eq + 1).Trim();

                // 去除包裹引号
                string val = Unquote(valRaw);

                if (key.Equals("wait", StringComparison.OrdinalIgnoreCase))
                {
                    cmd.wait = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                // 按类型填充 s/f/i
                if (float.TryParse(val, out var fv))
                {
                    EnsureF(cmd).Add(key, fv);
                }
                else if (int.TryParse(val, out var iv))
                {
                    EnsureI(cmd).Add(key, iv);
                }
                else
                {
                    EnsureS(cmd).Add(key, val);
                }
            }
            return cmd;
        }

        // 提取一行中的所有 [ ... ] 命令块，返回块列表，并输出去除块后的主文本部分
        private static List<string> ExtractCommandBlocks(string line, out string withoutBlocks)
        {
            var blocks = new List<string>();
            var sbMain = new StringBuilder();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '[')
                {
                    int depth = 1;
                    int j = i + 1;
                    while (j < line.Length && depth > 0)
                    {
                        if (line[j] == '[') depth++;
                        else if (line[j] == ']') depth--;
                        j++;
                    }
                    if (depth == 0)
                    {
                        string block = line.Substring(i, j - i);
                        blocks.Add(block);
                        i = j; // 跳过这一整块
                        continue;
                    }
                    else
                    {
                        // 方括号不匹配，视为普通文本
                        sbMain.Append(line[i]);
                        i++;
                    }
                }
                else
                {
                    sbMain.Append(line[i]);
                    i++;
                }
            }
            withoutBlocks = sbMain.ToString().Trim();
            return blocks;
        }

        // 辅助：判断“Name: ...”的冒号位置，但避开首字符为'['的命令行
        private static int IndexOfSpeakerColon(string line)
        {
            if (string.IsNullOrEmpty(line)) return -1;
            if (line.StartsWith("[")) return -1;
            // 找到第一个冒号，且冒号前必须有文字
            int idx = line.IndexOf(':');
            if (idx > 0) return idx;
            return -1;
        }

        // 工具函数们
        private static bool IsComment(string line)
        {
            return line.StartsWith("#") || line.StartsWith("//");
        }

        private static bool IsStepHeader(string line, out string label)
        {
            label = null;
            if (!line.StartsWith("[Step")) return false;
            // 允许形式：[Step] 或 [Step label]
            if (!line.EndsWith("]")) return false;
            string inner = line.Substring(1, line.Length - 2).Trim(); // 去掉 [ ]
            string[] parts = inner.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1 && parts[0].Equals("Step", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length >= 2) label = parts[1];
                return true;
            }
            return false;
        }

        private static bool IsChoiceHeader(string line)
        {
            return line.Equals("[Choice]", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureStep(ref StepData step, ScenarioData sc)
        {
            if (step == null)
            {
                step = new StepData();
                sc.steps.Add(step);
            }
        }

        private static CommandData NewCommand(string type, bool wait)
        {
            return new CommandData
            {
                type = type,
                wait = wait,
                s = new SerializableDict<string, string>(),
                f = new SerializableDict<string, float>(),
                i = new SerializableDict<string, int>()
            };
        }

        private static SerializableDict<string, string> EnsureS(CommandData c)
        {
            if (c.s == null) c.s = new SerializableDict<string, string>();
            return c.s;
        }
        private static SerializableDict<string, float> EnsureF(CommandData c)
        {
            if (c.f == null) c.f = new SerializableDict<string, float>();
            return c.f;
        }
        private static SerializableDict<string, int> EnsureI(CommandData c)
        {
            if (c.i == null) c.i = new SerializableDict<string, int>();
            return c.i;
        }

        private static string Unquote(string v)
        {
            if (string.IsNullOrEmpty(v)) return v;
            v = v.Trim();
            if ((v.StartsWith("\"") && v.EndsWith("\"")) || (v.StartsWith("'") && v.EndsWith("'")))
            {
                return v.Substring(1, v.Length - 2);
            }
            return v;
        }

        private static List<string> SplitTokens(string input)
        {
            // 支持引号保护，空白为分隔
            var res = new List<string>();
            if (string.IsNullOrEmpty(input)) return res;

            bool inQuote = false;
            char quoteChar = '\0';
            var buf = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if ((c == '"' || c == '\'') && (!inQuote || c == quoteChar))
                {
                    if (!inQuote) { inQuote = true; quoteChar = c; }
                    else { inQuote = false; quoteChar = '\0'; }
                    buf.Append(c);
                    continue;
                }

                if (!inQuote && char.IsWhiteSpace(c))
                {
                    if (buf.Length > 0) { res.Add(buf.ToString()); buf.Clear(); }
                }
                else
                {
                    buf.Append(c);
                }
            }
            if (buf.Length > 0) res.Add(buf.ToString());
            return res;
        }

        private static List<string> SplitLines(string txt)
        {
            return new List<string>(txt.Replace("\r\n", "\n").Split('\n'));
        }
    }

}