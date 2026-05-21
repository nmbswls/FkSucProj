


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
            failReason = "home_build_disabled";
            return false;
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
        }

        // 内城 placement 与大地图动态刷新无关：进内城时直接建 Record 并注册（与 AddPlacement 同路径）。
        public void EnsureHomeFacilityRecordsRegistered()
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
        }


        public bool CheckHasPlacement(string id)
        {
            return PlacementInfos.Find((item) => item.Id == id) != null;
        }



        public void AddPlacement(HomeFacilityCfg cfg, Vector3Int pivorPos, EPlacementRotation rot)
        {
        }

        public void MovePlacement(string id, Vector3Int pivorPos, EPlacementRotation rot)
        {
        }

        public List<HomeFacilityCfg> GetAllBuilableItems()
        {
            return new List<HomeFacilityCfg>();
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