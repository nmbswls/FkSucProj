
using Config;
using Map.Drop;
using Map.Logic.Events;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Player;
using My.UI;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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

public class GameLogicManager : ILogicEntityFactory
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

    public GlobalBuffManager globalBuffManager;
    public GlobalThrowManager globalThrowManager;
    public GlobalMapDropCollection globalDropCollection;

    public MapLogicEventBus LogicEventBus;

    public string CurrentArea = string.Empty;
    public ChunkMapExportDatabase cacheMapDb;

    public GameLogicAreaManager AreaManager;

    public PlayerDataManager playerDataManager;

    public GlobalDropTable DropTable;

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
        var playerRecord = new LogicEntityRecord4UnitBase()
        {
            Id = 1,
            EntityType = EEntityType.Player,
            CfgId = "0",

            AlwaysActive = true,
        };

        await AreaManager.InitilizeArea(areaName);

        AreaManager.RegisterEntityRecord(playerRecord);

        AreaManager.AddInterestPoint(new InterestPoint
        {
            Id = 1,
            Pos = () => playerLogicEntity.Pos,
            LogicRadius = 80f,
            WarmupRadius = 120f
        });
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
        if(!Initialized)
        {
            return;
        }

        globalBuffManager.Tick(dt);
        globalThrowManager.Tick(dt);


        foreach (var entity in AreaManager.Repo.Loaded.Values)
        {
            entity.Tick(dt);
            if(entity.MarkDead)
            {
            }
        }

        if(pendingNewEntities.Count > 0)
        {
            foreach(var entityRecord in pendingNewEntities)
            {
                AreaManager.RegisterEntityRecord(entityRecord);
            }
            pendingNewEntities.Clear();
        }

        AreaManager.Tick(dt);
    }

    public void CreateNewEntityRecord(LogicEntityRecord record)
    {
        pendingNewEntities.Add(record);
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
                    
            }

            if (executor != null)
            {
                EffectExecutors[effectType.GetType()] = executor;
            }
        }

        return executor;
    }

    public class LogicFightEffectContext
    {
        public GameLogicManager Env { get; protected set; }
        public LogicFightEffectContext(GameLogicManager env, SourceKey? sourceKey)
        {
            this.Env = env;
            this.SourceKey = sourceKey;
        }

        public SourceKey? SourceKey; // 

        public ILogicEntity Actor;         // 施动者
        public ILogicEntity Target;        // 目标对象（如门或敌人），可为空
        public Vector2? FaceDir;           // 面朝方向
        public Vector2? CastDir;           // 面朝方向
        public Vector2? Position;         // 施放位置（如脚下或点击点）

        // 变量集合
        public Dictionary<string, string> RunningVariables = new();
        public Dictionary<string, long> RunningStorage = new();


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

    public void HandleLogicFightEffect(MapFightEffectCfg effectConf, LogicFightEffectContext effectCtx)
    {
        var executor = GetLogicFightEffectExecutor(effectConf);
        executor?.Apply(effectConf, effectCtx);
    }
}


public class ProjectileHolder
{
    public Dictionary<long, LogicProjectileInfo> ProjectileInfos = new();
    public static long IdInstCounter = 10000;

    public event Action<LogicProjectileInfo> EventOnLogicProjectileSpawn;

    public LogicProjectileInfo CreateLogicProjectile(ProjectileData pData, ILogicEntity caster, Vector2 bornPos, Vector2 dir)
    {
        var projectilInfo = new LogicProjectileInfo
        {
            instId = ++IdInstCounter,
            ownerEntity = caster,
            pData = pData,
            spawnPos = bornPos,
            initialDir = dir,
        };

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
        if(pInfo != null)
        {
            // give effect
            //pInfo.
        }
    }
}