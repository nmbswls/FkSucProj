

using System;
using My.Config;
using My.Map.Entity;
using My.Map;
using System.Collections.Generic;
using My.Map.Logic;
using My.MapExport;
using UnityEngine;
using UnityEditor.SceneManagement;
using SuperScrollView;

namespace My
{

    public class SwitchAreaIntent
    {
        public string? OldAreaName;
        public string AreaName;
        public bool Reset;
        public LogicEntityRecord4Player SavedRecord;
        public bool Silent; // 是否静默

        public string? TargetPoint;
        public Vector2? TargetPos;

        public Action<int> OnComplete;
    }

    public partial class GameLogicManager
    {

        public SwitchAreaIntent? SwitchAreaIntent;

        public enum EMapSwitchStep
        {
            None,
            PreSwitch,
            Clearing,
            Loading,
            Loaded,
            Finished,
        }

        public EMapSwitchStep SwitchStep;
        

        public event Action<EMapSwitchStep> EventOnSwitchStageUpdate;

        /// <summary>
        /// 
        /// </summary>
        public void NextSwitchStep(EMapSwitchStep currStep)
        {
            if(MainStage != EMainGameStage.SwitchingMap)
            {
                Debug.LogError("NextSwitchStep called while MainStage is not SwitchingMap");
                return;
            }

            SwitchStep += 1;
            Debug.Log($"NextSwitchStep {SwitchStep}");

            if (SwitchStep == EMapSwitchStep.Clearing)
            {
                ClearPreviousArea();
            }
            else if (SwitchStep == EMapSwitchStep.Loading)
            {
                PrepareNewArea();
            }
            else if (SwitchStep == EMapSwitchStep.Loaded)
            {
                PostNewAreaLoaded();
            }

            else if (SwitchStep == EMapSwitchStep.Finished)
            {
                SwitchAreaIntent = null;
                SwitchStep = EMapSwitchStep.None;
                MainStage = EMainGameStage.Running;

                Debug.Log("LoadGameMain finished");
            }

            EventOnSwitchStageUpdate?.Invoke(SwitchStep);
        }

        /// <summary>
        /// 开始地图切换工作流
        /// </summary>
        private void StartMapSwitchingFlow()
        {
            MainStage = EMainGameStage.SwitchingMap;
            SwitchStep = EMapSwitchStep.None;
            NextSwitchStep(SwitchStep);
        }

        /// <summary>
        /// 前
        /// </summary>
        public void ClearPreviousArea()
        {
            SecretBase.Shutdown();

            EventOnHardAreaClearStarting?.Invoke();

            DelayedEffectQueue.Clear();
            AreaManager.CleanArea();
            globalBuffManager.Clear();
            globalDropCollection.Clear();

            // 清理引用
            playerLogicEntity = null;
        }

        /// <summary>
        /// 对新场景进行逻辑准备
        /// </summary>
        /// <param name="areaName"></param>
        private void PrepareNewArea()
        {
            var mapCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(SwitchAreaIntent.AreaName);

            AreaManager.InitilizeMap(SwitchAreaIntent.AreaName);
            MapMicroPlot?.RebuildForCurrentMap();

            if (mapCfg != null && mapCfg.IsHome && My.Home.LegacyHomeBuildFeature.Enabled)
            {
                homeDataManager.OnPlayerEnterHome();
            }


            var bornPos = new List<NamedPoint>();
            var ps = AreaManager.cacheDatabase.NamedPoints;
            foreach (var p in ps)
            {
                if (p.PointType == ENamedPointType.BornPos)
                {
                    bornPos.Add(p);
                }
            }


            Vector2? pos = null;
            if (SwitchAreaIntent.TargetPos != null)
            {
                pos = SwitchAreaIntent.TargetPos.Value;
            }
            else if (!string.IsNullOrEmpty(SwitchAreaIntent.TargetPoint))
            {
                var namedP = AreaManager.cacheDatabase.FindNamedPointByName(SwitchAreaIntent.TargetPoint);
                if(namedP == null)
                {
                    namedP = AreaManager.cacheDatabase.FindNamedPointByName("default");
                }
                pos = namedP?.Position ?? null;
            }

            if (pos == null)
            {
                if (bornPos.Count != 0)
                {
                    var randIdx = UnityEngine.Random.Range(0, bornPos.Count);
                    pos = bornPos[randIdx].Position;
                }
            }

            if (pos == null)
            {
                Debug.LogError("switch area pos lose");
                pos = Vector2.zero;
            }


            LogicEntityRecord4Player playerRecord;
            if (SwitchAreaIntent.Reset)
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
                playerRecord = SwitchAreaIntent.SavedRecord;
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

            // 强制执行一次刷新
            AreaManager.ForceCheckRefreshInfos();

            shopDataManager.RefreshOnNightStart();

            // 清空延迟信息
            DelayedEffectQueue.Clear();
        }


