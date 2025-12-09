

using UnityEngine.SceneManagement;
using UnityEngine;
using System.Threading.Tasks;
using My.UI;
using My.Encounter;

namespace My.Map.Encounter
{
    public class EncounterBattleLoader
    {

        /// <summary>
        /// 包括整个加载流程 外部的冻结 内部的继续运作
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static async Task LoadBattleAsync(EncounterBattleService.BattleContext ctx)
        {
            EncounterBattleService.Instance.PendingContext = ctx;
            // 显示过渡UI
            UIManager.Instance.ShowLoading();

            // 异步加载战斗场景
            var op = SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);
            while (!op.isDone) await Task.Yield();

            var battleScene = SceneManager.GetSceneByName("BattleScene");
            SceneManager.SetActiveScene(battleScene);

            // todo virtual camera
            MainGameManager.Instance.CameraCtrl.enabled = false;
            Camera.main.transform.position = new Vector2(1000, 1000);

            // 隐藏大地图输入
            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Battle);
            UIManager.Instance.HideLoading();
        }

        public static async Task UnloadBattleAsync()
        {
            UIManager.Instance.ShowLoading();
            var op = SceneManager.UnloadSceneAsync("BattleScene");
            while (!op.isDone) await Task.Yield();

            // todo virtual camera
            MainGameManager.Instance.CameraCtrl.enabled = true;
            Camera.main.transform.position = MainGameManager.Instance.playerScenePresenter.transform.position;

            // 恢复输入
            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld);
            UIManager.Instance.HideLoading();
        }
    }
    
}