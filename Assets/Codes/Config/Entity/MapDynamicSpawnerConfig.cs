using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using My.MapExport;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/DynamicSpawner")]
    [Serializable]
    public  class MapDynamicSpawnerConfig : ScriptableObject
    {
        public string CfgId;

        [Serializable]
        public class MemberInfo
        {
            public int MemberId;

            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        public List<MemberInfo> SpawnInfos = new List<MemberInfo>();

        public bool SpawnOnCreate = true;
    }
}
