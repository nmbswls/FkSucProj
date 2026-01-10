using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;

namespace My.Util
{


    public class DialogueEditorCard : EditorWindow
    {
        //// --- 数据引用 ---
        //private DialogueDataPoly currentData;
        //private SerializedObject serializedObject;
        //private SerializedProperty stepsProp;

        //// --- UI 列表 ---
        //private ReorderableList mainStepList;

        //// --- 缓存 ---
        //// 缓存内层 ReorderableList，避免每帧重复创建导致卡顿
        //// Key: Step ID 或 Index的字符串表示 (这里使用 index)
        //private Dictionary<string, ReorderableList> innerListCache = new Dictionary<string, ReorderableList>();

        //// 缓存反射获取的 Command 类型，用于下拉菜单
        //private static Type[] commandTypes;

        //private Vector2 scrollPos;

        //[MenuItem("Tools/Dialogue Editor (Card Style)")]
        //public static void ShowWindow()
        //{
        //    GetWindow<DialogueEditorCard>("Card Editor");
        //}

        //private void OnEnable()
        //{
        //    // 1. 获取所有继承自 CommandBase 的非抽象类
        //    commandTypes = typeof(CommandBase).Assembly.GetTypes()
        //        .Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract)
        //        .ToArray();

        //    // 2. 尝试恢复上次打开的数据
        //    if (currentData != null) InitSerializedObject();
        //}

        //private void OnGUI()
        //{
        //    // --- 顶部工具栏 ---
        //    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        //    EditorGUI.BeginChangeCheck();
        //    // 对象选择框
        //    currentData = (DialogueDataPoly)EditorGUILayout.ObjectField("Target Data", currentData, typeof(DialogueDataPoly), false, GUILayout.Width(300));

        //    // 如果更换了数据，重新初始化
        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        InitSerializedObject();
        //    }

        //    // 导出按钮 (集成之前的 JSON 导出功能，如有需要请取消注释并确保 DialogueExporter 存在)
        //    // if (currentData != null && GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
        //    // {
        //    //     DialogueExporter.ExportToJson(currentData);
        //    // }

        //    GUILayout.FlexibleSpace();
        //    EditorGUILayout.EndHorizontal();

        //    // --- 内容区域 ---
        //    if (serializedObject == null || currentData == null)
        //    {
        //        EditorGUILayout.HelpBox("Please select a Dialogue Data asset.", MessageType.Info);
        //        return;
        //    }

        //    serializedObject.Update();

        //    // 滚动视图
        //    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        //    // 绘制主列表
        //    if (mainStepList != null) mainStepList.DoLayoutList();
        //    else EditorGUILayout.HelpBox("List not initialized.", MessageType.Warning);

        //    EditorGUILayout.EndScrollView();

        //    serializedObject.ApplyModifiedProperties();
        //}

        //private void InitSerializedObject()
        //{
        //    if (currentData == null) return;

        //    serializedObject = new SerializedObject(currentData);
        //    stepsProp = serializedObject.FindProperty("steps");

        //    // 清理旧缓存
        //    innerListCache.Clear();

        //    // --- 初始化外层 Step 列表 ---
        //    mainStepList = new ReorderableList(serializedObject, stepsProp, true, true, true, true);

        //    // 1. 列表头
        //    mainStepList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Story Timeline ({stepsProp.arraySize} Steps)");

        //    // 2. 元素高度计算 (核心逻辑)
        //    mainStepList.elementHeightCallback = (index) =>
        //    {
        //        // 防止删除元素时数组越界
        //        if (index >= stepsProp.arraySize) return 0;

        //        SerializedProperty element = stepsProp.GetArrayElementAtIndex(index);
        //        bool isExpanded = element.FindPropertyRelative("isExpanded").boolValue;

        //        float headerHeight = EditorGUIUtility.singleLineHeight + 10; // 标题栏 + Padding

        //        if (!isExpanded) return headerHeight;

        //        // 如果展开，高度 = 标题 + 内层列表高度 + 额外 Padding
        //        ReorderableList innerList = GetInnerList(index);
        //        return headerHeight + innerList.GetHeight() + 15;
        //    };

        //    // 3. 绘制元素 (卡片样式)
        //    mainStepList.drawElementCallback = (rect, index, isActive, isFocused) =>
        //    {
        //        if (index >= stepsProp.arraySize) return;

        //        SerializedProperty element = stepsProp.GetArrayElementAtIndex(index);
        //        SerializedProperty id = element.FindPropertyRelative("id");
        //        SerializedProperty note = element.FindPropertyRelative("note");
        //        SerializedProperty isExpanded = element.FindPropertyRelative("isExpanded");

        //        rect.y += 2;

        //        // --- 背景卡片 ---
        //        Rect cardRect = new Rect(rect.x, rect.y, rect.width, rect.height - 4);
        //        GUI.Box(cardRect, "", EditorStyles.helpBox);

        //        // --- 标题栏区域 ---
        //        Rect headerRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, EditorGUIUtility.singleLineHeight + 4);

