using System.Text;
using My.MapExport;
using UnityEditor;
using UnityEngine;

// 统一地图导出窗口（Variant Chunk + Overlay MapExport + NavMesh）
public class MapExporterWindow : EditorWindow
{
    [SerializeField] GameObject areaRoot;
    [SerializeField] bool exportTilemapChunks = true;
    [SerializeField] bool exportVisualBake = true;
    [SerializeField] bool exportWalkGridPrefab = true;
    [SerializeField] bool exportNavMesh = true;

    MapOverlayExportCore.ScanSummary _scanSummary;
    MapExporterValidator.ValidationResult _validation;
    string _variantKey = string.Empty;
    Vector2 _scroll;

    [MenuItem("Window/Map Exporter")]
    public static void Open()
    {
        GetWindow<MapExporterWindow>("Map Exporter");
    }

    void OnEnable()
    {
        if (areaRoot == null)
        {
            areaRoot = MapChunkEditorUtility.FindInActiveScene()?.gameObject;
        }

        RefreshContext();
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        areaRoot = (GameObject)EditorGUILayout.ObjectField("Area Root", areaRoot, typeof(GameObject), true);
        if (GUILayout.Button("Use Selected / Refresh"))
        {
            if (Selection.activeGameObject != null)
            {
                var editor = MapChunkEditorUtility.Resolve(Selection.activeGameObject);
                if (editor != null)
                {
                    areaRoot = editor.gameObject;
                }
            }

            RefreshContext();
        }

        DrawContext();
        DrawValidation();
        DrawScanPreview();
        DrawExportOptions();
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    void DrawContext()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        EditorGUILayout.LabelField("Variant Key", string.IsNullOrEmpty(_variantKey) ? "(none)" : _variantKey);
        if (chunkEditor != null)
        {
            EditorGUILayout.LabelField("Chunk Origin", chunkEditor.ChunkOrigin.ToString());
            EditorGUILayout.LabelField("Grid",
                MapChunkEditorTilemapResolver.HasTilemapSource(chunkEditor) ? "Ready" : "Missing");
        }
    }

    void DrawValidation()
    {
        if (_validation.Issues == null || _validation.Issues.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        var msgType = _validation.CanExport ? MessageType.Warning : MessageType.Error;
        EditorGUILayout.HelpBox(MapExporterValidator.FormatIssues(_validation.Issues), msgType);
    }

    void DrawScanPreview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        if (_scanSummary.Overlays != null && _scanSummary.Overlays.Count > 0)
        {
            foreach (var o in _scanSummary.Overlays)
            {
                EditorGUILayout.LabelField($"  {o.OverlayId}",
                    $"static={o.StaticCount}, dynamic={o.DynamicCount}");
            }
        }
        else
        {
            EditorGUILayout.LabelField("  (no overlay scan yet)");
        }

        EditorGUILayout.LabelField("NamedPoint", _scanSummary.NamedPointCount.ToString());
        EditorGUILayout.LabelField("NamedPath", _scanSummary.NamedPathCount.ToString());
        EditorGUILayout.LabelField("PortalNetwork", _scanSummary.PortalNetworkCount.ToString());
    }

    void DrawExportOptions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Variant Export", EditorStyles.boldLabel);
        exportWalkGridPrefab = EditorGUILayout.Toggle("Walk Grid Prefab", exportWalkGridPrefab);
        exportTilemapChunks = EditorGUILayout.Toggle("Tilemap Chunks (tm_*)", exportTilemapChunks);
        using (new EditorGUI.DisabledScope(!exportTilemapChunks))
        {
            exportVisualBake = EditorGUILayout.Toggle("  Visual Bake", exportVisualBake);
        }

        exportNavMesh = EditorGUILayout.Toggle("NavMesh Data", exportNavMesh);

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Map Paint Background"))
        {
            EditorApplication.ExecuteMenuItem("Window/Map Paint Background");
        }
    }

    void DrawActions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan"))
        {
            RefreshContext();
        }

        using (new EditorGUI.DisabledScope(!_validation.CanExport))
        {
            if (GUILayout.Button("Export All", GUILayout.Height(28)))
            {
                RunExportAll();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Variant"))
            {
                RunExportVariant(showDialog: true);
            }

            if (GUILayout.Button("Export Overlays"))
            {
                RunExportOverlays(showDialog: true);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    void RefreshContext()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        _variantKey = MapChunkEditorUtility.ResolveMapChunkKey(chunkEditor) ?? string.Empty;
        _validation = MapExporterValidator.Validate(areaRoot, chunkEditor, _variantKey);
        _scanSummary = MapOverlayExportCore.ScanAll(areaRoot, chunkEditor, _variantKey);
    }

    void RunExportAll()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        RefreshContext();
        if (!_validation.CanExport)
        {
            EditorUtility.DisplayDialog("Map Export", MapExporterValidator.FormatIssues(_validation.Issues), "OK");
            return;
        }

        var log = new StringBuilder();

        var variantResult = RunExportVariant(showDialog: false);
        if (!variantResult)
        {
            return;
        }

        log.AppendLine("Variant export OK.");

        if (exportNavMesh)
        {
            var navResult = MapNavMeshExportCore.Export(areaRoot, _variantKey);
            if (!navResult.Success)
            {
                EditorUtility.DisplayDialog("Map Export", navResult.Message, "OK");
                return;
            }

            log.AppendLine(navResult.Skipped ? navResult.Message : navResult.Message);
        }

        var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot, chunkEditor, _variantKey);
        if (!overlayResult.Success)
        {
            EditorUtility.DisplayDialog("Map Export", overlayResult.Message, "OK");
            return;
        }

        log.AppendLine(overlayResult.Message);
        RefreshContext();
        EditorUtility.DisplayDialog("Map Export", log.ToString(), "OK");
    }

    bool RunExportVariant(bool showDialog)
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        if (chunkEditor == null)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Map Export", "MapChunkEditorRoot not found.", "OK");
            }

            return false;
        }

        if (!exportTilemapChunks && !exportWalkGridPrefab)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Map Export", "Enable at least one variant export option.", "OK");
            }

            return false;
        }

        var chunkSize = chunkEditor.ChunkWorldSize;
        var chunkOrigin = chunkEditor.ChunkOrigin;
        chunkEditor.SceneName = _variantKey;
        EditorUtility.SetDirty(chunkEditor);

        var result = MapChunkExportCore.Export(
            chunkEditor,
            _variantKey,
            chunkSize,
            chunkOrigin,
            exportTilemapChunks,
            exportWalkGridPrefab,
            exportVisualBake);

        if (!result.Success)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Map Export", result.Message, "OK");
            }

            Debug.LogWarning("[MapExport] " + result.Message);
            return false;
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Map Export", result.Message, "OK");
            if (result.Database != null)
            {
                EditorGUIUtility.PingObject(result.Database);
            }
        }

        return true;
    }

    void RunExportOverlays(bool showDialog)
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        RefreshContext();
        if (!_validation.CanExport)
        {
            EditorUtility.DisplayDialog("Map Export", MapExporterValidator.FormatIssues(_validation.Issues), "OK");
            return;
        }

        var result = MapOverlayExportCore.ExportAllOverlays(areaRoot, chunkEditor, _variantKey);
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Map Export", result.Message, result.Success ? "OK" : "OK");
        }

        if (result.Success)
        {
            RefreshContext();
        }
    }
}
