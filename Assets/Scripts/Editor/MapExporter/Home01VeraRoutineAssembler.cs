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

        var dynamicRoot = areaRoot.Find("DynamicRoot") ?? NewChild(areaRoot, "DynamicRoot");
        var common = dynamicRoot.Find("Common") ?? NewChild(dynamicRoot, "Common");
        var villagers = common.Find("镇民") ?? NewChild(common, "镇民");
        var vera = villagers.Find("home_vera");
        if (vera == null) vera = NewChild(villagers, "home_vera");
        vera.position = new Vector3(14.9f, 29.1f, 0);
        var generator = vera.GetComponent<DynamicEntityExportGenerator>() ?? vera.gameObject.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = "home_vera",
            InitInfo = new EntityInitInfo4Npc
            {
                CfgId = "home_vera",
                Position = Vector2.zero,
                FaceDir = Vector2.right,
                MoveMode = My.Map.UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                IsPeace = true,
                CharacterKey = "home_vera",
            },
        };
        EditorUtility.SetDirty(vera.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var chunkRoot = areaRoot.GetComponent<MapChunkEditorRoot>();
        var variantKey = chunkRoot != null && !string.IsNullOrEmpty(chunkRoot.MapVariantSceneName) ? chunkRoot.MapVariantSceneName : "Home_01";
        if (chunkRoot != null)
        {
            var chunkResult = MapChunkExportCore.Export(chunkRoot, variantKey, chunkRoot.ChunkWorldSize, chunkRoot.ChunkOrigin);
            var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, chunkRoot, variantKey);
            Debug.Log($"[Home01VeraRoutineAssembler] Map export chunk={chunkResult.Success} overlay={overlayResult.Success}");
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Home01VeraRoutineAssembler] Installed Vera routine practice and named points.");
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
        generator.Info = new NamedPoint { Name = name, PointType = ENamedPointType.Normal, Position = position, Rotation = point.rotation, Scale = point.localScale };
        EditorUtility.SetDirty(point.gameObject);
    }
}
#endif
