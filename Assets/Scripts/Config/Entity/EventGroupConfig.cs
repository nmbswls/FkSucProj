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
            public int MemberId;
            
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        public List<MemberInfo> GroupMemberInfos = new List<MemberInfo>();

        //[Serializable]
        //public class GroupEventOutput
        //{
        //    public enum EOutputType
        //    {
        //        None,
        //        UpdateInteractStatus,
        //        ActivateUnits,
        //        RemoveEntities,
        //    }

        //    public EOutputType OutputType;
        //    public long Param1;
        //    public long Param2;
        //    public string Param3;
        //    public string Param4;
        //}

        [Serializable]
        public class GroupInnerTrigger
        {
            public enum ETriggerType
            {
                None,
                SelfStatus,
                MemberCleared,
                AnyEnmity,
                GroupInteractableStatus,
            }
            public int TriggerId;
            public ETriggerType TriggerType;
            public int MaxTriggerCnt;
            public float MinTriggerInterval;

            public long Param1;
            public long Param2;
            public string Param3;
            public string Param4;

            public List<LogicInteractOutput> Outputs = new();
        }

        public List<GroupInnerTrigger> InnerTriggers = new();


        [Serializable]
        public class EventGroupStateInfo
        {
            public int StateId;
            public List<int> EnsureMemberIds = new();
            public List<int> ActiveTriggerIds = new();
        }

        public List<EventGroupStateInfo> EventGroupStateInfos = new();
    }
}
