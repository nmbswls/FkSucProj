using Map.Entity;
using Map.Logic;
using Map.Scene;
using Map.Scene.UI;
using Map.SmallGame.Zha;
using My;
using My.Config;
using My.Dialog;
using My.Encounter;
using My.Input;
using My.Map;
using My.Map.Encounter;
using My.Map.Entity;
using My.Map.Entity.AI;
using My.Map.Fight;
using My.Map.Logic;
using My.Map.Scene;
using My.Map.View;
using My.MiniGame;
using My.Player.Bag;
using My.Saving;
using My.UI;
using Newtonsoft.Json;
using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static MapSceneEffectManager;
using static UnityEngine.UI.ContentSizeFitter;


public enum ECampFilterType
{ 
    All,
    NotSelf,
    OnlySelf,
}


public struct EntityFilterParam
{
    public EEntityType FilterType;
    public List<EEntityType> FilterParamLists;

    public ECampFilterType CampFilterType;
    public EFactionId SelfCampId;
    public int FactionMask;

    public bool NeedEnmity;
    public bool NeedFriendly;
}

public interface IVisionSenser2D
{

    bool CanUnitSee(long selfEId, long targetEId);
    bool CanSee(Vector2 selftPos, Vector2 selfFace, Vector2 targetPos, float range, float fov);

    Vector2 ChoosePointAwayFromTarget(Vector2 orgPos, Vector2 centerPos, float awayDist);

    List<ILogicEntity> OverlapBoxAllEntity(Vector2 orgPos, Vector2 dir, Vector2 size, EntityFilterParam? filter);

    List<ILogicEntity> OverlapCircleAllEntity(Vector2 orgPos, float radius, EntityFilterParam? filter);

    bool CheckIsInAlertArea(Vector2 pos);

    void OverlapCheckDynamicObs(Vector2 orgPos, float radius, List<(Vector2, Vector2)> retList);
}


public class MainGameManager : MonoBehaviour, ISceneAbilityViewer
{
    public static float OrderFactor = 100f;

    public static MainGameManager Instance;

    public Transform SceneEffectLayer;



    public PlayerScenePresenter playerScenePresenter;

    public SceneInteractSystem interactSystem;

    public DefaultSceneVisionSenser2D VisionSenser2D;


    public MapSceneFadeAlphaManager SceneFadeManager;

    public MapSceneRangeWarnManager SceneRangeWarnManager;

    public MapSceneDropManager sceneDropManager;

    public MapFovMeshGenerator FovGenerator;

    public LogicTimeManager TimerManager;

    public GameLogicManager gameLogicManager;

    public QuickPlayerInputBinder inputBinder;

    public CameraFollow CameraCtrl;

    public UnityNavProvider NavProvider;

    public DialoguePlayer dialoguePlayer;

    public PlayerRumorTextSpawner RumorTextSpawner;

    private void Awake()
    {
        Instance = this;

        if(!SceneFadeManager)
        {
            SceneFadeManager = GetComponent<MapSceneFadeAlphaManager>();
        }

        VisionSenser2D = new();
        VisionSenser2D.ObstacleMask = 1 << LayerMask.NameToLayer("MapViewObc");

        interactSystem = new();

        NavProvider = new();
    }

    public async Task InitStartGame(string startParams, Action? onComplete)
    {
        AITemplateConfigLoader.Load("");

        CfgMgr.LoadGameConfigs();

        gameLogicManager = new();
        gameLogicManager.viewer = this;
        gameLogicManager.visionSenser = VisionSenser2D;
        gameLogicManager.navProvider = NavProvider;
        gameLogicManager.EventOnLogicEntitySpawned += OnLogicEntitySpawned;
        gameLogicManager.EventOnLogicEntityDespawned += OnLogicEntityDespawned;

        gameLogicManager.EventOnPlayerSwitchArea += OnPlayerSwitchArea;


        // 2. [后台线程] 执行异步读取
        // 此时画面不会卡死，转圈圈动画会流畅播放
        SaveData loadedData = await SaveSystem.LoadAsync(SAVE_FILE_NAME);
        if(loadedData == null)
        {
            Debug.Log("no saving found");
        }
        gameLogicManager.OnGameInit(loadedData);

        gameLogicManager.projectileHolder.EventOnLogicProjectileSpawn += (pInfo) =>
        {
            MapProjectileManager.Instance.Spawn(pInfo);
        };

        UIManager.Instance.ShowLoading("starting");
        

        // 3. [主线程] 应用数据 (Restore State)
        if (loadedData != null)
        {
            ApplyDataToGame(loadedData);
        }

        // 
        await LoadGameMain();

        UIManager.Instance.HideLoading();
    }


