using System;
using System.Collections;
using System.Threading.Tasks;
using My.Input;
using My.Map.View;
using My.UI;
using UnityEngine;
using static My.GameLogicManager;

namespace My
{ 

    /// <summary>
    /// 管理场景切换
    /// </summary>
    public partial class MainGameManager
    {
        Coroutine _localRoomTeleportFadeCo;

        private void OnHardAreaClearStarting()
        {
            _ambientSpiritVisuals?.Shutdown();

            if (_localRoomTeleportFadeCo != null)
            {
                StopCoroutine(_localRoomTeleportFadeCo);
                _localRoomTeleportFadeCo = null;
            }
            if (gameLogicManager != null)
                gameLogicManager.ReleaseLocalRoomTeleportLock();
        }

        private void HandleOnSwitchStageUpdate(EMapSwitchStep newStage)
        {
            switch(newStage)
            {
                case EMapSwitchStep.PreSwitch:
                    {
                        HandleStepPreSwitch(gameLogicManager.SwitchAreaIntent);
                    }
                    break;
                case EMapSwitchStep.Clearing:
                    {
                        HandleStepClearing(gameLogicManager.SwitchAreaIntent);
                    }
                    break;
                case EMapSwitchStep.Loading:
                    {
                        HandleStepLoading(gameLogicManager.SwitchAreaIntent);
                    }
                    break;
                case EMapSwitchStep.Loaded:
                    {
                        HandleAfterSwitchArea(gameLogicManager.SwitchAreaIntent);
                    }
                    break;
            }
        }

        /// <summary>
        /// 切换area之前
        /// </summary>
        private void HandleStepPreSwitch(SwitchAreaIntent intent)
        {
            // 非静默模式 显示loading界面
            if(!intent.Silent)
            {
                UIManager.Instance.ShowLoading("switching");
            }

            gameLogicManager.NextSwitchStep(EMapSwitchStep.Clearing);
        }

        /// <summary>
        /// 切换area之前
        /// </summary>
        private void HandleStepClearing(SwitchAreaIntent intent)
        {
            RunHandleStepClearingAsync(intent);
        }

