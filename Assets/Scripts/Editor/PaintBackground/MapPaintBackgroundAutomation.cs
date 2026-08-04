#if UNITY_EDITOR
using System;
using System.IO;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Command-line bridge used by the Codex map terrain/paint skill.
public static class MapPaintBackgroundAutomation
{
    public static void ExportForAiFromCommandLine()
    {
        var context = OpenContext();
        var coord = ReadCoord();
        var result = MapPaintBackgroundExporter.ExportSingleChunkForAi(
            context.Root, context.MapName, coord);
        Require(result.Success, result.Message);
        Save(context.Root);
        Debug.Log("[MapPaintAutomation] " + result.Message);
    }

    public static void ImportAndSyncFromCommandLine()
    {
        var context = OpenContext();
        var coord = ReadCoord();
        var inputPath = ReadRequiredArg("-mapPaintInput");
        inputPath = Path.GetFullPath(inputPath);

        var import = MapPaintBackgroundImporter.ImportPaintedPng(
            context.Root, context.MapName, coord, inputPath);
        Require(import.Success, import.Message);

        var sync = MapPaintBackgroundExporter.SyncChunkToDatabase(
            context.Root, context.MapName, coord);
        Require(sync.Success, sync.Message);

        Save(context.Root);
        ValidateRuntimeAssets(context.MapName, coord);
        Debug.Log("[MapPaintAutomation] " + import.Message + " " + sync.Message);
    }

    public static void ExportMapChunkFromCommandLine()
    {
        var context = OpenContext();
        var result = MapChunkExportCore.Export(
            context.Root,
            context.MapName,
            context.Root.ChunkWorldSize,
            context.Root.ChunkOrigin);
        Require(result.Success, result.Message);
        Save(context.Root);
        Debug.Log("[MapPaintAutomation] " + result.Message);
    }

    public static void ValidateRuntimeFromCommandLine()
    {
        var context = OpenContext();
        var coord = ReadCoord();
        ValidateRuntimeAssets(context.MapName, coord);
        Debug.Log($"[MapPaintAutomation] Runtime resources valid for {context.MapName} chunk ({coord.X},{coord.Y}).");
    }

    struct Context
    {
        public MapChunkEditorRoot Root;
        public string MapName;
    }

    static Context OpenContext()
    {
        var mapName = ReadOptionalArg("-mapPaintMap");
        var scenePath = ReadOptionalArg("-mapPaintScene");
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Require(!string.IsNullOrWhiteSpace(mapName),
                "Pass -mapPaintMap or -mapPaintScene.");
            scenePath = $"Assets/Scenes/Main/{mapName}_Editor.unity";
        }

        Require(File.Exists(scenePath), $"Editor scene not found: {scenePath}");
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var root = UnityEngine.Object.FindObjectOfType<MapChunkEditorRoot>();
        Require(root != null, $"MapChunkEditorRoot not found in {scenePath}.");
        if (string.IsNullOrWhiteSpace(mapName))
        {
            mapName = root.MapVariantSceneName;
        }

        Require(!string.IsNullOrWhiteSpace(mapName), "Map name is empty.");
        Require(root.MapVariantSceneName == mapName,
            $"Map name mismatch: scene root is '{root.MapVariantSceneName}', argument is '{mapName}'.");

        return new Context { Root = root, MapName = mapName };
    }

    static ChunkCoord ReadCoord()
    {
        var x = ReadIntArg("-mapPaintX");
        var y = ReadIntArg("-mapPaintY");
        return new ChunkCoord(x, y);
    }

    static int ReadIntArg(string key)
    {
        var raw = ReadRequiredArg(key);
        Require(int.TryParse(raw, out var value), $"Invalid integer for {key}: {raw}");
        return value;
    }

    static string ReadRequiredArg(string key)
    {
        var value = ReadOptionalArg(key);
        Require(!string.IsNullOrWhiteSpace(value), $"Missing command-line argument {key}.");
        return value;
    }

    static string ReadOptionalArg(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    static void Save(MapChunkEditorRoot root)
    {
        EditorUtility.SetDirty(root);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void ValidateRuntimeAssets(string mapName, ChunkCoord coord)
    {
        var dbPath = $"Assets/Resources/MapChunk/{mapName}.asset";
        var database = AssetDatabase.LoadAssetAtPath<MapChunkDatabase>(dbPath);
        Require(database != null, $"MapChunkDatabase missing: {dbPath}");

        var runtimeDatabase = Resources.Load<MapChunkDatabase>($"MapChunk/{mapName}");
        Require(runtimeDatabase != null, $"Resources.Load failed: MapChunk/{mapName}");
        Require(runtimeDatabase.GroundLayerNames != null && runtimeDatabase.GroundLayerNames.Length > 0,
            $"MapChunk/{mapName} has no GroundLayerNames.");
        Require(!string.IsNullOrWhiteSpace(runtimeDatabase.WalkGridKey),
            $"MapChunk/{mapName} has no WalkGridKey.");
        var walkGrid = Resources.Load<GameObject>(runtimeDatabase.WalkGridKey);
        Require(walkGrid != null, $"Resources.Load failed: {runtimeDatabase.WalkGridKey}");
        Require(walkGrid.GetComponentsInChildren<Tilemap>(true).Length > 0,
            $"WalkGrid prefab has no Tilemap components: {runtimeDatabase.WalkGridKey}");

        runtimeDatabase.InvalidateLookup();
        var item = runtimeDatabase.GetChunkItem(coord);
        Require(item != null, $"Chunk ({coord.X},{coord.Y}) missing from {dbPath}.");
        Require(!string.IsNullOrWhiteSpace(item.BackgroundKey),
            $"Chunk ({coord.X},{coord.Y}) has no BackgroundKey.");
        var background = Resources.Load<GameObject>(item.BackgroundKey);
        Require(background != null, $"Resources.Load failed: {item.BackgroundKey}");
        var spriteRenderer = background.GetComponentInChildren<SpriteRenderer>(true);
        Require(spriteRenderer != null && spriteRenderer.sprite != null,
            $"Background prefab has no SpriteRenderer sprite: {item.BackgroundKey}");
        if (!string.IsNullOrWhiteSpace(item.TilemapKey))
        {
            Require(Resources.Load<GameObject>(item.TilemapKey) != null,
                $"Resources.Load failed: {item.TilemapKey}");
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[MapPaintAutomation] " + message);
        }
    }
}
#endif
