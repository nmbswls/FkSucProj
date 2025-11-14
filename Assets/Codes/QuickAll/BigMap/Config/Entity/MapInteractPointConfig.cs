using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
        public class LogicInteractOutput
        {
            public enum EOutputType
            {
                Invalid,
                ChangeSelfStatus,
                FinishTask,
                GiveItems,
                CostItems,
            }

            public EOutputType OutputType;
            public int Param1;
            public int Param2;
            public long Param3;
            public long Param4;
        }

        [Serializable]
        public class InteractCheckCond
        {
            public enum EInteractCheckConfType
            {


            }

            public EInteractCheckConfType CheckConfType;

        }

        [Serializable]
        public class InteractStatusInfo
        {
            public int StatusId;

            public string InteractName;
            public InteractCheckCond CheckCond;
            public List<LogicInteractOutput> Outputs = new();
            public bool HasBlock = false;
        }

        public InteractStatusInfo MainStatusInfo;
        public List<InteractStatusInfo> ExtraStatusInfos;

    }
}
