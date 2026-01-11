using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using My.Map.Entity.AI;
using My.Map.Logic;
using My.MapExport;

// 针对 EntityInitInfo
[CustomPropertyDrawer(typeof(EntityInitInfo), true)]
public class EntityInitInfoDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(EntityInitInfo);
}