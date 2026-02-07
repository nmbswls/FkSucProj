


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Config;
using Map.Logic.Events;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Saving;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static My.MapExport.MapExportDatabase;

namespace My.Home
{

    public class HomeDataManager
    {
        public GameLogicManager LogicManager { get; private set; }

        public long HomePlacementIdCounter = 100;

        public float GridSize = 1;
        /// <summary>
        /// 
        /// </summary>
        public class HomeFacilityInstance
        {
            public long InstId;
            public string Id;
            public Vector3Int PivotPos;
            public EPlacementRotation Rot;

            public HomePlacementDetailInfo Info;

            public Dictionary<int, long> BindingRecordMap = new();

            public int ArrangePeopleNum;

            public bool Removed = false;

            public HomeFacilityCfg CfgRef;
        }

        public class HomePlacementDetailInfo
        { }


        public List<HomeFacilityInstance> PlacementInfos = new();

        private Dictionary<long, HomeFacilityInstance> homePlacementMap = new();

        /// <summary>
        /// 查找设施
        /// </summary>
        /// <param name="placementId"></param>
        /// <returns></returns>
        public HomeFacilityInstance FindPlacementById(long placementId)
        {
            homePlacementMap.TryGetValue(placementId, out var placement);
            return placement;
        }

        /// <summary>
        /// 已完成修复的facility列表
        /// 以唯一id存储 
        /// </summary>
        public List<string> RepairedFacilityList = new();

        public event Action<HomeFacilityInstance> EvOnPlacementUpdate;

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

        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / GridSize);
            int y = Mathf.FloorToInt(worldPos.y / GridSize);

            return new Vector3Int(x, y, 0);
        }

        public Vector3 CellToWorld(Vector3Int cellPos)
        {
            return new Vector3(cellPos.x * GridSize, cellPos.y * GridSize, 0);
        }

        /// <summary>
        /// 修复 直接不对了
        /// </summary>
        /// <param name="facilityId"></param>
        /// <param name="repairPos"></param>
        public void DoRepairFacility(string facilityId, Vector2 repairPos)
        {
            if(RepairedFacilityList.Contains(facilityId))
            {
                return;
            }

            RepairedFacilityList.Add(facilityId);

            var facilityCfg = MapFixFacilityCfgLoader.Get(facilityId);
            if(facilityCfg == null)
            {
                return;
            }

            var placement = facilityCfg.PlacementId;

            //var placementCfg = MapHomePlacementEntityCfgtLoader.Get(placement);
            //if (placementCfg == null)
            //{
            //    Debug.LogError($"DoRepairFacility PlacementEntity cfg not found {placement}" );
            //    return;
            //}

            var pCfg = HomeFacilityCfgtLoader.Get(placement);

            var buildPos = repairPos;
            var cellPos = WorldToCell(buildPos);
            AddPlacement(pCfg, cellPos, EPlacementRotation.R0);
        }


        public bool CheckHasPlacement(string id)
        {
            return PlacementInfos.Find((item) => item.Id == id) != null;
        }



        public void AddPlacement(HomeFacilityCfg cfg, Vector3Int pivorPos, EPlacementRotation rot)
        {
            var newInfo = new HomeFacilityInstance();
            newInfo.Id = cfg.CfgId;
            newInfo.PivotPos = pivorPos;
            newInfo.Rot = rot;
            newInfo.InstId = HomePlacementIdCounter++;
            newInfo.CfgRef = cfg;
            PlacementInfos.Add(newInfo);

            homePlacementMap[newInfo.InstId] = newInfo;

            Vector2 recordPos = CellToWorld(pivorPos);

            var initInfo = new EntityInitInfo4HomePlacement();
            initInfo.CfgId = cfg.CfgId;
            initInfo.Position = recordPos;
            initInfo.BindingFacilityId = newInfo.InstId;

            var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(initInfo);


            //if(placementCfg.BindingEntityInfoList.Count > 0)
            //{
            //    foreach(var oneEntity in placementCfg.BindingEntityInfoList)
            //    {
            //        int memberId = oneEntity.MemberId;

            //        var record = LogicManager.AreaManager.CreateEntityRecordFromInitInfo(oneEntity.InitInfo);
            //        if(record == null)
            //        {
            //            Debug.LogError("AddPlacement create entity fail.");
            //            continue;
            //        }

            //        newInfo.BindingRecordMap[memberId] = record.Id;

            //        LogicManager.AddNewEntityRecord(record);
            //    }
            //}

            //var record = new LogicEntityRecord()
            //{
            //    Id = GameLogicManager.LogicEntityIdInst++,
            //    EntityType = EEntityType.HomePlacement,
            //    CfgId = id,
            //    Position = new Vector2(pivorPos.x * 1f, pivorPos.y * 1.0f),
            //};

            LogicManager.AddNewEntityRecord(record, isCreate: true);

            //Placement2EntityMap[newInfo.InstId] = record.Id;

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



        public List<HomeFacilityCfg> GetAllBuilableItems()
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

            List<HomeFacilityCfg> ret = new();

            foreach (var name in names)
            {
                var conf = HomeFacilityCfgtLoader.Get(name);
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