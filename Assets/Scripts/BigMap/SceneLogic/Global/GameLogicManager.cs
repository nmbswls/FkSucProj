
using Map.Logic.Events;
using My.Config;
using My.Home;
using My.Map;
using My.Map.Drop;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Player;
using My.Saving;
using My.UI;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using cfg.demo;
using Unity.Profiling;
using UnityEngine;
using static MapSceneEffectManager;
using static My.Map.Fight.FightStruct;
using static My.MapExport.MapExportDatabase;

namespace My
{
    public static class GameConsts
    {
        public static int ChunkCellSize = 32;
    }


    public interface ILogicEntityFactory
    {
        // 根据Record创建运行时实例
        LogicEntityBase CreateEntityByRecord(LogicEntityRecord record);
        // 可选对象池：回收实例
        void RecycleEntity(ILogicEntity entity);
    }

    



    public partial class GameLogicManager : ILogicEntityFactory
    {
        public static long LogicEntityIdInst = 100;
        public static long ItemInstanceIdCounter = 100;


        public enum EMainGameStage
        {
            UnInitialized,
            Initialized,
            Running,
            SwitchingMap,
            Balance,
        }

        public EMainGameStage MainStage = EMainGameStage.UnInitialized;

        public bool NeedBalancing { get; set; }
        public bool IsBalancing { get; set; }
        public bool IsDialogPlayering { get; set; }



        public PlayerLogicEntity playerLogicEntity { get; set; }

        private List<(LogicEntityRecord, bool)> pendingNewEntities = new();

        public event Action<ILogicEntity> EventOnLogicEntitySpawned;
        public event Action<ILogicEntity> EventOnLogicEntityDespawned;

        // 即将丢弃当前区域/切图前触发：供表现层中断本地传送渐隐协程并释放传送锁，避免协程被 Stop 后锁泄漏
        public event Action EventOnHardAreaClearStarting;

        

        public ISceneAbilityViewer? viewer; // 表现层接口
        public IVisionSenser2D? visionSenser;
        public INavProvider? navProvider;


        public int TimePeriod;

        /// <summary>
        /// 世界结算日；推进时触发垂钓点按配置补满等。
        /// </summary>
        public int SettlementDayIndex { get; private set; }

        /// <summary>
        /// 结算日 +1，并按 Luban 垂钓配置为各点补鱼次数。
        /// </summary>
        public void AdvanceSettlementDayAndApplyFishingRules()
        {
            SettlementDayIndex++;
            worldPersistState?.ApplyFishingRestockForSettlement(SettlementDayIndex);
            playerDataManager?.RumorIntel?.PruneExpiredRumors(SettlementDayIndex);
        }


        /// <summary>
        /// 
        /// </summary>
        public GlobalBuffManager globalBuffManager;
        public GlobalThrowManager globalThrowManager;
        public GlobalMapDropCollection globalDropCollection;

        public MapLogicEventBus LogicEventBus;

        public string CurrentArea = string.Empty;
        public MapExportDatabase cacheMapDb;

        public GameLogicAreaManager AreaManager;
        public RumorIntelMapSpawn RumorIntelSpawn;
        public MapMicroPlotManager MapMicroPlot;
        public AreaWantedManager WantedManager;
        public WantedDynamicGuardController WantedGuardSpawner;

        public PlayerSystemManager playerDataManager;
        public GameWorldPersistStateManager worldPersistState;
        public HomeDataManager homeDataManager;
        public ShopDataManager shopDataManager;


        public MapControlEventManager controlEventManager;
        public FactionRelationManager factionRelationManager;

        public bool PlayerPeaceMode { get; set; } = false;

        // true = 人类形态；false = 真身形态（仅真身下维持衣装/暴露等玩法）
        public bool PlayerHumanMode { get; private set; } = true;

