using Config.Unit;
using Config;
using UnityEngine;
using Config.Map;
using System.Collections.Generic;
using System;
using static UnityEditor.Progress;
using My.Player.Bag;
using Map.Logic.Events;
using My.Map.Logic;
using static My.Home.HomeDataManager;


namespace My.Map
{
    public class HomeFacilityLogicEntity : LogicEntityBase
    {

        public HomeFacilityLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var realRecord = (LogicEntityRecord4HomeFacility)bindingRecord;
            HomePlacementId = realRecord.BindingFacilityId;
        }


        public override EEntityType Type => EEntityType.HomeFacility;

        public override void Initialize()
        {
            base.Initialize();

            InnerFacilityRef = LogicManager.homeDataManager.FindPlacementById(HomePlacementId);
            if (BindingRecord is LogicEntityRecord4HomeFacility hf && InnerFacilityRef != null)
            {
                InnerFacilityRef.ArrangePeopleNum = hf.ArrangePeopleNum;
            }

        }

        public override void SyncRecordForPersistence()
        {
            base.SyncRecordForPersistence();
            if (BindingRecord is LogicEntityRecord4HomeFacility hf && InnerFacilityRef != null)
            {
                hf.ArrangePeopleNum = InnerFacilityRef.ArrangePeopleNum;
            }
        }
        public long HomePlacementId;
        public HomeFacilityInstance InnerFacilityRef;

    }
     
}

