using System;
using System.Collections;
using System.Collections.Generic;
using Unit.Ability.Effect;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MapAbilityEffectCfg), true)]
public class BaseAbilityEffectDrawer : PropertyDrawer
{
    public static readonly Dictionary<EAbilityEffectType, Type> TypeMap = new()
    {
        { EAbilityEffectType.None, typeof(NoneEffectCfg) }, // 下面会定义一个占位类
        { EAbilityEffectType.ApplyBuff, typeof(MapAbilityEffectAddBuffCfg) },
        { EAbilityEffectType.FakeDamage, typeof(MapAbilityEffectCostResourceCfg) },
        { EAbilityEffectType.DashStart, typeof(MapAbilityEffectDashStartCfg) },
        { EAbilityEffectType.DashEnd, typeof(MapAbilityEffectDashEndCfg) },

         { EAbilityEffectType.HitBox, typeof(MapAbilityEffectHitBoxCfg) },
         { EAbilityEffectType.OpenLock, typeof(MapAbilityEffectUnlockLootPoint) },
         { EAbilityEffectType.RemoveBuff, typeof(MapAbilityEffectRemoveBuffCfg) },
         { EAbilityEffectType.SpawnBullet, typeof(MapAbilityEffectSpawnBulletCfg) },

         { EAbilityEffectType.UseItem, typeof(MapAbilityEffectUseItemCfg) },
         { EAbilityEffectType.UseWeapon, typeof(MapAbilityEffectUseWeaponCfg) },
    };

    // 占位类型
    [Serializable]
    private class NoneEffectCfg : MapAbilityEffectCfg
    {
        public string note = "Select an Effect Type";

        public NoneEffectCfg()
        {
            EffectType = EAbilityEffectType.None;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 动态计算高度：类型行 + 子类所有字段
        float h = EditorGUIUtility.singleLineHeight + 6;

        // 访问该对象的子属性（ManagedReference 的字段以子属性形式存在）
        var iterator = property.Copy();
        var end = iterator.GetEndProperty();
        bool expanded = property.isExpanded;

        // 如果折叠，显示一行即可
        if (!expanded)
            return h;

        // 进入第一个子属性（注意：如果没有子属性，NextVisible(true) 会返回 false）
        bool enter = iterator.NextVisible(true);
        while (enter && !SerializedProperty.EqualContents(iterator, end))
        {
            // 跳过你不想计算的字段
            //if (iterator.name != "EffectType")
            {
                h += EditorGUI.GetPropertyHeight(iterator, true) + 4;
            }

            enter = iterator.NextVisible(false);
        }

        return h + 6;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EnsureInstance(property);

        // Header foldout
        var y = position.y;
        var header = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, label, true);
        y += header.height + 2f;

        // Type row
        var typeRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        var typeProp = property.FindPropertyRelative("EffectType");
        if (typeProp == null || typeProp.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.HelpBox(typeRect, "Missing 'EffectType' enum field.", MessageType.Warning);
            y += typeRect.height + 0;
        }
        else
        {
            var oldType = (EAbilityEffectType)typeProp.enumValueIndex;
            var newType = (EAbilityEffectType)EditorGUI.EnumPopup(typeRect, "Effect Type", oldType);
            if (newType != oldType)
            {
                typeProp.enumValueIndex = (int)newType;
                SafeSwitchType(property, newType);
            }
            y += typeRect.height + 2f;
        }

        if (property.isExpanded)
        {
            // Draw remaining fields
            var it = property.Copy();
            var end = it.GetEndProperty();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren) && it.propertyPath != end.propertyPath)
            {
                enterChildren = false;
                if (it.name == "EffectType") continue;

                float h = EditorGUI.GetPropertyHeight(it, true);
                var r = new Rect(position.x, y, position.width, h);
                try
                {
                    EditorGUI.PropertyField(r, it, true);
                }
                catch (Exception e)
                {
                    EditorGUI.HelpBox(r, $"Draw error: {e.Message}", MessageType.Error);
                }
                y += h + 4f;
            }
        }

        EditorGUI.EndProperty();
    }

    private void EnsureInstance(SerializedProperty property)
    {
        // 仅在 SerializeReference 情况下有意义
        if (property.propertyType != SerializedPropertyType.ManagedReference) return;

        if (property.managedReferenceValue == null)
        {
            var noneType = TypeMap[EAbilityEffectType.None];
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(noneType);
            }
            catch { /* 忽略，保持 null */ }

            property.managedReferenceValue = instance;
            property.serializedObject?.ApplyModifiedProperties();
        }
        else
        {
            // 同步 EffectType 与实例类型的默认值（可选，不抛错）
            var typeProp = property.FindPropertyRelative("EffectType");
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                // 当实例与枚举不匹配时不强制纠正，避免误覆盖，交给用户切换
            }
        }
    }

    private void SafeSwitchType(SerializedProperty property, EAbilityEffectType newType)
    {
        var targetType = TypeMap[newType] ?? TypeMap[EAbilityEffectType.None];
        try
        {
            var instance = Activator.CreateInstance(targetType) as MapAbilityEffectCfg;
            instance.EffectType = newType;
            property.managedReferenceValue = instance;
            property.serializedObject?.ApplyModifiedProperties();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to switch type to {targetType}: {e.Message}");
            // 回退到 None
            try
            {
                var fallback = Activator.CreateInstance(TypeMap[EAbilityEffectType.None]);
                property.managedReferenceValue = fallback;
                property.serializedObject?.ApplyModifiedProperties();
            }
            catch { /* 静默失败 */ }
        }
    }
}