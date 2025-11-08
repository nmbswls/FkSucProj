using Codice.Client.BaseCommands;
using System;
using System.Collections;
using System.Collections.Generic;
using Unit.Ability.Effect;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(PolymorphicListAttribute))]
public class PolymorphicListDrawer : PropertyDrawer
{
    private ReorderableList _list;
    private SerializedProperty _arrayProp;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isArray)
            return EditorGUI.GetPropertyHeight(property, label, true); // 对元素或非数组，按默认高度
        EnsureList(property);
        return _list == null ? EditorGUIUtility.singleLineHeight : _list.GetHeight();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!property.isArray)
        {
            // 非数组（包括元素），不处理，让默认或元素自己的 Drawer 画
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        EnsureList(property);
        if (_list == null)
        {
            EditorGUI.LabelField(position, "PolymorphicList: target must be a List<BaseActionData> with [SerializeReference]");
            return;
        }
        _list.DoList(position);
    }

    private void EnsureList(SerializedProperty property)
    {
        if (_list != null && _arrayProp == property) return;

        _arrayProp = property;

        // 验证：必须是数组或列表
        if (!_arrayProp.isArray) { _list = null; return; }

        // 取属性上的参数
        var attr = (PolymorphicListAttribute)attribute;

        _list = new ReorderableList(property.serializedObject, _arrayProp, attr.draggable, attr.showHeader, attr.canAdd, attr.canRemove);

        _list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, $"{_arrayProp.displayName} (Polymorphic List)");
        };

        _list.elementHeightCallback = index =>
        {
            var element = _arrayProp.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 8;
        };

        _list.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = _arrayProp.GetArrayElementAtIndex(index);
            var r = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8);
            EditorGUI.PropertyField(r, element, new GUIContent($"Item {index}"), true);
        };

        _list.onAddCallback = list =>
        {
            int index = _arrayProp.arraySize;
            _arrayProp.InsertArrayElementAtIndex(index);

            var newElem = _arrayProp.GetArrayElementAtIndex(index);
            // 初始化为 None 类型实例
            var instance = System.Activator.CreateInstance(BaseAbilityEffectDrawer.TypeMap[EAbilityEffectType.None]);
            newElem.managedReferenceValue = instance;

            property.serializedObject.ApplyModifiedProperties();
        };

        _list.onRemoveCallback = list =>
        {
            if (list.index >= 0 && list.index < _arrayProp.arraySize)
            {
                _arrayProp.DeleteArrayElementAtIndex(list.index);
                property.serializedObject.ApplyModifiedProperties();
            }
        };

        _list.onCanAddCallback = l => attr.canAdd;
        _list.onCanRemoveCallback = l => attr.canRemove;
    }
}