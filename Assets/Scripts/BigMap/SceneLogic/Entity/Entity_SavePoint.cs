using My.Map;
using UnityEngine;

namespace My.Map.Entity
{
    public class LogicEntitySavePoint : LogicEntityBase
    {
        public LogicEntitySavePoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.SavePoint;
    }
}
