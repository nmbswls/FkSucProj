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
    public  class MapEventGroupConfig : MapInteractPointConfig
    {
        [Serializable]
        public class MemberInfo
        {
            public int GroupId;
            
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        // 受管理信息
        public List<MemberInfo> StaticGroupEntites = new List<MemberInfo>();

        [Serializable]
        public class GroupEventOutput
        {
            public enum EOutputType
            {
                None,
                UpdateInteractStatus,
                ActivateUnits,
                RemoveEntities,
            }

            public EOutputType OutputType;
            public long Param1;
            public long Param2;
            public string Param3;
            public string Param4;
        }

        [Serializable]
        public class GroupEventListener
        {
            public enum ETriggerType
            {
                None,
                Cleared,
                AnyEnmity,
                MemberIntStatus,
            }

            public ETriggerType TriggerType;
            public long Param1;
            public long Param2;
            public string Param3;
            public string Param4;

            public List<GroupEventOutput> Outputs = new();
        }

        public List<GroupEventListener> EventTriggers = new();
    }
}
