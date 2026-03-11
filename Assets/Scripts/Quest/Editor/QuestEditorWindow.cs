using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Newtonsoft.Json;
using My.Def.Quest;
using System.Linq;



/// <summary>
/// 任务编辑器
/// </summary>
public class QuestEditorWindow : EditorWindow
{
    private QuestDataSO currentQuest;
    private SerializedObject serializedObject;
    private SerializedProperty stepsProp;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    private int selectedStepIndex = -1;
    private ReorderableList stepList;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;

    [MenuItem("Tools/Quest Editor")]
    public static void ShowWindow()
    {
        GetWindow<QuestEditorWindow>("Quest Editor");
    }

    private void OnEnable()
    {
        if (currentQuest != null) InitSerializedObject();
    }

    private void OnDisable()
    {
        SaveAssetIfDirty();
    }

    private void SaveAssetIfDirty()
    {
        if (serializedObject != null && serializedObject.targetObject != null &&
            EditorUtility.IsDirty(serializedObject.targetObject))
        {
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        InitStyles();
        DrawToolbar();

        if (serializedObject == null || currentQuest == null)
        {
            EditorGUILayout.HelpBox("Please select a Quest Data asset.", MessageType.Info);
            return;
        }

        serializedObject.Update();
        DrawMainLayout();
        serializedObject.ApplyModifiedProperties();
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };
        }

