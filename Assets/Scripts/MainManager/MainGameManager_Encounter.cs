using My.Encounter;
using My.Map;
using My.Map.Encounter;
using My.UI;
using System.Threading.Tasks;
using UnityEngine;

namespace My
{
    public partial class MainGameManager
    {
        bool isSwitchingEncounter;

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
            }, TaskScheduler.FromCurrentSynchronizationContext());
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
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void QuitToSecretBase()
        {
            if (isSwitchingEncounter)
            {
                return;
            }

            isSwitchingEncounter = true;
            _ = InnerQuitEncounterAfterAbandon().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }
                isSwitchingEncounter = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
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

        protected async Task InnerQuitEncounterAfterAbandon()
        {
            UIManager.Instance.ShowLoading("good");

            await UIOrchestrator.Instance.SetStateAsync(UIAppState.Boot, null);

            await EncounterBattleLoader.UnloadBattleAsync();

            LogicTime.ReleasePause("encounter");

            UIManager.Instance.HideLoading();

            gameLogicManager.AbandonToSecretBase();
        }

        public void WaitingIntoDefeatedBattle()
        {
            _ = AsyncPrepareDefeatedBattle().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }
                isSwitchingEncounter = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected async Task AsyncPrepareDefeatedBattle()
        {
            await Task.Delay(5000);

            PlayDialog("defeated_01");
        }
    }
}
