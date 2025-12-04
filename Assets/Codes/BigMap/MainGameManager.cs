using Map.Encounter;
using Map.Entity;
using Map.Logic;
using Map.Scene;
using Map.Scene.UI;
using Map.SmallGame.Zha;
using My;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.Fight;
using My.Map.Logic;
using My.Map.Scene;
using My.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static Map.Encounter.EncounterBattleService;
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
}


public class MainGameManager : MonoBehaviour, ISceneAbilityViewer
{
    public static float OrderFactor = 100f;

    public static MainGameManager Instance;

    public Transform SceneEffectLayer;



    public PlayerScenePresenter playerScenePresenter;

    public SceneInteractSystem interactSystem;

    public DefaultSceneVisionSenser2D VisionSenser2D;

    public MapGlobalNoiseEmitter mapGlobalNoiseEmitter;

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
        gameLogicManager = new();
        gameLogicManager.viewer = this;
        gameLogicManager.visionSenser = VisionSenser2D;
        gameLogicManager.navProvider = NavProvider;
        gameLogicManager.EventOnLogicEntitySpawned += OnLogicEntitySpawned;
        gameLogicManager.EventOnLogicEntityDespawned += OnLogicEntityDespawned;

        gameLogicManager.EventOnPlayerSwitchArea += OnPlayerSwitchArea;

        gameLogicManager.OnGameInit();

        gameLogicManager.projectileHolder.EventOnLogicProjectileSpawn += (pInfo) =>
        {
            MapProjectileManager.Instance.Spawn(pInfo);
        };

        UIManager.Instance.ShowLoading("starting");
        // 
        await LoadGameMain("home");