        // 玩家在家园地图内主动切换人类/真身形态（非家园返回 false）
        public bool TrySetPlayerHumanMode(bool wantHuman)
        {
            if (AreaManager?.cacheMapCfg == null || !AreaManager.cacheMapCfg.IsHome)
            {
                return false;
            }

            ForcePlayerHumanMode(wantHuman, refreshDespitePendingSwitch: true);
            return true;
        }

        // 系统或界面强制形态（床铺潜入、结算回城等）；地图切换进行中时默认推迟到 PostNewAreaLoaded 再刷新运行时
        public void ForcePlayerHumanMode(bool human, bool refreshDespitePendingSwitch = false)
        {
            if (PlayerHumanMode == human)
            {
                return;
            }

            PlayerHumanMode = human;
            if (SwitchAreaIntent != null && !refreshDespitePendingSwitch)
            {
                return;
            }

            RefreshPlayerMagicClothesAndExposeForCurrentMode();
        }

        // 背包内快捷道具栏是否允许拖动编辑：人类模式，或真身且未暴露且未发情。（不限制背包/商店等面板是否打开。）
        public bool CanEditQuickSlotBar()
        {
            if (PlayerHumanMode)
            {
                return true;
            }

            if (playerLogicEntity == null)
            {
                return false;
            }

            return !playerLogicEntity.IsExposed && !playerLogicEntity.IsFaQing;
        }

        public void OnGameLogicInit(SaveData saveData)
        {
            LogicEventBus = new();
            AreaManager = new(this, new GameLogicAreaManager.Settings()
            {

            });
            RumorIntelSpawn = new RumorIntelMapSpawn(this);
            MapMicroPlot = new MapMicroPlotManager(this);

            worldPersistState = new GameWorldPersistStateManager();
            if (saveData != null)
            {
                SaveData.EnsureHydrated(saveData);
            }

            worldPersistState.InitFromSave(saveData);

            playerDataManager = new(this);
            playerDataManager.InitPlayerData(saveData);

            UnitAttrSystemUnits.InitGameAttrs();


            globalBuffManager = new(this);
            globalBuffManager.InitEventListening();

            globalThrowManager = new(this);

            globalDropCollection = new(this);

            projectileHolder = new();

            

            shopDataManager = new(this);

            homeDataManager = new(this);
            homeDataManager.LoadHomeData(saveData);
            homeDataManager.EvOnPlacementUpdate += (placementInfo) =>
            {
                

                //AreaManager.EntityRefreshInfo.Add(refreshInfo);
            };

            factionRelationManager = new();

            controlEventManager = new(this);
            controlEventManager.Initialize();

            WantedManager = new();
            WantedGuardSpawner = new WantedDynamicGuardController(this);
            EventOnHardAreaClearStarting += OnWantedGuardAreaClear;

            DropUtils.InitializeDropGroups();

            

            playerDataManager.InventorySystem.EventOnGainItem += (bagId, itemId, count) =>
            {
                if(UIGainSideNotifyPanel.Instance != null)
                {
                    UIGainSideNotifyPanel.Instance.EnqueueLog("gain " + itemId, null);
                }
            };

            CapturePersistenceFromSaveData(saveData);

            MainStage = EMainGameStage.Initialized;
        }


        public ILogicEntity GetLogicEntity(long instId, bool ensureExist = true)
        {
            return AreaManager.GetLogicEntiy(instId, ensureExist);
        }

        /// <summary>
        /// 玩家进入/切换场景
        /// </summary>
        /// <param name="areaName"></param>
        public void PreparePlayerSwitchArea(string mapName, bool reset, string? targetPoint = null, Vector2? targetPos = null, bool silent = false)
        {
            if(SwitchAreaIntent != null)
            {
                Debug.LogError("PlayerSwitchArea when have pending switch intent.");
                return;
            }

            TrySnapshotOpenWorldBeforeEnteringHome(mapName);

            var intent = new SwitchAreaIntent();
            intent.AreaName = mapName;
            intent.OldAreaName = AreaManager.MapName;
            intent.Reset = reset;
            intent.Silent = silent;
            intent.TargetPos = targetPos;

            intent.SavedRecord = new()
            {
                Id = 1,
                EntityType = EEntityType.Player,
                CfgId = "0",
                FactionId = EFactionId.Player,
            };
            intent.TargetPoint = targetPoint;

            SwitchAreaIntent = intent;

            EventOnHardAreaClearStarting?.Invoke();

            DelayedEffectQueue.Clear();

            AreaManager.CleanArea();
            globalBuffManager.Clear();
            globalDropCollection.Clear();
        }


