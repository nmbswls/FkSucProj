using System.Collections.Generic;
using System.IO;
using System.Text;
using My.Map.Scene;
using UnityEditor;
using UnityEngine;

public static class UnitPresentationHierarchyValidator
{
    const string NpcPrefabRoot = "Assets/Resources/Prefab/Presentations/Npc";
    const string PlayerPrefabPath = "Assets/Resources/Prefab/Presentations/FakePlayer.prefab";

    [MenuItem("Tools/Unit Presentation/Validate All Unit Prefabs")]
    public static void ValidateAllUnitPrefabsMenu()
    {
        var report = ValidateAllUnitPrefabs();
        Debug.Log(report);
        EditorUtility.DisplayDialog("Unit Presentation Validation", report, "OK");
    }

    public static string ValidateAllUnitPrefabs()
    {
        var paths = new List<string>();
        if (Directory.Exists(NpcPrefabRoot))
        {
            paths.AddRange(Directory.GetFiles(NpcPrefabRoot, "*.prefab", SearchOption.TopDirectoryOnly));
        }

        if (File.Exists(PlayerPrefabPath))
        {
            paths.Add(PlayerPrefabPath);
        }

        var sb = new StringBuilder();
        int issueCount = 0;
        foreach (var path in paths)
        {
            var normalized = path.Replace('\\', '/');
            var issues = ValidatePrefabAsset(normalized);
            if (issues.Count == 0)
            {
                sb.AppendLine($"[OK] {normalized}");
                continue;
            }

            issueCount += issues.Count;
            sb.AppendLine($"[FAIL] {normalized}");
            foreach (var issue in issues)
            {
                sb.AppendLine($"  - {issue}");
            }
        }

        sb.Insert(0, $"Validated {paths.Count} prefab(s), {issueCount} issue(s).\n\n");
        return sb.ToString();
    }

    public static List<string> ValidatePrefabAsset(string prefabPath)
    {
        var issues = new List<string>();
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var presenter = root.GetComponentInChildren<SceneUnitPresenter>(true);
            if (presenter == null)
            {
                issues.Add("No SceneUnitPresenter found.");
                return issues;
            }

            ValidatePresenterHierarchy(presenter, issues);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return issues;
    }

    static void ValidatePresenterHierarchy(SceneUnitPresenter presenter, List<string> issues)
    {
        var root = presenter.transform;

        var view = root.Find(UnitPresentationPaths.View) ?? root.Find(UnitPresentationPaths.ViewLegacy);
        if (view == null)
        {
            issues.Add($"Missing child '{UnitPresentationPaths.View}'.");
        }

        if (presenter.ViewRoot == null)
        {
            issues.Add("ViewRoot reference is not assigned.");
        }
        else if (view != null && presenter.ViewRoot != view)
        {
            issues.Add("ViewRoot does not point to the view transform.");
        }

        if (presenter.MainViewRt == null)
        {
            issues.Add("MainViewRt reference is not assigned.");
        }
        else if (presenter.ViewRoot != null && presenter.MainViewRt != presenter.ViewRoot)
        {
            issues.Add("MainViewRt should match ViewRoot.");
        }

        if (view != null)
        {
            bool hasAgentChild = view.Find(UnitPresentationPaths.Agent) != null;
            bool hasShadowChild = view.Find(UnitPresentationPaths.Shadow) != null;
            if (hasAgentChild || hasShadowChild)
            {
                if (!hasAgentChild)
                {
                    issues.Add($"view missing '{UnitPresentationPaths.Agent}'.");
                }

                if (!hasShadowChild)
                {
                    issues.Add($"view missing '{UnitPresentationPaths.Shadow}' (optional but recommended).");
                }
            }

            var weaponInView = view.Find(UnitPresentationPaths.WeaponRoot);
            var weaponAtRoot = root.Find(UnitPresentationPaths.WeaponRoot);
            if (weaponAtRoot != null && weaponInView == null)
            {
                issues.Add("WeaponRoot should be under view, not presenter root.");
            }
        }

        if (presenter.AgentView == null)
        {
            issues.Add("AgentView reference is not assigned.");
        }

        if (presenter.WeaponCtrl == null)
        {
            issues.Add("WeaponCtrl reference is not assigned (skip if unit has no weapons).");
        }
    }
}
