using System.IO;
using NavMeshPlus.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Variant 级 NavMeshData 导出
public static class MapNavMeshExportCore
{
    public struct ExportResult
    {
        public bool Success;
        public bool Skipped;
        public string Message;
        public string AssetPath;
    }

    const string NavFolder = "Assets/Resources/MapNav";

    public static ExportResult Export(GameObject areaRoot, string variantKey)
    {
        if (areaRoot == null)
        {
            return Fail("AreaRoot is null.");
        }

        if (string.IsNullOrWhiteSpace(variantKey))
        {
            return Fail("Variant key is empty.");
        }

        var surface = areaRoot.GetComponentInChildren<NavMeshSurface>(true);
        if (surface == null)
        {
            return new ExportResult
            {
                Success = true,
                Skipped = true,
                Message = "NavMeshSurface not found, skipped.",
            };
        }

        surface.BuildNavMesh();
        var baked = surface.navMeshData;
        if (baked == null)
        {
            return Fail("NavMesh bake produced no data.");
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder(NavFolder);

        var path = $"{NavFolder}/{variantKey.Trim()}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        var asset = Object.Instantiate(baked);
        asset.name = variantKey.Trim();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        return new ExportResult
        {
            Success = true,
            Message = $"NavMesh exported: {path}",
            AssetPath = path,
        };
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    static ExportResult Fail(string message)
    {
        Debug.LogError("[MapNavExport] " + message);
        return new ExportResult { Success = false, Message = message };
    }
}
