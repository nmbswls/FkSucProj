
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

        const float SavePointPeaceScanRadius = 30f;

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

        /// <summary>
        /// game session
        /// </summary>
        public PlayerGameSession GameSession { get; set; } = new();

        public NpcDirectControlSession NpcDirectControl { get; } = new();

        public PlayerLogicEntity playerLogicEntity { get; set; }

        private List<(LogicEntityRecord, bool)> pendingNewEntities = new();

        public event Action<ILogicEntity> EventOnLogicEntitySpawned;
        public event Action<ILogicEntity> EventOnLogicEntityDespawned;

        // 即将丢弃当前区域/切图前触发：供表现层中断本地传送渐隐协程并释放传送锁，避免协程被 Stop 后锁泄漏
        public event Action EventOnHardAreaClearStarting;

        public ISceneAbilityViewer? viewer; // 表现层接口
        public IVisionSenser2D? visionSenser;
        public INavProvider? navProvider;


        /// <summary>
        /// 
        /// </summary>
        public GlobalBuffManager globalBuffManager;
        public GlobalThrowManager globalThrowManager;
        public GlobalMapDropCollection globalDropCollection;

        public MapLogicEventBus LogicEventBus;

        public GameLogicAreaManager AreaManager;
        public LogicGroundLiquidManager GroundLiquidManager;
        public LogicGroundLiquidFieldManager GroundLiquidFieldManager;
        public LogicGroundMistManager GroundMistManager;

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

        


        public void NotifyPostSearchInvestigationComplete(long npcEntityId)
        {
            WantedGuardSpawner?.EnqueuePostSearchPolicyPending(npcEntityId);
        }

        public void OnGameLogicInit(SaveData saveData)
        {
            LogicEventBus = new();
            AreaManager = new(this, new GameLogicAreaManager.Settings()
            {

            });

            GroundLiquidManager = new(this);
            GroundLiquidFieldManager = new(this);
            GroundLiquidManager.BindFieldManager(GroundLiquidFieldManager);
            GroundMistManager = new(this);
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

            


            CapturePersistenceFromSaveData(saveData);

            BindSecretBaseOnInit();

            BeginFreeBigMapSession();

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
        public void PreparePlayerSwitchArea(string mapOverlayId, bool reset, string? targetPoint = null, Vector2? targetPos = null, bool silent = false)
        {
            if(SwitchAreaIntent != null)
            {
                Debug.LogError("PlayerSwitchArea when have pending switch intent.");
                return;
            }

            // TrySnapshotOpenWorldBeforeEnteringHome(mapOverlayId);
            TrySnapshotOpenWorldBeforeEnteringSecretBase(mapOverlayId);

            var intent = new SwitchAreaIntent();
            intent.AreaOverlayId = mapOverlayId;
            intent.OldAreaName = AreaManager.AreaOverlayId;
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

            if (IsInSecretBase)
            {
                TickSecretBaseOnly(dt);
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

            GroundLiquidManager?.Tick();
            GroundLiquidFieldManager?.Tick();
            GroundMistManager?.Tick();

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

                        playerLogicEntity.EventOnFaQingStateChange += () =>
                        {
                            LogicEventBus.Publish(new MLEPlayerFaQingStatusChangeEvent()
                            {
                                Ctx = new()
                                {
                                },
                            });
                            NotifyHumanQuickBarStateChanged();
                        };

                        playerLogicEntity.EventOnExposeStateChange += (isBroken) =>
                        {
                            LogicEventBus.Publish(new MLEPlayerExposeStatusChangeEvent()
                            {
                                Ctx = new()
                                {
                                },
                            });
                            NotifyHumanQuickBarStateChanged();
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
                case EEntityType.RemovableObstacle:
                    {
                        var removable = new LogicEntityRemovableObstacle(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = removable;
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
                case EEntityType.SkillProxy:
                    {
                        newEntity = new SkillProxyLogicEntity(this, record.Id, record.CfgId, record.Position, record);
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
                case EItemUseType.UnlockRuneUpgrade:
                    {
                        var upgradeId = useRow.S1;
                        if (string.IsNullOrEmpty(upgradeId))
                        {
                            Debug.LogWarning("HandleUseItem UnlockRuneUpgrade: empty upgrade_id.");
                            break;
                        }

                        if (cnt > 1)
                        {
                            Debug.LogWarning($"HandleUseItem UnlockRuneUpgrade: only one upgrade per use, cnt={cnt}.");
                        }

                        playerDataManager.TryUnlockRuneUpgrade(upgradeId);
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

            /*
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
            */
            LastOpenWorldBeforeHome = null;

            CaptureSecretBaseBookmarkFromSave(saveData);

            _pendingMapRuntimeByMapId = null;
            if (saveData.MapRuntimeByMapId != null && saveData.MapRuntimeByMapId.Count > 0)
            {
                _pendingMapRuntimeByMapId = new Dictionary<string, MapRuntimePersistData>(saveData.MapRuntimeByMapId);
            }

            SaveData.SyncLogicEntityIdCounterFromSave(saveData);
            SaveData.SyncItemInstanceIdCounterFromSave(saveData);

            if (saveData.GlobalRuntime != null)
            {
                AlertVal = saveData.GlobalRuntime.AlertVal;
                SettlementDayIndex = saveData.GlobalRuntime.SettlementDayIndex;
                GameSession.SPDesireShardDeposited = saveData.GlobalRuntime.SavePointVaultDesireShardDepositedThisRun;
                WantedManager.LastWantedTime = saveData.GlobalRuntime.WantedLastTime;
                if (saveData.GlobalRuntime.WantedChannels != null && saveData.GlobalRuntime.WantedChannels.Count > 0)
                {
                    WantedManager.ImportFromPersist(saveData.GlobalRuntime.WantedChannels);
                }
            }
            else
            {
                SettlementDayIndex = 0;
                GameSession.SPDesireShardDeposited = 0;
            }
        }

        public void ApplyPendingMapRuntimeAfterMapInit(string mapOverlayId)
        {
            if (string.IsNullOrEmpty(mapOverlayId) || AreaManager == null)
            {
                return;
            }

            if (_pendingMapRuntimeByMapId == null || !_pendingMapRuntimeByMapId.TryGetValue(mapOverlayId, out var chunk))
            {
                return;
            }

            AreaManager.ApplyMapRuntimePersistData(chunk);
            _pendingMapRuntimeByMapId.Remove(mapOverlayId);
        }

        // Home 书签暂不使用
        void TrySnapshotOpenWorldBeforeEnteringHome(string destinationMapName)
        {
            /*
            var destCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(destinationMapName);
            string srcMap = AreaManager.AreaOverlayId;
            if (string.IsNullOrEmpty(srcMap))
            {
                return;
            }

            var srcCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(srcMap);
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
            */
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

            /*
            data.LastOpenWorldBeforeHome = LastOpenWorldBeforeHome == null
                ? null
                : new OpenWorldReturnBookmark
                {
                    MapId = LastOpenWorldBeforeHome.MapId,
                    Pos = LastOpenWorldBeforeHome.Pos,
                };
            */
            data.LastOpenWorldBeforeHome = null;

            AppendSecretBaseBookmarkToSave(data);

            data.GlobalRuntime ??= new GlobalRuntimePersistData();
            data.GlobalRuntime.AlertVal = AlertVal;
            data.GlobalRuntime.WantedScaledVal = WantedManager != null ? WantedManager.CurrentWantedVal : 0;
            data.GlobalRuntime.WantedLastTime = WantedManager != null ? WantedManager.LastWantedTime : 0f;
            data.GlobalRuntime.WantedChannels = WantedManager != null ? WantedManager.ExportToPersist() : null;
            data.GlobalRuntime.SettlementDayIndex = SettlementDayIndex;
            data.GlobalRuntime.SavePointVaultDesireShardDepositedThisRun = GameSession.SPDesireShardDeposited;

            data.MapRuntimeByMapId ??= new Dictionary<string, MapRuntimePersistData>();
            if (AreaManager != null && !string.IsNullOrEmpty(AreaManager.AreaOverlayId))
            {
                data.MapRuntimeByMapId[AreaManager.AreaOverlayId] = AreaManager.BuildMapRuntimePersistData();
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
            SaveData.WriteItemInstanceIdHintToSave(data);

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
                    CachedPotencyAttrs = b.ExportCachedPotencyForPersist(),
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


