using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/PatrolGroup")]
    [Serializable]
    public  class MapPatrolGroupConfig : ScriptableObject
    {
        public string CfgId;
    }
}
