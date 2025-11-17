

using static Map.Encounter.EncounterBattleService;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Threading.Tasks;
using My.UI;

namespace Map.Encounter
{
    public class EncounterBattleLoader : MonoBehaviour
    {

        /// <summary>
        /// 包括整个加载流程 外部的冻结 内部的继续运作
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task LoadBattleAsync(BattleContext ctx)
        {
            EncounterBattleService.Instance.PendingContext = ctx;
            // 显示过渡UI
            UIManager.Instance.ShowLoading();

            // 异步加载战斗场景
            var op = SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive);
            while (!op.isDone) await Task.Yield();

            var battleScene = SceneManager.GetSceneByName("BattleScene");
            SceneManager.SetActiveScene(battleScene);

            // 隐藏大地图输入
            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Battle);
            UIManager.Instance.HideLoading();
        }

        public async Task UnloadBattleAsync()
        {
            UIManager.Instance.ShowLoading();
            var op = SceneManager.UnloadSceneAsync("BattleScene");
            while (!op.isDone) await Task.Yield();
            // 恢复输入
            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Overworld);
            UIManager.Instance.HideLoading();
        }
    }
    
}