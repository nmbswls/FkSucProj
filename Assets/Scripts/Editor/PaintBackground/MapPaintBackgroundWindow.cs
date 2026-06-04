#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using My.Map.Logic;
using My.MapExport;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

//Assets / Resources / MapChunk /{ mapName}/
//├── PaintExport /
//│   ├── atlas_for_ai.png              ← Generate 产出（给 AI 的整图）
//│   ├── manifest.asset
//│   ├── chunks/
//│   │   ├── chunk_{x}_{y}.png        ← 模板（Camera 拍摄 / Sync Outline）
//│   │   └── painted_{x}_{y}.png     ← Import 写入的回稿
//│   └── backup_rev{N}/               ← Import 前备份旧 painted_*
//├── Sprites/
//│   └── bg_{x}_{y}.png               ← 运行时背景 sprite（Import 会覆盖）
//└── Prefabs/
//    └── bg_{x}_{y}.prefab            ← 运行时背景 prefab（Import 会覆盖）



public class MapPaintBackgroundWindow : EditorWindow
{
    enum Tab
    {
        Export = 0,
        Import = 1,
    }

    [SerializeField] GameObject areaRoot;
    [SerializeField] string mapName = "Main_Area_01";
    [SerializeField] Tab currentTab = Tab.Export;
    [SerializeField] Texture2D importAtlas;
    [SerializeField] Texture2D importSingleChunk;
    [SerializeField] Vector2 chunkListScroll;
    [SerializeField] FilterMode resampleFilter = FilterMode.Bilinear;

    readonly Dictionary<(int x, int y), bool> _resetFlags = new Dictionary<(int, int), bool>();
    ChunkCoord? _selectedChunk;

    [MenuItem("Window/Map Paint Background")]
    public static void Open()
    {
        GetWindow<MapPaintBackgroundWindow>("Map Paint Background");
    }

    void OnGUI()
    {
        DrawHeader();
        currentTab = (Tab)GUILayout.Toolbar((int)currentTab, new[] { "Paint Export", "Paint Import" });
        EditorGUILayout.Space();

        var root = MapChunkEditorUtility.Resolve(areaRoot);
        if (root == null)
        {
            EditorGUILayout.HelpBox("AreaRoot 上需要 MapChunkEditorRoot 组件。", MessageType.Warning);
            return;
        }

        if (currentTab == Tab.Export)
        {
            DrawExportTab(root);
        }
        else
        {
            DrawImportTab(root);
        }
    }

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Map Source", EditorStyles.boldLabel);
        areaRoot = (GameObject)EditorGUILayout.ObjectField("Area Root", areaRoot, typeof(GameObject), true);
        if (GUILayout.Button("Use Selected AreaRoot"))
        {
            if (Selection.activeGameObject != null)
            {
                areaRoot = Selection.activeGameObject;
            }
        }

