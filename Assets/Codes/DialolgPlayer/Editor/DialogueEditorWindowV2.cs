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
using Codice.Client.BaseCommands.BranchExplorer;
using My.Quest;
using UnityEditor.U2D.Aseprite;
using Newtonsoft.Json;
using System.IO;

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
        private static Type[] conditionTypes;

        [MenuItem("Tools/Dialogue Editor (V2)")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEditorWindowV2>("Dialogue");
        }

        private void OnEnable()
        {
            commandTypes = typeof(EditorDialogCommand).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(EditorDialogCommand)) && !t.IsAbstract)
                .ToArray();


            conditionTypes = typeof(DialogCondition).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DialogCondition)) && !t.IsAbstract)
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

            GUILayout.FlexibleSpace(); // 把按钮推到最右边
            if (GUILayout.Button("Export to JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                if (currentData != null)
                {
                    ExportDialogueData(currentData);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a data file first!", "OK");
                }
            }

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
                                if(lastCmdElement.managedReferenceValue is EditorDialogueCommand4Text lastDialogCmd)
                                {
                                    if(lastDialogCmd.TextLines.Count > 0)
                                    {
                                        newSpeaker = lastDialogCmd.TextLines[0].Speaker;
                                    }
                                }
                            }
                            freshCommandsProp.InsertArrayElementAtIndex(cmdIndex);

                            // 3. 这里的 SetDirty 不是必须的，因为后面有 Apply

                            // 4. 获取刚插入的元素
                            var newElement = freshCommandsProp.GetArrayElementAtIndex(cmdIndex);

                            // 6. 赋值
                            var commandText = new EditorDialogueCommand4Text();
                            var firstLine = new EditorDialogueCommand4Text.TextLine();
                            firstLine.Speaker = newSpeaker;
                            newElement.managedReferenceValue = commandText;

                            commandText.TextLines.Add(firstLine);

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

            mainStepList.onAddCallback = l => { 
                ReorderableList.defaultBehaviours.DoAddButton(l); innerListCache.Clear();

                // 3. --- 开始清理新 Step 数据 ---
                var listProp = l.serializedProperty;

                // 获取刚刚添加的元素 (位于数组末尾)
                var newStepProp = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);

                newStepProp.FindPropertyRelative("Id").stringValue = (listProp.arraySize).ToString();
                newStepProp.FindPropertyRelative("Note").stringValue = "";

                SerializedProperty commandsProp = newStepProp.FindPropertyRelative("Commands");
                if (commandsProp != null && commandsProp.isArray)
                {
                    commandsProp.ClearArray();
                }

            };
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

                // 5. 根据真实类型分流
                if (wrapperProp.managedReferenceValue is EditorChoiceCommand)
                {
                    // 必须传入 innerDataProp，因为 ChoiceCommand 的字段 (如 Options) 在这一层
                    return GetChoiceCommandHeight(wrapperProp);
                }

                // 普通 Command 使用通用计算，也建议传入 innerDataProp
                return GetGenericCommandHeight(wrapperProp);
            };

            list.drawElementCallback = (r, i, active, focused) =>
            {
                if (i >= commandsProp.arraySize) return;
                var el = commandsProp.GetArrayElementAtIndex(i);
                DrawCommandElement(r, el, i);
            };

            list.onAddDropdownCallback = (r, l) => ShowAddMenu(stepIndex);
            innerListCache[key] = list;
            return list;
        }

        // --- 核心修改：统一绘制入口 ---
        private void DrawCommandElement(Rect rect, SerializedProperty element, int idx)
        {
            // --- 1. 布局规划 ---
            float btnSize = 20f;
            float spacing = 2f; // 按钮之间的间距

            // 计算右侧两个按钮的位置
            // [Delete] 在最右侧
            Rect deleteRect = new Rect(rect.x + rect.width - btnSize, rect.y, btnSize, EditorGUIUtility.singleLineHeight);
            // [Fold] 在 Delete 左侧
            Rect foldBtnRect = new Rect(deleteRect.x - btnSize - spacing, rect.y, btnSize, EditorGUIUtility.singleLineHeight);

            // 计算左侧内容的安全区域 (减去两个按钮的宽度)
            float safeWidth = rect.width - (btnSize * 2) - (spacing * 3);
            Rect contentAreaRect = new Rect(rect.x, rect.y, safeWidth, rect.height);

            // --- 2. 数据获取 ---
            var isFoldedProp = element.FindPropertyRelative("IsFolded");
            var cmdData = element.managedReferenceValue as EditorDialogCommand;

            // --- 3. 绘制折叠/展开逻辑 ---
            // 这里不再使用 EditorGUI.Foldout，而是根据状态手动绘制

            string summary = cmdData != null ? cmdData.GetSummary() : "Empty Slot";
            summary = $"{idx}:{summary}";

            // 如果是折叠状态 (IsFolded == true)
            if (isFoldedProp.boolValue)
            {
                Rect labelRect = new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, EditorGUIUtility.singleLineHeight);

                // 只画文本标签，没有点击热区，不会误触
                EditorGUI.LabelField(labelRect, summary, EditorStyles.boldLabel);
            }
            else
            {
                // 如果是展开状态 (IsFolded == false)
                // 此时直接画内容，头部没有 Label 遮挡

                if (cmdData is EditorChoiceCommand)
                {
                    // 给普通命令留一个标题高度，或者直接画属性
                    Rect contentRect = new Rect(contentAreaRect.x, contentAreaRect.y + EditorGUIUtility.singleLineHeight + 2, contentAreaRect.width, contentAreaRect.height);
                    // 这里可以补一个标题 Label 让界面好看点，或者直接留空
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, EditorGUIUtility.singleLineHeight), summary);
                    DrawChoiceCommandProperty(contentRect, element);
                }
                else if (cmdData is EditorDialogueCommand4Text)
                {
                    // === 剧本模式绘制 ===

                    float extraFieldsHeight = 0f;


                    // 1. 获取特定属性
                    var speakerProp = element.FindPropertyRelative("Speaker");
                    var contentProp = element.FindPropertyRelative("Content");

                    // 2. 绘制第一行 (Speaker + Content)
                    // 为了防止内容覆盖下面的属性，我们需要估算一个高度。
                    // 这里假设 TextArea 的高度是根据内容自动撑开的 (通常在 ElementHeight 里计算)
                    // 在这里我们让 TextRect 占据 rect 的绝大部分，只在底部留出空间。

                    // 此时我们用一种自动布局的方式来遍历剩余属性，这样比较通用

                    float singleLine = EditorGUIUtility.singleLineHeight;
                    float verticalSpacing = 2f;

                    // 临时创建一个 iterator 副本用于计算剩余属性的高度
                    var iterator = element.Copy();
                    var endProperty = iterator.GetEndProperty();
                    int extraFieldCount = 0;

                    iterator.NextVisible(true); // 进入子节点
                    while (!SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        if (iterator.name != "Speaker" && iterator.name != "Content" && iterator.name != "m_Script")
                        {
                            extraFieldCount++;
                        }
                        if (!iterator.NextVisible(false)) break;
                    }

                    float bottomAreaHeight = extraFieldCount * (singleLine + verticalSpacing);

                    // 计算上半部分给 TextArea 的高度
                    float topAreaHeight = contentAreaRect.height - bottomAreaHeight - verticalSpacing;
                    if (topAreaHeight < singleLine) topAreaHeight = singleLine; // 最小保底

                    // --- 绘制 Speaker 和 Content ---
                    Rect topRowRect = new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, topAreaHeight);

                    Rect speakerRect = new Rect(topRowRect.x, topRowRect.y, 80, topRowRect.height);
                    Rect textRect = new Rect(topRowRect.x + 85, topRowRect.y, topRowRect.width - 85, topRowRect.height);

                    EditorGUI.PropertyField(speakerRect, speakerProp, GUIContent.none);
                    contentProp.stringValue = EditorGUI.TextArea(textRect, contentProp.stringValue, EditorStyles.textArea);

                    // --- 绘制剩余字段 ---
                    if (extraFieldCount > 0)
                    {
                        // 起始 Y 坐标：在上半部分下方
                        float currentY = contentAreaRect.y + topAreaHeight + verticalSpacing;

                        iterator = element.Copy(); // 重置迭代器
                        iterator.NextVisible(true); // 进入内部

                        while (!SerializedProperty.EqualContents(iterator, endProperty))
                        {
                            // 跳过已绘制字段和脚本引用
                            if (iterator.name != "Speaker" && iterator.name != "Content" && iterator.name != "m_Script")
                            {
                                Rect fieldRect = new Rect(contentAreaRect.x, currentY, contentAreaRect.width, singleLine);

                                // 绘制属性 (包含 label)
                                EditorGUI.PropertyField(fieldRect, iterator, true);

                                currentY += singleLine + verticalSpacing;
                            }

                            if (!iterator.NextVisible(false)) break; // 不再进入更深层级
                        }
                    }
                }
                else
                {
                    Rect contentRect = new Rect(contentAreaRect.x, contentAreaRect.y + EditorGUIUtility.singleLineHeight + 2, contentAreaRect.width, contentAreaRect.height);
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, EditorGUIUtility.singleLineHeight), summary);
                    DrawGenericCommandProperties(element, contentRect);
                }
            }

            // --- 4. 绘制右侧按钮组 ---

            // [折叠/展开按钮]
            // 根据状态显示不同图标：_ (最小化) 或 □ (最大化/展开)
            string foldIcon = isFoldedProp.boolValue ? "□" : "_";
            if (GUI.Button(foldBtnRect, foldIcon, EditorStyles.miniButton))
            {
                isFoldedProp.boolValue = !isFoldedProp.boolValue;
            }

            // [删除按钮]
            if (GUI.Button(deleteRect, "×", EditorStyles.miniButton))
            {
                if (element.serializedObject != null) DeleteElement(element, idx);
            }

            // --- 5. 事件穿透防护 (关键) ---
            // 只要鼠标点击了这两个按钮的区域，就吞噬事件，防止 List 选中/拖拽/缩放
            if (Event.current.type == EventType.MouseDown)
            {
                if (deleteRect.Contains(Event.current.mousePosition) || foldBtnRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                }
            }
        }


        // 辅助删除方法
        private void DeleteElement(SerializedProperty element, int idx)
        {
            // 获取 SerializedObject 和 数组路径
            var serializedObject = element.serializedObject;

            // 通过简单的字符串截取找到父数组的路径
            // 比如 "Commands.Array.data[5]" -> "Commands"
            string propertyPath = element.propertyPath;
            string arrayPath = propertyPath.Substring(0, propertyPath.LastIndexOf(".Array"));

            // 使用 delayCall 防止在绘制帧报错
            EditorApplication.delayCall += () =>
            {
                serializedObject.Update();
                var listProp = serializedObject.FindProperty(arrayPath);

                // 双重保险：确保属性还存在，且索引有效
                if (listProp != null && idx < listProp.arraySize)
                {
                    // 如果是对象引用类型，第一次 Delete 可能是置空，第二次才是删除
                    // 但对于普通类（非 UnityEngine.Object），一次就删掉了
                    listProp.DeleteArrayElementAtIndex(idx);

                    // 它是对象引用且不为空，可能需要再删一次（视具体需求而定，通常一次够了）
                    // if (listProp.GetArrayElementAtIndex(index).objectReferenceValue != null) 
                    //     listProp.DeleteArrayElementAtIndex(index);

                    serializedObject.ApplyModifiedProperties();
                }

                // 3. 立即提交修改！不要等下一帧
                serializedObject.ApplyModifiedProperties();

                // 4. 强制重绘 Inspector！
                // 这一步是消除“延迟感”的关键。
                // 如果代码在 Editor 类里：
                this.Repaint(); 
            };
        }

        // --- 特殊绘制逻辑：Choice Command ---
        // 为了支持 SerializedProperty 的撤销系统，我们需要用 Property 查找子属性
        private void DrawChoiceCommandProperty(Rect rect, SerializedProperty choiceCmdProp)
        {
            // 1. 初始化当前绘制光标 y
            float currentY = rect.y;
            float standardHeight = EditorGUIUtility.singleLineHeight;
            float space = 2f;

            // 为了防止内容画出界，我们先不加 Box 背景，直接画内容
            // (如果想要背景，可以用 GUI.Box(rect, "")，但先确保内容能显示)

            SerializedProperty optionsProp = choiceCmdProp.FindPropertyRelative("Options");

            // 2. 遍历 Options 列表
            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(i);
                SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");

                // 基础高度 4行 + 15
                float fixedPartHeight = (standardHeight + space) * 5 + 15f;
                float conditionsPartHeight = 0f;
                // 2.2 累加每个 Condition 的高度
                if (condsProp.arraySize > 0)
                {
                    // 假设每个 Condition 占一行
                    conditionsPartHeight += condsProp.arraySize * (standardHeight + space);
                }

                float totalOptionHeight = fixedPartHeight + conditionsPartHeight;

                // 定义当前 Option 的区域
                Rect optionRect = new Rect(rect.x, currentY, rect.width, totalOptionHeight);
                GUI.Box(optionRect, GUIContent.none, "box"); // 画个框表示范围


                // --- 绘制 Header (Option X + Delete 按钮) ---
                Rect headerRect = new Rect(optionRect.x + 5, currentY + 5, 100, standardHeight);
                Rect deleteBtnRect = new Rect(optionRect.x + optionRect.width - 25, currentY + 5, 20, standardHeight);

                EditorGUI.LabelField(headerRect, $"Option {i + 1}", EditorStyles.boldLabel);
                if (GUI.Button(deleteBtnRect, "X"))
                {
                    optionsProp.DeleteArrayElementAtIndex(i);
                    return; // 立即返回，等待下一帧重绘
                }

                currentY += standardHeight + space + 5;

                // --- 绘制 Text 属性 ---
                Rect textRect = new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight);
                EditorGUI.PropertyField(textRect, optionProp.FindPropertyRelative("Text"));
                currentY += standardHeight + space;

                // --- 绘制 TargetStepId 属性 ---
                Rect targetRect = new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight);
                EditorGUI.PropertyField(targetRect, optionProp.FindPropertyRelative("TargetStepId"));
                currentY += standardHeight + space;

                // --- 绘制 show on fail 属性 ---
                Rect showFailRect = new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight);
                EditorGUI.PropertyField(showFailRect, optionProp.FindPropertyRelative("ShowWhenFail"));
                currentY += standardHeight + space;

                // --- 调用修改后的 Condition 绘制函数 ---
                Rect conditionStartRect = new Rect(rect.x, currentY, rect.width, 0); // 高度给0没事，函数只用 y
                // --- (暂时注释掉 ConditionList，确保基础能显示再放开) ---
                float conditionHeight = DrawConditionListForProperty(conditionStartRect, optionProp);
                currentY += conditionHeight + space; // 加上间距

                currentY += 10;
            }

            // 3. 绘制 "Add Option" 按钮
            // 这里的关键是：完全不用 GUILayout，直接算坐标
            Rect btnRect = new Rect(rect.x, currentY + 5, rect.width, 25);

            // --- 调试大法：如果你还看不见，这行红线会告诉你按钮在哪 ---
            // EditorGUI.DrawRect(btnRect, Color.red); 

            if (GUI.Button(btnRect, "+ Add Option"))
            {
                optionsProp.InsertArrayElementAtIndex(optionsProp.arraySize);
                var newElem = optionsProp.GetArrayElementAtIndex(optionsProp.arraySize - 1);

                newElem.FindPropertyRelative("Conditions1").ClearArray();

                newElem.FindPropertyRelative("Text").stringValue = "New Option";
                // newElem.FindPropertyRelative("Conditions1").ClearArray();
            }
        }

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
                h += 20;

                // Text + TargetStepId 属性高度
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("Text"));
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("TargetStepId"));
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("ShowWhenFail"));
                h += 6; // spacing

                // Header 行
                h += EditorGUIUtility.singleLineHeight + 2;

                // Condition 列表高度
                SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");
                h += EditorGUIUtility.singleLineHeight + 2; // "Conditions:" title row
                                                            // 每个 Condition 一行
                h += (condsProp.arraySize * (EditorGUIUtility.singleLineHeight + 2));
            }

            h += EditorGUIUtility.singleLineHeight + 10;

            return h;
        }

        // --- Condition 列表绘制 (基于 Property) ---
        private float DrawConditionListForProperty(Rect startRect, SerializedProperty optionProp)
        {
            SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");
            float currentY = startRect.y;
            float startX = startRect.x;
            float width = startRect.width;
            float lineHeight = EditorGUIUtility.singleLineHeight + 3f;
            float spacing = 2f;

            // --- 1. 标题行 + 添加按钮 ---
            Rect headerLabelRect = new Rect(startX + 5, currentY, 80, lineHeight);
            EditorGUI.LabelField(headerLabelRect, "Conditions:", EditorStyles.label);

            Rect addBtnRect = new Rect(startX + 100, currentY, 120, lineHeight);
            if (GUI.Button(addBtnRect, "+ Add Condition", EditorStyles.miniButton))
            {
                GenericMenu menu = new GenericMenu();

                foreach (var type in conditionTypes)
                {
                    var capturedType = type;

                    // 尝试获取 Attribute
                    var attr = (ConditionMenuNameAttribute)System.Attribute.GetCustomAttribute(type, typeof(ConditionMenuNameAttribute));

                    // 如果有 Attribute 就用 Attribute 的名字，否则用类名
                    string menuName = attr != null ? attr.MenuPath : type.Name;

                    menu.AddItem(new GUIContent(menuName), false, () =>
                    {
                        DialogCondition newCondition = (DialogCondition)System.Activator.CreateInstance(capturedType);
                        AddCondition(optionProp, newCondition);
                    });
                }

                menu.ShowAsContext();
            }

            currentY += lineHeight + spacing; // 换行

            // --- 2. 遍历 Conditions 列表 ---
            for (int i = 0; i < condsProp.arraySize; i++)
            {
                SerializedProperty condProp = condsProp.GetArrayElementAtIndex(i);
                DialogCondition condObj = GetTargetObjectOfProperty(condProp) as DialogCondition;

                Rect rowRect = new Rect(startX + 20, currentY, width - 25, lineHeight);

                // 删除按钮 (右侧 20px)
                Rect deleteRect = new Rect(rowRect.x + rowRect.width - 25, rowRect.y, 25, lineHeight);
                // 内容按钮 (剩余宽度)
                Rect contentRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 30, lineHeight);

                
                string summary = condObj.GetSummary();

                // 点击摘要弹出编辑窗口
                if (GUI.Button(contentRect, summary, EditorStyles.miniButton))
                {
                    // PopupWindow 需要屏幕坐标，Event.current.mousePosition 有时更好用，或者转换 Rect
                    // 这里直接传 rect 通常没问题
                    PopupWindow.Show(contentRect, new DialogueConditionPopup(condObj, this, currentData));
                }

                if (GUI.Button(deleteRect, "-", EditorStyles.miniButtonRight))
                {
                    condsProp.DeleteArrayElementAtIndex(i);
                    // 这里不需要 break，因为是 GUI 模式，稍后刷新即可，但为了安全可以 return
                    return currentY - startRect.y + lineHeight;
                }

                currentY += lineHeight + spacing; // 换行
            }

            // 返回总高度：当前 Y 减去 起始 Y
            return currentY - startRect.y;
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

                    // 6. 赋值
                    object instance = Activator.CreateInstance(type);
                    newElement.managedReferenceValue = instance;

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



        private static void ResetValues(SerializedProperty prop)
        {
            // 遍历该属性下的所有直接子属性并重置
            var iterator = prop.Copy();
            var end = iterator.GetEndProperty();

            // 进入第一个子节点
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, end)) break;

                    switch (iterator.propertyType)
                    {
                        case SerializedPropertyType.String:
                            iterator.stringValue = "";
                            break;
                        case SerializedPropertyType.Integer:
                            iterator.intValue = 0;
                            break;
                        case SerializedPropertyType.Float:
                            iterator.floatValue = 0f;
                            break;
                        case SerializedPropertyType.Boolean:
                            iterator.boolValue = false;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            iterator.objectReferenceValue = null;
                            break;
                        case SerializedPropertyType.Generic:
                            // 如果是数组/List，清空它
                            if (iterator.isArray) iterator.ClearArray();
                            break;
                    }
                } while (iterator.NextVisible(false)); // false 表示不进入更深层级，只重置当前层级的直接子字段
            }
        }



        private void ExportDialogueData(EditorDialogueData source)
        {
            // 1. 选择保存路径
            string path = EditorUtility.SaveFilePanel("Export JSON", Application.streamingAssetsPath, source.name, "json");
            if (string.IsNullOrEmpty(path)) return;

            // 2. 将 ScriptableObject 转换为 运行时 DTO 对象
            //    这一步是为了剥离编辑器专用的数据（如 Rect 坐标），并将引用转为字符串
            DialogueData runtimeData = new DialogueData();
            runtimeData.DialogId = source.name;

            foreach (var srcStep in source.Steps)
            {
                var runStep = new DialogueStepData();
                runStep.Id = srcStep.Id;
                runStep.Note = srcStep.Note; // 假设有 Content 字段

                foreach (var srcCommand in srcStep.Commands)
                {
                    DialogCommandData runCommand = null;

                    switch(srcCommand)
                    {
                        case EditorDialogueCommand4Text textCommand:
                            {
                                var runTextCommand = new DialogCommandData4Text();

                                //runTextCommand.Speaker = textCommand.Speaker;
                                //runTextCommand.Content = textCommand.Content;

                                runCommand = runTextCommand;
                            }
                            break;
                        case EditorSetImageCommand setImageCommand:
                            {
                                var runSetImageCommand = new DialogCommandData4SetImage();

                                runSetImageCommand.ImageName = AssetDatabase.GetAssetPath(setImageCommand.Image);

                                runCommand = runSetImageCommand;
                            }
                            break;

                        case EditorSimpleFuncCommand simleFuncCommand:
                            {
                                var runCommand2 = new DialogCommandData4SimpleFunc();
                                runCommand2.SimpleFuncType = (DialogCommandData4SimpleFunc.ESimpleFuncType)simleFuncCommand.SimpleFuncType;
                                runCommand2.Param1 = simleFuncCommand.Param1;
                                runCommand2.Param2 = simleFuncCommand.Param2;
                                runCommand2.Param3 = simleFuncCommand.Param3;
                                runCommand2.Param4 = simleFuncCommand.Param4;
                                runCommand2.Param5 = simleFuncCommand.Param5;
                                runCommand2.Param6 = simleFuncCommand.Param6;

                                runCommand = runCommand2;
                            }
                            break;
                        case EditorDialogueCommand4JumpTo jumpToCommand:
                            {
                                var runJumpToCommand = new DialogCommandData4JumpTo();

                                runJumpToCommand.TargetStepId = jumpToCommand.TargetStepId;

                                runCommand = runJumpToCommand;
                            }
                            break;
                        case EditorChoiceCommand choiceCommand:
                            {
                                var runChoiceCommand = new DialogCommandData4Choice();
                                runChoiceCommand.TimeLimit = choiceCommand.TimeLimit;

                                runChoiceCommand.Options.AddRange(choiceCommand.Options);

                                runCommand = runChoiceCommand;
                            }
                            break;
                    }

                    //// [难点1] 处理多态条件列表 (直接复制引用，依靠 Newtonsoft 序列化子类字段)
                    //// 注意：如果 Condition 类里有 UnityEngine.Object 引用，这里需要手动深拷贝转换
                    //runOption.Conditions = srcOption.Conditions1.ToList();

                    if(runCommand == null)
                    {
                        Debug.LogError("ExportDialogueData text command err");
                        continue;
                    }
                    runStep.Commands.Add(runCommand);
                }

                runtimeData.Steps.Add(runStep);
            }

            // 3. 序列化为 JSON
            // 使用 TypeNameHandling.Auto 可以自动记录 Condition 的子类类型名称
            // 这样运行时反序列化时，知道是 ConditionLocalVariableInt 还是 ConditionHasItem
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto, // 关键：保存多态类型信息
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(runtimeData, settings);

            // 4. 写入文件
            File.WriteAllText(path, json);

            // 5. 刷新资源目录 (如果保存到了 Assets 文件夹内)
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Exported to:\n{path}", "OK");
        }
    }
}



