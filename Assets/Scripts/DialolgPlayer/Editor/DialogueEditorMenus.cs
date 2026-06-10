using System.IO;
using UnityEditor;
using UnityEngine;

namespace My.Dialog
{
    public static class DialogueEditorMenus
    {
        public static string ToProjectRelativePath(string fullPath)
        {
            fullPath = fullPath.Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (fullPath.StartsWith(dataPath))
                return "Assets" + fullPath.Substring(dataPath.Length);
            return fullPath;
        }
    }
}
