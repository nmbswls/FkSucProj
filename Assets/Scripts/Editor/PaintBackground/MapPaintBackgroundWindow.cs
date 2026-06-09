#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class MapPaintBackgroundWindow : EditorWindow
{
    [SerializeField] GameObject areaRoot;
    [SerializeField] string mapName = "Main_Area_01";
    [SerializeField] Vector2 chunkListScroll;
    [SerializeField] FilterMode resampleFilter = FilterMode.Bilinear;

    ChunkCoord? _selectedChunk;

    [MenuItem("Window/Map Paint Background")]
    public static void Open()
    {
        GetWindow<MapPaintBackgroundWindow>("Map Paint Background");
    }

    void OnGUI()
    {
        DrawHeader();

        var root = MapChunkEditorUtility.Resolve(areaRoot);
        if (root == null)
        {
            EditorGUILayout.HelpBox("AreaRoot 上需要 MapChunkEditorRoot 组件。", MessageType.Warning);
            return;
        }

        DrawSettings(root);
        DrawChunkList(root);
        DrawWorkflow(root);
        DrawPreviewSection(root);
    }

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Map Source", EditorStyles.boldLabel);
        areaRoot = (GameObject)EditorGUILayout.ObjectField("Area Root", areaRoot, typeof(GameObject), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selected"))
            {
                if (Selection.activeGameObject != null)
                {
                    areaRoot = Selection.activeGameObject;
                }
            }

            if (GUILayout.Button("Sync Settings"))
            {
                SyncFromScene();
            }
        }

        mapName = EditorGUILayout.TextField("Scene Name", mapName);
    }

    void DrawSettings(MapChunkEditorRoot root)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        root.PaintWorldRect = EditorGUILayout.RectField("Paint World Rect", root.PaintWorldRect);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(root);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap Rect"))
            {
                root.PaintWorldRect = MapChunkUtility.SnapWorldRectToChunkGrid(
                    root.PaintWorldRect, root.ChunkOrigin, root.ChunkWorldSize);
                EditorUtility.SetDirty(root);
            }

            if (GUILayout.Button("Rect From Scene"))
            {
                root.PaintWorldRect = MapPaintBackgroundCapture.ComputeBoundsFromCaptureLayers(root);
                EditorUtility.SetDirty(root);
            }
        }

    }

    void DrawWorkflow(MapChunkEditorRoot root)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Export For AI → 外部编辑 → Import Painted PNG（写入 painted 目录）→ Sync（裁剪并打包 bg + Database）",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export For AI", GUILayout.Height(28f)))
                {
                    var result = MapPaintBackgroundExporter.ExportSingleChunkForAi(
                        root, mapName, _selectedChunk.Value, resampleFilter);
                    LogResult(result.Success, result.Message);
                    if (result.Success)
                    {
                        PingSelectedExport();
                    }
                }

                if (GUILayout.Button("Re-capture", GUILayout.Width(90f), GUILayout.Height(28f)))
                {
                    var result = MapPaintBackgroundExporter.RecaptureChunkTemplate(
                        root, mapName, _selectedChunk.Value);
                    LogResult(result.Success, result.Message);
                }
            }
        }

        using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue))
        {
            if (GUILayout.Button("Import Painted PNG...", GUILayout.Height(28f)))
            {
                var result = MapPaintBackgroundImporter.ImportPaintedPngFromFile(
                    root, mapName, _selectedChunk.Value);
                LogImportResult(result);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sync All (Pack + Database)", GUILayout.Height(28f)))
            {
                var result = MapPaintBackgroundExporter.SyncPaintRectToDatabase(root, mapName, resampleFilter);
                LogResult(result.Success, result.Message);
            }

            using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue))
            {
                if (GUILayout.Button("Sync Selected"))
                {
                    var result = MapPaintBackgroundExporter.SyncChunkToDatabase(
                        root, mapName, _selectedChunk.Value, resampleFilter);
                    LogResult(result.Success, result.Message);
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue))
            {
                if (GUILayout.Button("Ping Export File"))
                {
                    PingSelectedExport();
                }
            }
        }
    }

    void DrawPreviewSection(MapChunkEditorRoot root)
    {
        var settings = MapChunkEditorSettings.GetOrCreate();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        settings.PaintPreviewEnabled = EditorGUILayout.Toggle("Show In Scene", settings.PaintPreviewEnabled);
        settings.PaintAutoRefreshPreview = EditorGUILayout.Toggle("Auto Refresh", settings.PaintAutoRefreshPreview);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);
            if (settings.PaintPreviewEnabled)
            {
                MapPaintBackgroundPreview.TryAutoSync(root, mapName);
            }
            else
            {
                var previewRoot = root.transform.Find(MapPaintBackgroundPreview.PreviewRootName);
                if (previewRoot != null)
                {
                    previewRoot.gameObject.SetActive(false);
                }
            }
        }

        if (GUILayout.Button("Refresh Scene Preview"))
        {
            var result = MapPaintBackgroundPreview.SyncToScene(root, mapName);
            LogResult(result.Success, result.Message);
        }

        DrawSelectedChunkThumbnail(root);
    }

    void DrawSelectedChunkThumbnail(MapChunkEditorRoot root)
    {
        if (!_selectedChunk.HasValue)
        {
            return;
        }

        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(MapPaintBackgroundShared.GetManifestPath(mapName));
        var tex = MapPaintBackgroundPreview.LoadPreviewTexture(mapName, _selectedChunk.Value, manifest);
        if (tex == null)
        {
            return;
        }

        EditorGUILayout.LabelField($"Preview ({_selectedChunk.Value.X}, {_selectedChunk.Value.Y})", EditorStyles.miniLabel);
        const float maxSize = 128f;
        float aspect = tex.width / (float)Mathf.Max(1, tex.height);
        float w = aspect >= 1f ? maxSize : maxSize * aspect;
        float h = aspect >= 1f ? maxSize / aspect : maxSize;
        var rect = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, tex, null, ScaleMode.ScaleToFit);
    }

    void DrawChunkList(MapChunkEditorRoot root)
    {
        if (root.PaintWorldRect.width <= 0f || root.PaintWorldRect.height <= 0f)
        {
            return;
        }

        var coords = new HashSet<ChunkCoord>();
        MapPaintBackgroundShared.CollectPaintRectCoords(root, coords);
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(MapPaintBackgroundShared.GetManifestPath(mapName));

        EditorGUILayout.LabelField($"Chunks ({coords.Count})", EditorStyles.boldLabel);
        chunkListScroll = EditorGUILayout.BeginScrollView(chunkListScroll, GUILayout.MaxHeight(180f));
        foreach (var coord in coords.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            var info = manifest?.GetChunk(coord.X, coord.Y);
            var state = MapPaintChunkState.Resolve(mapName, info, coord);
            var label = MapPaintChunkState.GetLabel(state);
            bool selected = _selectedChunk.HasValue && _selectedChunk.Value.X == coord.X && _selectedChunk.Value.Y == coord.Y;
            bool isStale = info != null && info.TemplateStale;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(selected, $"{coord.X},{coord.Y} [{label}]", EditorStyles.miniButton))
                {
                    _selectedChunk = coord;
                }

                if (isStale)
                {
                    if (GUILayout.Button("Clear Stale", EditorStyles.miniButton, GUILayout.Width(68f)))
                    {
                        SetChunkStale(root, coord, false);
                    }
                }
                else
                {
                    if (GUILayout.Button("Mark Stale", EditorStyles.miniButton, GUILayout.Width(68f)))
                    {
                        SetChunkStale(root, coord, true);
                    }
                }

                bool hasPainted = MapPaintChunkState.HasPainted(mapName, info, coord);
                using (new EditorGUI.DisabledScope(!hasPainted))
                {
                    if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(44f)))
                    {
                        var result = MapPaintBackgroundExporter.ClearUserPainted(root, mapName, coord);
                        LogResult(result.Success, result.Message);
                        Repaint();
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void SetChunkStale(MapChunkEditorRoot root, ChunkCoord coord, bool stale)
    {
        var result = MapPaintBackgroundExporter.SetTemplateStale(root, mapName, coord, stale);
        LogResult(result.Success, result.Message);
        Repaint();
    }

    void PingSelectedExport()
    {
        if (!_selectedChunk.HasValue)
        {
            return;
        }

        var path = MapPaintBackgroundShared.GetChunkForAiPath(mapName, _selectedChunk.Value);
        if (File.Exists(path))
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
            return;
        }

        path = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, _selectedChunk.Value);
        if (File.Exists(path))
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        }
    }

    static void LogResult(bool success, string message)
    {
        if (success)
        {
            Debug.Log("[MapPaint] " + message);
        }
        else
        {
            EditorUtility.DisplayDialog("Map Paint", message, "OK");
            Debug.LogWarning("[MapPaint] " + message);
        }
    }

    void LogImportResult(MapPaintBackgroundImporter.ImportResult result)
    {
        if (!result.Success)
        {
            EditorUtility.DisplayDialog("Map Paint", result.Message, "OK");
            Debug.LogWarning("[MapPaintImport] " + result.Message);
            return;
        }

        Debug.Log("[MapPaintImport] " + result.Message);
        Repaint();
    }

    void SyncFromScene()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        float cellSize = chunkEditor != null ? chunkEditor.ChunkWorldSize : 32f;
        Vector2 origin = chunkEditor != null ? chunkEditor.ChunkOrigin : Vector2.zero;
        MapChunkEditorUtility.SyncChunkSettings(chunkEditor, ref cellSize, ref origin);
        var key = MapChunkEditorUtility.ResolveMapChunkKey(chunkEditor);
        if (!string.IsNullOrEmpty(key))
        {
            mapName = key;
        }
    }

    void OnEnable()
    {
        if (areaRoot == null)
        {
            areaRoot = MapChunkEditorUtility.FindInActiveScene()?.gameObject;
        }

        SyncFromScene();
    }
}
#endif
