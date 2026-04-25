


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Config;
using Map.Logic.Events;
using My;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Saving;
using UnityEngine;

namespace My.Home
{

    public class HomeDataManager
    {
        public GameLogicManager LogicManager { get; private set; }

        // 内城生态：繁荣度、当前人口（存档于 SaveData.PlayerData）
        public int TownProsperity { get; private set; }
        public int TownCurrentPopulation { get; private set; }

        public event Action EvOnTownEcoChanged;

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

            // LogicEntityRecord4HomeFacility 的 Id，与 BindingFacilityId=InstId 对应
            public long HomeFacilityLogicRecordId;
        }

        public class HomePlacementDetailInfo
        { }


        public List<HomeFacilityInstance> PlacementInfos = new();

        private Dictionary<long, HomeFacilityInstance> homePlacementMap = new();

        // 场景加载后：把 Repo 里 HomeFacility 记录写回 placement，便于移动与调试
        public void SyncPlacementLogicRecordIdsFromRepo()
        {
            if (LogicManager?.AreaManager?.Repo?.Records == null)
            {
                return;
            }

            foreach (var p in PlacementInfos)
            {
                if (p.Removed)
                {
                    continue;
                }

                foreach (var kv in LogicManager.AreaManager.Repo.Records)
                {
                    if (kv.Value is LogicEntityRecord4HomeFacility hf && hf.BindingFacilityId == p.InstId)
                    {
                        p.HomeFacilityLogicRecordId = kv.Key;
                        p.ArrangePeopleNum = hf.ArrangePeopleNum;
                        break;
                    }
                }
            }
        }

        private void ApplyPlacementWorldPosToLogicRecord(HomeFacilityInstance inst, Vector2 worldPos)
        {
            long rid = inst.HomeFacilityLogicRecordId;
            if (rid == 0 || LogicManager?.AreaManager?.Repo?.Records == null)
            {
                return;
            }

            if (!LogicManager.AreaManager.Repo.Records.TryGetValue(rid, out var rec))
            {
                return;
            }

            rec.Position = worldPos;
            LogicManager.AreaManager.UpdatePosition(rid, worldPos);
            var ent = LogicManager.AreaManager.GetLogicEntiy(rid, false) as LogicEntityBase;
            ent?.SetPosition(worldPos);
        }

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
            TownProsperity = saveData?.PlayerData?.HomeProsperity ?? 0;
            TownCurrentPopulation = saveData?.PlayerData?.HomeCurrentPopulation ?? 0;

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

        public void ApplyToSaveData(SaveData data)
        {
            if (data?.PlayerData == null)
            {
                return;
            }

            data.PlayerData.HomeProsperity = Mathf.Max(0, TownProsperity);
            data.PlayerData.HomeCurrentPopulation = Mathf.Max(0, TownCurrentPopulation);
        }

        public void SetTownProsperity(int value)
        {
            value = Mathf.Max(0, value);
            if (TownProsperity == value)
            {
                return;
            }

            TownProsperity = value;
            EvOnTownEcoChanged?.Invoke();
        }

        public void SetTownCurrentPopulation(int value)
        {
            value = Mathf.Max(0, value);
            if (TownCurrentPopulation == value)
            {
                return;
            }

            TownCurrentPopulation = value;
            EvOnTownEcoChanged?.Invoke();
        }

        public int ComputeAssignedWorkforceTotal()
        {
            int sum = 0;
            foreach (var p in PlacementInfos)
            {
                if (p.Removed)
                {
                    continue;
                }

                sum += Mathf.Max(0, p.ArrangePeopleNum);
            }

            return sum;
        }

