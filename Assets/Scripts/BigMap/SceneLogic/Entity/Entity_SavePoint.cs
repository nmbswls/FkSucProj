using My.Map;
using UnityEngine;

namespace My.Map.Entity
{
    // 由 DynamicEntityRefreshInfo + EntityInitInfo4SavePoint 驱动生成；不入地图 EntityRecords 持久化，仅依赖 StaticId 与出现/消失条件刷新。
    public class LogicEntitySavePoint : LogicEntityBase
    {
        public LogicEntitySavePoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.SavePoint;

        protected override void LoadCfg()
        {
        }

        public override void Tick(float dt)
        {
        }
    }
}
