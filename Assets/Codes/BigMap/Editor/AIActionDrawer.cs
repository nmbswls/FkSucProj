using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using My.Map.Entity.AI;

// 针对 AIAction
[CustomPropertyDrawer(typeof(AIAction), true)]
public class AIActionDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(AIAction);
}