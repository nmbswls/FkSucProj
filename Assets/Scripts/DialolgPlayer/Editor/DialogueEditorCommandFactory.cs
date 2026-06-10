using System;
using System.Collections.Generic;

namespace My.Dialog
{
    public static class DialogueEditorCommandFactory
    {
        public static DialogCommandData Create(Type type)
        {
            var cmd = (DialogCommandData)Activator.CreateInstance(type);

            switch (cmd)
            {
                case DialogCommandData4Text text:
                    text.TextLines = new List<OneTextLine>();
                    break;
                case DialogCommandData4BranchText branch:
                    branch.SimpleBranch = new List<string>();
                    branch.SimpleTextLines = new List<List<OneTextLine>>();
                    break;
                case DialogCommandData4Choice choice:
                    choice.Options = new List<DialogChoiceOption>();
                    break;
            }

            return cmd;
        }
    }
}
