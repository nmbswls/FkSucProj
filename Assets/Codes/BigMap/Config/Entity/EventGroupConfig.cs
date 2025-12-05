using System;
using System.Collections.Generic;
using My.Config;
using My.Map;
using My.Map.Fight;
using My.MapExport;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/EventGroup")]
    [Serializable]
    public  class EventGroupConfig : MapInteractPointConfig
    {
        public string CfgId;

        // 全部死亡才可交互
        public List<int> InteractCondDeadList = new();

        [Serializable]
        public class MemberInfo
        {
            public int GroupId;
            
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        // 受管理信息
        public List<MemberInfo> StaticGroupEntites = new List<MemberInfo>();
    }
}