    protected async Task LoadGameMain()
    {
        var intent = gameLogicManager.SwitchAreaIntent;
        if(intent == null)
        {
            Debug.LogError("LoadGameMain intent null");
            return;
        }

        // 逻辑上将玩家放入场景
        await gameLogicManager.OnSwitchAreaFinish(intent);

        //if(playerScenePresenter == null)
        //{
        //    var playerGo = Resources.Load<GameObject>("Prefab/Presentations/FakePlayer");
        //    var newGo = GameObject.Instantiate(playerGo, transform);
        //    playerScenePresenter = newGo.GetComponent<PlayerScenePresenter>();
        //}
        
        bool loaded = false;
        WorldAreaManager.Instance.LoadWorld(intent.NewAreaId, onComplete: (w, suc) => { loaded = true; });

        // 等待场景加载
        while(!loaded)
        {
            await Task.Yield();
        }

        await Task.Delay(500);

        //playerScenePresenter.Bind(gameLogicManager.playerLogicEntity);

        // 整理职责
        FovGenerator.OnAreaEnter();
        SceneAOIManager.Instance.InitArea(intent.NewAreaId);
        SceneFadeManager.OnEnterArea(WorldAreaManager.Instance.currentRoot.gameObject);

        UIOrchestrator.Instance.InitGameLogicEventListener();

        inputBinder.ApplyInputMode(QuickPlayerInputBinder.InputMode.Overworld);

        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld, null);


        if(HomeSceneManager.Instance != null)
        {
            HomeSceneManager.Instance.InitHomePlacements();
        }

        gameLogicManager.SwitchAreaIntent = null;
        gameLogicManager.Initialized = true;

