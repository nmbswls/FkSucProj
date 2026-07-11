using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ForestBatchmodeProbe
{
    [MenuItem("Window/Map/Probe Forest Batchmode")]
    public static void Run()
    {
        var projectRoot = Directory.GetCurrentDirectory();
        var outPath = Path.Combine(projectRoot, "Temp", "forest_batchmode_probe.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        var text = "ForestBatchmodeProbe.Run at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine;
        File.WriteAllText(outPath, text);
        Debug.Log("[ForestBatchmodeProbe] wrote " + outPath);
    }
}

