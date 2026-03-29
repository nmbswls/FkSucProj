using System.Collections.Generic;
using System;
using UnityEditor;

[CustomPropertyDrawer(typeof(MotionDataBase), true)]
public class ProjectileMotionDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(MotionDataBase);
}