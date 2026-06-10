using System.Collections.Generic;
using Newtonsoft.Json;

namespace My.Dialog
{
    public static class DialogueDataConverter
    {
        public static JsonSerializerSettings Settings => new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static DialogueData Deserialize(string json)
        {
            var data = JsonConvert.DeserializeObject<DialogueData>(json, Settings);
            if (data == null)
                data = new DialogueData();
            Normalize(data);
            return data;
        }

        public static string Serialize(DialogueData data)
        {
            return JsonConvert.SerializeObject(data, Settings);
        }

        public static DialogueData FromEditorSteps(List<DialogueStepData> steps)
        {
            var data = new DialogueData();
            if (steps != null)
                data.Steps.AddRange(steps);
            Normalize(data);
            return data;
        }

        public static void Normalize(DialogueData data)
        {
            if (data.Steps == null)
                data.Steps = new List<DialogueStepData>();

            foreach (var step in data.Steps)
            {
                if (step.Commands == null)
                    step.Commands = new List<DialogCommandData>();

                foreach (var cmd in step.Commands)
                {
                    if (cmd is DialogCommandData4BranchText branch)
                    {
                        if (branch.SimpleBranch == null)
                            branch.SimpleBranch = new List<string>();
                        if (branch.SimpleTextLines == null)
                            branch.SimpleTextLines = new List<List<OneTextLine>>();
                    }
                    else if (cmd is DialogCommandData4Text text)
                    {
                        if (text.TextLines == null)
                            text.TextLines = new List<OneTextLine>();
                    }
                    else if (cmd is DialogCommandData4Choice choice)
                    {
                        if (choice.Options == null)
                            choice.Options = new List<DialogChoiceOption>();
                    }
                }
            }
        }
    }
}
