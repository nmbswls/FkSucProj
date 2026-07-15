using UnityEngine;
using My.Map.Logic;

namespace My.Map
{
    public class HomeFacilityLogicEntity : LogicEntityBase
    {
        public HomeFacilityLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.HomeFacility;
        public string FacilityId => CfgId;
        public long FacilityInstanceId => Id;

        public override void Initialize()
        {
            base.Initialize();
            LogicManager.homeDataManager?.RefreshFixedFacilities();
        }
    }
}