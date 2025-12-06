
using System;
using System.Collections.Generic;
using UnityEngine;

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
        }

        public EOutputType OutputType;
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
        public bool HideWhenFail = false;

        public List<CommonCheckCond> CheckCond = new();
        public List<LogicInteractOutput> Outputs = new();
    }
}

