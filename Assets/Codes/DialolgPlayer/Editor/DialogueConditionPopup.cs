
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace My.Dialog
{
    // 这是一个临时的弹出窗口内容，用于编辑单个条件
    public class DialogueConditionPopup : PopupWindowContent
    {
        private readonly DialogCondition _condition;
        private readonly EditorWindow _ownerWindow; // 用于重绘主窗口
        private readonly ScriptableObject _dataObject; // 用于Undo记录

        // 缓存字段列表，避免每帧反射
        private FieldInfo[] _fields;
        // 构造函数接收需要编辑的数据实例
        public DialogueConditionPopup(DialogCondition condition, EditorWindow owner, ScriptableObject dataObject)
        {
            _condition = condition;
            _ownerWindow = owner;
            _dataObject = dataObject;

            _fields = _condition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        public override Vector2 GetWindowSize()
        {
            // 根据不同类型动态调整高度
            if (_condition is ConditionLocalVariableInt) return new Vector2(300, 160);
            if (_condition is ConditionLocalVariableString) return new Vector2(300, 160);
            return new Vector2(300, 100);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Edit Condition Parameters", EditorStyles.boldLabel);

            // 绘制背景
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck(); // 开始检测修改

            // 2. 通用反射绘制字段
            foreach (var field in _fields)
            {
                // 获取当前值
                object value = field.GetValue(_condition);
                string label = ObjectNames.NicifyVariableName(field.Name); // 驼峰转可读名称

                // 根据类型绘制不同的输入框
                object newValue = DrawField(label, field.FieldType, value);

                // 如果值变了，利用反射写回对象
                if (GUI.changed) // 注意：这里简单用GUI.changed判断，下面统一处理Undo
                {
                    field.SetValue(_condition, newValue);
                }
            }

            EditorGUILayout.EndVertical();

            // 如果发生修改
            if (EditorGUI.EndChangeCheck())
            {
                // 标记对象已脏，确保Ctrl+Z有效且能保存
                if (_dataObject != null) EditorUtility.SetDirty(_dataObject);
                // 强制主窗口重绘，以便按钮上的文字实时更新
                if (_ownerWindow != null) _ownerWindow.Repaint();
            }
        }

        // 辅助函数：根据类型画出对应的 GUI
        private object DrawField(string label, Type type, object value)
        {
            if (type == typeof(int))
            {
                return EditorGUILayout.IntField(label, (int)value);
            }
            else if (type == typeof(float))
            {
                return EditorGUILayout.FloatField(label, (float)value);
            }
            else if (type == typeof(string))
            {
                return EditorGUILayout.TextField(label, (string)value ?? "");
            }
            else if (type == typeof(bool))
            {
                return EditorGUILayout.Toggle(label, (bool)value);
            }
            else if (type.IsEnum)
            {
                return EditorGUILayout.EnumPopup(label, (Enum)value);
            }
            // 可以根据需要扩展更多类型 (Vector3, Color 等)

            EditorGUILayout.LabelField(label, "Unsupported Type");
            return value;
        }
    }
}