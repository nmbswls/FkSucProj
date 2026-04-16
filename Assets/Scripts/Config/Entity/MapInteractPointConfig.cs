using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using cfg.demo;
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

        public float NameOffset = -1f;

        public bool IsAlwaysActive = false;

        [Serializable]
        public class StatusInfo
        {
            public int StatusId;
            public string Desp;
            public List<MapInteractInfo> InteractInfos = new();

            public bool HasBlock = false;
            public bool AutoTriggerCollide = false;

            public bool AutoTrigger = false;
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
        /// 状态切换规则
        /// </summary>
        public List<StateChangeRule> StateChangeRules = new();
    }
}
