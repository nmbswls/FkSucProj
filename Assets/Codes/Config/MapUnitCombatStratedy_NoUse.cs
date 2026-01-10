using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Config.Unit
{
    [CreateAssetMenu(menuName = "GP/UnitAI/StrategyTemplate")]
    [Serializable]
    public class MapUnitStrategyTemplate : ScriptableObject
    {
        [Serializable]
        public class OneStrategyInfo
        {
            public string Name;
            public int Param1;
            public int Param2;
            public int Param3;
            public int Param4;
            public long Param5;
            public long Param6;
            public string StrParams;
        }

        [TextArea]
        public string Descption;

        [SerializeField]
        public List<OneStrategyInfo> StrategyInfos;
    }
}
