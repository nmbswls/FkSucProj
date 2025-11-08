using Map.Entity;
using Map.Logic.Chunk;
using Map.Scene;
using System.Collections;
using System.Collections.Generic;
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

    private void Awake()
    {
        Instance = this;

        if(!SceneFadeManager)
        {
            SceneFadeManager = GetComponent<MapSceneFadeAlphaManager>();
        }

        VisionSenser2D = new();

        interactSystem = new();

        gameLogicManager = new();
        gameLogicManager.viewer = this;
        gameLogicManager.visionSenser = VisionSenser2D;
        gameLogicManager.EventOnLogicEntitySpawned += OnLogicEntitySpawned;
        gameLogicManager.EventOnLogicEntityDespawned += OnLogicEntityDespawned;


        gameLogicManager.EventOnPlayerEnterArea += OnPlayerEnterArea;

        gameLogicManager.OnGameInit();

        gameLogicManager.projectileHolder.EventOnLogicProjectileSpawn += (pInfo) =>
        {
            MapProjectileManager.Instance.Spawn(pInfo);
        };

        // 
    }

    public GameLogicManager gameLogicManager;

    public PlayerScenePresenter playerScenePresenter;

    public SceneInteractSystem interactSystem;

    public DefaultSceneVisionSenser2D VisionSenser2D;

    public MapGlobalNoiseEmitter mapGlobalNoiseEmitter;

    public MapSceneFadeAlphaManager SceneFadeManager;

    public MapSceneDropManager sceneDropManager;

    void Start()
    {
        MainUIManager.Instance.InitGameLogicEventListener();
    }

    void Update()
    {
        interactSystem.Tick(Time.deltaTime);
        gameLogicManager.Tick(Time.time, Time.deltaTime);

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
            gameLogicManager.AreaManager.RegisterEntityRecord(new LogicEntityRecord()
            {



            });
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


    public void OnPlayerEnterArea(string? oldArea, string? newArea)
    {
        var areaInfo = Resources.Load<WorldAreaInfo>("1");

        gameLogicManager.playerLogicEntity.SetPosition(new Vector2(3, 3));
        if (playerScenePresenter != null)
        {
            playerScenePresenter.Bind(gameLogicManager.playerLogicEntity);
        }

        if (newArea != null)
            WorldAreaManager.Instance.LoadWorld(areaInfo, setActive: true);
        else
            Debug.LogWarning("WorldBootstrap: initialGroup not set.");
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
        return MainUIManager.Instance.ShowBottomProgress(hintText, progressTime);
    }

    public void TryCancelButtomProgress(long showId)
    {
        MainUIManager.Instance.TryCancelProgressComplete(showId);
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
}
