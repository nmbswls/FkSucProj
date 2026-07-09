
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
            EncounterBattleService.Instance.LastResult.IsWin = false;
            EncounterBattleService.Instance.LastResult.InvolvedEntites.AddRange(CurContext.InvolvedEntites);

            if (CurContext != null && CurContext.IsDefeatMode)
            {
                MainGameManager.Instance.QuitEncounterByDefeat();
                return;
            }

            MainGameManager.Instance.QuitEncounter();
        }
    }
}
