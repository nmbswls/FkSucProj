using Cinemachine;
using Map.Entity;
using Map.Logic;
using Map.Scene;
using My.Map;
using My.Map.Entity;
using My.Map.Hunting;
using My.Map.Logic;
using My.Input;
using My.Map.Scene;
using My.Saving;
using My.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using My.Map.View;

namespace My
{
    public enum GameStartSaveSource
    {
        NewGame,
        UserPersistentFile,
        BundledTestSave,
    }

    public partial class MainGameManager : MonoBehaviour, ISceneAbilityViewer
    {
        public static float OrderFactor = 100f;

        public static MainGameManager Instance;

        public bool Initialized { get; private set; }

        [Header("层")]
        public Transform SceneEffectLayer;

        public Transform AmbientSpiritLayer;

        AmbientSpiritVisualManager _ambientSpiritVisuals;

        public PlayerScenePresenter playerScenePresenter { get; set; }

        public SceneInteractSystem interactSystem;

        public HuntingModeManager huntingModeManager;

        public DefaultSceneVisionSenser2D VisionSenser2D;

        public WorldAreaManager WorldAreaManager;

        public MapSceneFadeAlphaManager SceneFadeManager;

        public MapSceneRangeWarnManager SceneRangeWarnManager;

        public HitStopManager HitStopManager;

        [Header("效果")]
        public PlayerRumorTextSpawner RumorTextSpawner;
        public CinemachineImpulseSource VcamInpulseSource;
        public PostProcessVignette postProcessVignette;

        public MapSceneDropManager sceneDropManager;

        public MapFovMeshGenerator FovGenerator;

        public LogicTimeManager TimerManager;

        public GameLogicManager gameLogicManager;

        public QuickPlayerInputBinder inputBinder;

        public CameraFollow CameraCtrl;
        public CinemachineVirtualCamera MainMapVCam;
        public CinemachineBrain CineBrain;

        public UnityNavProvider NavProvider;

        public DialoguePlayer dialoguePlayer;

        public SceneAOIManager AOIManager;

        public SceneGroundLiquidStrampManager LiquidStrampManager;
        public SceneGroundLiquidChunkFieldManager LiquidChunkFieldManager;
        public SceneGroundMistStrampManager MistStrampManager;

        public Transform DynamicRoot;

        void Awake()
        {
            Instance = this;

            if (!SceneFadeManager)
            {
                SceneFadeManager = GetComponent<MapSceneFadeAlphaManager>();
            }

            VisionSenser2D = new();
            VisionSenser2D.ObstacleMask = 1 << LayerMask.NameToLayer("MapViewObc");

            interactSystem = new();

            huntingModeManager = GetComponent<HuntingModeManager>();
            if (huntingModeManager == null)
            {
                huntingModeManager = gameObject.AddComponent<HuntingModeManager>();
            }

            InitCameraSystems();

            NavProvider = new();

            VcamInpulseSource = GetComponent<CinemachineImpulseSource>();

            gameLogicManager = new();
            gameLogicManager.EventOnSwitchStageUpdate += HandleOnSwitchStageUpdate;
            gameLogicManager.EventOnLocalRoomTeleportRequested += OnLocalRoomTeleportFade;
            gameLogicManager.EventOnHardAreaClearStarting += OnHardAreaClearStarting;
            gameLogicManager.EventOnNextDayPeriod += HandleNextDayPeriod;

            EnsureAmbientSpiritLayer();
            _ambientSpiritVisuals = new AmbientSpiritVisualManager(gameLogicManager, AmbientSpiritLayer);
        }

        void EnsureAmbientSpiritLayer()
        {
            if (AmbientSpiritLayer != null)
            {
                return;
            }

            var go = new GameObject("AmbientSpiritLayer");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            AmbientSpiritLayer = go.transform;
        }

        void OnDestroy()
        {
            if (gameLogicManager != null)
            {
                gameLogicManager.EventOnSwitchStageUpdate -= HandleOnSwitchStageUpdate;
                gameLogicManager.EventOnLocalRoomTeleportRequested -= OnLocalRoomTeleportFade;
                gameLogicManager.EventOnHardAreaClearStarting -= OnHardAreaClearStarting;
            }

            OnHardAreaClearStarting();
            EndCameraOverrideImmediate();
        }

        public async Task<bool> InitStartGame(string startParams, Action onComplete, GameStartSaveSource saveSource)
        {
            if (gameLogicManager.MainStage != GameLogicManager.EMainGameStage.UnInitialized)
            {
                return false;
            }

            SaveData loadedData = null;
            switch (saveSource)
            {
                case GameStartSaveSource.NewGame:
                    break;
                case GameStartSaveSource.UserPersistentFile:
                    loadedData = await SaveSystem.LoadAsync(SaveSystem.DefaultSaveFileName);
                    if (loadedData == null)
                    {
                        Debug.LogWarning("[MainGameManager] User save load failed or file missing/invalid.");
                        return false;
                    }
                    break;
                case GameStartSaveSource.BundledTestSave:
                    loadedData = SaveSystem.LoadBundledSaveFromResources(SaveSystem.BundledTestSaveResourcePath);
                    if (loadedData == null)
                    {
                        Debug.LogWarning("[MainGameManager] Bundled test save missing or invalid JSON.");
                        return false;
                    }
                    break;
                default:
                    Debug.LogError($"[MainGameManager] Unknown GameStartSaveSource: {saveSource}");
                    return false;
            }

            gameLogicManager.viewer = this;
            gameLogicManager.visionSenser = VisionSenser2D;
            gameLogicManager.navProvider = NavProvider;
            gameLogicManager.EventOnLogicEntitySpawned += OnLogicEntitySpawned;
            gameLogicManager.EventOnLogicEntityDespawned += OnLogicEntityDespawned;

            gameLogicManager.OnGameLogicInit(loadedData);

            sceneDropManager.OnGameInit();
            UIOrchestrator.Instance.InitGameLogicEventListener();

            LiquidChunkFieldManager?.RegisterEvents();
            MistStrampManager?.RegisterEvents();

            gameLogicManager.projectileHolder.EventOnLogicProjectileSpawn += (pInfo) =>
            {
                MapProjectileManager.Instance.Spawn(pInfo);
            };

            var playerMap = loadedData?.CurrentMapId ?? string.Empty;
            Vector2? savedPos = loadedData?.CurrentPos ?? Vector2.zero;

            if (string.IsNullOrEmpty(playerMap))
            {
                playerMap = "game_init";
                savedPos = new Vector2(-4.92f, -1.1f);
            }

            gameLogicManager.PreparePlayerSwitchArea(playerMap, true, targetPos: savedPos);
            onComplete?.Invoke();
            return true;
        }

