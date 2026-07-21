using My;
using My.Config;
using My.Farm;
using My.Map.Logic;
using UnityEngine;

namespace My.Map.Entity
{
    // 种子篮逻辑实体：只做交互入口与逻辑地图绑定；库存在 FarmSystem
    public class SeedBasketLogicEntity : LogicEntityBase
    {
        public string LogicAreaId { get; private set; }

        public SeedBasketLogicEntity(
            GameLogicManager logicManager,
            long instId,
            string cfgId,
            Vector2 orgPos,
            LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var rec = bindingRecord as LogicEntityRecord4SeedBasket;
            SrcUniqName = rec != null ? rec.SrcUniqName : string.Empty;
            LogicAreaId = !string.IsNullOrEmpty(rec?.LogicAreaId)
                ? rec.LogicAreaId
                : ResolveLogicAreaId(logicManager);
        }

        public override EEntityType Type => EEntityType.SeedBasket;

        static string ResolveLogicAreaId(GameLogicManager glm)
        {
            var id = TownFacilityUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);
            return string.IsNullOrEmpty(id) ? FarmCatalog.DefaultLogicAreaId : id;
        }
    }
}
