using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using My.Config;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/InteractPoint")]
    [Serializable]
    public  class MapInteractPointConfig : ScriptableObject
    {
        public string CfgId;

        [Serializable]
        public class InteractStatusInfo
        {
            public int StatusId;

            public MapInteractInfo InteractInfo;

            public bool HasBlock = false;
        }

        public InteractStatusInfo MainStatusInfo;
        public List<InteractStatusInfo> ExtraStatusInfos;

    }
}
