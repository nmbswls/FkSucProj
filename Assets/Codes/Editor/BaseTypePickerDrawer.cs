using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;

public abstract class BaseTypePickerDrawer : PropertyDrawer
{
    protected abstract Type BaseType { get; }

    private static readonly Dictionary<Type, Type[]> sTypesCache = new();
    private static readonly Dictionary<Type, string[]> sOptionsCache = new();

    private static void EnsureTypes(Type baseType)
    {
        if (sTypesCache.ContainsKey(baseType)) return;

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .Where(t => t != null && !t.IsAbstract && !t.ContainsGenericParameters && baseType.IsAssignableFrom(t))
            .OrderBy(t => t.Name)
            .ToArray();

        sTypesCache[baseType] = types;
        sOptionsCache[baseType] = types.Select(t => t.Name).ToArray();
    }

    // 手动刷新指定基类的缓存
    public static void RefreshTypeCache(Type baseType)
    {
        sTypesCache.Remove(baseType);
        sOptionsCache.Remove(baseType);
        EnsureTypes(baseType);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        EnsureTypes(BaseType);
        float h = EditorGUIUtility.singleLineHeight + 2f;
        if (property.isExpanded)
        {
            var it = property.Copy();
            var end = it.GetEndProperty();
            bool enter = it.NextVisible(true);
            while (enter && !SerializedProperty.EqualContents(it, end))
            {
                if (it.propertyPath.EndsWith(".m_Script")) { enter = it.NextVisible(false); continue; }
                h += EditorGUI.GetPropertyHeight(it, true) + 2f;
                enter = it.NextVisible(false);
            }
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureTypes(BaseType);

        var types = sTypesCache[BaseType];
        var options = sOptionsCache[BaseType];

        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        var popupRect = new Rect(line.x, line.y, line.width - 42f, line.height);
        var toggleRect = new Rect(line.xMax - 40f, line.y, 20f, line.height);
        var refreshRect = new Rect(line.xMax - 20f, line.y, 20f, line.height);

        Type currentType = GetManagedReferenceType(property);
        int idx = Math.Max(0, Array.FindIndex(types, t => t == currentType));

        EditorGUI.BeginChangeCheck();
        int newIdx = EditorGUI.Popup(popupRect, "Type", idx, options);
        bool changed = EditorGUI.EndChangeCheck();
        if (changed && types.Length > 0)
        {
            property.managedReferenceValue = CreateInstanceSafe(types[Mathf.Clamp(newIdx, 0, types.Length - 1)]);
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            return;
        }

        // 手动刷新按钮（可选）
        if (GUI.Button(refreshRect, "R", EditorStyles.miniButton))
        {
            RefreshTypeCache(BaseType);
        }

        property.isExpanded = GUI.Toggle(toggleRect, property.isExpanded, property.isExpanded ? "▼" : "▶", EditorStyles.label);

        float y = line.y + line.height + 2f;
        if (property.isExpanded && currentType != null)
        {
            var it = property.Copy();
            var end = it.GetEndProperty();
            bool enter = it.NextVisible(true);
            while (enter && !SerializedProperty.EqualContents(it, end))
            {
                if (it.propertyPath.EndsWith(".m_Script")) { enter = it.NextVisible(false); continue; }
                float h = EditorGUI.GetPropertyHeight(it, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), it, true);
                y += h + 2f;
                enter = it.NextVisible(false);
            }
        }
    }

    protected static Type GetManagedReferenceType(SerializedProperty property)
    {
        string full = property.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full)) return null;
        int space = full.IndexOf(' ');
        if (space < 0) return null;
        string asmName = full[..space];
        string typeName = full[(space + 1)..];
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == asmName);
        return asm?.GetType(typeName);
    }

    protected static object CreateInstanceSafe(Type t)
    {
        if (t == null) return null;
        try { return Activator.CreateInstance(t); }
        catch { return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t); }
    }

    public static void RefreshCacheFor(Type baseType)
    {
        sTypesCache.Remove(baseType);
        sOptionsCache.Remove(baseType);
    }
}