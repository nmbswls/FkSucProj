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
    [SerializeField] Texture2D importChunkTexture;
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
        EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        root.PaintWorldRect = EditorGUILayout.RectField("Paint World Rect", root.PaintWorldRect);
        root.PaintMaskColor = EditorGUILayout.ColorField("Mask Color", root.PaintMaskColor);
        root.PaintCaptureLayerMask = LayerMaskField("Capture Layers", root.PaintCaptureLayerMask);
        root.PaintCaptureCameraZ = EditorGUILayout.FloatField("Camera Z", root.PaintCaptureCameraZ);
        root.PaintExportPPU = EditorGUILayout.FloatField("Export PPU (0=TexturePPU)", root.PaintExportPPU);
        root.TexturePPU = EditorGUILayout.FloatField("Runtime PPU", root.TexturePPU);
        root.BackgroundSortingOrder = EditorGUILayout.IntField("Background Sort Order", root.BackgroundSortingOrder);
        root.PaintContextExpandRatio = EditorGUILayout.Slider("Context Expand", root.PaintContextExpandRatio, 0f, 0.49f);
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

        EditorGUILayout.LabelField("Slice Px", root.PaintSlicePixelSize.ToString());
    }

    void DrawWorkflow(MapChunkEditorRoot root)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Ready → Export → Import → Done\n" +
            "改 tile 后 Mark Stale；Stale+Painted 会导出 for_ai + painted_ref 两张图\n" +
            "对齐场景后 Re-capture（自动 Clear Stale）",
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

        importChunkTexture = (Texture2D)EditorGUILayout.ObjectField("Import PNG", importChunkTexture, typeof(Texture2D), false);
        using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue || importChunkTexture == null))
        {
            if (GUILayout.Button("Import Chunk", GUILayout.Height(28f)))
            {
                var result = MapPaintBackgroundImporter.ImportChunkForAi(
                    root, mapName, _selectedChunk.Value, importChunkTexture, resampleFilter);
                LogImportResult(result);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply To Database"))
            {
                var result = MapPaintBackgroundExporter.ApplyToDatabase(root, mapName);
                LogResult(result.Success, result.Message);
            }

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
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        root.PaintPreviewEnabled = EditorGUILayout.Toggle("Show In Scene", root.PaintPreviewEnabled);
        root.PaintAutoRefreshPreview = EditorGUILayout.Toggle("Auto Refresh", root.PaintAutoRefreshPreview);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(root);
            if (root.PaintPreviewEnabled)
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

    static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        int mask = EditorGUILayout.MaskField(label, layerMask.value, InternalEditorUtility.layers);
        return mask;
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
