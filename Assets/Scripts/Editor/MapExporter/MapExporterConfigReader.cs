using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using SimpleJSON;
using UnityEngine;

// Editor 内读取 Luban 配置，解析 variant / overlay 对应关系
public static class MapExporterConfigReader
{
    const string ConfigFolder = "Config/Json";

    static cfg.Tables _tables;
    static bool _variantLinked;

    public static void EnsureLoaded()
    {
        if (_tables != null)
        {
            return;
        }

        _tables = new cfg.Tables(file =>
        {
            var asset = Resources.Load<TextAsset>($"{ConfigFolder}/{file}");
            if (asset == null)
            {
                Debug.LogError($"[MapExporter] Config not found: {ConfigFolder}/{file}");
                return new JSONObject();
            }

            return JSON.Parse(asset.text);
        });
    }

    static void EnsureVariantLinked()
    {
        EnsureLoaded();
        if (_variantLinked)
        {
            return;
        }

        foreach (var overlay in _tables.TbAreaOverlayStateInfo.DataList)
        {
            overlay.BelongVariantInfo = _tables.TbAreaVariantInfo.GetOrDefault(overlay.VarId);
        }

        _variantLinked = true;
    }

    public static AreaVariantInfo FindVariantBySceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return null;
        }

        EnsureLoaded();
        var key = sceneName.Trim();
        return _tables.TbAreaVariantInfo.DataList
            .FirstOrDefault(v => v.SceneName == key);
    }

    public static List<AreaOverlayStateInfo> GetOverlaysForVariantScene(string variantSceneName)
    {
        var variant = FindVariantBySceneName(variantSceneName);
        if (variant == null)
        {
            return new List<AreaOverlayStateInfo>();
        }

        EnsureVariantLinked();
        return _tables.TbAreaOverlayStateInfo.DataList
            .Where(o => o.VarId == variant.VarId && !string.IsNullOrEmpty(o.MapDataName))
            .ToList();
    }
}
