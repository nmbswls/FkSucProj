using SimpleJSON;
using UnityEngine;

namespace My.Config
{

    public static class CfgMgr
    {

        private static cfg.Tables _gameConfigs = null;

        public static cfg.Tables Cfgs { get { return _gameConfigs; } }
        public static void LoadGameConfigs()
        {
            string gameConfDir = "Config/Json"; // 须与 gen.bat 中 outputDataDir 生成的 Resources 相对路径一致

            _gameConfigs = new cfg.Tables((file) => {
                var configAsset = Resources.Load<TextAsset>($"{gameConfDir}/{file}");
                return JSON.Parse(configAsset.text);
            });

            InitializeCfgs();
        }


        private static void InitializeCfgs()
        {
            var questStepList = Cfgs.TbQuestStepData.DataList;
            foreach(var step in questStepList)
            {
                var questId = step.QuestId;
                var quest = Cfgs.TbQuestData.GetOrDefault(questId);
                if(quest == null)
                {
                    Debug.LogError($"InitializeCfgs quest not found {questId}");
                    continue;
                }

                quest.AddQuestStep(step);

                foreach(var outcomeId in step.Outcomes)
                {
                    var oneOutcome = Cfgs.TbQuestStepOutcome.GetOrDefault(outcomeId);
                    if(oneOutcome == null)
                    {
                        Debug.LogError($"InitializeCfgs quest oneOutcome not found {questId}");
                        continue;
                    }

                    step.CfgOutcomes.Add(oneOutcome);
                }

                foreach (var objectiveId in step.Objectives)
                {
                    var oneObj = Cfgs.TbQuestStepObjective.GetOrDefault(objectiveId);
                    if (oneObj == null)
                    {
                        Debug.LogError($"InitializeCfgs quest oneObj not found {questId}");
                        continue;
                    }

                    step.CfgObjectives.Add(oneObj);
                }
            }

            ItemCatalog.RebuildItemCaches();
        }
    }
}