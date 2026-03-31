using System.Collections.Generic;
using System;
using UnityEditor;
using My;

[CustomPropertyDrawer(typeof(MotionDataBase), true)]
public class ProjectileMotionDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(MotionDataBase);
}