        UIManager.Instance.HideLoading();
    }


    protected async Task LoadGameMain(string initMap)
    {
        var areaInfo = Resources.Load<WorldAreaInfo>($"Area/{initMap}");

        // 逻辑上将玩家放入场景
        await gameLogicManager.PlayerEnterArea(initMap);

        //if(playerScenePresenter == null)
        //{
        //    var playerGo = Resources.Load<GameObject>("Prefab/Presentations/FakePlayer");
        //    var newGo = GameObject.Instantiate(playerGo, transform);
        //    playerScenePresenter = newGo.GetComponent<PlayerScenePresenter>();
        //}
        
        bool loaded = false;
        WorldAreaManager.Instance.LoadWorld(areaInfo, onComplete: (w) => { loaded = true; });

        // 等待场景加载
        while(!loaded)
        {
            await Task.Yield();
        }

        await Task.Delay(500);

        //playerScenePresenter.Bind(gameLogicManager.playerLogicEntity);

        // 整理职责
        FovGenerator.OnAreaEnter();
        SceneAOIManager.Instance.InitArea(areaInfo.worldName);
        SceneFadeManager.OnEnterArea(WorldAreaManager.Instance.currentRoot.gameObject);

        UIOrchestrator.Instance.InitGameLogicEventListener();

        inputBinder.ApplyInputMode(QuickPlayerInputBinder.InputMode.Overworld);

        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld, null);


        if(HomeSceneManager.Instance != null)
        {
            HomeSceneManager.Instance.InitHomePlacements();
        }

        gameLogicManager.Initialized = true;
    }

    void Update()
    {
        if(gameLogicManager.Initialized)
        {
            gameLogicManager.Tick(LogicTime.deltaTime);
            interactSystem.Tick(LogicTime.deltaTime);

            

            if(UnityEngine.Input.GetKeyDown(KeyCode.L))
            {
                //// 本地化
                //if (locJson)
                //{
                //    Loc.LoadFromText(locJson.text);
                //}
                var txt = "[Step intro]\r\nCameraMove pos=0,0,-10 duration=0\r\nCameraZoom fov=60 duration=0\r\nShowPortrait slot=Left characterId=hero expressionId=default fade=0.25\r\nShowPortrait slot=Right characterId=companion expressionId=smile fade=0.25\r\n\r\nHero: 终于到了约定的地点。\r\nCompanion: 你比我想象的准时。\r\n\r\n[Choice]\r\n- 立刻出发 -> branch_go\r\n- 再收集些情报 -> branch_info \r\n\r\n[Step branch_go]\r\nGiveItem itemId=Herb amount=1\r\nCompanion: 好，那就现在行动！\r\nChangeExpression slot=Right expressionId=smile fade=0.2\r\nCameraZoom fov=50 duration=0.4\r\nPlaySE name=step_confirm\r\nHero: 跟紧我。\r\nJump label=ending\r\n\r\n[Step branch_info]\r\nHero: 谨慎总是没错的。先打听一下附近的情况。\r\nChangeExpression slot=Left expressionId=think fade=0.2\r\nCameraMove pos=-0.3,0,-10 duration=0.4\r\nPlaySE name=ui_select\r\nCompanion: 那我联系一下线人。\r\nJump label=ending\r\n\r\n[Step ending]\r\nHidePortrait slot=Right fade=0.25\r\nChangeExpression slot=Left expressionId=default fade=0.2\r\nHero: 准备完毕，出发。\r\nEnterEncounter\r\nPlaySE name=ui_close";


                var data = TxtDialogueScriptParser.Parse(txt, "intro_from_txt");

                var dialogPanel = UIManager.Instance.ShowPanel("DialoguePanel") as DialogueUI;

                var runtime = new DialogueRuntime
                {
                    ui = dialogPanel,
                    //cam = cam,
                    //audio = audio,
                    driver = dialoguePlayer.GetComponent<DialogueTimeDriver>(),
                    //Localize = Loc.Tr,
                    JumpTo = label => dialoguePlayer.JumpToLabel(label)
                };

                dialoguePlayer.ui = dialogPanel;
                dialoguePlayer.PlayFromData(data, runtime, () =>
                {
                    // do dialog finish events;
                    UIManager.Instance.HidePanel("DialoguePanel");
                });
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.P))
            {
                playerScenePresenter.PlayerEntity.ApplyKnockBack(Vector2.right, 2f);
            }
        }

        if(!string.IsNullOrEmpty(SwitchAreaIntent))
        {
            _ = AsyncSwitchArea(SwitchAreaIntent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            SwitchAreaIntent = null;

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

    public string SwitchAreaIntent;

    public void OnPlayerSwitchArea(string? oldArea, string newArea)
    {

        SwitchAreaIntent = newArea;

    }

    public async Task AsyncSwitchArea(string newArea)
    {
        gameLogicManager.Initialized = false;

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

        await LoadGameMain(newArea);

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
        mapGlobalNoiseEmitter.EmitNoiseFixed(intensity, worldPos);
    }

    public void ShowClickkkWindow(string windowType, Vector2 showPos, float duration)
    {
        ShowClickkkUI.Instance.OpenClickkkHint(windowType, showPos, duration);
    }

    public void CloseClickkkWindow(string windowType, bool isInterrupt)
    {
        ShowClickkkUI.Instance.CloseClickkkWindow(windowType, isInterrupt);
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
                    gameLogicManager.AreaManager.CreateOneDig(entity.Pos, "dig_01", "dig_treasure");
                }
            }
        }
        else
        {

        }

        UIManager.Instance.HidePanel("DeepZhaQuMiniGame");
    }

    private bool isSwitchingEncounter = false;

    public void EnterEncounter()
    {
        if(isSwitchingEncounter)
        {
            return;
        }


        isSwitchingEncounter = true;
        _ = InnerEnterEncounter().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
            }
            isSwitchingEncounter = false;
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


    protected async Task InnerEnterEncounter()
    {
        UIManager.Instance.ShowLoading("good");

        LogicTime.RequestPause("encounter");

        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);

        BattleContext ctx = new();
        ctx.BattleId = 1;
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

}