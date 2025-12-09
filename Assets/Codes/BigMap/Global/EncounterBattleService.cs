using UnityEngine;

namespace My.Encounter
{
    public class EncounterBattleService
    {
        private static EncounterBattleService _instance;
        public static EncounterBattleService Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new EncounterBattleService();
                }
                return _instance;
            }
        }
        public class BattleContext
        {
            public int BattleId;
            public string BattleReason;
            public bool IsDefeatMode;

            public string EnemyId;
        }

        

        public bool IsInBattle;

        public BattleContext? PendingContext;
        public GameLogicManager.BattleResult LastResult;

        public void StartBattleContext()
        {

        }
    }
}
