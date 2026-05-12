using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    public enum ETrapPostTrigger
    {
        Destroy,
        SleepRecover,
    }

    [CreateAssetMenu(menuName = "Map/TrapSpec", fileName = "TrapSpec")]
    public class TrapSpecConfig : ScriptableObject
    {
        public float TriggerRadius = 1f;

        public ECampFilterType CampFilter = ECampFilterType.NotSelf;

        public bool OnlyPlayer = true;

        public ETrapPostTrigger PostTrigger = ETrapPostTrigger.Destroy;

        public float SleepDuration = 5f;

        [SerializeReference]
        public List<MapFightEffectCfg> TriggerEffects = new();
    }
}
