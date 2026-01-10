
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

        [SerializeReference]
        public List<EditorDialogCommand> Commands = new List<EditorDialogCommand>();
    }


    // --- 抽象基类 ---
    [Serializable]
    public abstract class EditorDialogCommand
    {
        public bool IsFolded = true; // 内部折叠
        public virtual string GetSummary() => $"";
    }


    // --- 具体子类示例 ---

    [Serializable]
    public class EditorDialogueTextCommand : EditorDialogCommand
    {
        public string Speaker;
        [TextArea(2, 5)] public string Content;
        public AudioClip VoiceLine;

        public override string GetSummary() => $"[Talk]";
    }

    [Serializable]
    public class EditorSetImageCommand : EditorDialogCommand
    {
        public Sprite Image;
        public enum ImgPos { Left, Center, Right, Background }
        public ImgPos position;

        public override string GetSummary() => $"[SetImage]";
    }

    [Serializable]
    public class EditorChoiceCommand : EditorDialogCommand
    {
        public float timeLimit = 0;
        public List<DialogChoiceOption> Options = new List<DialogChoiceOption>();

        public override string GetSummary() => $"[Choice]";
    }

}