        public bool TrySetPlacementWorkforce(long placementInstId, int workers, out string failReason)
        {
            failReason = null;
            var p = FindPlacementById(placementInstId);
            if (p == null || p.Removed || p.CfgRef == null)
            {
                failReason = "Invalid placement";
                return false;
            }

            if (!p.CfgRef.SupportsWorkforceAssignment)
            {
                failReason = "Not a workplace facility";
                return false;
            }

            int max = Mathf.Max(0, p.CfgRef.MaxWorkforce);
            workers = Mathf.Clamp(workers, 0, max);
            p.ArrangePeopleNum = workers;

            if (p.HomeFacilityLogicRecordId != 0 &&
                LogicManager?.AreaManager?.Repo?.Records.TryGetValue(p.HomeFacilityLogicRecordId, out var rec) == true &&
                rec is LogicEntityRecord4HomeFacility hf)
            {
                hf.ArrangePeopleNum = workers;
            }

            EvOnPlacementUpdate?.Invoke(p);
            EvOnTownEcoChanged?.Invoke();

            TryRefreshFacilityPresenterWorkforce(p.HomeFacilityLogicRecordId);
            return true;
        }

        private static void TryRefreshFacilityPresenterWorkforce(long logicEntityId)
        {
            if (logicEntityId == 0 || MainGameManager.Instance == null)
            {
                return;
            }

            var pres = SceneAOIManager.Instance != null
                ? SceneAOIManager.Instance.GetActivePresentation(logicEntityId)
                : null;
            if (pres is HomeFacilityPresenter hfp)
            {
                hfp.RefreshWorkforceVisuals();
            }
        }

        public void OnPlayerEnterHome()
        {
            EnsureHomeFacilityRecordsRegistered();
        }

        // 内城 placement 与大地图动态刷新无关：进内城时直接建 Record 并注册（与 AddPlacement 同路径）。
        public void EnsureHomeFacilityRecordsRegistered()
        {
            if (LogicManager?.AreaManager == null)
            {
                return;
            }

            var area = LogicManager.AreaManager;

            foreach (var p in PlacementInfos)
            {
                if (p.Removed || p.CfgRef == null)
                {
                    continue;
                }

                long existingId = 0;
                foreach (var kv in area.Repo.Records)
                {
                    if (kv.Value is LogicEntityRecord4HomeFacility hf && hf.BindingFacilityId == p.InstId)
                    {
                        existingId = kv.Key;
                        break;
                    }
                }

                if (existingId != 0)
                {
                    p.HomeFacilityLogicRecordId = existingId;
                    if (area.Repo.Records.TryGetValue(existingId, out var existRec) &&
                        existRec is LogicEntityRecord4HomeFacility hfExist)
                    {
                        p.ArrangePeopleNum = hfExist.ArrangePeopleNum;
                    }

                    continue;
                }

                var initInfo = new EntityInitInfo4HomePlacement
                {
                    CfgId = p.Id,
                    Position = (Vector2)CellToWorld(p.PivotPos),
                    BindingFacilityId = p.InstId,
                };

                var record = area.CreateEntityRecordFromInitInfo(initInfo);
                if (record == null)
                {
                    continue;
                }

                p.HomeFacilityLogicRecordId = record.Id;
                area.RegisterEntityRecord(record, isCreate: false);
            }
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
            newInfo.HomeFacilityLogicRecordId = record.Id;

            if (record is LogicEntityRecord4HomeFacility hfNew)
            {
                hfNew.ArrangePeopleNum = newInfo.ArrangePeopleNum;
            }

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

                var worldPos = (Vector2)CellToWorld(pivorPos);
                if (findIt.HomeFacilityLogicRecordId == 0)
                {
                    SyncPlacementLogicRecordIdsFromRepo();
                }

                if (findIt.HomeFacilityLogicRecordId == 0 && LogicManager?.AreaManager?.Repo?.Records != null)
                {
                    foreach (var kv in LogicManager.AreaManager.Repo.Records)
                    {
                        if (kv.Value is LogicEntityRecord4HomeFacility hf && hf.BindingFacilityId == findIt.InstId)
                        {
                            findIt.HomeFacilityLogicRecordId = kv.Key;
                            break;
                        }
                    }
                }

                ApplyPlacementWorldPosToLogicRecord(findIt, worldPos);

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