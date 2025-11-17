using UnityEngine;

namespace Map.Encounter
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

        }

        public class BattleResult
        {
            public bool IsWin;
        }

        public bool IsInBattle;

        public BattleContext? PendingContext;
        public BattleResult LastResult;

        public void StartBattleContext()
        {

        }
    }
}
