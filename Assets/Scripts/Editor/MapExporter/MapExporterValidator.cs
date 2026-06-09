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

    public static ValidationResult Validate(GameObject areaRoot, MapChunkEditorRoot chunkEditor, string mapVariantSceneName)
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

        if (string.IsNullOrWhiteSpace(mapVariantSceneName))
        {
            issues.Add(Error("Map variant scene name is empty. Set MapChunkEditorRoot.MapVariantSceneName."));
            return new ValidationResult { CanExport = false, Issues = issues };
        }

        if (!MapChunkEditorTilemapResolver.HasTilemapSource(chunkEditor))
        {
            issues.Add(Warn($"GridRoot / Tilemap not ready under {MapVariantSceneHierarchy.MapVariantRootName}."));
        }

        var variant = MapExporterConfigReader.FindVariantBySceneName(mapVariantSceneName);
        if (variant == null)
        {
            issues.Add(Warn($"No AreaVariantInfo for scene '{mapVariantSceneName}'."));
        }

        var overlays = MapExporterConfigReader.GetOverlaysForVariantScene(mapVariantSceneName);
        if (overlays.Count == 0)
        {
            issues.Add(Warn("No overlay entries in config; overlay export uses legacy scan."));
        }
        else
        {
            ValidateOverlayHierarchy(areaRoot.transform, overlays, issues);
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

    static void ValidateOverlayHierarchy(Transform areaRoot, List<cfg.demo.AreaOverlayStateInfo> overlays, List<Issue> issues)
    {
        var mapVariantRoot = MapVariantSceneHierarchy.ResolveMapVariantRoot(areaRoot);
        if (mapVariantRoot == null)
        {
            issues.Add(Warn($"{MapVariantSceneHierarchy.MapVariantRootName} not found."));
        }
        else if (mapVariantRoot.Find(MapVariantSceneHierarchy.DecorateFolderName) == null &&
                 mapVariantRoot.Find(MapVariantSceneHierarchy.TriggerFolderName) == null)
        {
            issues.Add(Warn(
                $"{MapVariantSceneHierarchy.MapVariantRootName} missing {MapVariantSceneHierarchy.DecorateFolderName}/" +
                $"{MapVariantSceneHierarchy.TriggerFolderName}; static export may be empty."));
        }

        var dynamicRoot = MapVariantSceneHierarchy.ResolveDynamicRoot(areaRoot);
        if (dynamicRoot == null)
        {
            issues.Add(Warn($"{MapVariantSceneHierarchy.DynamicRootName} not found."));
            return;
        }

        foreach (var overlay in overlays)
        {
            if (dynamicRoot.Find(overlay.Id) == null &&
                dynamicRoot.Find(MapVariantSceneHierarchy.CommonFolderName) == null)
            {
                issues.Add(Warn($"DynamicRoot missing folder '{overlay.Id}' (will fallback to full DynamicRoot)."));
            }
        }
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