        private async void RunHandleStepClearingAsync(SwitchAreaIntent intent)
        {
            try
            {
                await AsyncHandleStepClearing(intent).ConfigureAwait(true);
                if (gameLogicManager != null)
                {
                    gameLogicManager.NextSwitchStep(EMapSwitchStep.Loading);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void HandleStepLoading(SwitchAreaIntent intent)
        {
            RunHandleStepLoadingAsync(intent);
        }

        private async void RunHandleStepLoadingAsync(SwitchAreaIntent intent)
        {
            try
            {
                await AsyncHandleStepLoading(intent).ConfigureAwait(true);
                if (gameLogicManager != null)
                {
                    gameLogicManager.NextSwitchStep(EMapSwitchStep.Loading);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void HandleAfterSwitchArea(SwitchAreaIntent intent)
        {
            RunHandleAfterSwitchAreaAsync(intent);
        }

        private async void RunHandleAfterSwitchAreaAsync(SwitchAreaIntent intent)
        {
            try
            {
                await AsyncHandleAfterSwitchArea(intent).ConfigureAwait(true);
                if (gameLogicManager != null)
                {
                    gameLogicManager.NextSwitchStep(EMapSwitchStep.Loaded);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        ///// <summary>
        ///// 由mono层处理吗？
        ///// </summary>
        ///// <returns></returns>
        //public async Task<int> AsyncSwitchArea()
        //{
        //    // 应该在逻辑层处理？
        //    Initialized = false;
        //    gameLogicManager.NeedBalancing = false;
        //    gameLogicManager.IsBalancing = false;

        //    var intent = gameLogicManager.SwitchAreaIntent;

        //    UIManager.Instance.ShowLoading("switching");

        //    await AsyncOnBeforeSwitchArea();

        //    await PrepareNewMap(intent);

        //    UIManager.Instance.HideLoading();

        //    UIManager.Instance.FadeHideBlack(1.5f);

        //    return 0;
        //}

        public async Task AsyncHandleStepClearing(SwitchAreaIntent intent)
        {
            // 清理ui
            if(string.IsNullOrEmpty(intent.OldAreaName))
            {
                UIManager.Instance.HideAll("LoadingOverlay");
                // 设置ui状态为boot
                await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);
            }

            // 尝试清理旧世界
            bool isUnloading = false;
            WorldAreaManager.Instance.UnloadCurrentWorld(() =>
            {
                isUnloading = true;
            });

            while (!isUnloading)
            {
                await Task.Yield();
            }
            // 清理aoi 相关
            await SceneAOIManager.Instance.CleanupAllAsync();

            // 取消关联
            playerScenePresenter = null;

            sceneDropManager.ClearAllDrop();
        }

        private async Task AsyncHandleStepLoading(SwitchAreaIntent intent)
        {

            float loadStartTime = Time.realtimeSinceStartup;
            bool loaded = false;
            WorldAreaManager.Instance.LoadWorld(intent.AreaName, onComplete: (w, suc) => { loaded = true; });

            // 等待场景加载
            while (!loaded)
            {
                await Task.Yield();
            }

            if(Time.realtimeSinceStartup - loadStartTime < 0.5f)
            {
                int waitMilli = (int)((0.5f - (Time.realtimeSinceStartup - loadStartTime)) * 1000);
                await Task.Delay(waitMilli);
            }

            // 处理特殊视角
            FovGenerator.OnAreaEnter();

            SceneAOIManager.Instance.InitMapArea(intent.AreaName);
            SceneFadeManager.OnEnterArea(WorldAreaManager.Instance.currentRoot.gameObject);

            // 更新ui注册
            //UIOrchestrator.Instance.InitGameLogicEventListener();

            // qiehuan ui
            inputBinder.ApplyInputMode(QuickPlayerInputBinder.InputMode.Overworld);

            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld, null);


            if (HomeSceneManager.Instance != null)
            {
                HomeSceneManager.Instance.InitHomePlacements();
            }

            // 等待初始所有物体加载完成
            while (!AOIManager.CheckNoLoading())
            {
                await Task.Delay(100);
            }
        }

        private async Task AsyncHandleAfterSwitchArea(SwitchAreaIntent intent)
        {
            if(!intent.Silent)
            {
                UIManager.Instance.HideLoading();
                UIManager.Instance.FadeHideBlack(1.5f);
            }

            Initialized = true;
        }

        private void OnLocalRoomTeleportFade(LocalRoomTeleportRequest req)
        {
            //if (_localRoomTeleportFadeCo != null)
            //{
            //    StopCoroutine(_localRoomTeleportFadeCo);
            //    _localRoomTeleportFadeCo = null;
            //    gameLogicManager.ReleaseLocalRoomTeleportLock();
            //}
            //_localRoomTeleportFadeCo = StartCoroutine(CoLocalRoomTeleportFade(req));
            var t0 = Time.realtimeSinceStartup;

            const float stepDt = 1f / 60f;
            const int lifecyclePumpsPerYield = 32;
            const float wallSeconds = 14f;

            UIManager.Instance.DoFadeInAndOut(0.22f, 0.28f, () => {
                req.CommitTeleport();
            }, () =>
            {
                if(Time.realtimeSinceStartup - t0 > 10)
                {
                    return true;
                }

                if (gameLogicManager != null && gameLogicManager.AreaManager != null)
                    gameLogicManager.AreaManager.AdvanceLifecycleForTeleportPrewarm(stepDt, lifecyclePumpsPerYield);

                if (AOIManager != null)
                    AOIManager.PrewarmTickAtPlayerOnce(stepDt);

                bool aoiIdle = AOIManager != null && AOIManager.CheckNoLoading();
                bool logicIdle = gameLogicManager == null || gameLogicManager.AreaManager == null ||
                                 !gameLogicManager.AreaManager.HasPendingAreaLifecycleQueues();
                if (aoiIdle && logicIdle)
                    return true;

                return false;
            }, () => { gameLogicManager.ReleaseLocalRoomTeleportLock(); });
        }

        //private IEnumerator CoLocalRoomTeleportFade(LocalRoomTeleportRequest req)
        //{
        //    try
        //    {
        //        const float fadeToBlackDuration = 0.22f;
        //        const float fadeFromBlackDuration = 0.28f;
        //        if (UIManager.Instance == null)
        //        {
        //            req.CommitTeleport();
        //            yield break;
        //        }

        //        UIManager.Instance.FadeShowBlack(fadeToBlackDuration);
        //        yield return new WaitForSecondsRealtime(fadeToBlackDuration + 0.05f);
        //        req.CommitTeleport();
        //        yield return CoPrewarmLocalRoomAfterTeleportCommit();
        //        UIManager.Instance.FadeHideBlack(fadeFromBlackDuration);
        //    }
        //    finally
        //    {
        //        _localRoomTeleportFadeCo = null;
        //        if (gameLogicManager != null)
        //            gameLogicManager.ReleaseLocalRoomTeleportLock();
        //    }
        //}

        ///// <summary>
        ///// Commit 后黑屏内：加速推进 AreaManager 生命周期队列，并每帧驱动 AOI，直到 presenter 异步创建完毕或超时。
        ///// </summary>
        //private IEnumerator CoPrewarmLocalRoomAfterTeleportCommit()
        //{
            
        //    var t0 = Time.realtimeSinceStartup;

        //    while (Time.realtimeSinceStartup - t0 < wallSeconds)
        //    {
        //        if (gameLogicManager != null && gameLogicManager.AreaManager != null)
        //            gameLogicManager.AreaManager.AdvanceLifecycleForTeleportPrewarm(stepDt, lifecyclePumpsPerYield);

        //        if (AOIManager != null)
        //            AOIManager.PrewarmTickAtPlayerOnce(stepDt);

        //        bool aoiIdle = AOIManager != null && AOIManager.CheckNoLoading();
        //        bool logicIdle = gameLogicManager == null || gameLogicManager.AreaManager == null ||
        //                         !gameLogicManager.AreaManager.HasPendingAreaLifecycleQueues();
        //        if (aoiIdle && logicIdle)
        //            yield break;

        //        yield return null;
        //    }

        //    Debug.LogWarning("LocalRoomTeleport prewarm: timeout waiting for AOI / area queues.");
        //}

    }

}

