


using My.Map.Logic;
using UnityEngine;

namespace My.Map
{
    public class LogicEntityMpbNpc : LogicEntityBase
    {
        public LogicEntityMpbNpc(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.MobNpc;
    }
}