        if (subHeaderStyle == null)
        {
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 10, 0)
            };
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        currentQuest = (QuestDataSO)EditorGUILayout.ObjectField(
            "Target Quest", currentQuest, typeof(QuestData), false,
            GUILayout.Width(300));

        if (EditorGUI.EndChangeCheck())
        {
            selectedStepIndex = -1;
            InitSerializedObject();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            SaveAssetIfDirty();
        }

        if (GUILayout.Button("Export to JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            ExportToJson();
        }

        if (GUILayout.Button("Import from JSON", EditorStyles.toolbarButton, GUILayout.Width(120)))
        {
            ImportFromJson();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainLayout()
    {
        EditorGUILayout.BeginHorizontal();
        {
            // 左侧面板：步骤列表 (30%)
            DrawLeftPanel();

            // 右侧面板：详细编辑 (70%)
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        {
            DrawGlobalSettings();
            EditorGUILayout.Space(10);
            DrawStepList();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGlobalSettings()
    {
        EditorGUILayout.LabelField("Global Settings", headerStyle);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("questId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("title"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("initStepId"));

        EditorGUILayout.LabelField("Description:");
        SerializedProperty descProp = serializedObject.FindProperty("description");
        descProp.stringValue = EditorGUILayout.TextArea(
            descProp.stringValue, GUILayout.Height(60));
    }

    private void DrawStepList()
    {
        EditorGUILayout.LabelField("Steps", headerStyle);
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, GUI.skin.box);

        if (stepList != null)
        {
            stepList.DoLayoutList();
        }
        else
        {
            EditorGUILayout.HelpBox("No steps defined", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

        if (selectedStepIndex >= 0 && selectedStepIndex < stepsProp.arraySize)
        {
            SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(selectedStepIndex);
            DrawStepDetail(stepProp);
        }
        else
        {
            EditorGUILayout.LabelField("Select a Step to edit details",
                EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawStepDetail(SerializedProperty stepProp)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.LabelField($"Step: {stepProp.FindPropertyRelative("stepId").intValue}",
                headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("stepId"));
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("isRoot"));
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("isAuto"));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        DrawObjectivesSection(stepProp);
        DrawOutcomesSection(stepProp);
        DrawFailCondition(stepProp);
    }

    private void DrawObjectivesSection(SerializedProperty stepProp)
    {
        SerializedProperty objectivesProp = stepProp.FindPropertyRelative("objectives");

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Objectives ({objectivesProp.arraySize})", subHeaderStyle);

        if (GUILayout.Button("+ Add Objective", GUILayout.Width(150)))
        {
            objectivesProp.arraySize++;
            var newObj = objectivesProp.GetArrayElementAtIndex(objectivesProp.arraySize - 1);

            // 设置默认值
            newObj.FindPropertyRelative("objectiveId").intValue = objectivesProp.arraySize;
            newObj.FindPropertyRelative("text").stringValue = "New Objective";
            newObj.FindPropertyRelative("isHidden").boolValue = false;
            newObj.FindPropertyRelative("isOption").boolValue = false;

            // 清空条件
            var conditionProp = newObj.FindPropertyRelative("condition");
            conditionProp.managedReferenceValue = null;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < objectivesProp.arraySize; i++)
        {
            SerializedProperty objProp = objectivesProp.GetArrayElementAtIndex(i);
            DrawObjectiveItem(i, objProp, objectivesProp);
        }
    }

    private void DrawObjectiveItem(int index, SerializedProperty objProp, SerializedProperty listProp)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(objProp.FindPropertyRelative("objectiveId"),
                new GUIContent("ID"), GUILayout.Width(60));
            EditorGUILayout.PropertyField(objProp.FindPropertyRelative("text"), GUIContent.none);

            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                listProp.DeleteArrayElementAtIndex(index);
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(objProp.FindPropertyRelative("isHidden"),
                GUILayout.Width(100));
            EditorGUILayout.PropertyField(objProp.FindPropertyRelative("isOption"),
                GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Completion Condition:", EditorStyles.miniBoldLabel);
            DrawConditionField(objProp.FindPropertyRelative("condition"));

            EditorGUILayout.PropertyField(objProp.FindPropertyRelative("completionTags"),
                new GUIContent("Completion Tags"), true);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawOutcomesSection(SerializedProperty stepProp)
    {
        SerializedProperty outcomesProp = stepProp.FindPropertyRelative("outcomes");

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Outcomes ({outcomesProp.arraySize})", subHeaderStyle);

        if (GUILayout.Button("+ Add Outcome", GUILayout.Width(150)))
        {
            outcomesProp.arraySize++;
            var newOut = outcomesProp.GetArrayElementAtIndex(outcomesProp.arraySize - 1);

            // 设置默认值
            newOut.FindPropertyRelative("outcomeName").stringValue = "New Outcome";
            newOut.FindPropertyRelative("description").stringValue = "";
            newOut.FindPropertyRelative("completeId").intValue = 0;
            newOut.FindPropertyRelative("nextStepId").intValue = 0;
            newOut.FindPropertyRelative("needObjectiveIds").ClearArray();
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < outcomesProp.arraySize; i++)
        {
            SerializedProperty outProp = outcomesProp.GetArrayElementAtIndex(i);
            DrawOutcomeItem(i, outProp, outcomesProp);
        }
    }

    private void DrawOutcomeItem(int index, SerializedProperty outProp, SerializedProperty listProp)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(outProp.FindPropertyRelative("outcomeName"),
                new GUIContent("Name"));

            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                listProp.DeleteArrayElementAtIndex(index);
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(outProp.FindPropertyRelative("description"));
            EditorGUILayout.PropertyField(outProp.FindPropertyRelative("completeId"));
            EditorGUILayout.PropertyField(outProp.FindPropertyRelative("nextStepId"));

            SerializedProperty needIdsProp = outProp.FindPropertyRelative("needObjectiveIds");
            SerializedProperty objectivesProp = listProp.serializedObject.FindProperty(
                $"steps.Array.data[{selectedStepIndex}].objectives");

            EditorGUILayout.LabelField("Required Objectives:", EditorStyles.miniBoldLabel);
            DrawObjectiveSelector(needIdsProp, objectivesProp);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawFailCondition(SerializedProperty stepProp)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Fail Condition", subHeaderStyle);
        DrawConditionField(stepProp.FindPropertyRelative("failCondition"));
    }

    private void DrawConditionField(SerializedProperty conditionProp)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 显示当前类型
        string typeName = "No Condition";
        if (conditionProp.managedReferenceValue != null)
        {
            typeName = conditionProp.managedReferenceValue.GetType().Name;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Condition Type: {typeName}");

        // 类型选择按钮
        if (GUILayout.Button("Change Type", EditorStyles.miniButton, GUILayout.Width(100)))
        {
            ShowConditionTypeMenu(conditionProp);
        }
        EditorGUILayout.EndHorizontal();

        // 绘制条件内容
        if (conditionProp.managedReferenceValue != null)
        {
            // 绘制通用属性
            SerializedProperty negateProp = conditionProp.FindPropertyRelative("negate");
            EditorGUILayout.PropertyField(negateProp, new GUIContent("Negate"));

            // 绘制条件特定属性
            DrawSpecificConditionProperties(conditionProp);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpecificConditionProperties(SerializedProperty conditionProp)
    {
        object conditionObject = conditionProp.managedReferenceValue;
        if (conditionObject == null) return;

        Type conditionType = conditionObject.GetType();
        SerializedProperty prop = conditionProp.Copy();

        // 遍历所有属性
        while (prop.NextVisible(true))
        {
            if (prop.name == "negate") continue; // 已经单独处理

            if (conditionType == typeof(QuestCondition))
            {
                if (prop.name == "conditions")
                {
                    DrawCompositeConditions(prop);
                    continue;
                }
                else if (prop.name == "logicType")
                {
                    EditorGUILayout.PropertyField(prop);
                    continue;
                }
            }

            EditorGUILayout.PropertyField(prop, true);
        }
    }

    private void DrawCompositeConditions(SerializedProperty conditionsProp)
    {
        EditorGUILayout.LabelField("Sub-Conditions:", EditorStyles.boldLabel);

        for (int i = 0; i < conditionsProp.arraySize; i++)
        {
            SerializedProperty condProp = conditionsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // 删除按钮
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                conditionsProp.DeleteArrayElementAtIndex(i);
                break;
            }

            // 条件描述
            string description = "No Condition";
            if (condProp.managedReferenceValue != null)
            {
                var condition = condProp.managedReferenceValue as QuestCondition;
                description = condition.GetDescription();
            }
            EditorGUILayout.LabelField(description);

            EditorGUILayout.EndHorizontal();

            // 绘制条件内容
            DrawConditionField(condProp);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // 添加子条件按钮
        if (GUILayout.Button("+ Add Sub-Condition", EditorStyles.miniButton))
        {
            conditionsProp.arraySize++;
            var newCond = conditionsProp.GetArrayElementAtIndex(conditionsProp.arraySize - 1);
            newCond.managedReferenceValue = null;
        }
    }

    private void ShowConditionTypeMenu(SerializedProperty conditionProp)
    {
        GenericMenu menu = new GenericMenu();

        // 基础条件类型
        menu.AddItem(new GUIContent("Kill Enemy"), false,
            () => SetConditionType(conditionProp, typeof(QuestConditionKill)));
        menu.AddItem(new GUIContent("Has Item"), false,
            () => SetConditionType(conditionProp, typeof(QuestConditionHasSwitch)));

        //// 组合条件
        //menu.AddItem(new GUIContent("Composite (AND/OR)"), false,
        //    () => SetConditionType(conditionProp, typeof(CompositeCondition)));

        // 清除条件
        menu.AddItem(new GUIContent("Clear Condition"), false,
            () => conditionProp.managedReferenceValue = null);

        menu.ShowAsContext();
    }

    private void SetConditionType(SerializedProperty prop, Type type)
    {
        prop.managedReferenceValue = Activator.CreateInstance(type);
        prop.serializedObject.ApplyModifiedProperties();
    }

    private void DrawObjectiveSelector(SerializedProperty needIdsProp, SerializedProperty allObjectivesProp)
    {
        HashSet<int> selectedIds = new HashSet<int>();
        for (int i = 0; i < needIdsProp.arraySize; i++)
        {
            selectedIds.Add(needIdsProp.GetArrayElementAtIndex(i).intValue);
        }

        bool changed = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        for (int i = 0; i < allObjectivesProp.arraySize; i++)
        {
            SerializedProperty objProp = allObjectivesProp.GetArrayElementAtIndex(i);
            int id = objProp.FindPropertyRelative("objectiveId").intValue;
            string text = objProp.FindPropertyRelative("text").stringValue;

            bool isSelected = selectedIds.Contains(id);
            bool newSelected = EditorGUILayout.ToggleLeft(
                $"[{id}] {text}", isSelected);

            if (newSelected != isSelected)
            {
                if (newSelected) selectedIds.Add(id);
                else selectedIds.Remove(id);
                changed = true;
            }
        }

        if (allObjectivesProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("No objectives in this step");
        }
        EditorGUILayout.EndVertical();

        if (changed)
        {
            needIdsProp.ClearArray();
            foreach (int id in selectedIds)
            {
                needIdsProp.arraySize++;
                needIdsProp.GetArrayElementAtIndex(needIdsProp.arraySize - 1).intValue = id;
            }
        }
    }

    private void InitSerializedObject()
    {
        if (currentQuest == null) return;

        serializedObject = new SerializedObject(currentQuest);
        stepsProp = serializedObject.FindProperty("steps");

        stepList = new ReorderableList(serializedObject, stepsProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Quest Steps"),
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = stepsProp.GetArrayElementAtIndex(index);
                int id = element.FindPropertyRelative("stepId").intValue;
                bool isRoot = element.FindPropertyRelative("isRoot").boolValue;
                int outcomeCount = element.FindPropertyRelative("outcomes").arraySize;

                string label = $"Step {id}";
                if (isRoot) label += " [ROOT]";
                if (outcomeCount == 0) label += " (No Exit)";

                EditorGUI.LabelField(rect, label);
            },
            onSelectCallback = list => selectedStepIndex = list.index,
            onAddCallback = list =>
            {
                list.serializedProperty.arraySize++;
                var newElem = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);

                // 初始化新步骤
                int newId = 1;
                if (list.serializedProperty.arraySize > 1)
                {
                    var prevElem = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 2);
                    newId = prevElem.FindPropertyRelative("stepId").intValue + 1;
                }

                newElem.FindPropertyRelative("stepId").intValue = newId;
                newElem.FindPropertyRelative("isRoot").boolValue = false;
                newElem.FindPropertyRelative("isAuto").boolValue = false;
                newElem.FindPropertyRelative("outcomes").ClearArray();
                newElem.FindPropertyRelative("objectives").ClearArray();
                newElem.FindPropertyRelative("failCondition").managedReferenceValue = null;
            }
        };
    }

    private void ExportToJson()
    {
        if (currentQuest == null)
        {
            EditorUtility.DisplayDialog("Error", "No quest selected!", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Export Quest JSON",
            Application.dataPath, $"{currentQuest.name}_export", "json");

        if (string.IsNullOrEmpty(path)) return;

        // 创建数据副本用于序列化
        var exportData = new QuestDataExport
        {
            questId = currentQuest.questId,
            title = currentQuest.title,
            initStepId = currentQuest.InitStepId,
            description = currentQuest.description,
            steps = currentQuest.steps.ToList()
        };

        // 序列化为JSON
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        string json = JsonConvert.SerializeObject(exportData, settings);
        System.IO.File.WriteAllText(path, json);

        EditorUtility.DisplayDialog("Success", $"Exported to:\n{path}", "OK");
    }

    private void ImportFromJson()
    {
        if (currentQuest == null)
        {
            EditorUtility.DisplayDialog("Error", "No quest selected!", "OK");
            return;
        }

        string path = EditorUtility.OpenFilePanel("Import Quest JSON",
            Application.dataPath, "json");

        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = System.IO.File.ReadAllText(path);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            // 反序列化到临时对象
            var importData = JsonConvert.DeserializeObject<QuestDataExport>(json, settings);

            // 应用导入的数据
            Undo.RecordObject(currentQuest, "Import Quest Data");

            currentQuest.questId = importData.questId;
            currentQuest.title = importData.title;
            currentQuest.InitStepId = importData.initStepId;
            currentQuest.description = importData.description;
            currentQuest.steps = importData.steps.ToArray();

            EditorUtility.SetDirty(currentQuest);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", "Quest data imported successfully!", "OK");

            // 刷新编辑器
            InitSerializedObject();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Import failed:\n{e.Message}", "OK");
            Debug.LogError(e);
        }
    }

    // 用于JSON导出的数据结构
    [Serializable]
    public class QuestDataExport
    {
        public int questId;
        public string title;
        public int initStepId;
        public string description;
        public List<QuestStepData> steps;
    }
}
