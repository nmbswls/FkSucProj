using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using My.Map.Entity.AI;

[CustomPropertyDrawer(typeof(MotionDataBase), true)]
public class ProjectileMotionDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(MotionDataBase);
}