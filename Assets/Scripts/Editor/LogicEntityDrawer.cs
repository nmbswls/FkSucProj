using System.Collections.Generic;
using System;
using UnityEditor;
using My.MapExport;

// 针对 EntityInitInfo
[CustomPropertyDrawer(typeof(EntityInitInfo), true)]
public class EntityInitInfoDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(EntityInitInfo);
}