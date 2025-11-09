using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using TMPro;
using Unit.Ability.Effect;
using UnityEngine;

namespace Config.Map
{

    [CreateAssetMenu(menuName = "GP/Unit/MapAreaEffect")]
    [Serializable]
    public  class MapAreaEffectConfig : ScriptableObject
    {
        public string CfgId;

        public bool HastriggerArea;
        public enum EShape
        {
            None,
            Square,
            Circle,
        }

        public EShape Shape = EShape.None;
        public float Length;
        public float Radius;
        [SerializeReference]
        public List<MapFightEffectCfg> TriggerEffects;

        public string BindingBuffId;

        public float DefaultLifeTime = -1; // 正值表示有时间

    }
}
