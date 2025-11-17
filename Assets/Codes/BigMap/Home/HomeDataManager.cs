


using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Logic;
using UnityEngine;
using static My.MapExport.MapExportDatabase;

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


        public Dictionary<string, bool> VariableDict = new();

        public List<HomePlacementInfo> PlacementInfos = new();

        public event Action<HomePlacementInfo> EvOnPlacementUpdate;


        public void SetVariable(string id)
        {
            VariableDict[id] = true;

            // 变量事件

        }

        public bool CheckHasPlacement(string id)
        {
            return PlacementInfos.Find((item) => item.Id == id) != null;
        }

        public bool CheckHasParam(string id)
        {
            VariableDict.TryGetValue(id, out var val);
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

        public List<DynamicEntityRefreshInfo> GetAllValidLogicEntites()
        {
            List<DynamicEntityRefreshInfo> retList = new();

            int uniqId = 10;
            // home状态 读取信息
            {
                var refreshInfo = new DynamicEntityRefreshInfo();
                refreshInfo.UniqId = uniqId++;
                refreshInfo.EntityType = EEntityType.InteractPoint;
                refreshInfo.CfgId = "teleport";
                refreshInfo.Position = new Vector2(2.0f, 2.0f);


                retList.Add(refreshInfo);
            }

            {

                var refreshInfo = new DynamicEntityRefreshInfo();
                refreshInfo.UniqId = uniqId++;
                refreshInfo.EntityType = EEntityType.Npc;
                refreshInfo.CfgId = "home_liki";
                refreshInfo.Position = new Vector2(2.0f, 0f);

                refreshInfo.AppearCond = new CommonCheckCond()
                {
                    Type = ECommonCheckType.HasVariable,
                    Param5 = "liki",
                };

                var initInfo = new DynamicEntityInitInfo4Unit();
                refreshInfo.InitInfo = initInfo;

                initInfo.IsPeace = true;
                initInfo.MoveMode = BaseUnitLogicEntity.EMoveBehaveType.NoMove;
                

                retList.Add(refreshInfo);
            }

            return retList;
        }
    }
}