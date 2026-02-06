


using System;
using System.Collections.Generic;
using Config;
using Map.Logic.Events;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Saving;
using UnityEngine;
using static My.MapExport.MapExportDatabase;

namespace My.Home
{

    public class HomeDataManager
    {
        public GameLogicManager LogicManager { get; private set; }

        public long HomePlacementIdCounter = 100;

        public class HomePlacementInfo
        {
            public long InstId;
            public string Id;
            public Vector3Int PivotPos;
            public EPlacementRotation Rot;

            public HomePlacementDetailInfo Info;

            public Dictionary<int, long> BindingRecordMap = new();

            public int ArrangePeopleNum;
        }

        public class HomePlacementDetailInfo
        { }


        public List<HomePlacementInfo> PlacementInfos = new();

        /// <summary>
        /// 已完成修复的facility列表
        /// 以唯一id存储 
        /// </summary>
        public List<string> RepairedFacilityList = new();

        public event Action<HomePlacementInfo> EvOnPlacementUpdate;

        public Dictionary<long, long> Placement2EntityMap = new();

        public HomeDataManager(GameLogicManager logicManager)
        {
            this.LogicManager = logicManager;
        }

        public int DailyNormalHTimes = 0;

        private Dictionary<string, long> basicProduceOutput;
        private List<string> extraProduceEvents;

        /// <summary>
        /// 从存档中加载
        /// </summary>
        /// <param name="saveData"></param>
        public void LoadHomeData(SaveData saveData)
        {
            //// 初始化placement
            //foreach (var one in PlacementInfos)
            //{
            //    // 创建
            //    HomePlaceableObject cfg = HomePlacementCfgtLoader.Get(one.Id);

            //    if(cfg.IsFixed)
            //    {
            //        fixFacilityMap[cfg.id] = one;
            //    }

            //    foreach (var bindingOne in cfg.BindingEntityInfoList)
            //    {
            //        var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(bindingOne.InitInfo);

            //        LogicManager.AddNewEntityRecord(record);

            //        one.BindingRecordMap[bindingOne.MemberId] = record.Id;
            //    }
            //}

            //List<string> fixedFacilityIds= new() { "lab", "teleporter" };
            //List<Vector3Int> fixedFacilityPos = new();
            //foreach (var facilityId in fixedFacilityList)
            //{
            //    if (fixFacilityMap.ContainsKey(facilityId))
            //    {
            //        continue;
            //    }

            //    var newPlacement = new HomePlacementInfo()
            //    {
            //        Id = facilityId,
            //        PivotPos =
            //    };

            //}
        }

        public void OnPlayerEnterHome()
        {

        }


        public bool CheckHasPlacement(string id)
        {
            return PlacementInfos.Find((item) => item.Id == id) != null;
        }



        public void AddPlacement(string id, Vector3Int pivorPos, EPlacementRotation rot)
        {
            var newInfo = new HomePlacementInfo();
            newInfo.Id = id;
            newInfo.PivotPos = pivorPos;
            newInfo.Rot = rot;
            newInfo.InstId = HomePlacementIdCounter++;


            PlacementInfos.Add(newInfo);

            var record = new LogicEntityRecord()
            {
                Id = GameLogicManager.LogicEntityIdInst++,
                EntityType = EEntityType.HomePlacement,
                CfgId = id,
                Position = new Vector2(pivorPos.x * 1f, pivorPos.y * 1.0f),
            };

            Vector2 faceDir = Vector2.right;
            switch (rot)
            {
                case EPlacementRotation.R90:
                    {
                        faceDir = new Vector2(0, 1);
                    }
                    break;
                case EPlacementRotation.R180:
                    {
                        faceDir = new Vector2(-1, 0);
                    }
                    break;
                case EPlacementRotation.R270:
                    {
                        faceDir = new Vector2(0, -1);
                    }
                    break;
            }

            record.FaceDir = faceDir;

            LogicManager.AddNewEntityRecord(record);

            Placement2EntityMap[newInfo.InstId] = record.Id;

            EvOnPlacementUpdate?.Invoke(newInfo);
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



        public List<HomePlaceableObject> GetAllBuilableItems()
        {
            List<string> names = new List<string>()
            {
                "small_01",
                "small_02",
                "small_03",
                "middle_01",
                "middle_02",
                "big_01",
                "big_02",
                "big_03",
            };

            List<HomePlaceableObject> ret = new();

            foreach (var name in names)
            {
                var conf = HomePlacementCfgtLoader.Get(name);
                ret.Add(conf);
            }

            return ret;
        }

        public List<DynamicEntityRefreshInfo> GetAllValidLogicEntites()
        {
            List<DynamicEntityRefreshInfo> retList = new();

            int uniqId = 10;
            //// home状态 读取信息
            //{
            //    var refreshInfo = new DynamicEntityRefreshInfo();
            //    refreshInfo.UniqId = uniqId++;
            //    refreshInfo.EntityType = EEntityType.InteractPoint;
            //    refreshInfo.CfgId = "teleport";
            //    refreshInfo.Position = new Vector2(2.0f, 2.0f);


            //    retList.Add(refreshInfo);
            //}

            {

                //var refreshInfo = new DynamicEntityRefreshInfo();
                //refreshInfo.UniqId = uniqId++;
                //refreshInfo.InitInfo = new EntityInitInfo4Npc()
                //{
                //    CfgId = "home_liki",
                //    Position = new Vector2(2.0f, 0f),
                //    IsPeace = true,
                //    MoveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove
                //};

                //refreshInfo.AppearCond = new CommonCheckCond()
                //{
                //    Type = ECommonCheckType.CheckVariable,
                //    Param1 = 1,
                //    Param5 = "liki",
                //};

                //retList.Add(refreshInfo);
            }

            return retList;
        }

        public void RefreshProduceValue()
        {
            basicProduceOutput["gold"] = 10; 
        }

        public void DoDayEndBalance()
        {
            DailyNormalHTimes = 0;

            // 进行基础生产
            // 找到建筑物 放入
        }
    }
}