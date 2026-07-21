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
            public enum EPlacementMode
            {
                RelativeToGroup,
                NamedPoint,
            }

            public int MemberId;
            public List<string> Tags = new();
            public EPlacementMode PlacementMode;
            public string NamedPointName;
            
            [SerializeReference]
            public EntityInitInfo InitInfo;
        }

        public enum EStageConditionType
        {
            None,
            AllMembersInteractStatus,
            AllMembersDefeated,
        }

        public enum EConditionMode
        {
            All,
            Any,
        }

        [Serializable]
        public class StageCondition
        {
            public EStageConditionType ConditionType;
            public List<int> MemberIds = new();
            public string MemberTag;
            public int RequiredStatusId = 1;
        }

        [Serializable]
        public class StageInfo
        {
            public int StageId;
            public List<int> EnsureMemberIds = new();
            public List<int> ActiveTriggerIds = new();
            public EConditionMode ConditionMode;
            public List<StageCondition> CompletionConditions = new();
            public int CompleteInteractId;
            public int NextStageId = -1;
            public bool CompleteEvent;
            public bool DestroyOnComplete = true;
        }

        // New assets use stages. Empty keeps the legacy trigger/state flow intact.
        public List<StageInfo> Stages = new();

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
                StageEntered,
                MemberStatusChanged,
                MemberDefeated,
            }

            public enum EFirePolicy
            {
                Once,
                EveryOccurrence,
                Cooldown,
            }

            public int TriggerId;
            public ETriggerType TriggerType;
            public EFirePolicy FirePolicy;
            public int MaxTriggerCnt;
            public float MinTriggerInterval;

            public long Param1;
            public long Param2;
            public string Param3;
            public string Param4;

            // Typed selectors are used by stage-flow triggers. Param fields remain for legacy assets.
            public List<int> MemberIds = new();
            public string MemberTag;
            public int RequiredStatusId = 1;
            public int ActionInteractId;

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
