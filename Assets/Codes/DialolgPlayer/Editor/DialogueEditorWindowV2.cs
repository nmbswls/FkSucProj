using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using My.Util;
using static PlasticPipe.Server.MonitorStats;
using static UnityEditor.LightingExplorerTableColumn;

namespace My.Dialog
{
    public class DialogueEditorWindowV2 : EditorWindow
    {
        private EditorDialogueData currentData;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProp;

        // --- UI 列表 ---
        private ReorderableList mainStepList;

        // --- 缓存 ---
        private Dictionary<string, ReorderableList> innerListCache = new Dictionary<string, ReorderableList>();
        private Vector2 scrollPos;

        private static Type[] commandTypes;

        [MenuItem("Tools/Dialogue Editor (V2)")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEditorWindowV2>("Dialogue");
        }

        private void OnEnable()
        {
            // 获取所有 DialogCommandBase 的子类
            commandTypes = typeof(DialogCommandBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DialogCommandBase)) && !t.IsAbstract)
                .ToArray();

            if (currentData != null) InitSerializedObject();
        }

        private void OnGUI()
        {
            // --- 顶部工具栏 ---
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            currentData = (EditorDialogueData)EditorGUILayout.ObjectField("Target Data", currentData, typeof(EditorDialogueData), false, GUILayout.Width(300));

            if (EditorGUI.EndChangeCheck()) InitSerializedObject();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (serializedObject == null || currentData == null)
            {
                EditorGUILayout.HelpBox("Please select a Dialogue Data asset.", MessageType.Info);
                return;
            }

            serializedObject.Update();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (mainStepList != null) mainStepList.DoLayoutList();
            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
        }

        private void InitSerializedObject()
        {
            if (currentData == null) return;
            serializedObject = new SerializedObject(currentData);
            stepsProp = serializedObject.FindProperty("Steps");
            innerListCache.Clear();

            mainStepList = new ReorderableList(serializedObject, stepsProp, true, true, true, true);
            mainStepList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Story Timeline ({stepsProp.arraySize} Steps)");

            // 计算外层高度
            mainStepList.elementHeightCallback = (index) =>
            {
                // 1. 安全检查：防止索引越界
                if (index >= stepsProp.arraySize) return 0;

                // 2. 获取当前的 Step 元素
                SerializedProperty stepElement = stepsProp.GetArrayElementAtIndex(index);

                // 3. 检查 Step 是否折叠
                // 注意：这里使用的是 Step 自己的 isExpanded 属性
                bool isStepExpanded = stepElement.FindPropertyRelative("IsExpanded").boolValue;

                float headerHeight = EditorGUIUtility.singleLineHeight + 10; // 标题栏 + Padding

                // 如果折叠，只返回标题栏高度
                if (!isStepExpanded) return headerHeight;

                // 4. 如果展开，需要计算内层列表 (InnerList) 的实际高度
                // GetInnerList 会从缓存中获取或创建该 Step 对应的 ReorderableList
                ReorderableList innerList = GetInnerList(index);

                // innerList.GetHeight() 会自动调用内层列表自己的 elementHeightCallback 并累加
                return headerHeight + innerList.GetHeight() + 40 + 10; // 加上底部 Padding
            };

            // 绘制外层元素
            mainStepList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= stepsProp.arraySize) return;
                SerializedProperty element = stepsProp.GetArrayElementAtIndex(index);
                SerializedProperty id = element.FindPropertyRelative("Id");
                SerializedProperty note = element.FindPropertyRelative("Note");
                SerializedProperty isExpanded = element.FindPropertyRelative("IsExpanded");

                rect.y += 2;

                // 1. 绘制背景框
                GUI.Box(new Rect(rect.x, rect.y, rect.width, rect.height - 4), "", EditorStyles.helpBox);

                // 定义一些常量高度和位置
                float headerHeight = EditorGUIUtility.singleLineHeight;
                float currentY = rect.y + 2;

                // --- 2. 交互逻辑分离 ---

                // A. 折叠箭头 (单独的点击区域)
                Rect arrowRect = new Rect(rect.x + 5, currentY, 20, headerHeight);
                // 使用 Foldout 控件本身来处理点击，而不是覆盖一个 Button
                bool newExpandedState = EditorGUI.Foldout(arrowRect, isExpanded.boolValue, "");
                if (newExpandedState != isExpanded.boolValue)
                {
                    isExpanded.boolValue = newExpandedState;
                }

                // B. ID 标签和输入框 (正常绘制，不被 Button 覆盖)
                float currentX = rect.x + 25;
                Rect idLabelRect = new Rect(currentX, currentY, 30, headerHeight);
                EditorGUI.LabelField(idLabelRect, "ID:", EditorStyles.miniBoldLabel);
                currentX += 30;

                Rect idFieldRect = new Rect(currentX, currentY, 100, headerHeight);
                EditorGUI.PropertyField(idFieldRect, id, GUIContent.none);
                currentX += 110;

                // C. Note 标签和输入框
                Rect noteLabelRect = new Rect(currentX, currentY, 40, headerHeight);
                EditorGUI.LabelField(noteLabelRect, "Note:", EditorStyles.miniLabel);
                currentX += 40;

                Rect noteFieldRect = new Rect(currentX, currentY, rect.width - (currentX - rect.x) - 5, headerHeight);
                EditorGUI.PropertyField(noteFieldRect, note, GUIContent.none);

                // D. 点击空白处折叠 (可选)
                // 只有当鼠标点击的位置 *不在* 两个输入框内时，才触发折叠
                // 这是一个更高级的体验优化，如果你觉得麻烦，可以只依赖箭头折叠。
                // 下面是实现逻辑：检测鼠标点击事件
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    // 定义标题栏区域
                    Rect headerBarRect = new Rect(rect.x, rect.y, rect.width, headerHeight + 4);

                    // 如果点击在标题栏内
                    if (headerBarRect.Contains(Event.current.mousePosition))
                    {
                        // 且没有点在 ID 输入框内，也没有点在 Note 输入框内，也没有点在箭头上
                        if (!idFieldRect.Contains(Event.current.mousePosition) &&
                            !noteFieldRect.Contains(Event.current.mousePosition) &&
                            !arrowRect.Contains(Event.current.mousePosition))
                        {
                            isExpanded.boolValue = !isExpanded.boolValue;
                            Event.current.Use(); // 消耗掉事件，防止传递给列表选中逻辑
                        }
                    }
                }

                //// 3. 绘制内层列表
                //if (isExpanded.boolValue)
                //{
                //    ReorderableList innerList = GetInnerList(index);
                //    innerList.DoList(new Rect(rect.x + 15, rect.y + headerHeight + 5, rect.width - 30, rect.height - headerHeight - 10));
                //}
                // --- 3. 绘制内层列表与底部按钮 ---
                if (isExpanded.boolValue)
                {
                    ReorderableList innerList = GetInnerList(index);
                    float listHeight = innerList.GetHeight();

                    // A. 绘制列表
                    // 注意：列表高度由 innerList.GetHeight() 决定
                    innerList.DoList(new Rect(rect.x + 15, rect.y + headerHeight + 5, rect.width - 30, listHeight));

                    // B. 绘制底部快捷按钮
                    float buttonsY = rect.y + headerHeight + 5 + listHeight + 4; // 列表下方留点空隙
                    float buttonHeight = 30;

                    // 按钮 1: "Add Dialogue Line" (占据主要宽度)
                    Rect quickAddRect = new Rect(rect.x + 15, buttonsY, rect.width - 30 - 35, buttonHeight);
                    if (GUI.Button(quickAddRect, "Add Dialogue Line"))
                    {
                        // 请将 ShowTextCommand 替换为你项目中实际的对话类名 (例如 DialogCommandShowText)
                        {
                            serializedObject.Update();

                            // 1. 重新找到那个 Step 下的 commands 数组
                            var steps = serializedObject.FindProperty("Steps");
                            var currentStep = steps.GetArrayElementAtIndex(index);

                            var freshCommandsProp = currentStep.FindPropertyRelative("Commands");

                            // 2. 插入逻辑
                            int cmdIndex = freshCommandsProp.arraySize;
                            string newSpeaker = string.Empty;
                            if(cmdIndex > 0)
                            {
                                var lastCmdElement = freshCommandsProp.GetArrayElementAtIndex(cmdIndex - 1);
                                var lastInnerCmd = lastCmdElement.FindPropertyRelative("CommandData");
                                if(lastInnerCmd.managedReferenceValue is DialogueTextCommand lastDialogCmd)
                                {
                                    newSpeaker = lastDialogCmd.Speaker;
                                }
                            }
                            freshCommandsProp.InsertArrayElementAtIndex(cmdIndex);

                            // 3. 这里的 SetDirty 不是必须的，因为后面有 Apply

                            // 4. 获取刚插入的元素
                            var newElement = freshCommandsProp.GetArrayElementAtIndex(cmdIndex);

                            // 5. 初始化内部字段默认值 (防止空指针)
                            // 这一步很重要，因为 Insert 有时会复制上一个元素的值，有时是默认值
                            var innerData = newElement.FindPropertyRelative("CommandData");

                            // 6. 赋值
                            var commandText = new DialogueTextCommand();
                            commandText.Speaker = newSpeaker;
                            innerData.managedReferenceValue = commandText;

                            serializedObject.ApplyModifiedProperties();
                            innerListCache.Clear();
                        }
                        //AddCommand(index, new ShowTextCommand());
                    }

                    // 按钮 2: "+" (更多类型)
                    Rect moreRect = new Rect(quickAddRect.xMax + 5, buttonsY, 30, buttonHeight);
                    if (GUI.Button(moreRect, "+"))
                    {
                        ShowAddMenu(index);
                    }
                }
            };

            mainStepList.onAddCallback = l => { ReorderableList.defaultBehaviours.DoAddButton(l); innerListCache.Clear(); };
            mainStepList.onRemoveCallback = l => { ReorderableList.defaultBehaviours.DoRemoveButton(l); innerListCache.Clear(); };
            mainStepList.onReorderCallback = l => innerListCache.Clear();
        }

        private ReorderableList GetInnerList(int stepIndex)
        {
            string key = stepIndex.ToString();
            if (innerListCache.ContainsKey(key) && innerListCache[key].serializedProperty != null) return innerListCache[key];

            SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(stepIndex);
            SerializedProperty commandsProp = stepProp.FindPropertyRelative("Commands");

            ReorderableList list = new ReorderableList(stepProp.serializedObject, commandsProp, true, true, false, false);
            list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Command Flow");

            //list.elementHeightCallback = (i) =>
            //{
            //    if (i >= commandsProp.arraySize) return 0;
            //    var el = commandsProp.GetArrayElementAtIndex(i);

            //    // 如果折叠，只返回标题高度
            //    if (el.FindPropertyRelative("IsFolded").boolValue) return EditorGUIUtility.singleLineHeight + 4;

            //    // 特殊处理 ChoiceCommand 的高度计算
            //    object cmdObj = GetTargetObjectOfProperty(el);
            //    if (cmdObj is ChoiceCommand) return GetChoiceCommandHeight(el);

            //    // 普通 Command 使用通用计算
            //    return GetGenericCommandHeight(el);
            //};

            //list.drawElementCallback = (r, i, active, focused) =>
            //{
            //    if (i >= commandsProp.arraySize) return;
            //    var el = commandsProp.GetArrayElementAtIndex(i);
            //    DrawCommandElement(r, el);
            //};
            // --- 修复重点：高度计算回调 ---
            list.elementHeightCallback = (i) =>
            {
                if (i >= commandsProp.arraySize) return 0;

                // 1. 获取外层包装器 (EditorDialogCommand)
                var wrapperProp = commandsProp.GetArrayElementAtIndex(i);

                // 2. 检查外层的折叠状态
                // 如果是折叠的，只需要一行的高度
                if (wrapperProp.FindPropertyRelative("IsFolded").boolValue)
                    return EditorGUIUtility.singleLineHeight + 4;

                // 3. 【关键修改】深入获取内部数据属性 (CommandData)
                var innerDataProp = wrapperProp.FindPropertyRelative("CommandData");

                // 4. 【关键修改】获取内部数据的真实对象类型
                // 注意：这里传入的是 innerDataProp，而不是 wrapperProp
                object realCommandObj = GetTargetObjectOfProperty(innerDataProp);

                // 5. 根据真实类型分流
                if (realCommandObj is ChoiceCommand)
                {
                    // 必须传入 innerDataProp，因为 ChoiceCommand 的字段 (如 Options) 在这一层
                    return GetChoiceCommandHeight(innerDataProp);
                }

                // 普通 Command 使用通用计算，也建议传入 innerDataProp
                return GetGenericCommandHeight(innerDataProp);
            };

            list.drawElementCallback = (r, i, active, focused) =>
            {
                if (i >= commandsProp.arraySize) return;
                var el = commandsProp.GetArrayElementAtIndex(i);
                DrawCommandElement(r, el);
            };

            list.onAddDropdownCallback = (r, l) => ShowAddMenu(stepIndex);
            innerListCache[key] = list;
            return list;
        }

        // --- 核心修改：统一绘制入口 ---
        private void DrawCommandElement(Rect rect, SerializedProperty element)
        {
            // 1. 获取包装器里的属性
            var isFoldedProp = element.FindPropertyRelative("IsFolded");
            var innerDataProp = element.FindPropertyRelative("CommandData");

            // 2. 获取内部数据的实际对象 (用于显示 Summary 和判断类型)
            // 注意：这里我们要获取 innerDataProp 的目标对象，而不是 element 的
            DialogCommandBase cmdData = GetTargetObjectOfProperty(innerDataProp) as DialogCommandBase;

            string summary = cmdData != null ? cmdData.GetSummary() : "Empty Slot";

            rect.y += 2;
            Rect titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            // 绘制折叠栏
            isFoldedProp.boolValue = EditorGUI.Foldout(titleRect, isFoldedProp.boolValue, summary, true);

            if (!isFoldedProp.boolValue)
            {
                Rect contentRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, rect.height);

                // 3. 根据内部数据的类型分流
                if (cmdData is ChoiceCommand) // 假设 ChoiceCommand 继承自 DialogCommandBase
                {
                    // 注意：这里传 innerDataProp (实际数据)，而不是 element (包装器)
                    DrawChoiceCommandProperty(contentRect, innerDataProp);
                }
                else if (cmdData is DialogueTextCommand)
                {
                    // 不再画折叠箭头，直接画内容，省去点击展开的步骤
                    // 计算布局
                    Rect speakerRect = new Rect(rect.x, rect.y, 80, rect.height); // 左边名字
                    Rect textRect = new Rect(rect.x + 85, rect.y, rect.width - 85, rect.height); // 右边内容

                    var speakerProp = innerDataProp.FindPropertyRelative("Speaker");
                    var contentProp = innerDataProp.FindPropertyRelative("Content");

                    // 名字栏
                    EditorGUI.PropertyField(speakerRect, speakerProp, GUIContent.none);

                    // 内容栏 (使用 TextArea 支持多行)
                    contentProp.stringValue = EditorGUI.TextArea(textRect, contentProp.stringValue, EditorStyles.textArea);
                }
                else
                {
                    // 通用绘制，也传 innerDataProp
                    DrawGenericCommandProperties(innerDataProp, contentRect);
                }
            }

            // --- 新增：通用删除按钮 (右上角的小X) ---
            // 计算位置：最右侧，留出一点边距
            float buttonSize = 20;
            Rect deleteRect = new Rect(rect.x + rect.width - buttonSize, rect.y, buttonSize, EditorGUIUtility.singleLineHeight);

            // 使用一个小样式的按钮
            if (GUI.Button(deleteRect, "X", EditorStyles.miniButton))
            {
                // 获取当前元素在 list 中的索引
                // 注意：wrapperProp 是 GetArrayElementAtIndex 获取的，我们可以反推
                // 或者更简单的，把 index 参数传给 DrawCommandElement (推荐做法)

                // 由于这里没有传 index，我们用一个稍微 "黑魔法" 一点但很方便的方法：
                // 直接操作 property 删除自身
                DeleteElement(element);
            }
        }


        // 辅助删除方法
        private void DeleteElement(SerializedProperty element)
        {
            // 获取所属的数组属性
            SerializedProperty listProp = element.serializedObject.FindProperty(element.propertyPath.Split(new string[] { ".Array" }, StringSplitOptions.None)[0]);

            // 找到自己的索引
            // 这是一个简单的遍历查找，因为 propertyPath 包含索引信息 "steps.Array.data[0].Commands.Array.data[5]"
            // 但为了稳健，我们通过 content hash 或循环对比

            // 最简单的做法：其实你最好修改 DrawCommandElement 的签名，传入 int index
            // 假设你修改了签名: DrawCommandElement(Rect rect, int index, SerializedProperty wrapperProp, SerializedProperty listProp)
            // listProp.DeleteArrayElementAtIndex(index);

            // 如果不想改签名，且列表不是很长，可以用这个简易版逻辑：
            // *警告*：直接在 Draw 中删除可能导致报错 (EndLayoutGroup)，
            // 最好用 EditorApplication.delayCall 延迟一帧执行删除

            var propertyPath = element.propertyPath;
            EditorApplication.delayCall += () =>
            {
                element.serializedObject.Update();
                // 重新查找属性（防止引用丢失）
                var target = element.serializedObject.FindProperty(propertyPath);
                if (target != null)
                {
                    // 这是一个比较通用的删除当前 Property 的技巧
                    // 但最稳妥的还是在 Loop 外面删。
                    // 鉴于 ReorderableList 的封装性，推荐下面的 "方案 B"
                }
            };
        }

        // --- 特殊绘制逻辑：Choice Command ---
        // 为了支持 SerializedProperty 的撤销系统，我们需要用 Property 查找子属性
        private void DrawChoiceCommandProperty(Rect rect, SerializedProperty choiceCmdProp)
        {
            // 因为我们是在 OnGUI 内部计算布局，这里使用 BeginArea 来在一个固定 Rect 中绘制复杂 UI
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical(); // 不再加 box，因为外层已经可能有背景了

            SerializedProperty optionsProp = choiceCmdProp.FindPropertyRelative("Options");

            // 遍历 Options 列表
            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");

                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel, GUILayout.Width(70));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    optionsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Fields
                EditorGUILayout.PropertyField(optionProp.FindPropertyRelative("Text"));
                EditorGUILayout.PropertyField(optionProp.FindPropertyRelative("TargetStepId"));

                // Conditions List
                DrawConditionListForProperty(optionProp);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Option"))
            {
                optionsProp.InsertArrayElementAtIndex(optionsProp.arraySize);
                // 初始化新元素（避免数据残留）
                var newElem = optionsProp.GetArrayElementAtIndex(optionsProp.arraySize - 1);
                newElem.FindPropertyRelative("Text").stringValue = "New Option";
                newElem.FindPropertyRelative("Conditions1").ClearArray();
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        //private void DrawScriptModeDialogue(Rect rect, SerializedProperty innerDataProp)
        //{
        //    // 获取字段
        //    var speakerProp = innerDataProp.FindPropertyRelative("Speaker"); 
        //    var contentProp = innerDataProp.FindPropertyRelative("Content");   

        //    // 1. 【读取合成】：把数据变成 "Speaker : Content" 的格式显示
        //    string speaker = speakerProp.stringValue;
        //    string content = contentProp.stringValue;
        //    string displayValue;

        //    if (string.IsNullOrWhiteSpace(speaker))
        //    {
        //        displayValue = content; // 没有说话人，直接显示内容
        //    }
        //    else
        //    {
        //        displayValue = $"{speaker} : {content}"; // 加上冒号
        //    }

        //    // 2. 绘制 TextArea
        //    // 稍微调整 rect 留出一点边距
        //    Rect textAreaRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);

        //    EditorGUI.BeginChangeCheck();

        //    // 使用 TextArea 支持多行，并且自带边框
        //    string newValue = EditorGUI.TextArea(textAreaRect, displayValue, EditorStyles.textArea);

        //    // 3. 【写入解析】：当用户修改文本后，拆分回填
        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        // 兼容中文冒号，把 '：' 替换成 ':' 方便统一处理
        //        string parseValue = newValue.Replace("：", ":");

        //        int colonIndex = parseValue.IndexOf(':');

        //        if (colonIndex >= 0)
        //        {
        //            // 有冒号 -> 拆分
        //            // 前半部分是名字，去除首尾空格
        //            string newSpeaker = parseValue.Substring(0, colonIndex).Trim();
        //            // 后半部分是内容，去除开头的空格 (保留内容里的换行等)
        //            string newContent = parseValue.Substring(colonIndex + 1).TrimStart();

        //            speakerProp.stringValue = newSpeaker;
        //            contentProp.stringValue = newContent;
        //        }
        //        else
        //        {
        //            // 没有冒号 -> 只有内容，清空名字
        //            speakerProp.stringValue = "";
        //            contentProp.stringValue = newValue; // 保持原样，不Trim，允许用户打空格
        //        }
        //    }
        //}


        // --- Choice Command 的高度计算 ---
        // 必须手动计算，否则 ReorderableList 会遮挡内容
        private float GetChoiceCommandHeight(SerializedProperty choiceCmdProp)
        {
            float h = EditorGUIUtility.singleLineHeight + 6; // Title

            SerializedProperty optionsProp = choiceCmdProp.FindPropertyRelative("Options");
            // 基础 Padding + 按钮高度
            h += 30;

            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(i);
                // 每个 Option 的 Box Padding
                h += 10;
                // Text + TargetStepId 属性高度
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("Text"));
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("TargetStepId"));
                h += 4; // spacing

                // Header 行
                h += EditorGUIUtility.singleLineHeight + 2;

                // Condition 列表高度
                SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");
                h += EditorGUIUtility.singleLineHeight + 4; // "Conditions:" title row
                                                            // 每个 Condition 一行
                h += (condsProp.arraySize * (20 + 2));
            }

            return h;
        }

        // --- Condition 列表绘制 (基于 Property) ---
        private void DrawConditionListForProperty(SerializedProperty optionProp)
        {
            SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Conditions:", EditorStyles.miniLabel, GUILayout.Width(70));

            if (GUILayout.Button("+ Add Condition", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Int Variable Check"), false, () => AddCondition(optionProp, new ConditionLocalVariableInt()));
                menu.AddItem(new GUIContent("String Variable Check"), false, () => AddCondition(optionProp, new ConditionLocalVariableString()));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < condsProp.arraySize; i++)
            {
                SerializedProperty condProp = condsProp.GetArrayElementAtIndex(i);

                // 为了获取 Condition 对象的实际值用于显示摘要和传给 Popup，这里需要反射
                DialogCondition condObj = GetTargetObjectOfProperty(condProp) as DialogCondition;

                EditorGUILayout.BeginHorizontal();

                // 显示摘要
                string summary = GetConditionSummary(condObj);
                if (GUILayout.Button(summary, EditorStyles.miniButton, GUILayout.Height(20)))
                {
                    Rect btnRect = GUILayoutUtility.GetLastRect();
                    // 弹出窗口修改的是具体的对象
                    PopupWindow.Show(btnRect, new DialogueConditionPopup(condObj, this, currentData));
                }

                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    condsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // --- 辅助：添加 Condition 的桥接方法 ---
        private void AddCondition(SerializedProperty optionProp, DialogCondition newCond)
        {
            // 因为 SerializedProperty 很难直接添加多态对象，这里我们操作底层对象然后 Update
            DialogChoiceOption optObj = GetTargetObjectOfProperty(optionProp) as DialogChoiceOption;
            if (optObj != null)
            {
                Undo.RecordObject(currentData, "Add Condition");
                optObj.Conditions1.Add(newCond);
                // 强制刷新 SerializedObject，否则 Property 不知道对象变了
                serializedObject.Update();
                // 清除缓存以重算高度
                innerListCache.Clear();
            }
        }

        // --- 之前的通用 Command 逻辑 ---
        private float GetGenericCommandHeight(SerializedProperty rootProp)
        {
            float h = EditorGUIUtility.singleLineHeight + 6;
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            prop.NextVisible(true);
            do
            {
                if (SerializedProperty.EqualContents(prop, endProp)) break;
                if (prop.name == "IsFolded") continue;
                h += EditorGUI.GetPropertyHeight(prop, true) + 2;
            } while (prop.NextVisible(false));
            return h;
        }

        private void DrawGenericCommandProperties(SerializedProperty rootProp, Rect startRect)
        {
            float curY = startRect.y;
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            prop.NextVisible(true);
            do
            {
                if (SerializedProperty.EqualContents(prop, endProp)) break;
                if (prop.name == "IsFolded") continue;

                float h = EditorGUI.GetPropertyHeight(prop, true);
                Rect drawRect = new Rect(startRect.x, curY, startRect.width, h);

                GUIContent label = new GUIContent(prop.displayName);
                if (prop.name == "Speaker") label.text = "Character ID";
                if (prop.name == "Content") label.text = "Dialogue Text";

                EditorGUI.PropertyField(drawRect, prop, label, true);
                curY += h + 2;
            } while (prop.NextVisible(false));
        }

        private void ShowAddMenu(int stepIndex)
        {
            var menu = new GenericMenu();

            // 遍历所有可能的逻辑数据类型 (例如: PlayMusic, ShowText, Option...)
            foreach (var type in commandTypes)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    serializedObject.Update();

                    // 1. 重新找到那个 Step 下的 commands 数组
                    var steps = serializedObject.FindProperty("Steps");
                    var currentStep = steps.GetArrayElementAtIndex(stepIndex);

                    var freshCommandsProp = currentStep.FindPropertyRelative("Commands");

                    // 2. 插入逻辑
                    int index = freshCommandsProp.arraySize;
                    freshCommandsProp.InsertArrayElementAtIndex(index);

                    // 3. 这里的 SetDirty 不是必须的，因为后面有 Apply

                    // 4. 获取刚插入的元素
                    var newElement = freshCommandsProp.GetArrayElementAtIndex(index);

                    // 5. 初始化内部字段默认值 (防止空指针)
                    // 这一步很重要，因为 Insert 有时会复制上一个元素的值，有时是默认值
                    var innerData = newElement.FindPropertyRelative("CommandData");

                    // 6. 赋值
                    object instance = Activator.CreateInstance(type);
                    innerData.managedReferenceValue = instance;

                    serializedObject.ApplyModifiedProperties();
                    innerListCache.Clear();
                });
            }

            // 如果没有找到任何命令类型，显示提示
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Command Types Found inheriting DialogCommandBase"));
            }

            menu.ShowAsContext();
        }

        // --- 反射工具 (保持不变) ---
        private object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null) return null;
            var path = prop.propertyPath.Replace(".Array.data[", "[").Replace("]", "");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else obj = GetValue_Imp(obj, element);
            }
            return obj;
        }
        private object GetValue_Imp(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null) return f.GetValue(source);
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null) return p.GetValue(source, null);
                type = type.BaseType;
            }
            return null;
        }
        private object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++) if (!enm.MoveNext()) return null;
            return enm.Current;
        }

        private string GetConditionSummary(DialogCondition cond)
        {
            if (cond is ConditionLocalVariableInt intC)
            {
                string op = "";
                switch (intC.Compare)
                {
                    case ConditionLocalVariableInt.CompareType.Equals: op = "=="; break;
                    case ConditionLocalVariableInt.CompareType.Greater: op = ">"; break;
                    case ConditionLocalVariableInt.CompareType.Less: op = "<"; break;
                    case ConditionLocalVariableInt.CompareType.GE: op = ">="; break;
                    case ConditionLocalVariableInt.CompareType.LE: op = "<="; break;
                    case ConditionLocalVariableInt.CompareType.NotEquals: op = "!="; break;
                }
                return $"[Int] {intC.VariableKey} {op} {intC.Value}";
            }
            else if (cond is ConditionLocalVariableString strC)
            {
                string op = (strC.Compare == ConditionLocalVariableString.CompareType.Equals) ? "==" : "!=";
                return $"[String] {strC.VariableKey} {op} \"{strC.Value}\"";
            }
            return "[Unknown]";
        }
    }
}