        Debug.Log("LoadGameMain finished");
    }

    void Update()
    {
        if(gameLogicManager.Initialized)
        {
            gameLogicManager.Tick(LogicTime.deltaTime);
            interactSystem.Tick(LogicTime.deltaTime);
        }

        if(switchAreaFlag)
        {
            _ = AsyncSwitchArea().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }

                gameLogicManager.SwitchAreaIntent = null;

            }, TaskScheduler.FromCurrentSynchronizationContext());

            switchAreaFlag = false;
        }

        if(Input.GetKeyDown(KeyCode.V))
        {
            //_ = OnSaveClicked().ContinueWith(t =>
            //{
            //    if (t.IsFaulted)
            //    {
            //        Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
            //    }

            //}, TaskScheduler.FromCurrentSynchronizationContext());



            //if (UIGainRewardCoordinator.Instance != null)
            //{
            //    UIGainRewardCoordinator.Instance.CreateScreenItem("1", 1, null);
            //}

            MapSpeechBubbleManager.Instance.Say(playerScenePresenter, "怎么会？");
        }

        //if (playerScenePresenter == null || !playerScenePresenter.IsInBusyZone)
        //{
        //    RumorTextSpawner.IsActive = false;
        //}
        //else
        //{
        //    RumorTextSpawner.IsActive = true;
        //}
        RumorTextSpawner.IsActive = false;

        if (Input.GetKeyDown(KeyCode.M))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            mouseWorld.z = 0;
            gameLogicManager.playerLogicEntity.entityMotorComp.TryMoveTo(mouseWorld);
        }
    }




    public bool IsMouseOnUIOrBlock()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        return false;
    }


    public Vector3 GetWorldPosFromLogicPos(Vector2 pos)
    {
        return pos;
    }

    /// <summary>
    /// todo 需要根据房间裁剪等方式 转换为逻辑坐标 空间可能是重叠的
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public Vector2 GetLogicPosFromWorldPos(Vector3 worldPos)
    {
        // 先检查是否映射在子区域中
        // 


        // 根据结果 返回区域加逻辑坐标


        return new Vector2(worldPos.x, worldPos.y);
    }

    private bool switchAreaFlag = false;
    public void OnPlayerSwitchArea()
    {
        switchAreaFlag = true;
    }

    public async Task AsyncSwitchArea()
    {
        gameLogicManager.Initialized = false;
        gameLogicManager.NeedBalancing = false;
        gameLogicManager.IsBalancing = false;

        gameLogicManager.DelayedEffectQueue.Clear();
        gameLogicManager.AreaManager.CleanArea();
        gameLogicManager.globalBuffManager.Clear();
        gameLogicManager.globalDropCollection.Clear();

        UIManager.Instance.ShowLoading("switching");

        UIManager.Instance.HideAll("LoadingOverlay");
        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);
        bool isUnloading = false;
        WorldAreaManager.Instance.UnloadCurrentWorld(() =>
        {
            isUnloading = true;
        });

        while(!isUnloading)
        {
            await Task.Yield();
        }

        await SceneAOIManager.Instance.CleanupAllAsync();

        await LoadGameMain();

        UIManager.Instance.HideLoading();
    }



    public void OnLogicEntitySpawned(ILogicEntity entity)
    {
        SceneAOIManager.Instance.RegisterEntity(entity, entity.Pos);
    }

    public void OnLogicEntityDespawned(ILogicEntity entity)
    {
        SceneAOIManager.Instance.UnregisterEntity(entity);
    }

    public Transform DynamicRoot;

    public Transform GetDynamicRoot()
    {
        return DynamicRoot;
    }

    public Transform GetWorldStaticPrefabRoot(string worldName)
    {
        return WorldAreaManager.Instance.currentRoot.StaticPrefabRoot;
    }

    /// <summary>
    /// 显示进度条
    /// </summary>
    /// <param name="hintText"></param>
    /// <param name="progressTime"></param>
    public long ShowBottomProgress(string hintText, float progressTime)
    {
        return OverworldHUDPanel.Instance?.ShowBottomProgress(hintText, progressTime) ?? 0;
    }

    public void TryCancelButtomProgress(long showId)
    {
        OverworldHUDPanel.Instance?.TryCancelProgressComplete(showId);
    }

    public void ShowFakeFxEffect(string hintText, Vector2 logicPos)
    {
        var worldPos = MainGameManager.Instance.GetWorldPosFromLogicPos(logicPos);
        FakeHintTextManager.ShowWorld(hintText, worldPos);
    }

    public void ShowNoiseEffect(float intensity, Vector2 logicPos)
    {
        var worldPos = GetWorldPosFromLogicPos(logicPos);

        var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(worldPos, 1, "ChamEffect", null);
        if (fxCtx == null)
        {
            return;
        }

        var ring = fxCtx.EffectGo.GetComponent<MapNoiseRing>();
        ring.transform.position = worldPos;
        ring.gameObject.SetActive(true);
        ring.Play(Mathf.Clamp01(intensity), worldPos);
        ring.autoDestroy = false;
    }

    public void ShowClickkkWindow(string windowType, Vector2 showPos, float duration)
    {
        ShowClickkkUI.Instance.OpenClickkkHint(windowType, showPos, duration);
    }

    public void CloseClickkkWindow(string windowType, bool isInterrupt)
    {
        ShowClickkkUI.Instance.CloseClickkkWindow(windowType, isInterrupt);
    }


    public void ShowPauseCloseupWindow(string showName, float duration)
    {
        PauseCloseupWindow.Show(showName, duration);
    }

    public void DoDeepZhaquSmallGame(long targetUnitId, object extraParam)
    {
        LogicTime.ReleasePause("deep");

        var retPanel = UIManager.Instance.ShowPanel("DeepZhaQuMiniGame") as DeepZhaQuMiniGamePanel;
        if(retPanel != null)
        {
            retPanel.InitializeGame(targetUnitId, 0.2f, 4f);
        }
    }

    public void OnSmallGameFinish(long targetUnitId, bool success, object resultInfo)
    {
        LogicTime.ReleasePause("deep");
        if(success)
        {
            Debug.Log("OnSmallGameFinish " + targetUnitId + " success.");

            var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(targetUnitId);
            if (entity != null && entity is BaseUnitLogicEntity unitEntity)
            {
                unitEntity.ApplyResourceChange(AttrIdConsts.DeepZhaChance, -1, true, FightStruct.EDmgFlag.None, null);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(0.3f, 0.3f), true, unitEntity.Pos);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.3f, 0.1f), true, unitEntity.Pos);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.1f, 0.6f), true, unitEntity.Pos);

                if(UnityEngine.Random.Range(0, 10000) < 2000)
                {
                    Debug.Log("OnSmallGameFinish deep zha create dig.");
                    gameLogicManager.AreaManager.CreateOneDig(entity.Pos, "dig_01", 100);
                }
            }
        }
        else
        {

        }

        UIManager.Instance.HidePanel("DeepZhaQuMiniGame");
    }

    private bool isSwitchingEncounter = false;

    public void EnterEncounter(int battleId, string battleReason, bool isDefeatMode = false)
    {
        if(isSwitchingEncounter)
        {
            return;
        }

        isSwitchingEncounter = false;

        _ = InnerEnterEncounter(battleId, battleReason, isDefeatMode).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext()); ;
    }

    public void QuitEncounter()
    {
        if (isSwitchingEncounter)
        {
            return;
        }


        isSwitchingEncounter = true;
        _ = InnerQuitEncounter().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
            }
            isSwitchingEncounter = false;
        }, TaskScheduler.FromCurrentSynchronizationContext()); ;
    }


    protected async Task InnerEnterEncounter(int battleId, string battleReason, bool isDefeatMode = false)
    {
        UIManager.Instance.ShowLoading("good");

        LogicTime.RequestPause("encounter");

        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);

        EncounterBattleService.BattleContext ctx = new();

        
        ctx.BattleId = battleId;
        ctx.BattleReason = battleReason;
        ctx.IsDefeatMode = isDefeatMode;

        await EncounterBattleLoader.LoadBattleAsync(ctx);


        UIManager.Instance.HideLoading();
    }

    protected async Task InnerQuitEncounter()
    {
        UIManager.Instance.ShowLoading("good");


        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);

        await EncounterBattleLoader.UnloadBattleAsync();

        LogicTime.ReleasePause("encounter");

        UIManager.Instance.HideLoading();

        gameLogicManager.OnBattleEnd(EncounterBattleService.Instance.LastResult);
    }

    public int ShowRangeWarnEffect(FightStruct.Shape shape, Vector2 centerPos, Vector2 dir, float duration, Vector2 offset)
    {
        switch(shape.Type)
        {
            case FightStruct.EShapeType.Circle:
                {
                    return SceneRangeWarnManager.ShowSceneWarnRangeCircle(centerPos, dir, shape.Radius, duration, offset);
                }
                break;
            case FightStruct.EShapeType.Square:
                {
                    return SceneRangeWarnManager.ShowSceneWarnRangeRect(centerPos, dir, shape.Width, shape.Length, duration, offset);
                }
                break;
        }
        return 0;
    }


    public void UpdateRangeWarnEffect(int eId, Vector2 pos, Vector2 dir)
    {
        SceneRangeWarnManager.UpdateSceneWarnRangeRect(eId, pos, dir);
    }


    public void DestroySceneFxEffect(int effectId)
    {
        MapSceneEffectManager.Instance.ForceDestroy(effectId);
    }


    /// <summary>
    /// 特殊移动
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="fromPos"></param>
    /// <param name="duration"></param>
    public void DoPlayerSpecialMove(Vector2 targetPos, Vector2 fromPos, float duration, Action onCompelete = null)
    {
        if(playerScenePresenter != null)
        {
            var ctx = MapSceneEffectManager.Instance.ShowSceneEffect(fromPos, duration + 1f, "PlayerSpecialMove", null);

            var effectGo = ctx.EffectGo.GetComponent<PlayerGhostMoveFxCtrl>();
            effectGo.playerSR = playerScenePresenter.transform.Find("view").Find("agent").GetComponentInChildren<SpriteRenderer>();
            effectGo.PlayMoveFx(playerScenePresenter.transform, targetPos, () => { onCompelete?.Invoke(); }, () => { });
        }
    }

    /// <summary>
    /// 等待击败战斗
    /// </summary>
    public void WaitingIntoDefeatedBattle()
    {
        _ = AsyncPrepareDefeatedBattle().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
            }
            isSwitchingEncounter = false;
        }, TaskScheduler.FromCurrentSynchronizationContext()); ;
    }

    protected async Task AsyncPrepareDefeatedBattle()
    {
        await Task.Delay(5000);

        PlayDialog("defeated_01");
    }

    private const string SAVE_FILE_NAME = "mysave.json";

    // --- 保存流程 ---
    public async Task OnSaveClicked()
    {
        if (SaveSystem.IsBusy) return;


        // 2. [主线程] 采集数据 (Capture State)
        // 必须在主线程做，因为后台线程不能访问 transform.position 等 Unity API
        SaveData dataToSave = new SaveData();

        // 填充元数据
        dataToSave.Meta.SaveTime = System.DateTime.Now.ToString();
        dataToSave.Meta.Version = Application.version;

        // 填充玩家数据
        //dataToSave.Player.Level = this.PlayerLevel;
        //dataToSave.Player.CurrentHP = this.PlayerHP;
        //dataToSave.Player.MaxHP = 100f;

        // 填充位置数据 (转为纯 float 数组)
        //Vector3 pos = PlayerTransform.position;
        //Vector3 rot = PlayerTransform.eulerAngles;
        //dataToSave.World.Position = new float[] { pos.x, pos.y, pos.z };
        //dataToSave.World.Rotation = new float[] { rot.x, rot.y, rot.z };
        //dataToSave.World.SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // 填充背包 (模拟数据)
        dataToSave.Inventory.Add(new InventoryItemData { ItemID = "Sword_01", Amount = 1 });

        // 3. [后台线程] 执行异步保存
        // 此时 UI 会继续渲染，不会卡顿
        await SaveSystem.SaveAsync(SAVE_FILE_NAME, dataToSave);

        Debug.Log("UI: 保存流程结束");
    }

    /// <summary>
    ///  将读出来的数据应用到游戏对象上
    /// </summary>
    /// <param name="data"></param>
    private void ApplyDataToGame(SaveData data)
    {
        Debug.Log($"ApplyDataToGame finish, 时间: {data.Meta.SaveTime}");
    }


    public void PlayDialog(string dialogId, long? srcEntityId = null)
    {
        var dialogAsset = Resources.Load<TextAsset>($"Dialogue/output/{dialogId}");
        if(dialogAsset == null)
        {
            Debug.Log("PlayerDialog not found dialog " + dialogId);
            return;
        }

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        var dialogData = JsonConvert.DeserializeObject<DialogueData>(dialogAsset.text, settings);

        var dialogPanel = UIManager.Instance.ShowPanel("DialoguePanel") as DialogueUI;

        var runtime = new DialogueRuntime
        {
            ui = dialogPanel,
            driver = dialoguePlayer.GetComponent<DialogueTimeDriver>(),
            JumpTo = label => dialoguePlayer.JumpToStep(label),
            SrcEntityId = srcEntityId,
        };

        dialoguePlayer.ui = dialogPanel;
        dialoguePlayer.PlayFromData(dialogData, runtime, () =>
        {
            UIManager.Instance.HidePanel("DialoguePanel");
        });

        if(srcEntityId != null)
        {
            var entity = gameLogicManager.GetLogicEntity(srcEntityId.Value);
            if(entity != null && entity is BaseUnitLogicEntity unitEntity)
            {
                unitEntity.RegisterGaze("Dialog", gameLogicManager.playerLogicEntity.Id, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Interact);
            }
        }
    }

    public void StartLoot(ILootableObj lootObj)
    {
        UIOrchestrator.Instance.TryEnterLootDetailMode(lootObj);
    }

    public void StartHitStop(float duration)
    {
        HitStopManager.Instance.TriggerHitStop(duration);
    }
}


