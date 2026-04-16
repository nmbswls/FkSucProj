using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using My.Map.Entity;
using My.Map.Fight;
using TMPro;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Config/Entity/MapAreaEffect")]
    [Serializable]
    public  class MapAreaEffectConfig : ScriptableObject
    {
        public string CfgId;

        public FightStruct.Shape ShapeInfo = new();
        public string AreaBuffId;
        public ECampFilterType CampFilterType = ECampFilterType.NotSelf;

        public float DefaultLifeTime = -1; // 正值表示有时间

    }
}
