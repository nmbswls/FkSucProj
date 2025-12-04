using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using System.Linq;
using My.Map.Entity.AI;

// 针对 AIAction
[CustomPropertyDrawer(typeof(AIActionCfg), true)]
public class AIActionCfgDrawer : BaseTypePickerDrawer
{
    protected override Type BaseType => typeof(AIActionCfg);
}