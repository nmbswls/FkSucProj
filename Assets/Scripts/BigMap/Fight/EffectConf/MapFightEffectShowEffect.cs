


using UnityEngine;

namespace My.Map.Entity
{

    public class MapFightEffectShowEffect : MapFightEffectCfg
    {
        public enum EShowMode
        {
            TriggerPos,
            Fixed,
            TargetAligned,
            CasterAligned,
        }
        public EShowMode ShowMode;

        public Vector2 ShowPos;
        public Vector2 ShorRotation;
        public string EffectName;

        public bool IsFake;
    }

}