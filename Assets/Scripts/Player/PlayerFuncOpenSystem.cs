

using My.Player;
using My.Quest;
using My.Saving;
using System.Collections.Generic;

namespace My
{

    public class PlayerFuncOpenSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public enum EFuncOpenType
        {
            Invalid,
            Hunger,
            San,
            Clothes,
            Expose,

            Skills,
        }

        public HashSet<EFuncOpenType> FuncOpenSet { get; private set; } = new();

        private Dictionary<string, int> _dialogTriggerCounter = new();
        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

        }

        public void Tick(float dt)
        {

            if(!FuncOpenSet.Contains(EFuncOpenType.Hunger))
            {
                if(LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    FuncOpenSet.Add(EFuncOpenType.Hunger);

                    var e = new PlayerFuncUnlockEvent();
                    e.OpenType = EFuncOpenType.Hunger;
                    PlayerEventBus.Publish(e);
                }
            }

        }
    }


}


