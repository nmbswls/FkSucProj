using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpFontProbe
{
    [MenuItem("Window/Map/Probe TMP Font")]
    public static void Run()
    {
        var projectRoot = Directory.GetCurrentDirectory();
        var outPath = Path.Combine(projectRoot, "Temp", "tmp_font_probe.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/MSYH SDF.asset");
        if (font == null)
        {
            File.WriteAllText(outPath, "Font not found");
            Debug.LogError("[TmpFontProbe] Font not found: Assets/Fonts/MSYH SDF.asset");
            return;
        }

        var sample = "家园小镇 村子1 村子2 小 村 镇";
        using var writer = new StreamWriter(outPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("TMP Font Probe " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        writer.WriteLine("Font: " + font.name);
        writer.WriteLine("AtlasPopulationMode: " + font.atlasPopulationMode);
        writer.WriteLine("Sample: " + sample);

        foreach (var ch in sample)
        {
            if (ch == ' ')
                continue;

            var has = font.characterLookupTable.TryGetValue(ch, out var character);
            writer.WriteLine($"U+{(int)ch:X4} '{ch}' => has={has}, glyphIndex={(has ? character.glyphIndex.ToString() : "-")}");
        }

        Debug.Log("[TmpFontProbe] wrote " + outPath);
    }
}
