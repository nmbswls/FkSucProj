
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Dialog
{
    [CreateAssetMenu(fileName = "NewDialoguePoly", menuName = "Dialogue/Dialogue Data (Poly)")]
    public class EditorDialogueData : ScriptableObject
    {
        public string DialogId;
        public List<EditorStepData> Steps = new List<EditorStepData>();
    }

    [Serializable]
    public class EditorStepData
    {
        public string Id;
        public string Note;
        public bool IsExpanded = false; // 控制卡片折叠

        public List<EditorDialogCommand> Commands = new List<EditorDialogCommand>();
    }


    // --- 抽象基类 ---
    [Serializable]
    public class EditorDialogCommand
    {
        public bool IsFolded = true; // 内部折叠

        [SerializeReference]
        public DialogCommandBase CommandData;
    }

}