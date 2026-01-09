
using System;
using System.Collections.Generic;
using UnityEngine;
using static My.Config.LogicInteractOutput;

namespace My.Config
{

    

    [Serializable]
    public class LogicInteractOutput
    {
        public enum EOutputType
        {
            Invalid,
            ChangeSelfStatus,
            FinishTask,
            GiveItems,
            CostItems,
            Teleport,
            OpenPanel,


            ActivateEventGroup,
            SpecialMoveTo,

            StartStealth,

        }

        public EOutputType OutputType;
        public long Param1;
        public long Param2;
        public string Param3;
        public string Param4;

        public float DelayTime;
    }

    [Serializable]
    public class InteractCheckCond
    {
        public enum ECheckType
        {
            None,
            NotHide,
            HasLocalSwitch,
            NoLocalSwitch,
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

        public List<CommonCheckCond> CheckCommonCond = new();
        public List<InteractCheckCond> CheckInteractCond = new();
        public List<LogicInteractOutput> Outputs = new();
    }
}

