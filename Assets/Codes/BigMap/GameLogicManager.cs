
using Config;
using Map.Logic.Events;
using My.Home;
using My.Map;
using My.Map.Drop;
using My.Map.Entity;
using My.Map.Logic;
using My.MapExport;
using My.Player;
using My.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using static MapSceneEffectManager;
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
        ILogicEntity CreateEntityByRecord(LogicEntityRecord record);
        // 可选对象池：回收实例
        void RecycleEntity(ILogicEntity entity);
    }

    public partial class GameLogicManager : ILogicEntityFactory
    {
        public static long LogicEntityIdInst = 100;

        public bool Initialized { get; set; }

        public PlayerLogicEntity playerLogicEntity;

        private List<LogicEntityRecord> pendingNewEntities = new();

        public event Action<ILogicEntity> EventOnLogicEntitySpawned;
        public event Action<ILogicEntity> EventOnLogicEntityDespawned;

        /// <summary>
        /// 通知上层玩家需要切换场景
        /// </summary>
        public event Action<string?, string?> EventOnPlayerSwitchArea;

        public ISceneAbilityViewer? viewer; // 表现层接口
        public IVisionSenser2D? visionSenser;
        public INavProvider? navProvider;

        public GlobalBuffManager globalBuffManager;
        public GlobalThrowManager globalThrowManager;
        public GlobalMapDropCollection globalDropCollection;

        public MapLogicEventBus LogicEventBus;

        public string CurrentArea = string.Empty;
        public MapExportDatabase cacheMapDb;

        public GameLogicAreaManager AreaManager;

        public PlayerDataManager playerDataManager;
        public HomeDataManager homeDataManager;
        public ShopDataManager shopDataManager;

        public GlobalDropTable DropTable;

        public MapControlEventManager controlEventManager;
        public FactionRelationManager factionRelationManager;

        public bool PlayerPeaceMode = false;

        public void OnGameInit()
        {
            LogicEventBus = new();
            AreaManager = new(this, new GameLogicAreaManager.Settings()
            {

            });

            UnitAttrSystemUnits.InitGameAttrs();


            globalBuffManager = new(this);
            globalBuffManager.InitEventListening();

            globalThrowManager = new(this);

            globalDropCollection = new(this);

            projectileHolder = new();

            playerDataManager = new(this);
            playerDataManager.InitPlayer();

            shopDataManager = new(this);

            homeDataManager = new(this);
            homeDataManager.EvOnPlacementUpdate += (placementInfo) =>
            {
                

                //AreaManager.EntityRefreshInfo.Add(refreshInfo);
            };

            factionRelationManager = new();

            controlEventManager = new(this);
            controlEventManager.Initialize();

            DropTable = Resources.Load<GlobalDropTable>("Config/DropTable");
        }


        public ILogicEntity GetLogicEntity(long instId, bool ensureExist = true)
        {
            return AreaManager.GetLogicEntiy(instId, ensureExist);
        }

        /// <summary>
        /// 玩家进入/切换场景
        /// </summary>
        /// <param name="areaName"></param>
        public async Task PlayerEnterArea(string areaName)
        {
            await AreaManager.InitilizeArea(areaName);

            if(areaName == "home")
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

            if(areaName == "home")
            {
                PlayerPeaceMode = true;
            }
            else
            {
                PlayerPeaceMode = false;
            }

            var vec = Vector2.zero;
            if(bornPos.Count != 0)
            {
                var randIdx = UnityEngine.Random.Range(0, bornPos.Count);
                vec = bornPos[randIdx].Position;
            }

            var playerRecord = new LogicEntityRecord4UnitBase()
            {
                Id = 1,
                EntityType = EEntityType.Player,
                CfgId = "0",
                FactionId = EFactionId.Player,

                Position = vec,
            };


            AreaManager.RegisterEntityRecord(playerRecord);

            AreaManager.AddInterestPoint(new InterestPoint
            {
                Id = 1,
                Pos = () => playerLogicEntity.Pos,
                LogicRadius = 80f,
                WarmupRadius = 120f
            });

            shopDataManager.RefreshOnNightStart();

            // 清空延迟信息
            DelayedEffectQueue.Clear();
        }

        /// <summary>
        /// 玩家进入/切换场景
        /// </summary>
        /// <param name="areaName"></param>
        public void PlayerSwitchArea(string areaName)
        {
            EventOnPlayerSwitchArea?.Invoke(null, areaName);
        }
        public void Tick(float dt)
        {
            if (!Initialized)
            {
                return;
            }

            globalBuffManager.Tick(dt);
            globalThrowManager.Tick(dt);


            foreach (var entity in AreaManager.Repo.Loaded.Values)
            {
                entity.Tick(dt);
                if (entity.MarkDead)
                {
                }
            }

            if (pendingNewEntities.Count > 0)
            {
                foreach (var entityRecord in pendingNewEntities)
                {
                    AreaManager.RegisterEntityRecord(entityRecord);
                }
                pendingNewEntities.Clear();
            }



            AreaManager.Tick(dt);

            globalDropCollection?.Tick(dt);

            TickPendingEffect();
        }

        public void AddNewEntityRecord(LogicEntityRecord record)
        {
            pendingNewEntities.Add(record);
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

                    var executor = GetLogicFightEffectExecutor(wrapped.effectConf);
                    executor?.Apply(wrapped.effectConf, wrapped.ctx);
                    handled += 1;
                }
            }
        }


        public ProjectileHolder projectileHolder;

        // 根据Record创建运行时实例
        public ILogicEntity CreateEntityByRecord(LogicEntityRecord record)
        {
            LogicEntityBase newEntity = null;
            switch (record.EntityType)
            {
                case EEntityType.Player:
                    {
                        playerLogicEntity = new PlayerLogicEntity(this, record.Id, "0", new Vector2(0, 0), record);
                        playerLogicEntity.viewer = this.viewer;

                        newEntity = playerLogicEntity;
                    }
                    break;
                case EEntityType.Monster:
                    {
                        var newMonster = new MonsterUnitLogicEntity(this, record.Id, record.CfgId, record.Position, record);

                        newEntity = newMonster;
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
                        var newIntPoint = new InteractPointLogic(this, record.Id, record.CfgId, record.Position, record);
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

                case EEntityType.HomePlacement:
                    {
                        var homePlacement = new HomePlacementLogicEntity(this, record.Id, record.CfgId, record.Position, record);
                        newEntity = homePlacement;
                    }
                    break;

                case EEntityType.EventGroup:
                    {
                        var egEntity = new EventGroupLogicEntity(this, record.Id, record.CfgId, record.Position, record);

                        newEntity = egEntity;
                    }
                    break;
            }

            if (newEntity != null)
            {
                newEntity.Initialize();
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

        public Vector2 GetNamedPointPos(string pointName)
        {
            var areaName = CurrentArea;
            //Config
            return Vector2.zero;
        }

        private Dictionary<Type, AbilityEffectExecutor> EffectExecutors = new(); // executor
        private AbilityEffectExecutor GetLogicFightEffectExecutor(MapFightEffectCfg effectType)
        {
            if (!EffectExecutors.TryGetValue(effectType.GetType(), out var executor))
            {
                switch (effectType)
                {
                    case MapAbilityEffectUnlockLootPoint:
                        {
                            executor = new AbilityEffectExecutor4UnlockLootPoint();
                        }
                        break;
                    case MapAbilityEffectUseLootPoint:
                        {
                            executor = new AbilityEffectExecutor4UseLootPoint();
                        }
                        break;
                    case MapAbilityEffectCostResourceCfg:
                        {
                            executor = new AbilityEffectExecutor4CostResource();
                        }
                        break;

                    case MapAbilityEffectApplyDamageCfg:
                        {
                            executor = new AbilityEffectExecutor4ApplyDamage();
                        }
                        break;

                    case MapAbilityEffectThrowStartCfg:
                        {
                            executor = new AbilityEffectExecutor4ThrowStart();
                        }
                        break;

                    case MapAbilityEffectAddResourceCfg:
                        {
                            executor = new AbilityEffectExecutor4AddResource();
                        }
                        break;
                    case MapAbilityEffectUseItemCfg:
                        {
                            executor = new AbilityEffectExecutor4UseItem();
                        }
                        break;
                    case MapAbilityEffectSpawnBulletCfg:
                        {
                            executor = new AbilityEffectExecutor4SpawnBullet();
                        }
                        break;
                    case MapAbilityEffectUseWeaponCfg:
                        {
                            executor = new AbilityEffectExecutor4UseWeapon();
                        }
                        break;

                    case MapAbilityEffectDefaultInteractCfg:
                        {
                            executor = new AbilityEffectExecutor4DefaultInteract();
                        }
                        break;

                    case MapAbilityEffectDashStartCfg:
                        {
                            executor = new AbilityEffectExecutor4DashStart();
                        }
                        break;
                    case MapAbilityEffectAddBuffCfg:
                        {
                            executor = new AbilityEffectExecutor4AddBuff();
                        }
                        break;
                    case MapAbilityEffectRemoveBuffCfg:
                        {
                            executor = new AbilityEffectExecutor4RemoveBuff();
                        }
                        break;
                    case MapAbilityEffectHitBoxCfg:
                        {
                            executor = new AbilityEffectExecutor4HitBox();
                        }
                        break;
                    case MapAbilityEffectIfBranchCfg:
                        {
                            executor = new AbilityEffectExecutor4IfBranch();
                        }
                        break;
                    case MapAbilityEffectOpenClickWindowCfg:
                        {
                            executor = new AbilityEffectExecutor4OpenClickWindow();
                        }
                        break;
                    case MapAbilityEffectDeepZhaquCfg:
                        {
                            executor = new AbilityEffectExecutor4DeepZhaqu();
                        }
                        break;

                    case MapAbilityEffectSpawnEntityCfg:
                        {
                            executor = new AbilityEffectExecutor4SpawnEntity();
                        }
                        break;
                    case MapAbilityEffectRangePreviewCfg:
                        {
                            executor = new AbilityEffectExecutor4RangePreview();
                        }
                        break;
                    case MapAbilityEffectNextPhaseCfg:
                        {
                            executor = new AbilityEffectExecutor4NextPhase();
                        }
                        break;

                }

                if (executor != null)
                {
                    EffectExecutors[effectType.GetType()] = executor;
                }
            }

            return executor;
        }

        public enum ESourceType
        {
            Unknown,
            Ability,
            Buff,
            BuffTrigger,
            BuffEffect,
            Item,
            Env,
            Aura,
            AreaEffect,
            Bullet,
            Mechanism,
            Throw,
        }

        /// <summary>
        /// 效果源信息
        /// </summary>
        [Serializable]
        public class EffectSourceInfo
        {
            public ESourceType SrcType; // 
            public long SrcEntityId;
            public long SrcInstId;
            public string SrcCfgId;

            public long SrcBuffId;
            public EFactionId SrcFactionId;
        }


        public class LogicFightEffectContext
        {
            public GameLogicManager Env { get; protected set; }
            public LogicFightEffectContext(GameLogicManager env, EffectSourceInfo sourceInfo)
            {
                this.Env = env;
                this.SourceInfo = sourceInfo;
            }

            public EffectSourceInfo SourceInfo; // 

            public long TargetId;              // 锁定对象 如果来自技能 则在释放时锁定 如果来自效果触发 则在逻辑中绑定
            public Vector2? TriggerPos;        // 发生地点 技能释放位置 子弹碰撞位置 buff触发位置等
            public Vector2? CastVec1;          // 施法参数1 技能施法参数
            public Vector2? CastVec2;          // 施法参数2

            // 变量集合
            public Dictionary<string, string> RunningVariables = new();
            public Dictionary<string, long> RunningStorage = new();

            public Dictionary<string, long> CacheAttrVal = new();

            public List<int> BindSceneFxIds = new();

            public string GetVariatyRawVal(OneVariaty oneVariaty)
            {
                if (oneVariaty.ValType == EOneVariatyType.Invalid)
                {
                    return string.Empty;
                }

                string strVal = oneVariaty.RawVal;
                if (!string.IsNullOrEmpty(oneVariaty.ReferName))
                {
                    do
                    {
                        if (RunningVariables != null && RunningVariables.TryGetValue(oneVariaty.ReferName, out var runningVal))
                        {
                            strVal = runningVal;
                            break;
                        }
                    }
                    while (false);
                }

                return strVal;
            }
        }


        public class DelayedFightEffectWrapper
        {
            public MapFightEffectCfg effectConf;
            public LogicFightEffectContext ctx;
            public float exeTIme;
        }
        public List<DelayedFightEffectWrapper> DelayedEffectQueue = new();

        private bool _delayQueueDirty = false; 
           
        public void HandleLogicFightEffect(MapFightEffectCfg effectConf, LogicFightEffectContext effectCtx)
        {
            if(effectConf.PendingTime > 0)
            {
                DelayedEffectQueue.Add(new DelayedFightEffectWrapper()
                {
                    effectConf = effectConf,
                    ctx = effectCtx,
                    exeTIme = LogicTime.time + effectConf.PendingTime,
                });
                _delayQueueDirty = true;
                return;
            }
            
            var executor = GetLogicFightEffectExecutor(effectConf);
            executor?.Apply(effectConf, effectCtx);
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
    }


    public class ProjectileHolder
    {
        public Dictionary<long, LogicProjectileInfo> ProjectileInfos = new();
        public static long IdInstCounter = 10000;

        public event Action<LogicProjectileInfo> EventOnLogicProjectileSpawn;

        public LogicProjectileInfo CreateLogicProjectile(ProjectileData pData, ILogicEntity caster, Vector2 bornPos, Vector2 dir, long? homingTarget = null)
        {
            var projectilInfo = new LogicProjectileInfo
            {
                instId = ++IdInstCounter,
                ownerEntity = caster,
                pData = pData,
                spawnPos = bornPos,
                initialDir = dir,
            };

            projectilInfo.homingTargetId = homingTarget;
            ProjectileInfos.Add(projectilInfo.instId, projectilInfo);
            EventOnLogicProjectileSpawn?.Invoke(projectilInfo);
            return projectilInfo;
        }

        public void TickLogicProjectile()
        {

        }

        public void OnProjectileTriggered(long projectileId)
        {
            ProjectileInfos.TryGetValue(projectileId, out var pInfo);
            if (pInfo != null)
            {
                // give effect
                //pInfo.
            }
        }

    }
}


