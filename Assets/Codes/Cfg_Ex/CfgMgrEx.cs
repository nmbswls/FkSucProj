
using System.Collections.Generic;
using My.Config;

namespace cfg.demo
{

    public partial class QuestStepData
    {

        public List<QuestStepOutcome> CfgOutcomes = new();
        public List<QuestStepObjective> CfgObjectives = new();

    }


    public partial class QuestData
    {
        protected Dictionary<string, QuestStepData> _innerSteps { get; set; }
        public void AddQuestStep(QuestStepData stepData)
        {
            _innerSteps[stepData.StepId] = stepData;
        }

        public QuestStepData GetStep(string stepId)
        {
            _innerSteps.TryGetValue(stepId, out var step);
            return step;
        }
    }

}
