using cfg.demo;
using My.Quest;
using My.Saving;
using System.Collections.Generic;

namespace My.Player
{
    public class PlayerFuncOpenSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }

        public HashSet<EFuncOpenType> FuncOpenSet { get; private set; } = new();

        public bool IsFuncOpen(EFuncOpenType type) =>
            type != EFuncOpenType.Invalid && FuncOpenSet.Contains(type);

        public bool TryOpenFunc(EFuncOpenType type)
        {
            if (type == EFuncOpenType.Invalid || FuncOpenSet.Contains(type))
            {
                return false;
            }

            FuncOpenSet.Add(type);
            PlayerEventBus.Publish(new PlayerFuncUnlockEvent { OpenType = type });
            return true;
        }

        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            LogicManager = ctx;

            if (savingData?.PlayerData?.FuncOpenList != null)
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
            if (!FuncOpenSet.Contains(EFuncOpenType.Hunger))
            {
                if (LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    TryOpenFunc(EFuncOpenType.Hunger);
                }
            }

            if (!FuncOpenSet.Contains(EFuncOpenType.Desire))
            {
                if (LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    TryOpenFunc(EFuncOpenType.Desire);
                }
            }

            if (!FuncOpenSet.Contains(EFuncOpenType.Clothes))
            {
                if (LogicManager.playerDataManager.QuestSystem.CheckQuestFinish(101))
                {
                    TryOpenFunc(EFuncOpenType.Clothes);
                }
            }
        }
    }
}