        void Update()
        {
            if (gameLogicManager.MainStage != GameLogicManager.EMainGameStage.UnInitialized)
            {
                gameLogicManager.Tick(LogicTime.deltaTime);
                _ambientSpiritVisuals?.Tick(LogicTime.deltaTime);
            }

            if (gameLogicManager.MainStage == GameLogicManager.EMainGameStage.Running)
            {
                interactSystem.Tick(Time.deltaTime);
            }

            TryRefreshCameraShowStatus();

            RumorTextSpawner.IsActive = false;

            if (playerScenePresenter != null)
            {
                var glm = Instance.gameLogicManager;
                var mapCfg = glm?.AreaManager?.cacheMapOverlayCfg;
                if (glm != null && mapCfg != null && mapCfg.IsCivilArea
                    && !glm.PlayerHumanMode
                    && playerScenePresenter.PlayerEntity.IsExposed)
                {
                    postProcessVignette.SetDangerState(true);
                    FovGenerator.NeedMask = false;
                }
                else
                {
                    postProcessVignette.SetDangerState(false);
                    FovGenerator.NeedMask = true;
                }
            }
        }

        public bool IsMouseOnUIOrBlock()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            return false;
        }

        public Vector3 GetWorldPosFromLogicPos(Vector2 pos)
        {
            return MapLogicPosition.LogicToWorld(pos);
        }

        public Vector3 GetWorldPosFromLogicPos(ILogicEntity entity)
        {
            return MapLogicPosition.LogicToWorld(entity);
        }

        public Vector2 GetLogicPosFromWorldPos(Vector3 worldPos)
        {
            return MapLogicPosition.WorldToLogicPos(worldPos);
        }

        public void OnLogicEntitySpawned(ILogicEntity entity)
        {
            SceneAOIManager.Instance.RegisterEntity(entity, entity.Pos);
        }

        public void OnLogicEntityDespawned(ILogicEntity entity)
        {
            SceneAOIManager.Instance.UnregisterEntity(entity);
        }

        public Transform GetDynamicRoot()
        {
            return DynamicRoot;
        }

        public Transform GetWorldMapVariantRoot(string worldName)
        {
            var areaRoot = WorldAreaManager?.currentRoot;
            return areaRoot != null ? areaRoot.MapVariantRoot : null;
        }

        void HandleNextDayPeriod()
        {
            SecretBaseHudPanel.Instance?.RefreshUI();
        }
    }

    public class UnityNavProvider : INavProvider
    {
        static bool IsValidNavPoint(Vector3 point)
        {
            return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        }

        public bool TryBuildPath(Vector3 start, Vector3 destination, out NavPath path)
        {
            path = default;
            if (!IsValidNavPoint(start) || !IsValidNavPoint(destination))
            {
                return false;
            }

            NavMeshPath nmPath = new NavMeshPath();

            int walkable = NavMesh.GetAreaFromName("Walkable");
            int mask = 1 << walkable;

            if (!NavMesh.SamplePosition(start, out var h1, 2f, mask))
            {
                return false;
            }

            if (!NavMesh.SamplePosition(destination, out var h2, 2f, mask))
            {
                return false;
            }

            if (!IsValidNavPoint(h1.position) || !IsValidNavPoint(h2.position))
            {
                return false;
            }

            bool ok = NavMesh.CalculatePath(h1.position, h2.position, mask, nmPath);
            path = new NavPath
            {
                Waypoints = new Vector2[nmPath.corners.Length],
            };

            for (int i = 0; i < nmPath.corners.Length; i++)
            {
                path.Waypoints[i] = nmPath.corners[i];
            }

            return ok && nmPath.corners.Length > 0;
        }

        public bool TryGetFollowPoint(ILogicEntity target, float predictionSeconds, Vector2 offset, out Vector3 followPoint)
        {
            if (target == null)
            {
                followPoint = default;
                return false;
            }

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
}

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
    bool SimpleCanSee(Vector2 selftPos, Vector2 selfFace, Vector2 targetPos, float range, float fov);

    Vector2 ChoosePointAwayFromTarget(Vector2 orgPos, Vector2 centerPos, float awayDist);

    IEnumerable<ILogicEntity> OverlapBoxAllEntity(Vector2 orgPos, Vector2 dir, Vector2 size, EntityFilterParam? filter, float hitHeight = 0.3f, float heightTolerance = 0.2f);

    IEnumerable<ILogicEntity> OverlapCircleAllEntity(Vector2 orgPos, float radius, EntityFilterParam? filter, float atkHeight = 0.3f, float heightTolerance = 0.2f);

    bool CheckIsInAlertArea(Vector2 pos);

    void OverlapCheckDynamicObs(Vector2 orgPos, float radius, List<(Vector2, Vector2)> retList);
}
