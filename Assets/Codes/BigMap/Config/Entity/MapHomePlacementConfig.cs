using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using My.Config;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/HomePlacement")]
    [Serializable]
    public  class MapHomePlacementEntityConfig : ScriptableObject
    {
        public string CfgId;

        public MapInteractInfo InteractInfo;
    }
}
