
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
using static UnityEditor.PlayerSettings;

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

        

        public ISceneAbilityViewer? viewer; // 表现层接口
        public IVisionSenser2D? visionSenser;
        public INavProvider? navProvider;


        public int TimePeriod;


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
        public WantedManager WantedManager;

        public PlayerSystemManager playerDataManager;
        public HomeDataManager homeDataManager;
        public ShopDataManager shopDataManager;


        public MapControlEventManager controlEventManager;
        public FactionRelationManager factionRelationManager;

        public bool PlayerPeaceMode { get; set; } = false;

        public void OnGameLogicInit(SaveData saveData)
        {
            LogicEventBus = new();
            AreaManager = new(this, new GameLogicAreaManager.Settings()
            {

            });

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

            DropUtils.InitializeDropGroups();

            

            playerDataManager.inventoryModel.EventOnGainItem += (itemId, count) =>
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
        public void DoSwitchArea(SwitchAreaIntent intent)
        {

            var mapCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(intent.AreaName);


            AreaManager.InitilizeMap(intent.AreaName);

            if(mapCfg != null && mapCfg.IsHome)
            {
                homeDataManager.OnPlayerEnterHome();
            }
            

            var bornPos = new List<NamedPoint>();
            var ps = AreaManager.cacheDatabase.NamedPoints;
            foreach(var p in ps)
            {
                if(p.PointType == ENamedPointType.BornPos)
                {
                    bornPos.Add(p);
                }
            }


            Vector2? pos = null;
            if(intent.TargetPos != null)
            {
                pos = intent.TargetPos.Value;
            }
            else if(!string.IsNullOrEmpty(intent.TargetPoint))
            {
                pos = AreaManager.cacheDatabase.FindNamedPointByName(intent.TargetPoint)?.Position ?? null;
            }

            if(pos == null)
            {
                if (bornPos.Count != 0)
                {
                    var randIdx = UnityEngine.Random.Range(0, bornPos.Count);
                    pos = bornPos[randIdx].Position;
                }
            }
            
            if(pos == null)
            {
                Debug.LogError("switch area pos lose");
                pos = Vector2.zero;
            }
            

            LogicEntityRecord4Player playerRecord;
            if (intent.Reset)
            {
                playerRecord = new LogicEntityRecord4Player()
                {
                    Id = 1,
                    EntityType = EEntityType.Player,
                    CfgId = "0",
                    FactionId = EFactionId.Player,

                    Position = pos.Value,
                };
            }
            else
            {
                playerRecord = intent.SavedRecord;
                playerRecord.Position = pos.Value;
            }

            AreaManager.RegisterEntityRecord(playerRecord);

            AreaManager.AddInterestPoint(new InterestPoint
            {
                Id = 1,
                Pos = () => playerLogicEntity.Pos,
                LogicRadius = 40f,
                WarmupRadius = 60f
            });

            shopDataManager.RefreshOnNightStart();

            // 清空延迟信息
            DelayedEffectQueue.Clear();
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

            globalDropCollection?.Tick(dt);

            TickPendingEffect();

            TickPeaceMode();

            // 帧末再处理buff
            globalBuffManager.Tick(dt);

            WantedManager?.Tick(dt);
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


        private void TickPendingEffect()
        {
            if(_delayQueueDirty)
            {
                _delayQueueDirty = false;
                DelayedEffectQueue.Sort((itemA, itemB) => { return itemA.exeTIme.CompareTo(itemB.exeTIme); });
            }

            if(DelayedEffectQueue.Count > 0)
            {
                int handled = 0;
                while(DelayedEffectQueue.Count > 0 && handled < 10)
                {
                    if(LogicTime.time < DelayedEffectQueue[0].exeTIme)
                    {
                        break;
                    }

                    var wrapped = DelayedEffectQueue[0];
                    DelayedEffectQueue.RemoveAt(0);

                    switch(wrapped)
                    {
                        case DelayedFightEffectWrapper fightEffectWrapper:
                            {
                                var executor = GetLogicFightEffectExecutor(fightEffectWrapper.effectConf);
                                executor?.Apply(fightEffectWrapper.effectConf, fightEffectWrapper.ctx);
                            }
                            break;
                    }
                    
                    handled += 1;
                }
            }
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

                        playerLogicEntity.EventOnFaQingStateChange += () =>
                        {
                            LogicEventBus.Publish(new MLEPlayerFaQingStatusChangeEvent()
                            {
                                Ctx = new()
                                {
                                },
                            });
                        };

                        playerLogicEntity.EventOnExposeStateChange += () =>
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
                        var newGatherPoint = new LogicEntityFacilityRuin(this, record.Id, record.CfgId, record.Position, record);
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
            if(!playerDataManager.CheckHasParam("base_clear"))
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
                                playerDataManager.TryGiveItem(items[i].Item1, items[i].Item2, 0);
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

        public OpenWorldReturnBookmark LastOpenWorldBeforeHome { get; private set; }

        void CapturePersistenceFromSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                _pendingPlayerBuffRestore = null;
                LastOpenWorldBeforeHome = null;
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

            data.PlayerBuffs ??= new List<BuffPersistData>();
            data.PlayerBuffs.Clear();
            if (playerLogicEntity == null)
            {
                return;
            }

            foreach (var b in playerLogicEntity.BuffContainer.Values)
            {
                data.PlayerBuffs.Add(new BuffPersistData
                {
                    BuffId = b.BuffId,
                    Layer = b.Layer,
                    RemainingLifetime = b.Lifetime,
                    CasterEntityId = b.CasterId,
                    SrcBuffId = b.SrcBuffId,
                });
            }
        }

        #endregion

    }

}


