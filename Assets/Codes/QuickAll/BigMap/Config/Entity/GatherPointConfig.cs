using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Config.Unit
{
    [CreateAssetMenu(menuName = "GP/Entity/DestoryObj")]
    [Serializable]
    public class GatherPointConfig : ScriptableObject
    {
        public string CfgId;

        public bool CanRefresh = false;
        public float RefreshInterval = 200;

        public int MaxCount = 3;
        public float GatherTime = 1.5f;

        public string DropBundleId;
    }
}
