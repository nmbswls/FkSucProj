
using System.Collections.Generic;
using Map.Logic.Events;
using My.Map.Entity;

namespace My.Map
{
    public class MapControlAction
    {
        public enum EActionType
        {
            None,
            SpawnShouWei
        }
        public EActionType ActionType;

        public int Param1;
        public int Param2;
        public string Param3;
        public string Param4;
    }


    public class MapControlEvent
    {

        public enum ETriggerType
        {
            None = 0,
            JingJie = 1,
        }

        public ETriggerType TriggerType;
        public int TriggerP1;
        public int TriggerP2;
        public string TriggerP5;
        public string TriggerP6;

        public List<MapControlAction> Actions = new();

        public bool CheckMatch(IMapLogicEvent logicEvent)
        {
            switch (TriggerType)
            {
                case ETriggerType.None:
                    {
                        return false;
                    }
                case ETriggerType.JingJie:
                    {
                        if(logicEvent is not MLECommonGameEvent commonEvent || commonEvent.Name != "AlertTrigger")
                        {
                            return false;
                        }

                        int level = TriggerP1;
                        if(commonEvent.Param1 == level)
                        {
                            return true;
                        }
                        return false;
                    }
                    break;
            }

            return false;
        }
    }

}