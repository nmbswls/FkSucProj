
using System;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;
using UnityEngine.UIElements;
using static My.Config.LogicInteractOutput;

namespace My.Config
{

    

    [Serializable]
    public class LogicInteractOutput
    {
        public enum EOutputType
        {
            Invalid = 0,
            ChangeSelfStatus,
            FinishTask,
            GiveItems,
            CostItems,
            Teleport,
            OpenPanel,


            ActivateEventGroup,
            SpecialMoveTo,
            PresentationMoveTo,

            StartStealth,

            SetLocalSwitch,
            UnsetLocalSwitch,
            SetGlobalSwitch,

            OpenDialog,

            SelfAnim,
            Wait,

            StartRetreat,
            TriggerSpawner,

            NextDayPeriod,
            MarkCharacterValue, // 标记人物变量

            EGMemberChangeState = 100,
            EGMemberActivate = 101,
        }

        public EOutputType OutputType;
        public long Param1;
        public long Param2;
        public string Param3;
        public string Param4;
        public long Param5;
        public long Param6;

        public float DelayTime;

        public enum ETargetType
        {
            Default,
            GroupMember,
            StaticName,
            DynamicEntity,
        }
        public ETargetType TargetType;
        public int MemberId;
        public string StaticName;
        public string DynamicEntityVariable;
    }

    [Serializable]
    public class InteractCheckCond
    {
        public enum ECheckType
        {
            None = 0,
            NotHide,
            HasLocalSwitch,
            NoLocalSwitch,

            PlayerNotRetreating,
        }

        public ECheckType CheckType;
        public long Param1;
        public long Param2;
        public string Param3;
        public string Param4;
    }


    [Serializable]
    public class MapInteractInfo
    {
        // 所有交互放一起
        public int InteractId;
        public string Label; // 选项
        public string UnLabel; // 灰色选项
        public bool HideWhenFail = true;
        public float NeedDist = 0.4f;
        public bool Passive = false;

        public List<CommonCheckCond> CheckCommonCond = new();
        public List<InteractCheckCond> CheckInteractCond = new();
        public List<LogicInteractOutput> Outputs = new();
    }
}

