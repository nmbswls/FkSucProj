

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

        Skills = 10,
    }

    public class PlayerFuncOpenSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public HashSet<EFuncOpenType> FuncOpenSet { get; private set; } = new();

        public bool IsFuncOpen(EFuncOpenType type) =>
            type != EFuncOpenType.Invalid && FuncOpenSet.Contains(type);

        private Dictionary<string, int> _dialogTriggerCounter = new();
        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            if(savingData?.PlayerData?.FuncOpenList != null)
            {
                foreach (var f in savingData.PlayerData.FuncOpenList)
                {
                    FuncOpenSet.Add(f);
                }
            }
        }

        public void PostInit(PlayerSystemManager owner)
        {
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

            if (!FuncOpenSet.Contains(EFuncOpenType.Desire))
            {
                if (LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    FuncOpenSet.Add(EFuncOpenType.Desire);

                    var e = new PlayerFuncUnlockEvent();
                    e.OpenType = EFuncOpenType.Desire;
                    PlayerEventBus.Publish(e);
                }
            }

            if (!FuncOpenSet.Contains(EFuncOpenType.Clothes))
            {
                if (LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    FuncOpenSet.Add(EFuncOpenType.Clothes);

                    var e = new PlayerFuncUnlockEvent();
                    e.OpenType = EFuncOpenType.Clothes;
                    PlayerEventBus.Publish(e);
                }
            }

        }
    }


}


