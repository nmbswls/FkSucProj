using System;
using System.Collections;
using System.Collections.Generic;
using Unit.Ability.Effect;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class PolymorphicListAttribute : PropertyAttribute
{
    // 可选：是否允许折叠元素、是否显示索引、是否允许复制等
    public bool showHeader = true;
    public bool draggable = true;
    public bool canAdd = true;
    public bool canRemove = true;

    public PolymorphicListAttribute() { }

    public PolymorphicListAttribute(bool showHeader = true, bool draggable = true, bool canAdd = true, bool canRemove = true)
    {
        this.showHeader = showHeader;
        this.draggable = draggable;
        this.canAdd = canAdd;
        this.canRemove = canRemove;
    }
}

