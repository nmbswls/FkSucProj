#if UNITY_EDITOR
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Home01VeraRoutineAssembler
{
    [MenuItem("Tools/Maps/Home 01/Install Vera Routine Practice")]
    public static void Install()
    {
        const string scenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var areaRoot = GameObject.Find("AreaRoot")?.transform;
        if (areaRoot == null) throw new System.Exception("Home_01_Editor has no AreaRoot");

        var namedRoot = areaRoot.Find("NamedPoint") ?? NewChild(areaRoot, "NamedPoint");
        EnsurePoint(namedRoot, "vera_default", new Vector3(14.9f, 29.1f, 0));
        EnsurePoint(namedRoot, "vera_work_area", new Vector3(15.5f, 29.8f, 0));
        EnsurePoint(namedRoot, "vera_sleep_area", new Vector3(14.3f, 28.7f, 0));
        EnsurePoint(namedRoot, "vera_wait_report", new Vector3(15.0f, 29.2f, 0));
        EnsurePoint(namedRoot, "home_pre_survivor_fire", new Vector3(16.0f, 29.0f, 0));
        EnsurePoint(namedRoot, "home_pre_east_watch", new Vector3(22.0f, 29.0f, 0));
        EnsurePoint(namedRoot, "home_pre_herb_shed", new Vector3(18.0f, 29.0f, 0));
        EnsurePoint(namedRoot, "home_pre_injured_shelter", new Vector3(15.0f, 31.0f, 0));
        EnsurePoint(namedRoot, "home_post_fire", new Vector3(16.0f, 30.0f, 0));
        EnsurePoint(namedRoot, "home_post_herb_shed", new Vector3(18.0f, 30.0f, 0));
        EnsurePoint(namedRoot, "home_post_watch", new Vector3(22.0f, 30.0f, 0));
        EnsurePoint(namedRoot, "home_post_east_path", new Vector3(23.0f, 30.0f, 0));
        EnsurePoint(namedRoot, "home_post_sample_table", new Vector3(20.0f, 30.0f, 0));
        EnsurePoint(namedRoot, "home_post_vera_room", new Vector3(14.0f, 29.0f, 0));

        RemoveNpcRefreshGenerators(areaRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var chunkRoot = areaRoot.GetComponent<MapChunkEditorRoot>();
        var variantKey = chunkRoot != null && !string.IsNullOrEmpty(chunkRoot.MapVariantSceneName)
            ? chunkRoot.MapVariantSceneName
            : "Home_01";
        if (chunkRoot != null)
        {
            var chunkResult = MapChunkExportCore.Export(chunkRoot, variantKey, chunkRoot.ChunkWorldSize, chunkRoot.ChunkOrigin);
            var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, chunkRoot, variantKey);
            Debug.Log($"[Home01VeraRoutineAssembler] Map export chunk={chunkResult.Success} overlay={overlayResult.Success}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Home01VeraRoutineAssembler] Installed routine named points and removed NPC refresh generators.");
    }

    static void RemoveNpcRefreshGenerators(Transform areaRoot)
    {
        var generators = areaRoot.GetComponentsInChildren<DynamicEntityExportGenerator>(true);
        foreach (var generator in generators)
        {
            if (generator == null || generator.RefreshInfo?.InitInfo is not EntityInitInfo4Npc)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(generator.gameObject);
        }
    }

    static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsurePoint(Transform parent, string name, Vector3 position)
    {
        var point = parent.Find(name) ?? NewChild(parent, name);
        point.position = position;
        var generator = point.GetComponent<NamePointGenerator>() ?? point.gameObject.AddComponent<NamePointGenerator>();
        generator.Info = new NamedPoint
        {
            Name = name,
            PointType = ENamedPointType.Normal,
            Position = position,
            Rotation = point.rotation,
            Scale = point.localScale,
        };
        EditorUtility.SetDirty(point.gameObject);
    }
}
#endif