        //        // 绘制折叠按钮 (整行可点)
        //        if (GUI.Button(headerRect, GUIContent.none, GUIStyle.none))
        //        {
        //            isExpanded.boolValue = !isExpanded.boolValue;
        //            // 强制重绘以刷新高度
        //            // GUI.FocusControl(null); 
        //        }

        //        // 绘制折叠箭头
        //        Rect arrowRect = new Rect(headerRect.x + 5, headerRect.y + 2, 20, EditorGUIUtility.singleLineHeight);
        //        EditorGUI.LabelField(arrowRect, isExpanded.boolValue ? "▼" : "?", EditorStyles.boldLabel);

        //        // 绘制 ID 和 Note
        //        float fieldHeight = EditorGUIUtility.singleLineHeight;
        //        float currentX = headerRect.x + 25;

        //        // ID Label
        //        EditorGUI.LabelField(new Rect(currentX, headerRect.y + 2, 30, fieldHeight), "ID:", EditorStyles.miniBoldLabel);
        //        currentX += 30;
        //        // ID Field
        //        EditorGUI.PropertyField(new Rect(currentX, headerRect.y + 2, 100, fieldHeight), id, GUIContent.none);
        //        currentX += 110;

        //        // Note Label
        //        EditorGUI.LabelField(new Rect(currentX, headerRect.y + 2, 40, fieldHeight), "Note:", EditorStyles.miniLabel);
        //        currentX += 40;
        //        // Note Field
        //        EditorGUI.PropertyField(new Rect(currentX, headerRect.y + 2, headerRect.width - currentX - 5, fieldHeight), note, GUIContent.none);

        //        // --- 展开后的内容区 (内层列表) ---
        //        if (isExpanded.boolValue)
        //        {
        //            ReorderableList innerList = GetInnerList(index);
        //            // 计算内层列表的绘制区域 (留出边距)
        //            Rect innerListRect = new Rect(
        //                rect.x + 15,
        //                rect.y + headerRect.height + 5,
        //                rect.width - 30,
        //                rect.height - headerRect.height - 10
        //            );

        //            // 绘制内层列表
        //            innerList.DoList(innerListRect);
        //        }
        //    };

        //    // 4. 列表回调：当外层列表变化时，清空缓存，防止索引错乱
        //    mainStepList.onAddCallback = (list) => {
        //        ReorderableList.defaultBehaviours.DoAddButton(list);
        //        innerListCache.Clear();
        //    };
        //    mainStepList.onRemoveCallback = (list) => {
        //        ReorderableList.defaultBehaviours.DoRemoveButton(list);
        //        innerListCache.Clear();
        //    };
        //    mainStepList.onReorderCallback = (list) => innerListCache.Clear();
        //}

        //// --- 获取或创建内层 ReorderableList ---
        //private ReorderableList GetInnerList(int stepIndex)
        //{
        //    string key = stepIndex.ToString();

        //    if (innerListCache.ContainsKey(key))
        //    {
        //        // 简单校验：防止缓存对应的 serializedObject 失效
        //        if (innerListCache[key].serializedProperty != null &&
        //            innerListCache[key].serializedProperty.serializedObject.targetObject != null)
        //        {
        //            return innerListCache[key];
        //        }
        //    }

        //    // 获取 Commands 属性
        //    SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(stepIndex);
        //    SerializedProperty commandsProp = stepProp.FindPropertyRelative("commands");

        //    // 创建新的列表
        //    ReorderableList list = new ReorderableList(stepProp.serializedObject, commandsProp, true, true, true, true);

        //    // 列表头
        //    list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Command Flow");

        //    // 元素高度
        //    list.elementHeightCallback = (i) =>
        //    {
        //        if (i >= commandsProp.arraySize) return 0;
        //        var el = commandsProp.GetArrayElementAtIndex(i);
        //        var folded = el.FindPropertyRelative("isFolded");

        //        if (folded.boolValue) return EditorGUIUtility.singleLineHeight + 4;

        //        // 计算展开后的所有属性高度
        //        return GetCommandHeight(el);
        //    };

        //    // 元素绘制
        //    list.drawElementCallback = (r, i, active, focused) =>
        //    {
        //        if (i >= commandsProp.arraySize) return;
        //        var el = commandsProp.GetArrayElementAtIndex(i);
        //        DrawCommandElement(r, el);
        //    };

        //    // 添加下拉菜单
        //    list.onAddDropdownCallback = (r, l) => ShowAddMenu(commandsProp);

        //    innerListCache[key] = list;
        //    return list;
        //}

        //// --- 绘制单个 Command 元素 ---
        //private void DrawCommandElement(Rect rect, SerializedProperty element)
        //{
        //    var isFolded = element.FindPropertyRelative("isFolded");

        //    // 获取摘要 (Summary)
        //    CommandBase cmdInstance = GetTargetObjectOfProperty(element) as CommandBase;
        //    string summary = cmdInstance != null ? cmdInstance.GetSummary() : "Unknown Command";

