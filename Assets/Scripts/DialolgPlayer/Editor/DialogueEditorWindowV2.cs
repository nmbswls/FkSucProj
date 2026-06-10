using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
namespace My.Dialog
{
    public class DialogueEditorWindowV2 : EditorWindow
    {
        private EditorDialogueData currentData;
        private SerializedObject serializedObject;
        private SerializedProperty stepsProp;
        private SerializedProperty linkedJsonPathProp;

        private ReorderableList mainStepList;

        private readonly Dictionary<string, ReorderableList> innerListCache = new Dictionary<string, ReorderableList>();
        private readonly Dictionary<string, bool> stepExpandedCache = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> cmdFoldedCache = new Dictionary<string, bool>();

        private Vector2 scrollPos;

        private static Type[] commandTypes;
        private static Type[] conditionTypes;

        private int spritePickerControlId;
        private SerializedProperty pendingImageNameProp;

        [MenuItem("Tools/Dialogue Editor (V2)")]
        public static void ShowWindow()
        {
            GetWindow<DialogueEditorWindowV2>("Dialogue");
        }

        private void OnEnable()
        {
            commandTypes = typeof(DialogCommandData).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DialogCommandData)) && !t.IsAbstract)
                .OrderBy(t => DialogueEditorCommandLabels.GetMenuName(t))
                .ToArray();

            conditionTypes = typeof(DialogCondition).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DialogCondition)) && !t.IsAbstract)
                .ToArray();

            if (currentData != null)
                InitSerializedObject();
        }

        private void OnGUI()
        {
            HandleSpritePicker();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            currentData = (EditorDialogueData)EditorGUILayout.ObjectField("Target Data", currentData, typeof(EditorDialogueData), false, GUILayout.Width(280));
            if (EditorGUI.EndChangeCheck())
                InitSerializedObject();

            if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                if (currentData != null)
                    ImportFromJson();
                else
                    EditorUtility.DisplayDialog("Error", "Please select a data file first!", "OK");
            }

            if (GUILayout.Button("Export JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                if (currentData != null)
                    ExportToJson(currentData);
                else
                    EditorUtility.DisplayDialog("Error", "Please select a data file first!", "OK");
            }

            if (GUILayout.Button("New SO", EditorStyles.toolbarButton, GUILayout.Width(60)))
                CreateSoFromJsonPicker();

            if (GUILayout.Button("Sync All SO", EditorStyles.toolbarButton, GUILayout.Width(80)))
                SyncAllSoFromOutput();

            EditorGUILayout.EndHorizontal();

            if (serializedObject == null || currentData == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a Dialogue Data asset, or use New SO / Sync All SO to bootstrap from Resources/Dialogue/output.",
                    MessageType.Info);
                return;
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(linkedJsonPathProp, new GUIContent("Linked JSON Path"));

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            if (mainStepList != null)
                mainStepList.DoLayoutList();
            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleSpritePicker()
        {
            if (Event.current.commandName != "ObjectSelectorUpdated")
                return;
            if (EditorGUIUtility.GetObjectPickerControlID() != spritePickerControlId)
                return;
            if (pendingImageNameProp == null)
                return;

            var picked = EditorGUIUtility.GetObjectPickerObject();
            if (picked is Sprite sprite)
            {
                RecordUndo("Pick Image");
                pendingImageNameProp.stringValue = AssetDatabase.GetAssetPath(sprite);
                pendingImageNameProp.serializedObject.ApplyModifiedProperties();
                Repaint();
            }
        }

        private void InitSerializedObject()
        {
            if (currentData == null)
                return;

            serializedObject = new SerializedObject(currentData);
            stepsProp = serializedObject.FindProperty("Steps");
            linkedJsonPathProp = serializedObject.FindProperty("LinkedJsonPath");
            innerListCache.Clear();

            mainStepList = new ReorderableList(serializedObject, stepsProp, true, true, true, true);
            mainStepList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Story Timeline ({stepsProp.arraySize} Steps)");

            mainStepList.elementHeightCallback = index =>
            {
                if (index >= stepsProp.arraySize)
                    return 0;

                float headerHeight = EditorGUIUtility.singleLineHeight + 10;
                if (!IsStepExpanded(index))
                    return headerHeight;

                ReorderableList innerList = GetInnerList(index);
                return headerHeight + innerList.GetHeight() + 50;
            };

            mainStepList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= stepsProp.arraySize)
                    return;

                SerializedProperty element = stepsProp.GetArrayElementAtIndex(index);
                SerializedProperty id = element.FindPropertyRelative("Id");
                SerializedProperty note = element.FindPropertyRelative("Note");

                rect.y += 2;
                GUI.Box(new Rect(rect.x, rect.y, rect.width, rect.height - 4), "", EditorStyles.helpBox);

                float headerHeight = EditorGUIUtility.singleLineHeight;
                float currentY = rect.y + 2;

                Rect arrowRect = new Rect(rect.x + 5, currentY, 20, headerHeight);
                bool expanded = IsStepExpanded(index);
                bool newExpanded = EditorGUI.Foldout(arrowRect, expanded, "");
                if (newExpanded != expanded)
                    SetStepExpanded(index, newExpanded);

                float currentX = rect.x + 25;
                EditorGUI.LabelField(new Rect(currentX, currentY, 30, headerHeight), "ID:", EditorStyles.miniBoldLabel);
                currentX += 30;

                Rect idFieldRect = new Rect(currentX, currentY, 100, headerHeight);
                EditorGUI.PropertyField(idFieldRect, id, GUIContent.none);
                currentX += 110;

                EditorGUI.LabelField(new Rect(currentX, currentY, 40, headerHeight), "Note:", EditorStyles.miniLabel);
                currentX += 40;

                Rect noteFieldRect = new Rect(currentX, currentY, rect.width - (currentX - rect.x) - 5, headerHeight);
                EditorGUI.PropertyField(noteFieldRect, note, GUIContent.none);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    Rect headerBarRect = new Rect(rect.x, rect.y, rect.width, headerHeight + 4);
                    if (headerBarRect.Contains(Event.current.mousePosition)
                        && !idFieldRect.Contains(Event.current.mousePosition)
                        && !noteFieldRect.Contains(Event.current.mousePosition)
                        && !arrowRect.Contains(Event.current.mousePosition))
                    {
                        SetStepExpanded(index, !IsStepExpanded(index));
                        Event.current.Use();
                    }
                }

                if (IsStepExpanded(index))
                {
                    ReorderableList innerList = GetInnerList(index);
                    float listHeight = innerList.GetHeight();
                    innerList.DoList(new Rect(rect.x + 15, rect.y + headerHeight + 5, rect.width - 30, listHeight));

                    float buttonsY = rect.y + headerHeight + 5 + listHeight + 4;
                    float buttonHeight = 30;

                    Rect quickAddRect = new Rect(rect.x + 15, buttonsY, rect.width - 30 - 35, buttonHeight);
                    if (GUI.Button(quickAddRect, "Add Dialogue Lines"))
                    {
                        RecordUndo("Add Dialogue Lines");
                        serializedObject.Update();

                        var currentStep = stepsProp.GetArrayElementAtIndex(index);
                        var commandsProp = currentStep.FindPropertyRelative("Commands");
                        int cmdIndex = commandsProp.arraySize;

                        string newSpeaker = string.Empty;
                        if (cmdIndex > 0)
                        {
                            var lastCmd = commandsProp.GetArrayElementAtIndex(cmdIndex - 1);
                            if (lastCmd.managedReferenceValue is DialogCommandData4Text lastText
                                && lastText.TextLines != null
                                && lastText.TextLines.Count > 0)
                            {
                                newSpeaker = lastText.TextLines[0].Speaker;
                            }
                        }

                        commandsProp.InsertArrayElementAtIndex(cmdIndex);
                        var textCmd = new DialogCommandData4Text();
                        textCmd.TextLines.Add(new OneTextLine { Speaker = newSpeaker, Content = string.Empty });
                        commandsProp.GetArrayElementAtIndex(cmdIndex).managedReferenceValue = textCmd;

                        serializedObject.ApplyModifiedProperties();
                        innerListCache.Clear();
                    }

                    Rect moreRect = new Rect(quickAddRect.xMax + 5, buttonsY, 30, buttonHeight);
                    if (GUI.Button(moreRect, "+"))
                        ShowAddMenu(index);
                }
            };

            mainStepList.onAddCallback = l =>
            {
                RecordUndo("Add Step");
                ReorderableList.defaultBehaviours.DoAddButton(l);
                innerListCache.Clear();

                var newStepProp = l.serializedProperty.GetArrayElementAtIndex(l.serializedProperty.arraySize - 1);
                newStepProp.FindPropertyRelative("Id").stringValue = l.serializedProperty.arraySize.ToString();
                newStepProp.FindPropertyRelative("Note").stringValue = string.Empty;
                newStepProp.FindPropertyRelative("Commands").ClearArray();
            };

            mainStepList.onRemoveCallback = l =>
            {
                RecordUndo("Remove Step");
                ReorderableList.defaultBehaviours.DoRemoveButton(l);
                innerListCache.Clear();
            };

            mainStepList.onReorderCallback = _ => innerListCache.Clear();
        }

        private ReorderableList GetInnerList(int stepIndex)
        {
            string key = stepIndex.ToString();
            if (innerListCache.TryGetValue(key, out var cached) && cached.serializedProperty != null)
                return cached;

            SerializedProperty stepProp = stepsProp.GetArrayElementAtIndex(stepIndex);
            SerializedProperty commandsProp = stepProp.FindPropertyRelative("Commands");

            ReorderableList list = new ReorderableList(stepProp.serializedObject, commandsProp, true, true, false, false);
            list.drawHeaderCallback = r => EditorGUI.LabelField(r, "Command Flow");

            list.elementHeightCallback = i =>
            {
                if (i >= commandsProp.arraySize)
                    return 0;

                var wrapperProp = commandsProp.GetArrayElementAtIndex(i);
                if (IsCmdFolded(stepIndex, i))
                    return EditorGUIUtility.singleLineHeight + 4;

                if (wrapperProp.managedReferenceValue is DialogCommandData4Choice)
                    return GetChoiceCommandHeight(wrapperProp);
                if (wrapperProp.managedReferenceValue is DialogCommandData4Text)
                    return GetDialogueTextCommandHeight(wrapperProp);
                if (wrapperProp.managedReferenceValue is DialogCommandData4BranchText)
                    return GetSimpleBranchHeight(wrapperProp);
                if (wrapperProp.managedReferenceValue is DialogCommandData4SetImage)
                    return GetSetImageCommandHeight(wrapperProp);

                return GetGenericCommandHeight(wrapperProp);
            };

            list.drawElementCallback = (r, i, active, focused) =>
            {
                if (i >= commandsProp.arraySize)
                    return;
                DrawCommandElement(r, commandsProp.GetArrayElementAtIndex(i), stepIndex, i);
            };

            list.onAddDropdownCallback = (_, __) => ShowAddMenu(stepIndex);
            innerListCache[key] = list;
            return list;
        }

        private void DrawCommandElement(Rect rect, SerializedProperty element, int stepIndex, int cmdIndex)
        {
            float btnSize = 20f;
            float spacing = 2f;

            Rect deleteRect = new Rect(rect.x + rect.width - btnSize, rect.y, btnSize, EditorGUIUtility.singleLineHeight);
            Rect foldBtnRect = new Rect(deleteRect.x - btnSize - spacing, rect.y, btnSize, EditorGUIUtility.singleLineHeight);
            float safeWidth = rect.width - (btnSize * 2) - (spacing * 3);
            Rect contentAreaRect = new Rect(rect.x, rect.y, safeWidth, rect.height);

            var cmdData = element.managedReferenceValue as DialogCommandData;
            string summary = $"{cmdIndex}:{DialogueEditorCommandLabels.GetSummary(cmdData)}";
            bool folded = IsCmdFolded(stepIndex, cmdIndex);

            if (folded)
            {
                EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, EditorGUIUtility.singleLineHeight), summary, EditorStyles.boldLabel);
            }
            else
            {
                float lineH = EditorGUIUtility.singleLineHeight;
                if (cmdData is DialogCommandData4Choice)
                {
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, lineH), summary);
                    DrawChoiceCommandProperty(new Rect(contentAreaRect.x, contentAreaRect.y + lineH + 2, contentAreaRect.width, contentAreaRect.height), element);
                }
                else if (cmdData is DialogCommandData4BranchText)
                {
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, lineH), summary);
                    DrawSimpleBranchElement(new Rect(contentAreaRect.x, contentAreaRect.y + lineH + 2, contentAreaRect.width, contentAreaRect.height), element);
                }
                else if (cmdData is DialogCommandData4Text)
                {
                    DrawDialogueTextElement(contentAreaRect, element);
                }
                else if (cmdData is DialogCommandData4SetImage)
                {
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, lineH), summary);
                    DrawSetImageElement(new Rect(contentAreaRect.x, contentAreaRect.y + lineH + 2, contentAreaRect.width, contentAreaRect.height), element);
                }
                else
                {
                    EditorGUI.LabelField(new Rect(contentAreaRect.x, contentAreaRect.y, contentAreaRect.width, lineH), summary);
                    DrawGenericCommandProperties(element, new Rect(contentAreaRect.x, contentAreaRect.y + lineH + 2, contentAreaRect.width, contentAreaRect.height));
                }
            }

            if (GUI.Button(foldBtnRect, folded ? "□" : "_", EditorStyles.miniButton))
                SetCmdFolded(stepIndex, cmdIndex, !folded);

            if (GUI.Button(deleteRect, "×", EditorStyles.miniButton))
            {
                RecordUndo("Delete Command");
                DeleteElement(element, cmdIndex);
            }

            if (Event.current.type == EventType.MouseDown)
            {
                if (deleteRect.Contains(Event.current.mousePosition) || foldBtnRect.Contains(Event.current.mousePosition))
                    Event.current.Use();
            }
        }

        private void DrawSetImageElement(Rect rect, SerializedProperty element)
        {
            var imageNameProp = element.FindPropertyRelative("ImageName");
            var positionProp = element.FindPropertyRelative("Position");
            float lineH = EditorGUIUtility.singleLineHeight;
            float y = rect.y;
            float pickerBtnW = 28f;

            Rect pathRect = new Rect(rect.x, y, rect.width - pickerBtnW - 4, lineH);
            Rect btnRect = new Rect(pathRect.xMax + 4, y, pickerBtnW, lineH);
            EditorGUI.PropertyField(pathRect, imageNameProp, new GUIContent("Image Path"));

            if (GUI.Button(btnRect, "..."))
            {
                Sprite current = null;
                if (!string.IsNullOrEmpty(imageNameProp.stringValue))
                    current = AssetDatabase.LoadAssetAtPath<Sprite>(imageNameProp.stringValue);

                spritePickerControlId = GUIUtility.GetControlID(FocusType.Passive);
                pendingImageNameProp = imageNameProp.Copy();
                EditorGUIUtility.ShowObjectPicker<Sprite>(current, false, "t:Sprite", spritePickerControlId);
            }

            y += lineH + 4;

            if (!string.IsNullOrEmpty(imageNameProp.stringValue))
            {
                var preview = AssetDatabase.LoadAssetAtPath<Sprite>(imageNameProp.stringValue);
                if (preview != null && preview.texture != null)
                {
                    float previewSize = 48f;
                    Rect previewRect = new Rect(rect.x, y, previewSize, previewSize);
                    GUI.DrawTexture(previewRect, preview.texture, ScaleMode.ScaleToFit);
                    y += previewSize + 4;
                }
            }

            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineH), positionProp, new GUIContent("Position"));
        }

        private float GetSetImageCommandHeight(SerializedProperty element)
        {
            float h = EditorGUIUtility.singleLineHeight + 6;
            h += EditorGUIUtility.singleLineHeight + 4;
            var imageNameProp = element.FindPropertyRelative("ImageName");
            if (!string.IsNullOrEmpty(imageNameProp.stringValue)
                && AssetDatabase.LoadAssetAtPath<Sprite>(imageNameProp.stringValue) != null)
            {
                h += 52f;
            }
            h += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("Position"), true) + 2;
            return h;
        }

        private bool DrawSingleTextLine(Rect rect, SerializedProperty lineProp)
        {
            var speakerProp = lineProp.FindPropertyRelative("Speaker");
            var contentProp = lineProp.FindPropertyRelative("Content");
            var voiceProp = lineProp.FindPropertyRelative("VoiceLine");

            float speakerWidth = 80f;
            float spacing = 2f;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            Rect speakerRect = new Rect(rect.x, rect.y, speakerWidth, lineHeight);
            EditorGUI.PropertyField(speakerRect, speakerProp, GUIContent.none);
            EditorGUI.LabelField(new Rect(rect.x, rect.y + lineHeight, speakerWidth, lineHeight), "Speaker", EditorStyles.miniLabel);

            float textAreaHeight = rect.height - lineHeight - spacing * 2;
            Rect textRect = new Rect(rect.x + speakerWidth + 5, rect.y, rect.width - speakerWidth - 25, textAreaHeight);
            contentProp.stringValue = EditorGUI.TextArea(textRect, contentProp.stringValue, EditorStyles.textArea);

            Rect voiceRect = new Rect(textRect.x, rect.y + textAreaHeight + spacing, textRect.width, lineHeight);
            EditorGUI.PropertyField(voiceRect, voiceProp, new GUIContent("Voice Path"));

            Rect removeRect = new Rect(rect.width + rect.x - 20, rect.y, 20, lineHeight);
            if (GUI.Button(removeRect, "-", EditorStyles.miniButton))
                return true;

            return false;
        }

        private void DrawSimpleBranchElement(Rect rect, SerializedProperty element)
        {
            var branchNamesProp = element.FindPropertyRelative("SimpleBranch");
            var textLinesProp = element.FindPropertyRelative("SimpleTextLines");
            float singleLine = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;
            float branchSpacing = 6f;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, singleLine), "Branch Options", EditorStyles.boldLabel);
            float currentY = rect.y + singleLine + spacing;

            for (int i = 0; i < branchNamesProp.arraySize; i++)
            {
                var optionTextProp = branchNamesProp.GetArrayElementAtIndex(i);
                SerializedProperty resultLinesProp = null;
                if (i < textLinesProp.arraySize)
                    resultLinesProp = textLinesProp.GetArrayElementAtIndex(i);

                Rect branchHeaderRect = new Rect(rect.x, currentY, rect.width, singleLine + 4);
                GUI.Box(branchHeaderRect, GUIContent.none, EditorStyles.helpBox);

                EditorGUI.LabelField(new Rect(rect.x + 4, currentY + 2, 80, singleLine), $"Option {i + 1}:");
                optionTextProp.stringValue = EditorGUI.TextField(new Rect(rect.x + 90, currentY + 2, rect.width - 120, singleLine), optionTextProp.stringValue);

                if (GUI.Button(new Rect(rect.x + rect.width - 25, currentY + 2, 20, singleLine), "X"))
                {
                    RecordUndo("Remove Branch");
                    branchNamesProp.DeleteArrayElementAtIndex(i);
                    if (i < textLinesProp.arraySize)
                        textLinesProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                currentY += singleLine + 8f;

                float indent = 15f;
                float innerWidth = rect.width - indent;
                float innerX = rect.x + indent;

                if (resultLinesProp != null && resultLinesProp.isArray)
                {
                    for (int j = 0; j < resultLinesProp.arraySize; j++)
                    {
                        var lineProp = resultLinesProp.GetArrayElementAtIndex(j);
                        var contentProp = lineProp.FindPropertyRelative("Content");
                        float lineHeight = CalculateSingleTextLineHeight(contentProp.stringValue, innerWidth - 90);

                        Rect lineRect = new Rect(innerX, currentY, innerWidth, lineHeight);
                        GUI.Box(lineRect, GUIContent.none, EditorStyles.helpBox);

                        if (DrawSingleTextLine(new Rect(innerX + 2, currentY + 2, innerWidth - 4, lineHeight - 4), lineProp))
                        {
                            RecordUndo("Remove Branch Line");
                            resultLinesProp.DeleteArrayElementAtIndex(j);
                            break;
                        }

                        currentY += lineHeight + spacing;
                    }
                }
                else
                {
                    EditorGUI.LabelField(new Rect(innerX, currentY, innerWidth, singleLine), "No dialogue lines for this option.", EditorStyles.miniLabel);
                    currentY += singleLine + spacing;
                }

                if (GUI.Button(new Rect(innerX + 20, currentY, innerWidth - 40, singleLine), "+ Add Result Line"))
                {
                    RecordUndo("Add Branch Line");
                    EnsureBranchLineList(textLinesProp, i);
                    var linesList = textLinesProp.GetArrayElementAtIndex(i);
                    linesList.InsertArrayElementAtIndex(linesList.arraySize);
                }

                currentY += singleLine + branchSpacing * 2;
            }

            if (GUI.Button(new Rect(rect.x + 20, currentY, rect.width - 40, singleLine), "+ Add New Branch Option"))
            {
                RecordUndo("Add Branch");
                branchNamesProp.InsertArrayElementAtIndex(branchNamesProp.arraySize);
                branchNamesProp.GetArrayElementAtIndex(branchNamesProp.arraySize - 1).stringValue = "New Option";
                textLinesProp.InsertArrayElementAtIndex(textLinesProp.arraySize);
            }
        }

        private static void EnsureBranchLineList(SerializedProperty textLinesProp, int branchIndex)
        {
            while (textLinesProp.arraySize <= branchIndex)
                textLinesProp.InsertArrayElementAtIndex(textLinesProp.arraySize);
        }

        private void DrawDialogueTextElement(Rect rect, SerializedProperty element)
        {
            var linesProp = element.FindPropertyRelative("TextLines");
            float singleLine = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, singleLine), "Dialogue Group", EditorStyles.boldLabel);
            float currentY = rect.y + singleLine + spacing;

            for (int i = 0; i < linesProp.arraySize; i++)
            {
                var lineProp = linesProp.GetArrayElementAtIndex(i);
                var contentProp = lineProp.FindPropertyRelative("Content");
                float lineTotalHeight = CalculateSingleTextLineHeight(contentProp.stringValue, rect.width - 90);

                Rect itemRect = new Rect(rect.x, currentY, rect.width, lineTotalHeight);
                GUI.Box(itemRect, GUIContent.none, EditorStyles.helpBox);

                if (DrawSingleTextLine(new Rect(itemRect.x + 2, itemRect.y + 2, itemRect.width - 4, itemRect.height - 4), lineProp))
                {
                    RecordUndo("Remove Dialogue Line");
                    linesProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                currentY += lineTotalHeight + spacing;
            }

            if (GUI.Button(new Rect(rect.x + 40, currentY, rect.width - 80, singleLine), "+ Add Dialogue Line"))
            {
                RecordUndo("Add Dialogue Line");
                linesProp.InsertArrayElementAtIndex(linesProp.arraySize);
            }
        }

        private void DrawChoiceCommandProperty(Rect rect, SerializedProperty choiceCmdProp)
        {
            float currentY = rect.y;
            float standardHeight = EditorGUIUtility.singleLineHeight;
            float space = 2f;

            SerializedProperty optionsProp = choiceCmdProp.FindPropertyRelative("Options");

            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(i);
                SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");

                float fixedPartHeight = (standardHeight + space) * 6 + 15f;
                float conditionsPartHeight = condsProp.arraySize > 0
                    ? condsProp.arraySize * (standardHeight + space)
                    : 0f;
                float totalOptionHeight = fixedPartHeight + conditionsPartHeight;

                Rect optionRect = new Rect(rect.x, currentY, rect.width, totalOptionHeight);
                GUI.Box(optionRect, GUIContent.none, "box");

                EditorGUI.LabelField(new Rect(optionRect.x + 5, currentY + 5, 100, standardHeight), $"Option {i + 1}", EditorStyles.boldLabel);
                if (GUI.Button(new Rect(optionRect.x + optionRect.width - 25, currentY + 5, 20, standardHeight), "X"))
                {
                    RecordUndo("Remove Choice Option");
                    optionsProp.DeleteArrayElementAtIndex(i);
                    return;
                }

                currentY += standardHeight + space + 5;
                EditorGUI.PropertyField(new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight), optionProp.FindPropertyRelative("Text"));
                currentY += standardHeight + space;
                EditorGUI.PropertyField(new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight), optionProp.FindPropertyRelative("TargetStepId"));
                currentY += standardHeight + space;

                var targetDialogProp = optionProp.FindPropertyRelative("TargetDialogId");
                if (targetDialogProp != null)
                {
                    EditorGUI.PropertyField(new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight), targetDialogProp,
                        new GUIContent("Target Dialog Id", "Literal dialog id or @NpcResolvedPeace"));
                    currentY += standardHeight + space;
                }

                EditorGUI.PropertyField(new Rect(rect.x + 5, currentY, rect.width - 10, standardHeight), optionProp.FindPropertyRelative("ShowWhenFail"));
                currentY += standardHeight + space;

                float conditionHeight = DrawConditionListForProperty(new Rect(rect.x, currentY, rect.width, 0), optionProp);
                currentY += conditionHeight + space + 10;
            }

            if (GUI.Button(new Rect(rect.x, currentY + 5, rect.width, 25), "+ Add Option"))
            {
                RecordUndo("Add Choice Option");
                optionsProp.InsertArrayElementAtIndex(optionsProp.arraySize);
                var newElem = optionsProp.GetArrayElementAtIndex(optionsProp.arraySize - 1);
                newElem.FindPropertyRelative("Text").stringValue = "New Option";
                newElem.FindPropertyRelative("Conditions1").ClearArray();
            }
        }

        private float GetChoiceCommandHeight(SerializedProperty choiceCmdProp)
        {
            float h = EditorGUIUtility.singleLineHeight + 6;
            SerializedProperty optionsProp = choiceCmdProp.FindPropertyRelative("Options");
            h += 30;

            for (int i = 0; i < optionsProp.arraySize; i++)
            {
                SerializedProperty optionProp = optionsProp.GetArrayElementAtIndex(i);
                h += 20;
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("Text"));
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("TargetStepId"));
                var targetDialogH = optionProp.FindPropertyRelative("TargetDialogId");
                if (targetDialogH != null)
                    h += EditorGUI.GetPropertyHeight(targetDialogH);
                h += EditorGUI.GetPropertyHeight(optionProp.FindPropertyRelative("ShowWhenFail"));
                h += 6;
                h += EditorGUIUtility.singleLineHeight + 2;

                SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");
                h += EditorGUIUtility.singleLineHeight + 2;
                h += condsProp.arraySize * (EditorGUIUtility.singleLineHeight + 2);
            }

            return h + EditorGUIUtility.singleLineHeight + 10;
        }

        private float DrawConditionListForProperty(Rect startRect, SerializedProperty optionProp)
        {
            SerializedProperty condsProp = optionProp.FindPropertyRelative("Conditions1");
            float currentY = startRect.y;
            float lineHeight = EditorGUIUtility.singleLineHeight + 3f;
            float spacing = 2f;

            EditorGUI.LabelField(new Rect(startRect.x + 5, currentY, 80, lineHeight), "Conditions:", EditorStyles.label);
            if (GUI.Button(new Rect(startRect.x + 100, currentY, 120, lineHeight), "+ Add Condition", EditorStyles.miniButton))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var type in conditionTypes)
                {
                    var capturedType = type;
                    var attr = (ConditionMenuNameAttribute)Attribute.GetCustomAttribute(type, typeof(ConditionMenuNameAttribute));
                    string menuName = attr != null ? attr.MenuPath : type.Name;
                    menu.AddItem(new GUIContent(menuName), false, () =>
                    {
                        AddCondition(optionProp, (DialogCondition)Activator.CreateInstance(capturedType));
                    });
                }
                menu.ShowAsContext();
            }

            currentY += lineHeight + spacing;

            for (int i = 0; i < condsProp.arraySize; i++)
            {
                SerializedProperty condProp = condsProp.GetArrayElementAtIndex(i);
                DialogCondition condObj = GetTargetObjectOfProperty(condProp) as DialogCondition;

                Rect rowRect = new Rect(startRect.x + 20, currentY, startRect.width - 25, lineHeight);
                Rect deleteRect = new Rect(rowRect.x + rowRect.width - 25, rowRect.y, 25, lineHeight);
                Rect contentRect = new Rect(rowRect.x, rowRect.y, rowRect.width - 30, lineHeight);

                if (GUI.Button(contentRect, condObj != null ? condObj.GetSummary() : "?", EditorStyles.miniButton))
                    PopupWindow.Show(contentRect, new DialogueConditionPopup(condObj, this, currentData));

                if (GUI.Button(deleteRect, "-", EditorStyles.miniButtonRight))
                {
                    RecordUndo("Remove Condition");
                    condsProp.DeleteArrayElementAtIndex(i);
                    return currentY - startRect.y + lineHeight;
                }

                currentY += lineHeight + spacing;
            }

            return currentY - startRect.y;
        }

        private void AddCondition(SerializedProperty optionProp, DialogCondition newCond)
        {
            if (GetTargetObjectOfProperty(optionProp) is DialogChoiceOption optObj)
            {
                RecordUndo("Add Condition");
                optObj.Conditions1.Add(newCond);
                serializedObject.Update();
                innerListCache.Clear();
            }
        }

        private float GetDialogueTextCommandHeight(SerializedProperty element)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 2;
            var linesProp = element.FindPropertyRelative("TextLines");
            float textAreaWidth = Mathf.Max(100, EditorGUIUtility.currentViewWidth - 120);

            if (linesProp != null)
            {
                for (int i = 0; i < linesProp.arraySize; i++)
                {
                    var contentProp = linesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Content");
                    totalHeight += CalculateSingleTextLineHeight(contentProp.stringValue, textAreaWidth) + 2;
                }
            }

            return totalHeight + EditorGUIUtility.singleLineHeight + 4;
        }

        private float CalculateSingleTextLineHeight(string content, float availableWidth)
        {
            float minHeight = EditorGUIUtility.singleLineHeight * 2;
            float textHeight = EditorStyles.textArea.CalcHeight(new GUIContent(content ?? string.Empty), availableWidth);
            float contentHeight = Mathf.Max(textHeight, minHeight);
            return contentHeight + EditorGUIUtility.singleLineHeight + 6f;
        }

        private float GetSimpleBranchHeight(SerializedProperty element)
        {
            float totalHeight = EditorGUIUtility.singleLineHeight + 4;
            var branchNamesProp = element.FindPropertyRelative("SimpleBranch");
            var textLinesProp = element.FindPropertyRelative("SimpleTextLines");
            float singleLine = EditorGUIUtility.singleLineHeight;
            float nestedWidth = Mathf.Max(100, EditorGUIUtility.currentViewWidth - 140);

            for (int i = 0; i < branchNamesProp.arraySize; i++)
            {
                float branchHeight = singleLine + 8f;
                if (i < textLinesProp.arraySize)
                {
                    var resultLinesProp = textLinesProp.GetArrayElementAtIndex(i);
                    for (int j = 0; j < resultLinesProp.arraySize; j++)
                    {
                        var contentProp = resultLinesProp.GetArrayElementAtIndex(j).FindPropertyRelative("Content");
                        branchHeight += CalculateSingleTextLineHeight(contentProp.stringValue, nestedWidth) + 2f;
                    }
                }
                else
                {
                    branchHeight += singleLine;
                }

                branchHeight += singleLine + 12f;
                totalHeight += branchHeight + 4f;
            }

            return totalHeight + singleLine + 8f;
        }

        private float GetGenericCommandHeight(SerializedProperty rootProp)
        {
            float h = EditorGUIUtility.singleLineHeight + 6;
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            if (!prop.NextVisible(true))
                return h;

            do
            {
                if (SerializedProperty.EqualContents(prop, endProp))
                    break;
                h += EditorGUI.GetPropertyHeight(prop, true) + 2;
            } while (prop.NextVisible(false));

            return h;
        }

        private void DrawGenericCommandProperties(SerializedProperty rootProp, Rect startRect)
        {
            float curY = startRect.y;
            SerializedProperty prop = rootProp.Copy();
            SerializedProperty endProp = rootProp.GetEndProperty();
            if (!prop.NextVisible(true))
                return;

            do
            {
                if (SerializedProperty.EqualContents(prop, endProp))
                    break;

                float h = EditorGUI.GetPropertyHeight(prop, true);
                GUIContent label = new GUIContent(prop.displayName);
                if (prop.name == "Speaker") label.text = "Character ID";
                if (prop.name == "Content") label.text = "Dialogue Text";

                EditorGUI.PropertyField(new Rect(startRect.x, curY, startRect.width, h), prop, label, true);
                curY += h + 2;
            } while (prop.NextVisible(false));
        }

        private void ShowAddMenu(int stepIndex)
        {
            var menu = new GenericMenu();
            foreach (var type in commandTypes)
            {
                var capturedType = type;
                menu.AddItem(new GUIContent(DialogueEditorCommandLabels.GetMenuName(type)), false, () =>
                {
                    RecordUndo("Add Command");
                    serializedObject.Update();

                    var commandsProp = stepsProp.GetArrayElementAtIndex(stepIndex).FindPropertyRelative("Commands");
                    int index = commandsProp.arraySize;
                    commandsProp.InsertArrayElementAtIndex(index);
                    commandsProp.GetArrayElementAtIndex(index).managedReferenceValue = DialogueEditorCommandFactory.Create(capturedType);
                    serializedObject.ApplyModifiedProperties();
                    innerListCache.Clear();
                });
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("No command types found"));

            menu.ShowAsContext();
        }

        private void DeleteElement(SerializedProperty element, int idx)
        {
            var so = element.serializedObject;
            string propertyPath = element.propertyPath;
            string arrayPath = propertyPath.Substring(0, propertyPath.LastIndexOf(".Array"));

            EditorApplication.delayCall += () =>
            {
                so.Update();
                var listProp = so.FindProperty(arrayPath);
                if (listProp != null && idx < listProp.arraySize)
                {
                    listProp.DeleteArrayElementAtIndex(idx);
                    so.ApplyModifiedProperties();
                }
                Repaint();
            };
        }

        private void CreateSoFromJsonPicker()
        {
            string path = EditorUtility.OpenFilePanel("Create SO from JSON", DialogueSoBootstrap.OutputFolder, "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var so = DialogueSoBootstrap.CreateFromJsonFile(path);
                if (so == null)
                    return;

                currentData = so;
                InitSerializedObject();
                Selection.activeObject = so;
                EditorUtility.DisplayDialog("Success", $"Created SO:\n{AssetDatabase.GetAssetPath(so)}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Create SO from JSON failed: {ex}");
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        private void SyncAllSoFromOutput()
        {
            if (!EditorUtility.DisplayDialog(
                    "Sync All SO",
                    $"Rebuild all SO in {DialogueSoBootstrap.SoFolder} from {DialogueSoBootstrap.OutputFolder}?\nExisting SO will be overwritten.",
                    "Sync",
                    "Cancel"))
                return;

            int count = DialogueSoBootstrap.BatchCreateFromOutputFolder();
            EditorUtility.DisplayDialog("Success", $"Synced {count} dialogue SO assets.", "OK");
        }

        private void ImportFromJson()
        {
            string path = ResolveJsonPath(forImport: true);
            if (string.IsNullOrEmpty(path))
                return;

            if (!EditorUtility.DisplayDialog("Import JSON", $"Overwrite Steps from:\n{path}?", "Import", "Cancel"))
                return;

            try
            {
                string json = File.ReadAllText(path);
                var data = DialogueDataConverter.Deserialize(json);

                Undo.RegisterCompleteObjectUndo(currentData, "Import Dialogue from JSON");
                currentData.Steps = data.Steps ?? new List<DialogueStepData>();
                DialogueDataConverter.Normalize(new DialogueData { Steps = currentData.Steps });

                if (linkedJsonPathProp != null && string.IsNullOrEmpty(currentData.LinkedJsonPath))
                {
                    string relative = DialogueEditorMenus.ToProjectRelativePath(path);
                    if (relative.StartsWith("Assets/"))
                        currentData.LinkedJsonPath = relative;
                }

                EditorUtility.SetDirty(currentData);
                InitSerializedObject();
                EditorUtility.DisplayDialog("Success", "Imported dialogue from JSON.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Import dialogue JSON failed: {ex}");
                EditorUtility.DisplayDialog("Error", $"Import failed:\n{ex.Message}", "OK");
            }
        }

        private void ExportToJson(EditorDialogueData source)
        {
            string path = ResolveJsonPath(forImport: false);
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var data = DialogueDataConverter.FromEditorSteps(source.Steps);
                string json = DialogueDataConverter.Serialize(data);
                File.WriteAllText(path, json);

                if (path.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/')))
                    AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", $"Exported to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Export dialogue JSON failed: {ex}");
                EditorUtility.DisplayDialog("Error", $"Export failed:\n{ex.Message}", "OK");
            }
        }

        private string ResolveJsonPath(bool forImport)
        {
            if (!string.IsNullOrEmpty(currentData.LinkedJsonPath))
            {
                string linked = currentData.LinkedJsonPath.Replace('\\', '/');
                string full = linked.StartsWith("Assets/")
                    ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), linked))
                    : linked;

                if (!forImport)
                    return full;

                if (File.Exists(full))
                {
                    if (EditorUtility.DisplayDialog("Use Linked JSON", $"Use linked file?\n{linked}", "Yes", "Pick other"))
                        return full;
                }
            }

            string dir = Directory.Exists("Assets/Resources/Dialogue/output")
                ? "Assets/Resources/Dialogue/output"
                : Application.dataPath;

            return forImport
                ? EditorUtility.OpenFilePanel("Import JSON", dir, "json")
                : EditorUtility.SaveFilePanel("Export JSON", dir, currentData.name, "json");
        }

        private string GetAssetKey()
        {
            if (currentData == null)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(currentData);
            return string.IsNullOrEmpty(path) ? currentData.GetInstanceID().ToString() : AssetDatabase.AssetPathToGUID(path);
        }

        private bool IsStepExpanded(int stepIndex) =>
            stepExpandedCache.TryGetValue($"{GetAssetKey()}_{stepIndex}", out bool v) ? v : true;

        private void SetStepExpanded(int stepIndex, bool expanded) =>
            stepExpandedCache[$"{GetAssetKey()}_{stepIndex}"] = expanded;

        private bool IsCmdFolded(int stepIndex, int cmdIndex) =>
            cmdFoldedCache.TryGetValue($"{GetAssetKey()}_{stepIndex}_{cmdIndex}", out bool v) ? v : true;

        private void SetCmdFolded(int stepIndex, int cmdIndex, bool folded) =>
            cmdFoldedCache[$"{GetAssetKey()}_{stepIndex}_{cmdIndex}"] = folded;

        private void RecordUndo(string label)
        {
            if (currentData != null)
                Undo.RecordObject(currentData, label);
        }

        private object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null)
                return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[").Replace("]", "");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValueImp(obj, elementName, index);
                }
                else
                {
                    obj = GetValueImp(obj, element);
                }
            }

            return obj;
        }

        private static object GetValueImp(object source, string name)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);
                type = type.BaseType;
            }

            return null;
        }

        private static object GetValueImp(object source, string name, int index)
        {
            if (GetValueImp(source, name) is IEnumerable enumerable)
            {
                var enm = enumerable.GetEnumerator();
                for (int i = 0; i <= index; i++)
                {
                    if (!enm.MoveNext())
                        return null;
                }
                return enm.Current;
            }

            return null;
        }
    }
}