        public void Tick(float dt)
        {
            if (MainStage == EMainGameStage.SwitchingMap)
            {
                TickStageSwitching();
                return;
            }

            if (MainStage == EMainGameStage.Balance)
            {
                TickStageBalancing();
                return;
            }

            if(SwitchAreaIntent != null)
            {
                StartMapSwitchingFlow();
                return;
            }

            if (NeedBalancing)
            {
                NeedBalancing = false;
                DelayedEffectQueue.Clear();

                EventOnHardAreaClearStarting?.Invoke();

                AreaManager.CleanArea();
                globalBuffManager.Clear();
                globalDropCollection.Clear();

                BigMapFinishPanel.Create();

                MainStage = EMainGameStage.Balance;
                return;
            }

            globalThrowManager.Tick(dt);
            playerDataManager.Tick(dt);

            foreach (var entity in AreaManager.Repo.Loaded.Values)
            {
                entity.Tick(dt);
                if (entity.MarkDestroyed)
                {
                }
            }

            if (pendingNewEntities.Count > 0)
            {
                foreach (var entityRecord in pendingNewEntities)
                {
                    AreaManager.RegisterEntityRecord(entityRecord.Item1, entityRecord.Item2);
                }
                pendingNewEntities.Clear();
            }


            AreaManager.Tick(dt);

            MapMicroPlot?.Tick(dt);

            globalDropCollection?.Tick(dt);

            TickPendingDelayedEffect();

            TickPeaceMode();

            // 帧末再处理buff
            globalBuffManager.Tick(dt);

            WantedManager?.Tick(dt);
            WantedGuardSpawner?.Tick(dt);
        }


        void OnWantedGuardAreaClear()
        {
            WantedGuardSpawner?.ClearAll();
        }

        private void TickStageSwitching()
        {

        }
        private void TickStageBalancing()
        {

        }
        public void AddNewEntityRecord(LogicEntityRecord record, bool isCreate = false)
        {
            pendingNewEntities.Add((record, isCreate));
        }

        

        private HashSet<long> InBattleUnitDict = new();
        public void OnUnitCombatStateUpdate(BaseUnitLogicEntity unit)
        {
            if(unit.IsInCombat)
            {
                InBattleUnitDict.Add(unit.Id);
            }
            else
            {
                InBattleUnitDict.Remove(unit.Id);
            }

            if (InBattleUnitDict.Count == 0)
            {
                PlayerPeaceMode = true;
            }
            else
            {
                PlayerPeaceMode = false;
            }
        }

        private float _peaceModeTimer = 0;
        private List<long> inbattleCache = new();
        private void TickPeaceMode()
        {
            if(LogicTime.time - _peaceModeTimer < 3.0f)
            {
                return;
            }
            _peaceModeTimer = LogicTime.time;

            foreach(var id in InBattleUnitDict)
            {
                var logicEntity = AreaManager.GetLogicEntiy(id, false);
                if (logicEntity == null || logicEntity is not BaseUnitLogicEntity unitEntity || !unitEntity.IsInCombat)
                {
                    inbattleCache.Add(id);
                }
            }

            foreach(var id in inbattleCache)
            {
                InBattleUnitDict.Remove(id);
            }

            //bool allPeace = true;
            //var rett = AreaManager.FindEntityInRange(playerLogicEntity.Pos, 30.0f);
            //foreach(var one in rett)
            //{
            //    if(one is not NpcUnitLogicEntity npcUnit)
            //    {
            //        continue;
            //    }

            //    if(npcUnit.CombatState == NpcCombatStateComp.ECombatState.InCombat)
            //    {
            //        allPeace = false;
            //        break;
            //    }
            //}

            if(InBattleUnitDict.Count == 0)
            {
                PlayerPeaceMode = true;
            }
            else
            {
                PlayerPeaceMode = false;
            }
        }

