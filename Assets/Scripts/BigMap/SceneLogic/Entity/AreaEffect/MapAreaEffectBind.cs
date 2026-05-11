using cfg.demo;
using My.Config;
using My.Map.Fight;

namespace My.Map
{
    // Luban MapAreaEffect 与战斗层 Shape / 阵营枚举的映射
    public static class MapAreaEffectBind
    {
        public static MapAreaEffect GetRow(string cfgId)
        {
            return CfgMgr.Cfgs?.TbMapAreaEffect?.GetOrDefault(cfgId);
        }

        public static FightStruct.Shape ToShape(MapAreaEffect row)
        {
            if (row == null)
            {
                return new FightStruct.Shape();
            }

            return new FightStruct.Shape
            {
                Type = (FightStruct.EShapeType)(int)row.ShapeType,
                Length = row.ShapeLength,
                Width = row.ShapeWidth,
                Radius = row.ShapeRadius,
                Angle = row.ShapeAngle,
            };
        }

        public static ECampFilterType ToCampFilter(MapAreaEffect row)
        {
            if (row == null)
            {
                return ECampFilterType.NotSelf;
            }

            return (ECampFilterType)(int)row.CampFilter;
        }
    }
}
