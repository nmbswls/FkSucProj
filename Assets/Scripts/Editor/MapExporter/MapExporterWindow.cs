using System.Text;
using My.MapExport;
using UnityEditor;
using UnityEngine;

// 统一地图导出窗口（Variant Chunk + Overlay MapExport）
public class MapExporterWindow : EditorWindow
{
    [SerializeField] GameObject areaRoot;

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
        DrawActions();

        EditorGUILayout.EndScrollView();
    }

    void DrawContext()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        EditorGUILayout.LabelField("MapVariant Scene", string.IsNullOrEmpty(_variantKey) ? "(none)" : _variantKey);
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

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Map Paint Background"))
        {
            EditorApplication.ExecuteMenuItem("Window/Map Paint Background");
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

        var chunkSize = chunkEditor.ChunkWorldSize;
        var chunkOrigin = chunkEditor.ChunkOrigin;
        chunkEditor.MapVariantSceneName = _variantKey;
        EditorUtility.SetDirty(chunkEditor);

        var result = MapChunkExportCore.Export(chunkEditor, _variantKey, chunkSize, chunkOrigin);

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
