
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
    }
    
}