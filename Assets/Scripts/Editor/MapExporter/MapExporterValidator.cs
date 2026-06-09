using System.Collections.Generic;
using System.Text;
using My.MapExport;
using UnityEngine;

// 导出前基础校验
public static class MapExporterValidator
{
    public struct Issue
    {
        public bool IsError;
        public string Message;
    }

    public struct ValidationResult
    {
        public bool CanExport;
        public List<Issue> Issues;
    }

    public static ValidationResult Validate(GameObject areaRoot, MapChunkEditorRoot chunkEditor, string variantSceneName)
    {
        var issues = new List<Issue>();
        if (areaRoot == null)
        {
            issues.Add(Error("AreaRoot is not assigned."));
            return new ValidationResult { CanExport = false, Issues = issues };
        }

        if (chunkEditor == null)
        {
            issues.Add(Error("MapChunkEditorRoot not found on AreaRoot."));
            return new ValidationResult { CanExport = false, Issues = issues };
        }

        if (string.IsNullOrWhiteSpace(variantSceneName))
        {
            issues.Add(Error("Variant scene key is empty. Set MapChunkEditorRoot.SceneName."));
            return new ValidationResult { CanExport = false, Issues = issues };
        }

        if (!MapChunkEditorTilemapResolver.HasTilemapSource(chunkEditor))
        {
            issues.Add(Warn("GridRoot / Tilemap not ready under StaticRoot."));
        }

        var variant = MapExporterConfigReader.FindVariantBySceneName(variantSceneName);
        if (variant == null)
        {
            issues.Add(Warn($"No AreaVariantInfo for scene '{variantSceneName}'."));
        }

        var overlays = MapExporterConfigReader.GetOverlaysForVariantScene(variantSceneName);
        if (overlays.Count == 0)
        {
            issues.Add(Warn("No overlay entries in config; overlay export uses legacy DynamicRoot scan."));
        }
        else
        {
            var dynamicRoot = areaRoot.transform.Find("DynamicRoot");
            var staticRoot = areaRoot.transform.Find("StaticRoot");
            foreach (var overlay in overlays)
            {
                if (dynamicRoot != null && dynamicRoot.Find(overlay.Id) == null && dynamicRoot.Find(MapOverlayExportCore.CommonFolderName) == null)
                {
                    issues.Add(Warn($"DynamicRoot missing folder '{overlay.Id}' (will fallback to full DynamicRoot)."));
                }

                var overlayRoot = staticRoot != null ? staticRoot.Find(MapOverlayExportCore.StaticOverlayFolderName) : null;
                if (overlayRoot != null && overlayRoot.Find(overlay.Id) == null && overlayRoot.Find(MapOverlayExportCore.CommonFolderName) == null)
                {
                    issues.Add(Warn($"StaticOverlay missing folder '{overlay.Id}'."));
                }
            }
        }

        bool hasError = false;
        foreach (var issue in issues)
        {
            if (issue.IsError)
            {
                hasError = true;
                break;
            }
        }

        return new ValidationResult { CanExport = !hasError, Issues = issues };
    }

    public static string FormatIssues(IReadOnlyList<Issue> issues)
    {
        if (issues == null || issues.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var issue in issues)
        {
            sb.Append(issue.IsError ? "[Error] " : "[Warn] ");
            sb.AppendLine(issue.Message);
        }

        return sb.ToString();
    }

    static Issue Error(string message) => new Issue { IsError = true, Message = message };
    static Issue Warn(string message) => new Issue { IsError = false, Message = message };
}