        //    rect.y += 2;
        //    Rect titleRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

        //    // 绘制折叠箭头 + 标题
        //    isFolded.boolValue = EditorGUI.Foldout(titleRect, isFolded.boolValue, summary, true);

        //    if (!isFolded.boolValue)
        //    {
        //        // 绘制详细属性
        //        Rect propsRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, rect.height);
        //        DrawCommandProperties(element, propsRect);
        //    }
        //}

        //// --- 下拉菜单逻辑 ---
        //private void ShowAddMenu(SerializedProperty commandsProp)
        //{
        //    var menu = new GenericMenu();
        //    foreach (var type in commandTypes)
        //    {
        //        menu.AddItem(new GUIContent(type.Name), false, () =>
        //        {
        //            // 1. 反射创建实例
        //            CommandBase newCmd = (CommandBase)Activator.CreateInstance(type);

        //            // 2. 添加到 Property
        //            int index = commandsProp.arraySize;
        //            commandsProp.insertArrayElementAtIndex(index);
        //            var p = commandsProp.GetArrayElementAtIndex(index);
        //            p.managedReferenceValue = newCmd; // 关键：赋值多态引用

        //            serializedObject.ApplyModifiedProperties();

        //            // 3. 必须清空缓存，因为某个 List 的高度变了
        //            innerListCache.Clear();
        //        });
        //    }
        //    menu.ShowAsContext();
        //}

        //// --- 辅助：计算属性总高度 ---
        //private float GetCommandHeight(SerializedProperty rootProp)
        //{
        //    float h = EditorGUIUtility.singleLineHeight + 6; // Title + Padding

        //    SerializedProperty prop = rootProp.Copy();
        //    SerializedProperty endProp = rootProp.GetEndProperty();

        //    // 进入第一个子节点
        //    prop.NextVisible(true);
        //    do
        //    {
        //        if (SerializedProperty.EqualContents(prop, endProp)) break;
        //        if (prop.name == "isFolded") continue;

        //        h += EditorGUI.GetPropertyHeight(prop, true) + 2; // +2 for spacing

        //    } while (prop.NextVisible(false));

        //    return h;
        //}

        //// --- 辅助：绘制属性 (支持自定义标签) ---
        //private void DrawCommandProperties(SerializedProperty rootProp, Rect startRect)
        //{
        //    float curY = startRect.y;

        //    SerializedProperty prop = rootProp.Copy();
        //    SerializedProperty endProp = rootProp.GetEndProperty();

        //    prop.NextVisible(true);
        //    do
        //    {
        //        if (SerializedProperty.EqualContents(prop, endProp)) break;
        //        if (prop.name == "isFolded") continue;

        //        float h = EditorGUI.GetPropertyHeight(prop, true);
        //        Rect drawRect = new Rect(startRect.x, curY, startRect.width, h);

        //        // --- UI Label 自定义 ---
        //        GUIContent label = new GUIContent(prop.displayName);
        //        if (prop.name == "speaker") label.text = "Character ID";
        //        if (prop.name == "content") label.text = "Dialogue Text";
        //        if (prop.name == "image") label.text = "Sprite Asset";
        //        if (prop.name == "targetStepId") label.text = "Jump To ID";
        //        if (prop.name == "timeLimit") label.text = "Timer (sec)";

        //        EditorGUI.PropertyField(drawRect, prop, label, true);

        //        curY += h + 2;

        //    } while (prop.NextVisible(false));
        //}

        //// --- 辅助：反射获取真实对象 (用于 GetSummary) ---
        //private object GetTargetObjectOfProperty(SerializedProperty prop)
        //{
        //    if (prop == null) return null;
        //    var path = prop.propertyPath.Replace(".Array.data[", "[").Replace("]", "");
        //    object obj = prop.serializedObject.targetObject;
        //    var elements = path.Split('.');
        //    foreach (var element in elements)
        //    {
        //        if (element.Contains("["))
        //        {
        //            var elementName = element.Substring(0, element.IndexOf("["));
        //            var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
        //            obj = GetValue_Imp(obj, elementName, index);
        //        }
        //        else
        //        {
        //            obj = GetValue_Imp(obj, element);
        //        }
        //    }
        //    return obj;
        //}

        //private object GetValue_Imp(object source, string name)
        //{
        //    if (source == null) return null;
        //    var type = source.GetType();
        //    while (type != null)
        //    {
        //        var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        //        if (f != null) return f.GetValue(source);
        //        var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        //        if (p != null) return p.GetValue(source, null);
        //        type = type.BaseType;
        //    }
        //    return null;
        //}

        //private object GetValue_Imp(object source, string name, int index)
        //{
        //    var enumerable = GetValue_Imp(source, name) as IEnumerable;
        //    if (enumerable == null) return null;
        //    var enm = enumerable.GetEnumerator();
        //    for (int i = 0; i <= index; i++)
        //    {
        //        if (!enm.MoveNext()) return null;
        //    }
        //    return enm.Current;
        //}
    }
}