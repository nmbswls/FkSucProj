
using My.Map.Encounter;
using UnityEngine;

namespace My.Encounter
{

    public class EncounterBattleManager : MonoBehaviour
    {

        public static EncounterBattleManager Instance;
        public void Awake()
        {
            Instance = this;
        }

        public void OnDestroy()
        {
            Instance = null;
        }

        public EncounterBattleService.BattleContext CurContext;

        public void Start()
        {
            // load
            var ctx = EncounterBattleService.Instance.PendingContext;
            if(ctx == null)
            {
                Debug.LogError("EncounterBattleManager");
            }
            else
            {
                Debug.Log("EncounterBattleManager " + ctx.BattleId);
            }

            CurContext = ctx;
        }

        public void FinishBattle()
        {
            if (CurContext != null && CurContext.IsDefeatMode)
            {
                EncounterBattleService.Instance.LastResult.IsWin = false;
                MainGameManager.Instance.QuitEncounterByDefeat();
                return;
            }

            // The normal encounter HUD is the victory settlement path. Defeat mode
            // is handled explicitly above and never reaches this branch.
            var result = EncounterBattleService.Instance.LastResult;
            result.IsWin = true;
            result.InvolvedEntites.Clear();
            if (CurContext != null)
            {
                result.InvolvedEntites.AddRange(CurContext.InvolvedEntites);
            }

            MainGameManager.Instance.QuitEncounter();
        }
    }
}