        public ProjectileHolder projectileHolder;

        // 根据Record创建运行时实例
        public LogicEntityBase CreateEntityByRecord(LogicEntityRecord record)
        {
            LogicEntityBase newEntity = null;
            switch (record.EntityType)
            {
                case EEntityType.Player:
                    {
                        playerLogicEntity = new PlayerLogicEntity(this, record.Id, "0", record.Position, record);
                        playerLogicEntity.viewer = this.viewer;

                        newEntity = playerLogicEntity;

                        playerDataManager?.EquipmentManager?.NotifyPlayerReady(this);

                        playerLogicEntity.EventOnFaQingStateChange += () =>
                        {
                            LogicEventBus.Publish(new MLEPlayerFaQingStatusChangeEvent()
                            {
                                Ctx = new()
                                {
                                },
                            });
                        };

                        playerLogicEntity.EventOnExposeStateChange += (isBroken) =>
                        {
                            LogicEventBus.Publish(new MLEPlayerExposeStatusChangeEvent()
                            {
                                Ctx = new()
                                {
                                },
                            });
                        };
                        
                    }
                    break;
                
                case EEntityType.LootPoint:
                    {
                        var newLoot = new LootPointLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newLoot.EventOnLootPointUnlock += (lootPoint) =>
                        {
                            //// 是否进入模式
                            //if (MainGameManager.Instance.interactSystem.currnteractObj != null && MainGameManager.Instance.interactSystem.currnteractObj.GetLogicEntity() == newLoot)
                            //{
                            //    MainUIManager.Instance.TryEnterLootDetailMode(newLoot);
                            //}
                        };

                        newLoot.EventOnLootPointUsed += (lootPoint) =>
                        {
                            // 是否进入模式
                            UIOrchestrator.Instance.TryEnterLootDetailMode(newLoot);
                        };

                        newEntity = newLoot;
                    }
                    break;
                case EEntityType.Npc:
                    {
                        var newNpc = new NpcUnitLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newNpc;
                    }
                    break;
                case EEntityType.AreaEffect:
                    {
                        var areaEffect = new AreaEffectLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = areaEffect;
                    }
                    break;
                case EEntityType.InteractPoint:
                    {
                        var newIntPoint = new LogicEntityInteractPoint(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newIntPoint;
                    }
                    break;
                case EEntityType.DestroyObj:
                    {
                        var newDdestroyObj = new DestroyObjLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newDdestroyObj;
                    }
                    break;
                case EEntityType.GatherPoint:
                    {
                        var newGatherPoint = new GatherPointLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newGatherPoint;
                    }
                    break;
                case EEntityType.AttractPoint:
                    {
                        var newAttractPoint = new AttractPointLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newAttractPoint;
                    }
                    break;

                case EEntityType.PatrolGroup:
                    {
                        var patrolGroup = new PatrolGroupLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = patrolGroup;
                    }
                    break;

                case EEntityType.HomeFacility:
                    {
                        var homePlacement = new HomeFacilityLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = homePlacement;
                    }
                    break;

                case EEntityType.EventGroup:
                    {
                        var egEntity = new EventGroupLogicEntity(this, record.Id, record.CfgId, record.Position, record);

                        newEntity = egEntity;
                    }
                    break;
                case EEntityType.FacilityRuin:
                    {
                        var newGatherPoint = new LogicEntityRepairPoint(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newGatherPoint;
                    }
                    break;
                case EEntityType.DynamicSpawner:
                    {
                        var newGatherPoint = new DynamicSpawnerLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newGatherPoint;
                    }
                    break;

                case EEntityType.Teleporter:
                    {
                        var newTeleporter = new LogicEntityTeleporter(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newTeleporter;
                    }
                    break;
                case EEntityType.SimpleBlock:
                    {
                        var newTeleporter = new LogicEntitySimpleBlock(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = newTeleporter;
                    }
                    break;
                case EEntityType.SavePoint:
                    {
                        newEntity = new LogicEntitySavePoint(this, record.Id, record.CfgId, record.Position, record);
                    }
                    break;
                case EEntityType.FishingSpot:
                    {
                        newEntity = new FishingSpotLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                    }
                    break;
                case EEntityType.Trap:
                    {
                        newEntity = new TrapLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                    }
                    break;
                default:
                    {
                        ;
                    }
                    break;
            }

            if (newEntity != null)
            {
                newEntity.viewer = this.viewer;

                EventOnLogicEntitySpawned?.Invoke(newEntity);
            }

            return newEntity;
        }

        // 可选对象池：回收实例
        public void RecycleEntity(ILogicEntity entity)
        {
            EventOnLogicEntityDespawned?.Invoke(entity);
        }

        ///// <summary>
        ///// 刷新entity
        ///// </summary>
        ///// <param name="entityType"></param>
        ///// <param name="cfgId"></param>
        ///// <param name="bornPos"></param>
        ///// <param name="initialCampId"></param>
        //public LogicEntityBase CreateNewEntity(EEntityType entityType, string cfgId, Vector2 bornPos, int initialCampId)
        //{

        //}

        public void EntityTeleportTo(long entityId, Vector2 pos)
        {
            var entity = AreaManager.GetLogicEntiy(entityId);
            if(entity == null)return;

            entity.TeleportTo(pos);
        }

        
        #region 全局警戒

        public int AlertVal = 0;
        public void AddAlertVal(int addVal)
        {
            int oldAlert = this.AlertVal;
            this.AlertVal += addVal;
            if (oldAlert < 0 && AlertVal >= 50)
            {
                LogicEventBus.Publish(new MLECommonGameEvent()
                {
                    Ctx = new()
                    {
                        HappenPos = Vector2.zero,
                        SourceEntity = null,
                    },
                    Name = "AlertTrigger",
                    Param1 = 1,
                });
            }
        }

        #endregion


        #region 战斗回调

        public class BattleResult
        {
            public bool IsWin;
            public long JingExp;
            public List<long> InvolvedEntites = new();
        }

        public void OnBattleEnd(BattleResult result)
        {
            if (result.IsWin)
            {
                // 回血
                
                foreach(var i in result.InvolvedEntites)
                {
                    var e = GetLogicEntity(i);
                    if(e != null && e is BaseUnitLogicEntity unit)
                    {
                        unit.ForceDie();
                    }
                }
            }
            else
            {

                // 死亡
                var reviveMapName = GetCurrentReviveMap();
                //var bornP = playerDataManager.SavedBornPoint;
                //if(!string.IsNullOrEmpty(bornP))
                //{
                //    bornP = "initial";
                //}

                //var bornCfg = CfgMgr.Cfgs.TbBornPoint.GetOrDefault(bornP);
                // 回城
                PreparePlayerSwitchArea(reviveMapName, true);
            }
        }

        #endregion

        /// <summary>
        /// todo 保存地图名还是出生点名？
        /// </summary>
        /// <returns></returns>
        public string GetCurrentReviveMap()
        {
            if(playerDataManager == null || !playerDataManager.CheckHasParam("base_clear"))
            {
                return "game_init";
            }

            return "base_01";
            //return playerDataManager.SavedBornPoint;
        }

        public bool HandleUseItem(long userUnit, long cnt, ItemUse useRow)
        {
            var srcActor = GetLogicEntity(userUnit) as BaseUnitLogicEntity;
            if (srcActor == null)
            {
                return false;
            }

            switch (useRow.UseType)
            {
                case EItemUseType.AddHunger:
                    {
                        srcActor.ApplyResourceChange(AttrIdConsts.PlayerHunger, +useRow.P1 * 100 * cnt, false, EDmgFlag.None, srcActor.Id);
                    }
                    break;
                case EItemUseType.GiveDrop:
                    {
                        int dropId = (int)useRow.P1;
                        if (dropId <= 0 && int.TryParse(useRow.S1, out var parsedDrop))
                        {
                            dropId = parsedDrop;
                        }

                        if (dropId <= 0)
                        {
                            Debug.LogError("HandleUseItem GiveDrop invalid drop bundle id.");
                            break;
                        }

                        if(cnt > 100)
                        {
                            Debug.Log($"HandleUseItem too much bundle {cnt}");
                            break; 
                        }

                        int it = 0;
                        while(it++ < cnt)
                        {
                            var items = DropUtils.GetBundleDropItems(dropId);
                            for (int i = 0; i < items.Count; i++)
                            {
                                playerDataManager.GiveItemToPlayer(items[i].Item1, items[i].Item2);
                            }
                        }
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 完成撤退
        /// </summary>
        public void OnBigMapRetreatSuccess()
        {
            NeedBalancing = true;
        }


        public IEnumerable<ILogicEntity> FindEntityInRange(Vector2 pos, float radius)
        {
            return AreaManager.FindEntityInRange(pos, radius);
        }

        #region Persistence (save / buff restore)

        private List<BuffPersistData> _pendingPlayerBuffRestore;

        private Dictionary<string, MapRuntimePersistData> _pendingMapRuntimeByMapId;

        public OpenWorldReturnBookmark LastOpenWorldBeforeHome { get; private set; }

        void CapturePersistenceFromSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                _pendingPlayerBuffRestore = null;
                LastOpenWorldBeforeHome = null;
                _pendingMapRuntimeByMapId = null;
                return;
            }

            if (saveData.PlayerBuffs != null && saveData.PlayerBuffs.Count > 0)
            {
                _pendingPlayerBuffRestore = new List<BuffPersistData>(saveData.PlayerBuffs);
            }
            else
            {
                _pendingPlayerBuffRestore = null;
            }

            if (saveData.LastOpenWorldBeforeHome != null)
            {
                LastOpenWorldBeforeHome = new OpenWorldReturnBookmark
                {
                    MapId = saveData.LastOpenWorldBeforeHome.MapId,
                    Pos = saveData.LastOpenWorldBeforeHome.Pos,
                };
            }
            else
            {
                LastOpenWorldBeforeHome = null;
            }

            _pendingMapRuntimeByMapId = null;
            if (saveData.MapRuntimeByMapId != null && saveData.MapRuntimeByMapId.Count > 0)
            {
                _pendingMapRuntimeByMapId = new Dictionary<string, MapRuntimePersistData>(saveData.MapRuntimeByMapId);
            }

            SaveData.SyncLogicEntityIdCounterFromSave(saveData);

            if (saveData.GlobalRuntime != null)
            {
                AlertVal = saveData.GlobalRuntime.AlertVal;
                SettlementDayIndex = saveData.GlobalRuntime.SettlementDayIndex;
                WantedManager.LastWantedTime = saveData.GlobalRuntime.WantedLastTime;
                if (saveData.GlobalRuntime.WantedChannels != null && saveData.GlobalRuntime.WantedChannels.Count > 0)
                {
                    WantedManager.ImportFromPersist(saveData.GlobalRuntime.WantedChannels);
                }
            }
            else
            {
                SettlementDayIndex = 0;
            }
        }

        public void ApplyPendingMapRuntimeAfterMapInit(string mapName)
        {
            if (string.IsNullOrEmpty(mapName) || AreaManager == null)
            {
                return;
            }

            if (_pendingMapRuntimeByMapId == null || !_pendingMapRuntimeByMapId.TryGetValue(mapName, out var chunk))
            {
                return;
            }

            AreaManager.ApplyMapRuntimePersistData(chunk);
            _pendingMapRuntimeByMapId.Remove(mapName);
        }

        void TrySnapshotOpenWorldBeforeEnteringHome(string destinationMapName)
        {
            var destCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(destinationMapName);
            string srcMap = AreaManager.MapName;
            if (string.IsNullOrEmpty(srcMap))
            {
                return;
            }

            var srcCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(srcMap);
            if (destCfg == null || !destCfg.IsHome)
            {
                return;
            }

            if (srcCfg == null || srcCfg.IsHome)
            {
                return;
            }

            if (playerLogicEntity == null)
            {
                return;
            }

            LastOpenWorldBeforeHome = new OpenWorldReturnBookmark
            {
                MapId = srcMap,
                Pos = playerLogicEntity.Pos,
            };
        }

        public void RestorePendingPlayerBuffsIfAny(IEntityBuffOwner owner)
        {
            if (_pendingPlayerBuffRestore == null || _pendingPlayerBuffRestore.Count == 0)
            {
                return;
            }

            foreach (var snap in _pendingPlayerBuffRestore)
            {
                globalBuffManager.RehydrateBuffFromPersist(owner, snap);
            }

            _pendingPlayerBuffRestore = null;
        }

        public void AppendRuntimePersistenceToSaveData(SaveData data)
        {
            if (data == null) return;

            data.LastOpenWorldBeforeHome = LastOpenWorldBeforeHome == null
                ? null
                : new OpenWorldReturnBookmark
                {
                    MapId = LastOpenWorldBeforeHome.MapId,
                    Pos = LastOpenWorldBeforeHome.Pos,
                };

            data.GlobalRuntime ??= new GlobalRuntimePersistData();
            data.GlobalRuntime.AlertVal = AlertVal;
            data.GlobalRuntime.WantedScaledVal = WantedManager != null ? WantedManager.CurrentWantedVal : 0;
            data.GlobalRuntime.WantedLastTime = WantedManager != null ? WantedManager.LastWantedTime : 0f;
            data.GlobalRuntime.WantedChannels = WantedManager != null ? WantedManager.ExportToPersist() : null;
            data.GlobalRuntime.SettlementDayIndex = SettlementDayIndex;

            data.MapRuntimeByMapId ??= new Dictionary<string, MapRuntimePersistData>();
            if (AreaManager != null && !string.IsNullOrEmpty(AreaManager.MapName))
            {
                data.MapRuntimeByMapId[AreaManager.MapName] = AreaManager.BuildMapRuntimePersistData();
            }

            long maxEntityId = data.NextLogicEntityIdHint;
            foreach (var pair in data.MapRuntimeByMapId)
            {
                var block = pair.Value;
                if (block?.EntityRecords == null) continue;
                foreach (var rec in block.EntityRecords)
                {
                    if (rec != null) maxEntityId = Math.Max(maxEntityId, rec.Id);
                }
            }

            if (playerLogicEntity != null)
            {
                maxEntityId = Math.Max(maxEntityId, playerLogicEntity.Id);
            }

            data.NextLogicEntityIdHint = maxEntityId;

            worldPersistState?.ApplyRuntimeToSaveData(data);
            playerDataManager?.ApplyRuntimeToSaveData(data);
            homeDataManager?.ApplyToSaveData(data);

            data.PlayerBuffs ??= new List<BuffPersistData>();
            data.PlayerBuffs.Clear();
            if (playerLogicEntity == null)
            {
                return;
            }

            foreach (var b in playerLogicEntity.BuffContainer.Values)
            {
                var row = new BuffPersistData
                {
                    BuffId = b.BuffId,
                    Layer = b.Layer,
                    RemainingLifetime = b.Lifetime,
                    CasterEntityId = b.CasterId,
                    SrcBuffId = b.SrcBuffId,
                };
                if (b.UsesIndependentStack)
                {
                    row.StackLayers = b.ExportStackLayersForPersist();
                    row.RemainingLifetime = -1f;
                }

                data.PlayerBuffs.Add(row);
            }
        }

        #endregion


    }

}


