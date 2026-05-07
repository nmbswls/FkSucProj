

using System.Numerics;

namespace My.Map.Entity
{



    public class MapFightEffectShowEffect : MapFightEffectCfg
    {
        public enum EShowMode
        {
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