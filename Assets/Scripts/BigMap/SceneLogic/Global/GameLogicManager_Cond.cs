
using System;
using cfg.demo;
using My.Player;
using UnityEngine;

namespace My

{
    public partial class GameLogicManager
    {
        // 列表内条件全部满足才为 true；空列表视为满足
        public bool CheckCommonCondsAll(System.Collections.Generic.IReadOnlyList<CommonCheckCond> conds, int playerId = GamePlayerIds.Local)
        {
            if (conds == null || conds.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < conds.Count; i++)
            {
                if (!CheckCommonCond(conds[i], playerId))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckCommonCond(CommonCheckCond cond, int playerId = GamePlayerIds.Local)
        {
            var playerSystem = GetPlayerSystem(playerId);

            switch(cond.Type)
            {
                case cfg.demo.ECommonCheckType.None:
                    {
                        return true;
                    }
                case cfg.demo.ECommonCheckType.AlwaysFail:
                    {
                        return false;
                    }
                case cfg.demo.ECommonCheckType.CheckVariable:
                    {
                        bool checkHas = false;
                        if(cond.Param1 == 0)
                        {
                            checkHas = true;
                        }
                        if(checkHas && playerSystem != null && playerSystem.CheckHasParam(cond.Param5))
                        {
                            return true;
                        }
                        if (!checkHas && playerSystem != null && !playerSystem.CheckHasParam(cond.Param5))
                        {
                            return true;
                        }
                    }
                    break;
                case cfg.demo.ECommonCheckType.OwnItem:
                    {
                        string itemId = cond.Param5;
                        long itemCnt = cond.Param1;

                        if(playerSystem != null && playerSystem.CheckHaveItem(itemId, itemCnt))
                        {
                            return true;
                        }
                    }
                    break;

                case cfg.demo.ECommonCheckType.TaskFinish:
                    {
                        int questId = (int)cond.Param1;
                        if (playerSystem != null && playerSystem.QuestSystem.CheckQuestFinish(questId))
                        {
                            return true;
                        }
                    }
                    break;
                case cfg.demo.ECommonCheckType.TaskStep:
                    {
                        int questId = (int)cond.Param1;
                        string stepId = cond.Param5;
                        if (playerSystem == null)
                        {
                            return false;
                        }

                        var quest = playerSystem.QuestSystem.GetQuest(questId);
                        if(quest == null)
                        {
                            return false;
                        }
                        if (quest.ActiveStep == null || quest.ActiveStep.CacheStepCfg.StepId != stepId)
                        {
                            return false;
                        }

                        return true;
                    }
                case cfg.demo.ECommonCheckType.FuncOpen:
                    {
                        if (playerSystem?.FuncOpenSystem == null)
                        {
                            return false;
                        }

                        if (!Enum.IsDefined(typeof(EFuncOpenType), (int)cond.Param1))
                        {
                            return false;
                        }

                        return playerSystem.FuncOpenSystem.IsFuncOpen((EFuncOpenType)cond.Param1);
                    }
                case cfg.demo.ECommonCheckType.CharacterFavorLevel:
                    {
                        if (string.IsNullOrEmpty(cond.Param5) || cond.Param1 < 0)
                        {
                            return false;
                        }

                        var registry = worldPersistState?.NpcCharacters;
                        return registry != null
                               && registry.GetFavorLevel(cond.Param5, this) >= cond.Param1;
                    }
                case cfg.demo.ECommonCheckType.HumanTechUnlockedCount:
                    {
                        return playerSystem?.ProgressionSystem?.HumanCivilization != null
                               && playerSystem.ProgressionSystem.HumanCivilization.GetUnlockedTechCount() >= cond.Param1;
                    }
            }
            return false;
        }
    }
}
