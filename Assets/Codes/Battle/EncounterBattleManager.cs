
using My.Map.Encounter;
using UnityEngine;

namespace My.Encounter
{

    public class EncounterBattleManager : MonoBehaviour
    {

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
        }
    }
}