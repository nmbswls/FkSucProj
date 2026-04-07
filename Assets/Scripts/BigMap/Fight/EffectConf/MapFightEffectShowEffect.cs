

using System.Numerics;

namespace My.Map.Entity
{



    public class MapFightEffectShowEffect : MapFightEffectCfg
    {
        public enum EShowMode
        {
            Fixed,
            EntityAligned,
        }
        public EShowMode ShowMode;

        public Vector2 ShowPos;
        public Vector2 ShorRotation;
        public string EffectName;
    }

}