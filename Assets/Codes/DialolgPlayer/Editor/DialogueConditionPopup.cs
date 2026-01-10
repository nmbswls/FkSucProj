
using UnityEditor;
using UnityEngine;

namespace My.Dialog
{
    // 这是一个临时的弹出窗口内容，用于编辑单个条件
    public class DialogueConditionPopup : PopupWindowContent
    {
        private readonly DialogCondition _condition;
        private readonly EditorWindow _ownerWindow; // 用于重绘主窗口
        private readonly ScriptableObject _dataObject; // 用于Undo记录

        // 构造函数接收需要编辑的数据实例
        public DialogueConditionPopup(DialogCondition condition, EditorWindow owner, ScriptableObject dataObject)
        {
            _condition = condition;
            _ownerWindow = owner;
            _dataObject = dataObject;
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

            // --- 类型 1: Int 变量 ---
            if (_condition is ConditionLocalVariableInt intCond)
            {
                intCond.VariableKey = EditorGUILayout.TextField("Variable Key", intCond.VariableKey);
                intCond.Compare = (ConditionLocalVariableInt.CompareType)EditorGUILayout.EnumPopup("Operator", intCond.Compare);
                intCond.Value = EditorGUILayout.IntField("Value", intCond.Value);

                // 显示人性化的预览
                string opSymbol = GetOpSymbol(intCond.Compare);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"Logic: if ( {intCond.VariableKey} {opSymbol} {intCond.Value} )", MessageType.Info);
            }
            // --- 类型 2: String 变量 ---
            else if (_condition is ConditionLocalVariableString strCond)
            {
                strCond.VariableKey = EditorGUILayout.TextField("Variable Key", strCond.VariableKey);
                strCond.Compare = (ConditionLocalVariableString.CompareType)EditorGUILayout.EnumPopup("Operator", strCond.Compare);
                strCond.Value = EditorGUILayout.TextField("Value", strCond.Value);

                string opSymbol = strCond.Compare == ConditionLocalVariableString.CompareType.Equals ? "==" : "!=";
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"Logic: if ( {strCond.VariableKey} {opSymbol} \"{strCond.Value}\" )", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Unknown Condition Type");
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

        private string GetOpSymbol(ConditionLocalVariableInt.CompareType type)
        {
            switch (type)
            {
                case ConditionLocalVariableInt.CompareType.Equals: return "==";
                case ConditionLocalVariableInt.CompareType.Greater: return ">";
                case ConditionLocalVariableInt.CompareType.Less: return "<";
                case ConditionLocalVariableInt.CompareType.GE: return ">=";
                case ConditionLocalVariableInt.CompareType.LE: return "<=";
                case ConditionLocalVariableInt.CompareType.NotEquals: return "!=";
                default: return "?";
            }
        }
    }
}