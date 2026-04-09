

using System.Threading.Tasks;
using My.Input;
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
             _ = AsyncHandleStepClearing(intent).ContinueWith(t =>
             {
                 if (t.IsFaulted)
                 {
                     Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                 }

                 gameLogicManager.NextSwitchStep(EMapSwitchStep.Loading);

             }, TaskScheduler.FromCurrentSynchronizationContext()); ;
        }

        private void HandleStepLoading(SwitchAreaIntent intent)
        {
            _ = AsyncHandleStepLoading(intent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }

                gameLogicManager.NextSwitchStep(EMapSwitchStep.Loading);

            }, TaskScheduler.FromCurrentSynchronizationContext()); ;
        }

        private void HandleAfterSwitchArea(SwitchAreaIntent intent)
        {
            _ = AsyncHandleAfterSwitchArea(intent).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }

                gameLogicManager.NextSwitchStep(EMapSwitchStep.Loaded);

            }, TaskScheduler.FromCurrentSynchronizationContext()); ;
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

            UIManager.Instance.HidePanel("InteractMenu");

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

    }

}

