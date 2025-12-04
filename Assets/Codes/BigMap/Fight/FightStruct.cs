
using System;

namespace My.Map.Fight
{
    public class FightStruct
    {
        public enum EShapeType
        {
            None,
            Square,
            Circle,
            Sector,
        }

        [Serializable]
        public class Shape
        {
            public EShapeType Type = EShapeType.None;
            public float Length;
            public float Width;
            public float Radius;
            public float Angle;
        }

        public enum ESelectPolicy
        {
            None,
            PrimaryTarget,
            Self,
            LowHpAlly,
            LowHpEnmity,
            Random,
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum EDmgFlag
        {
            None,
            ZiWei = 0x08,
            Xixue = 1 << 4,
        }
    }
    
}