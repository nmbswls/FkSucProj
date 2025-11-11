using Map.Entity;
using Map.Logic;
using Map.Scene;
using Map.Scene.UI;
using Map.SmallGame.Zha;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using My.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;


public enum ECampFilterType
{ 
    All,
    NotSelf,
    OnlyCityzon,
}


public struct EntityFilterParam
{
    public EEntityType FilterType;
    public List<EEntityType> FilterParamLists;

    public ECampFilterType CampFilterType;
    public EEntityCampId SelfCampId;

    public bool NeedEnmity;
    public bool NeedFriendly;
}

public interface IVisionSenser2D
{
    bool CanSee(Vector2 selftPos, Vector2 selfFace, Vector2 targetPos, float range, float fov);

    Vector2 ChoosePointAwayFromTarget(Vector2 orgPos, Vector2 centerPos, float awayDist);

    List<ILogicEntity> OverlapBoxAllEntity(Vector2 orgPos, Vector2 dir, Vector2 size, EntityFilterParam? filter);

    List<ILogicEntity> OverlapCircleAllEntity(Vector2 orgPos, float radius, EntityFilterParam? filter);
}


public class MainGameManager : MonoBehaviour, ISceneAbilityViewer
{
    public static MainGameManager Instance;


    public PlayerScenePresenter playerScenePresenter;

    public SceneInteractSystem interactSystem;

    public DefaultSceneVisionSenser2D VisionSenser2D;

    public MapGlobalNoiseEmitter mapGlobalNoiseEmitter;

    public MapSceneFadeAlphaManager SceneFadeManager;

    public MapSceneDropManager sceneDropManager;

    public MapFovMeshGenerator FovGenerator;

    public LogicTimeManager TimerManager;

    public GameLogicManager gameLogicManager;

    public QuickPlayerInputBinder inputBinder;

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
    }

    public void InitStartGame(string startParams, Action? onComplete)
    {
        gameLogicManager = new();
        gameLogicManager.viewer = this;
        gameLogicManager.visionSenser = VisionSenser2D;
        gameLogicManager.EventOnLogicEntitySpawned += OnLogicEntitySpawned;
        gameLogicManager.EventOnLogicEntityDespawned += OnLogicEntityDespawned;

        gameLogicManager.EventOnPlayerSwitchArea += OnPlayerSwitchArea;

        gameLogicManager.OnGameInit();

        gameLogicManager.projectileHolder.EventOnLogicProjectileSpawn += (pInfo) =>
        {
            MapProjectileManager.Instance.Spawn(pInfo);
        };

        // 
        _ = LoadGameMain();
    }


    protected async Task LoadGameMain()
    {
        UIManager.Instance.ShowLoading("starting");

        string initMap = "1";
        var areaInfo = Resources.Load<WorldAreaInfo>(initMap);

        // 逻辑上将玩家放入场景
        await gameLogicManager.PlayerEnterArea(initMap);

        bool loaded = false;
        WorldAreaManager.Instance.LoadWorld(areaInfo, onComplete: (w) => { loaded = true; });

        // 等待场景加载
        while(!loaded)
        {
            await Task.Yield();
        }

        playerScenePresenter.Bind(gameLogicManager.playerLogicEntity);

        FovGenerator.OnAreaEnter();
        SceneAOIManager.Instance.InitArea(areaInfo.worldName);
        SceneFadeManager.OnEnterArea(WorldAreaManager.Instance.currentRoot.gameObject);

        UIOrchestrator.Instance.InitGameLogicEventListener();

        inputBinder.ApplyInputMode(QuickPlayerInputBinder.InputMode.Menu);

        await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld, null);

        gameLogicManager.Initialized = true;
        UIManager.Instance.HideLoading();
    }

    void Update()
    {
        interactSystem.Tick(LogicTime.deltaTime);
        gameLogicManager.Tick(LogicTime.deltaTime);

        if(!IsMouseOnUIOrBlock())
        {
            Vector3 playerScreenPos = Camera.main.WorldToScreenPoint(playerScenePresenter.transform.position);
            var castDir = (Input.mousePosition - playerScreenPos).normalized;

            if ((playerScreenPos - Input.mousePosition).magnitude < 1e-1)
            {
                return;
            }
            gameLogicManager.playerLogicEntity.FaceDir = castDir;
            if (Input.GetMouseButtonDown(0))
            {
                gameLogicManager.playerLogicEntity.PlayerAbilityController.TryShoot(castDir);
            }
            else if(Input.GetMouseButtonDown(1))
            {
                gameLogicManager.playerLogicEntity.PlayerAbilityController.TrySlash(castDir);
            }
        }

        if(Input.GetKeyDown(KeyCode.F))
        {
            //gameLogicManager.AreaManager.RegisterEntityRecord(new LogicEntityRecord()
            //{



            //});
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


    public void OnPlayerSwitchArea(string? oldArea, string? newArea)
    {
        //var areaInfo = Resources.Load<WorldAreaInfo>("1");

        //gameLogicManager.playerLogicEntity.SetPosition(new Vector2(3, 3));
        //if (playerScenePresenter != null)
        //{
        //    playerScenePresenter.Bind(gameLogicManager.playerLogicEntity);
        //}

        //if (newArea != null)
        //    WorldAreaManager.Instance.LoadWorld(areaInfo, setActive: true);
        //else
        //    Debug.LogWarning("WorldBootstrap: initialGroup not set.");


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
        DeepZhaQuSmallGameManager.Instance.InitializeGame(targetUnitId, 0.2f, 4f);
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
                unitEntity.ApplyResourceChange(AttrIdConsts.DeepZhaChance, -1, true, null);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(0.3f, 0.3f), true);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.3f, 0.1f), true);
                gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.1f, 0.6f), true);
            }
        }
        else
        {

        }
    }
}
