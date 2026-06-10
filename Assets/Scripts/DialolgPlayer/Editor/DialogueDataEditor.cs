using System.Collections.Generic;
using UnityEngine;

namespace My.Dialog
{
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
    public class EditorDialogueData : ScriptableObject
    {
        // 可选：关联的运行时 JSON 路径（Assets 下相对路径），便于一键 Import/Export
        public string LinkedJsonPath;

        public List<DialogueStepData> Steps = new List<DialogueStepData>();
    }
}
