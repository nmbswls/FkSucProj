using Cinemachine;
using DamageNumbersPro;
using Map.Entity;
using Map.Logic;
using Map.Scene;
using Map.Scene.UI;
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
using My.Map.SmallGame.Zha;
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
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;


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

        public PlayerScenePresenter playerScenePresenter { get; set; }

        public SceneInteractSystem interactSystem;

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


        private void Awake()
        {
            Instance = this;

            if (!SceneFadeManager)
            {
                SceneFadeManager = GetComponent<MapSceneFadeAlphaManager>();
            }

            VisionSenser2D = new();
            VisionSenser2D.ObstacleMask = 1 << LayerMask.NameToLayer("MapViewObc");

            interactSystem = new();

            NavProvider = new();

            VcamInpulseSource = GetComponent<CinemachineImpulseSource>();

            gameLogicManager = new();
            gameLogicManager.EventOnSwitchStageUpdate += HandleOnSwitchStageUpdate;
            gameLogicManager.EventOnLocalRoomTeleportRequested += OnLocalRoomTeleportFade;
            gameLogicManager.EventOnHardAreaClearStarting += OnHardAreaClearStarting;
            gameLogicManager.EventOnNextDayPeriod += HandleNextDayPeriod;

            //Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnDestroy()
        {
            if (gameLogicManager != null)
            {
                gameLogicManager.EventOnSwitchStageUpdate -= HandleOnSwitchStageUpdate;
                gameLogicManager.EventOnLocalRoomTeleportRequested -= OnLocalRoomTeleportFade;
                gameLogicManager.EventOnHardAreaClearStarting -= OnHardAreaClearStarting;
            }

            OnHardAreaClearStarting();
        }

        public async Task<bool> InitStartGame(string startParams, Action? onComplete, GameStartSaveSource saveSource)
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
            if(gameLogicManager.MainStage != GameLogicManager.EMainGameStage.UnInitialized)
            {
                var a = Time.deltaTime;
                gameLogicManager.Tick(LogicTime.deltaTime);
            }

            if (gameLogicManager.MainStage == GameLogicManager.EMainGameStage.Running)
            {
                interactSystem.Tick(Time.deltaTime);
            }


            if (UnityEngine.Input.GetKeyDown(KeyCode.V))
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

                ShowDamageNumber(playerScenePresenter.PivotHeader.position, "?", playerScenePresenter.PivotHeader);

                //MapSpeechBubbleManager.Instance.Say(playerScenePresenter, "怎么会？");
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

            

            if (playerScenePresenter != null)
            {
                if (playerScenePresenter.PlayerEntity.IsExposed)
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

        public void ShowSceneFxEffect(string effectName, Vector2 pos, Vector2 dir)
        {
            var worldPos = MainGameManager.Instance.GetWorldPosFromLogicPos(pos);

            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(worldPos, 1, effectName, null, dir: dir);
            if (fxCtx == null)
            {
                Debug.LogError("ShowSceneFxEffect faaa " + effectName);
                return;
            }
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
            if (retPanel != null)
            {
                retPanel.InitializeGame(targetUnitId, 0.2f, 4f);
            }
        }

        public void OnSmallGameFinish(long targetUnitId, bool success, object resultInfo)
        {
            LogicTime.ReleasePause("deep");
            if (success)
            {
                Debug.Log("OnSmallGameFinish " + targetUnitId + " success.");

                var entity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(targetUnitId);
                if (entity != null && entity is BaseUnitLogicEntity unitEntity)
                {
                    unitEntity.ApplyResourceChange(AttrIdConsts.DeepZhaChance, -1, true, FightStruct.EDmgFlag.None, null);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(0.3f, 0.3f), true, unitEntity.Pos);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.3f, 0.1f), true, unitEntity.Pos);
                    gameLogicManager.globalDropCollection.CreateDrop("jinghua", 3, unitEntity.Pos + new Vector2(-0.1f, 0.6f), true, unitEntity.Pos);

                    if (UnityEngine.Random.Range(0, 10000) < 2000)
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
            if (isSwitchingEncounter)
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
            UIManager.Instance.FadeHideBlack(1.5f);

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
            switch (shape.Type)
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
            if (playerScenePresenter != null)
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

        // --- 保存流程：主线程采集，字段与 SaveData 注释一致 ---
        SaveData BuildRuntimeSaveSnapshot()
        {
            var data = new SaveData();
            data.Meta.SaveTime = System.DateTime.Now.ToString("o");
            data.Meta.Version = Application.version;

            // 占位：待与真实养成/战斗数值对接
            // data.PlayerData.Level = ...
            data.Inventory.Add(new InventoryItemData { ItemID = "Sword_01", Amount = 1 });

            if (gameLogicManager?.AreaManager != null &&
                !string.IsNullOrEmpty(gameLogicManager.AreaManager.MapName))
            {
                data.CurrentMapId = gameLogicManager.AreaManager.MapName;
            }

            if (gameLogicManager?.playerLogicEntity != null)
            {
                data.CurrentPos = gameLogicManager.playerLogicEntity.Pos;
            }

            gameLogicManager?.AppendRuntimePersistenceToSaveData(data);

            SaveData.EnsureHydrated(data);

            return data;
        }

        public async Task OnSaveClicked()
        {
            if (SaveSystem.IsBusy) return;

            SaveData dataToSave = BuildRuntimeSaveSnapshot();
            await SaveSystem.SaveAsync(SaveSystem.DefaultSaveFileName, dataToSave);

            Debug.Log("UI: save flow finished");
        }



        public bool PlayDialog(string dialogId, long? srcEntityId = null, bool pause = false, System.Action onDialogEnd = null)
        {

            var dialogMetaInfo = CfgMgr.Cfgs.TbDialogMetaInfo.Get(dialogId);
            if (dialogMetaInfo == null)
            {
                Debug.LogError($"PlayDialog dialog not found {dialogId}.");
                return false;
            }

            var dialogAsset = Resources.Load<TextAsset>($"Dialogue/output/{dialogMetaInfo.JsonDataName}");
            if (dialogAsset == null)
            {
                Debug.LogError("PlayerDialog not found dialog " + dialogId + "asset " + dialogMetaInfo.JsonDataName);
                return false;
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
            dialoguePlayer.PlayFromData(dialogMetaInfo, dialogData, runtime, () =>
            {
                LogicTime.ClearPauseSource("Dialog");
                UIManager.Instance.HidePanel("DialoguePanel");
                onDialogEnd?.Invoke();
            });

            if (pause)
            {
                LogicTime.RequestPause("Dialog");
            }

            if (srcEntityId != null)
            {
                var entity = gameLogicManager.GetLogicEntity(srcEntityId.Value);
                if (entity != null && entity is BaseUnitLogicEntity unitEntity)
                {
                    //unitEntity.RegisterGaze("Dialog", gameLogicManager.playerLogicEntity.Id, Vector2.zero, BaseUnitLogicEntity.EGazePriority.Interact);
                }
            }

            return true;
        }

        public void StartLoot(ILootableObj lootObj)
        {
            UIOrchestrator.Instance.TryEnterLootDetailMode(lootObj);
        }

        public void StartHitStop(float duration)
        {
            HitStopManager.Instance.TriggerHitStop(duration);

            VcamInpulseSource.GenerateImpulse(0.06f);
        }

        public void ShowMapSpeachBubble(long entityId, string content, float duration, int priority = 1, float extraInteval = 0)
        {
            var pres = SceneAOIManager.Instance.GetActivePresentation(entityId);
            if (pres == null) return;
            MapSpeechBubbleManager.Instance.Say(pres, content, duration, priority);
        }

        /// <summary>
        /// 显示跳字
        /// </summary>
        /// <param name="worldPos"></param>
        /// <param name="content"></param>
        /// <param name="bindTrans"></param>
        public void ShowDamageNumber(Vector2 worldPos, string content, Transform? bindTrans = null)
        {
            var dmgResource = SimpleResManager.Load<DamageNumber>($"SceneEffect/JumpText/Damage_01");
            if (bindTrans == null)
            {
                dmgResource.Spawn(worldPos, content);
            }
            else
            {
                dmgResource.Spawn(worldPos, content, bindTrans);
            }
        }

        private void HandleNextDayPeriod()
        {
            UIManager.Instance.DoFadeInAndOut(0.25f, 0.25f, ()=>
            {
                if(gameLogicManager.DayPeriodLeft == 0)
                {
                    SceneMaskPanel.Instance?.ShowDawn();
                }
                else
                {
                    SceneMaskPanel.Instance?.ShowDayTime();
                }
            }, null, null);
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


