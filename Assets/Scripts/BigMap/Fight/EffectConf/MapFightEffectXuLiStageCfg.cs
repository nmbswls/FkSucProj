using Map.Entity;
using My.Map.Fight;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class MapFightEffectXuLiStageCfg : MapFightEffectCfg
    {
        [Serializable]
        public class EStageInfo
        {
            public float NeedTime;

            public List<MapFightEffectCfg> StageEffects = new();
        }

        public string CheckPhaseName;

        public List<EStageInfo> StageInfos = new();
    }

}


