


using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Logic;
using UnityEngine;

namespace My.Home
{

    public class HomeDataManager
    {

        [Serializable]
        public class HomePlacementInfo
        {
            public long InstId;
            public string Id;
            public Vector3Int PivotPos;
            public EPlacementRotation Rot;

            public HomePlacementDetailInfo Info;
        }

        [Serializable]
        public class HomePlacementDetailInfo
        { }


        public Dictionary<string, bool> ParamDict = new();

        public List<HomePlacementInfo> PlacementInfos = new();

        public event Action<HomePlacementInfo> EvOnPlacementUpdate;

        public bool CheckHasPlacement(string id)
        {
            return PlacementInfos.Find((item) => item.Id == id) != null;
        }

        public bool CheckHasParam(string id)
        {
            ParamDict.TryGetValue(id, out var val);
            return val;
        }

        public void AddPlacement(string id, Vector3Int pivorPos, EPlacementRotation rot)
        {
            var newInfo = new HomePlacementInfo();
            newInfo.Id = id;
            newInfo.PivotPos = pivorPos;
            newInfo.Rot = rot;
            PlacementInfos.Add(newInfo);
        }

        public void MovePlacement(string id, Vector3Int pivorPos, EPlacementRotation rot)
        {
            var findIt = PlacementInfos.Find(item => item.Id == id);
            if (findIt != null)
            {
                findIt.PivotPos = pivorPos; 
                findIt.Rot = rot;

                EvOnPlacementUpdate?.Invoke(findIt);
            }
        }

        public List<LogicEntityRecord> GetAllValidLogicEntites()
        {
            List<LogicEntityRecord> retList = new();


            // home×´Ì¬ ¶ÁÈ¡ÐÅÏ¢
            {
                var record = new LogicEntityRecord4InteractPoint();
                record.Id = GameLogicManager.LogicEntityIdInst++;
                record.EntityType = EEntityType.InteractPoint;
                record.CfgId = "teleport";
                record.Position = new Vector2(2.0f, 2.0f);

                retList.Add(record);
            }

            {
                var record = new LogicEntityRecord4UnitBase();
                record.Id = GameLogicManager.LogicEntityIdInst++;
                record.EntityType = EEntityType.Npc;
                record.FactionId = Map.Entity.EFactionId.Player;
                record.CfgId = "home_liki";
                record.Position = new Vector2(2.0f, 0f);

                record.IsPeace = true;
                record.MoveBehaveType = BaseUnitLogicEntity.EMoveBehaveType.NoMove;

                retList.Add(record);
            }

            return retList;
        }
    }
}