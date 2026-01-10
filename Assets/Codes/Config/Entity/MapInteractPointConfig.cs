using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using My;
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
        public string ShowName;
        public string PrefabName;

        [Serializable]
        public class StatusInfo
        {
            public int StatusId;

            public List<MapInteractInfo> InteractInfos = new();

            public bool HasBlock = false;
        }

        public StatusInfo MainStatusInfo;
        public List<StatusInfo> ExtraStatusInfos;

        public int InitState;

        [Serializable]
        public class StateChangeView
        {
            public float ChangingDuration = 0;
            public string ChangingAnimName;
            public string ChangingEffect;
        }



        [Serializable]
        public class StateChangeRule
        {
            public int FromStatus;
            public List<CommonCheckCond> CommonConds = new();
            public List<string> NeedSelfFlag = new();
            public int ToStatus;

            public StateChangeView ChangeView;
        }

        /// <summary>
        /// ×´Ì¬ÇÐ»»¹æÔò
        /// </summary>
        public List<StateChangeRule> StateChangeRules = new();
    }
}