public class UnityNavProvider : INavProvider
{

    public bool TryBuildPath(Vector3 start, Vector3 destination, out NavPath path)
    {
        NavMeshPath nmPath = new NavMeshPath();

        int walkable = NavMesh.GetAreaFromName("Walkable");
        int mask = 1 << walkable; // 或组合多个区域

        var ret1 = NavMesh.SamplePosition(start, out var h1, 2f, mask);
        var ret2 = NavMesh.SamplePosition(destination, out var h2, 2f, mask);

        bool ok = NavMesh.CalculatePath(h1.position, h2.position, mask, nmPath);
        path = new NavPath { 
            Waypoints = new Vector2[nmPath.corners.Length],
        };

        for(int i=0;i< nmPath.corners.Length;i++)
        {
            path.Waypoints[i] = nmPath.corners[i];
        }
        return ok && nmPath.corners.Length > 0;
    }

    public bool TryGetFollowPoint(ILogicEntity target, float predictionSeconds, Vector2 offset, out Vector3 followPoint)
    {
        // 由上层维护一个查找表：EntityId => Transform
        if (target == null)
        {
            followPoint = default;
            return false;
        }



        // 简单预测：目标位置 + 速度 * 预测时间（需上层提供速度）
        //var v = EntityLocator.FindVelocity(targetId);

        Vector2 v = Vector2.zero;

        followPoint = target.Pos + offset + v * predictionSeconds;
        return true;
    }

    public bool TryReplan(Vector3 current, Vector3 goal, out NavPath path)
    {
        return TryBuildPath(current, goal, out path);
    }

    public bool Linecast(Vector3 from, Vector3 to, out Vector3 hitPoint)
    {
        // 可用NavMesh.Raycast 或 Physics.Raycast
        NavMeshHit hit;
        bool hitNav = NavMesh.Raycast(from, to, out hit, NavMesh.AllAreas);
        hitPoint = hit.position;
        return hitNav;
    }

    public Vector3? GetClosestValidPos(Vector3 pos)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
        {
            return pos;
        }
        return null;
    }
}