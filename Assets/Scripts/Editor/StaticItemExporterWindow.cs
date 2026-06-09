using System;
using UnityEditor;

[Obsolete("Use Window/Map Exporter instead.")]
public class StaticItemExporterWindow : EditorWindow
{
    [MenuItem("Window/Static Item Exporter")]
    public static void Open()
    {
        MapExporterWindow.Open();
    }
}
