

using My.Player;
using My.Quest;
using My.Saving;
using System.Collections.Generic;

namespace My
{
    public enum EFuncOpenType
    {
        Invalid,
        Hunger,
        Desire,
        Clothes,
        Expose,

        Skills,
    }

    public class PlayerFuncOpenSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public HashSet<EFuncOpenType> FuncOpenSet { get; private set; } = new();

        private Dictionary<string, int> _dialogTriggerCounter = new();
        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            foreach (var f in savingData.FuncOpenList)
            {
                FuncOpenSet.Add(f);
            }
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