        public void PostNewAreaLoaded()
        {
            RefreshPlayerMagicClothesAndExposeForCurrentMode();
            RumorIntelSpawn?.ApplyPurchasedRumorsOnMapLoaded();
            OnSecretBasePostAreaLoaded();
        }

        // 按当前 PlayerHumanMode 与地图类型同步魔力衣装运行时与人类形态屏蔽（暴露/衣装上限等）
        public void RefreshPlayerMagicClothesAndExposeForCurrentMode()
        {
            var player = playerLogicEntity;
            if (player == null || playerDataManager == null)
            {
                return;
            }

            if (PlayerHumanMode)
            {
                player.ApplyHumanModeShieldingState();
                NotifyHumanQuickBarStateChanged();
                return;
            }

            var magic = playerDataManager.MagicClothes;
            var cfg = AreaManager.cacheMapCfg;
            if (cfg != null && cfg.IsCivilArea)
            {
                magic.OnStealthMapPlayerInitialized(player);
                return;
            }

            if (magic.IsLockedWithSelection)
            {
                magic.ApplyToPlayer(player);
            }

            NotifyHumanQuickBarStateChanged();
        }

        // 玩家同地图房间传送：仅发事件 + Commit 回调，不直接调 MainGameManager / UI
        public event Action<LocalRoomTeleportRequest> EventOnLocalRoomTeleportRequested;

        bool _localRoomTeleportLock;

        public bool IsLocalRoomTeleportLocked => _localRoomTeleportLock;

        public void ReleaseLocalRoomTeleportLock()
        {
            _localRoomTeleportLock = false;
        }

        public void RequestLocalRoomTeleport(Vector2 targetWorldPos, Action onAfterTeleport = null)
        {
            if (_localRoomTeleportLock)
                return;

            var player = playerLogicEntity;
            if (player == null)
                return;

            var from = player.Pos;
            void ApplyTeleport()
            {
                player.TeleportTo(targetWorldPos);
                onAfterTeleport?.Invoke();
            }

            var req = new LocalRoomTeleportRequest(from, targetWorldPos, ApplyTeleport);
            if (EventOnLocalRoomTeleportRequested == null)
            {
                _localRoomTeleportLock = true;
                try
                {
                    ApplyTeleport();
                }
                finally
                {
                    _localRoomTeleportLock = false;
                }
                return;
            }

            _localRoomTeleportLock = true;
            EventOnLocalRoomTeleportRequested.Invoke(req);
        }

    }

    // 本地房间传送：表现层在遮罩全黑后调用 CommitTeleport 再渐亮
    public sealed class LocalRoomTeleportRequest
    {
        public Vector2 From { get; }
        public Vector2 To { get; }
        public Action CommitTeleport { get; }

        public LocalRoomTeleportRequest(Vector2 from, Vector2 to, Action commitTeleport)
        {
            From = from;
            To = to;
            CommitTeleport = commitTeleport ?? throw new ArgumentNullException(nameof(commitTeleport));
        }
    }

}