        mapName = EditorGUILayout.TextField("Scene Name (Variant)", mapName);
        if (GUILayout.Button("Sync From MapChunkEditorRoot"))
        {
            SyncFromScene();
        }
    }

    void DrawExportTab(MapChunkEditorRoot root)
    {
        EditorGUI.BeginChangeCheck();
        root.PaintWorldRect = EditorGUILayout.RectField("Paint World Rect", root.PaintWorldRect);
        root.PaintMaskColor = EditorGUILayout.ColorField("Mask Color (Camera Clear)", root.PaintMaskColor);
        root.PaintCaptureLayerMask = LayerMaskField("Capture Layer Mask", root.PaintCaptureLayerMask);
        root.PaintCaptureCameraZ = EditorGUILayout.FloatField("Capture Camera Z", root.PaintCaptureCameraZ);
        root.PaintExportPPU = EditorGUILayout.FloatField("Paint Export PPU (0=TexturePPU)", root.PaintExportPPU);
        root.TexturePPU = EditorGUILayout.FloatField("Runtime Texture PPU", root.TexturePPU);
        root.BackgroundSortingOrder = EditorGUILayout.IntField("Background Sorting Order", root.BackgroundSortingOrder);
        root.PaintContextExpandRatio = EditorGUILayout.Slider("Context Expand Ratio", root.PaintContextExpandRatio, 0f, 0.49f);
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(root);
        }

        EditorGUILayout.LabelField("Paint Slice Px", root.PaintSlicePixelSize.ToString());
        EditorGUILayout.LabelField("Effective Paint PPU", root.EffectivePaintExportPpu.ToString());
        if (root.PaintContextExpandRatio > 0f)
        {
            int ctxSize = MapPaintBackgroundContext.ComputeContextSize(root.PaintSlicePixelSize, root.PaintContextExpandRatio);
            int margin = MapPaintBackgroundContext.ComputeMarginPx(root.PaintSlicePixelSize, root.PaintContextExpandRatio);
            EditorGUILayout.LabelField("Single Chunk For AI Size", $"{ctxSize}x{ctxSize} (margin {margin}px)");
        }

        EditorGUILayout.HelpBox(
            "Export 使用正交 Camera 逐 chunk 拍摄当前 Editor 场景。" +
            "Layer Mask 内的 Tilemap、SpriteRenderer 等都会进入模板；Clear Color 为 Magenta 留白。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Snap Rect To Chunk Grid"))
            {
                root.PaintWorldRect = MapChunkUtility.SnapWorldRectToChunkGrid(
                    root.PaintWorldRect,
                    root.ChunkOrigin,
                    root.ChunkWorldSize);
                EditorUtility.SetDirty(root);
            }

            if (GUILayout.Button("Rect From Capture Layers"))
            {
                root.PaintWorldRect = MapPaintBackgroundCapture.ComputeBoundsFromCaptureLayers(root);
                EditorUtility.SetDirty(root);
            }
        }

        DrawChunkList(root);
        DrawPreviewSection(root);

        using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue))
        {
            if (GUILayout.Button("Export Selected Chunk For AI (With Context)"))
            {
                ApplyResetFlagsToManifest(root);
                var result = MapPaintBackgroundExporter.ExportSingleChunkForAi(
                    root,
                    mapName,
                    _selectedChunk.Value,
                    resampleFilter);
                ShowResult(result.Success, result.Message, result.Manifest);
                if (result.Success)
                {
                    var path = MapPaintBackgroundShared.GetChunkForAiPath(mapName, _selectedChunk.Value);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
                }
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Paint Atlas"))
        {
            ApplyResetFlagsToManifest(root);
            var result = MapPaintBackgroundExporter.GenerateAtlas(root, mapName, resampleFilter);
            ShowResult(result.Success, result.Message, result.Manifest);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sync Outline (All)"))
            {
                var result = MapPaintBackgroundExporter.SyncOutline(root, mapName, null, true, resampleFilter);
                ShowResult(result.Success, result.Message, result.Manifest);
            }

            if (GUILayout.Button("Sync Outline (Selected)"))
            {
                if (!_selectedChunk.HasValue)
                {
                    EditorUtility.DisplayDialog("Sync Outline", "Select a chunk in the list first.", "OK");
                }
                else
                {
                    var result = MapPaintBackgroundExporter.SyncOutline(
                        root,
                        mapName,
                        new[] { _selectedChunk.Value },
                        false,
                        resampleFilter);
                    ShowResult(result.Success, result.Message, result.Manifest);
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear Painted (All)"))
            {
                var result = MapPaintBackgroundExporter.ClearPainted(root, mapName, null, true);
                ShowResult(result.Success, result.Message, result.Manifest);
            }

            if (GUILayout.Button("Clear Painted (Selected)"))
            {
                if (!_selectedChunk.HasValue)
                {
                    EditorUtility.DisplayDialog("Clear Painted", "Select a chunk in the list first.", "OK");
                }
                else
                {
                    var result = MapPaintBackgroundExporter.ClearPainted(
                        root,
                        mapName,
                        new[] { _selectedChunk.Value },
                        false);
                    ShowResult(result.Success, result.Message, result.Manifest);
                }
            }
        }

        if (GUILayout.Button("Apply bg_* To MapChunk Database"))
        {
            var result = MapPaintBackgroundExporter.ApplyToDatabase(root, mapName);
            ShowResult(result.Success, result.Message, null);
        }

        DrawOutputPathsHelp(mapName);
    }

    void DrawImportTab(MapChunkEditorRoot root)
    {
        resampleFilter = (FilterMode)EditorGUILayout.EnumPopup("Resample Filter", resampleFilter);
        importAtlas = (Texture2D)EditorGUILayout.ObjectField("Painted Atlas", importAtlas, typeof(Texture2D), false);

        EditorGUILayout.HelpBox(
            "Import 写入 painted_{x}_{y}.png（不覆盖 chunk_{x}_{y}.png 模板），" +
            "并更新 Sprites/bg_* 与 Prefabs/bg_*。",
            MessageType.Info);

        if (GUILayout.Button("Import Painted Atlas"))
        {
            var result = MapPaintBackgroundImporter.ImportPaintedAtlas(root, mapName, importAtlas, resampleFilter);
            ShowImportResult(result);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Single Chunk Import", EditorStyles.boldLabel);
        if (_selectedChunk.HasValue)
        {
            EditorGUILayout.LabelField("Selected", $"{_selectedChunk.Value.X}, {_selectedChunk.Value.Y}");
            var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(MapPaintBackgroundShared.GetManifestPath(mapName));
            int slicePx = manifest != null && manifest.SlicePixelSize > 0 ? manifest.SlicePixelSize : root.PaintSlicePixelSize;
            float ratio = manifest != null && manifest.ContextExpandRatio > 0f
                ? manifest.ContextExpandRatio
                : root.PaintContextExpandRatio;
            int ctxSize = MapPaintBackgroundContext.ComputeContextSize(slicePx, ratio);
            EditorGUILayout.LabelField("Context Import Expected Size", $"{ctxSize}x{ctxSize}");
        }

        EditorGUILayout.HelpBox(
            "外扩导出：Export Selected Chunk For AI → 编辑 export_ai/chunk_*_for_ai.png → " +
            "Import (Crop Context Margin) 裁回 painted_*。拼接时优先使用 painted_* 作为邻块/已绘参考。",
            MessageType.Info);

        importSingleChunk = (Texture2D)EditorGUILayout.ObjectField("Chunk PNG", importSingleChunk, typeof(Texture2D), false);
        using (new EditorGUI.DisabledScope(!_selectedChunk.HasValue || importSingleChunk == null))
        {
            if (GUILayout.Button("Import Selected Chunk (Exact Size)"))
            {
                var result = MapPaintBackgroundImporter.ImportSingleChunk(
                    root,
                    mapName,
                    _selectedChunk.Value,
                    importSingleChunk,
                    resampleFilter);
                ShowImportResult(result);
            }

            if (GUILayout.Button("Import Selected Chunk (Crop Context Margin)"))
            {
                var result = MapPaintBackgroundImporter.ImportSingleChunkWithContext(
                    root,
                    mapName,
                    _selectedChunk.Value,
                    importSingleChunk,
                    resampleFilter);
                ShowImportResult(result);
            }
        }

        DrawOutputPathsHelp(mapName);
        DrawPreviewSection(root);
    }

    void DrawPreviewSection(MapChunkEditorRoot root)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        root.PaintPreviewEnabled = EditorGUILayout.Toggle("Show In Scene", root.PaintPreviewEnabled);
        root.PaintAutoRefreshPreview = EditorGUILayout.Toggle("Auto Refresh After Import/Generate", root.PaintAutoRefreshPreview);
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

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Scene Preview"))
            {
                var result = MapPaintBackgroundPreview.SyncToScene(root, mapName);
                if (!result.Success)
                {
                    EditorUtility.DisplayDialog("Paint Preview", result.Message, "OK");
                }
                else
                {
                    Debug.Log("[MapPaintPreview] " + result.Message);
                }
            }

            if (GUILayout.Button("Clear Scene Preview"))
            {
                var result = MapPaintBackgroundPreview.ClearPreview(root);
                Debug.Log("[MapPaintPreview] " + result.Message);
            }
        }

        EditorGUILayout.HelpBox(
            $"在 AreaRoot 下创建 {MapPaintBackgroundPreview.PreviewRootName}，" +
            "按 chunk 网格实例化 bg_* prefab，与运行时摆放方式一致。",
            MessageType.Info);

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
        EditorGUILayout.LabelField($"Selected Thumbnail ({_selectedChunk.Value.X}, {_selectedChunk.Value.Y})", EditorStyles.boldLabel);
        if (tex == null)
        {
            EditorGUILayout.LabelField("(no chunk / painted / bg texture yet)", EditorStyles.miniLabel);
            return;
        }

        const float maxSize = 140f;
        float aspect = tex.width / (float)Mathf.Max(1, tex.height);
        float w = aspect >= 1f ? maxSize : maxSize * aspect;
        float h = aspect >= 1f ? maxSize / aspect : maxSize;
        var rect = GUILayoutUtility.GetRect(w, h, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, tex, null, ScaleMode.ScaleToFit);
        EditorGUILayout.LabelField($"{tex.width} x {tex.height} px", EditorStyles.miniLabel);
    }

    static void DrawOutputPathsHelp(string mapName)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output Paths", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(MapPaintBackgroundShared.GetAtlasPath(mapName), EditorStyles.miniLabel, GUILayout.Height(16f));
        EditorGUILayout.SelectableLabel(MapPaintBackgroundShared.GetChunksFolder(mapName), EditorStyles.miniLabel, GUILayout.Height(16f));
        EditorGUILayout.SelectableLabel(MapPaintBackgroundShared.GetExportAiFolder(mapName), EditorStyles.miniLabel, GUILayout.Height(16f));
        EditorGUILayout.SelectableLabel($"{MapPaintBackgroundShared.GetMapRootFolder(mapName)}/Sprites", EditorStyles.miniLabel, GUILayout.Height(16f));

        var atlasPath = MapPaintBackgroundShared.GetAtlasPath(mapName);
        if (File.Exists(atlasPath) && GUILayout.Button("Ping Atlas"))
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(atlasPath));
        }
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

        EditorGUILayout.LabelField($"Chunks In Rect ({coords.Count})", EditorStyles.boldLabel);
        chunkListScroll = EditorGUILayout.BeginScrollView(chunkListScroll, GUILayout.MaxHeight(220f));
        foreach (var coord in coords.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            var info = manifest?.GetChunk(coord.X, coord.Y);
            var source = info != null ? info.Source.ToString() : "New";
            bool selected = _selectedChunk.HasValue && _selectedChunk.Value.X == coord.X && _selectedChunk.Value.Y == coord.Y;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(selected, $"{coord.X},{coord.Y} [{source}]", EditorStyles.miniButton))
                {
                    _selectedChunk = coord;
                }

                bool reset = GetResetFlag(coord, info);
                bool newReset = EditorGUILayout.ToggleLeft("Reset", reset, GUILayout.Width(60f));
                if (newReset != reset)
                {
                    _resetFlags[(coord.X, coord.Y)] = newReset;
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        var layers = InternalEditorUtility.layers;
        int mask = layerMask.value;
        mask = EditorGUILayout.MaskField(label, mask, layers);
        return mask;
    }

    bool GetResetFlag(ChunkCoord coord, MapPaintChunkInfo info)
    {
        if (_resetFlags.TryGetValue((coord.X, coord.Y), out var flag))
        {
            return flag;
        }

        return info != null && info.ResetOnExport;
    }

    void ApplyResetFlagsToManifest(MapChunkEditorRoot root)
    {
        var manifest = AssetDatabase.LoadAssetAtPath<MapPaintManifest>(MapPaintBackgroundShared.GetManifestPath(mapName));
        if (manifest == null)
        {
            return;
        }

        foreach (var pair in _resetFlags)
        {
            var info = manifest.GetOrCreateChunk(new ChunkCoord(pair.Key.x, pair.Key.y));
            info.ResetOnExport = pair.Value;
        }

        EditorUtility.SetDirty(manifest);
    }

    void ShowResult(bool success, string message, MapPaintManifest manifest)
    {
        if (!success)
        {
            EditorUtility.DisplayDialog("Map Paint", message, "OK");
            Debug.LogWarning("[MapPaint] " + message);
            return;
        }

        Debug.Log("[MapPaint] " + message);
        EditorUtility.DisplayDialog("Map Paint", message, "OK");
        if (manifest != null)
        {
            EditorGUIUtility.PingObject(manifest);
        }
    }

    void ShowImportResult(MapPaintBackgroundImporter.ImportResult result)
    {
        if (!result.Success)
        {
            EditorUtility.DisplayDialog("Paint Import", result.Message, "OK");
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
