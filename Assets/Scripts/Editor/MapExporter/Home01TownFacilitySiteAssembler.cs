#if UNITY_EDITOR

using cfg.demo;

using My.Home;

using My.Map;

using SimpleJSON;

using UnityEditor;

using UnityEditor.SceneManagement;

using UnityEngine;



public static class Home01TownFacilitySiteAssembler

{

    const string ScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";

    const string OverlayId = "homestead_01";



    const int SiteTavern = 1;

    const int SiteTransportCamp = 2;



    [MenuItem("Tools/Maps/Home 01/Install Town Facility Sites")]

    public static void Install()

    {

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var areaRoot = GameObject.Find("AreaRoot")?.transform;

        if (areaRoot == null)

        {

            throw new System.Exception("Home_01_Editor has no AreaRoot");

        }



        var overlayRoot = EnsureOverlayRoot(areaRoot, OverlayId);

        EnsureSite(

            overlayRoot,

            "site_tavern_ruin",

            new Vector3(14f, 31f, 0f),

            "Presentations/FacilityRuin/ruin_tavern",

            SiteTavern,

            MakeNoneCond(),

            MakeSiteLevelCond(SiteTavern, 1));

        EnsureSite(

            overlayRoot,

            "site_tavern_built",

            new Vector3(14f, 31f, 0f),

            "Presentations/HomeFacility/tavern",

            SiteTavern,

            MakeSiteLevelCond(SiteTavern, 1),

            MakeNoneCond());

        EnsureSite(

            overlayRoot,

            "site_transport_ruin",

            new Vector3(21f, 27.5f, 0f),

            "Presentations/FacilityRuin/ruin_transport_camp",

            SiteTransportCamp,

            MakeNoneCond(),

            MakeSiteLevelCond(SiteTransportCamp, 1));

        EnsureSite(

            overlayRoot,

            "site_transport_built",

            new Vector3(21f, 27.5f, 0f),

            "Presentations/HomeFacility/transport_camp",

            SiteTransportCamp,

            MakeSiteLevelCond(SiteTransportCamp, 1),

            MakeNoneCond());



        RemoveDynamicEntity(areaRoot, OverlayId, "site_tavern_ruin");

        RemoveDynamicEntity(areaRoot, OverlayId, "site_tavern_built");

        RemoveDynamicEntity(areaRoot, OverlayId, "site_transport_ruin");

        RemoveDynamicEntity(areaRoot, OverlayId, "site_transport_built");



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

            Debug.Log($"[Home01TownFacilitySiteAssembler] Map export chunk={chunkResult.Success} overlay={overlayResult.Success}");

        }



        AssetDatabase.SaveAssets();

        Debug.Log("[Home01TownFacilitySiteAssembler] Installed town facility static sites.");

    }



    static void EnsureSite(

        Transform overlayRoot,

        string nodeName,

        Vector3 position,

        string prefabKey,

        int siteId,

        CommonCheckCond appearCond,

        CommonCheckCond disappearCond)

    {

        var node = overlayRoot.Find(nodeName) ?? new GameObject(nodeName).transform;

        node.SetParent(overlayRoot, false);

        node.position = position;



        var provider = node.GetComponent<MapScenePrefabProvider>() ?? node.gameObject.AddComponent<MapScenePrefabProvider>();

        provider.Key = prefabKey;

        provider.AppearCond = appearCond;

        provider.DisappearCond = disappearCond;



        var siteProvider = node.GetComponent<TownFacilitySiteProvider>() ?? node.gameObject.AddComponent<TownFacilitySiteProvider>();

        siteProvider.SiteId = siteId;



        var interact = node.GetComponent<TownFacilitySiteInteract>() ?? node.gameObject.AddComponent<TownFacilitySiteInteract>();

        interact.SiteId = siteId;



        EditorUtility.SetDirty(node.gameObject);

    }



    static void RemoveDynamicEntity(Transform areaRoot, string overlayId, string uniqName)

    {

        var dynamicRoot = areaRoot.Find("DynamicRoot");

        var overlayRoot = dynamicRoot != null ? dynamicRoot.Find(overlayId) : null;

        var target = overlayRoot != null ? overlayRoot.Find(uniqName) : null;

        if (target == null)

        {

            return;

        }



        var generator = target.GetComponent<My.MapExport.DynamicEntityExportGenerator>();

        if (generator == null)

        {

            return;

        }



        Object.DestroyImmediate(target.gameObject);

        EditorUtility.SetDirty(areaRoot.gameObject);

    }



    static Transform EnsureOverlayRoot(Transform areaRoot, string overlayId)

    {

        var dynamicRoot = EnsureChild(areaRoot, "DynamicRoot");

        EnsureChild(dynamicRoot, "Common");

        return EnsureChild(dynamicRoot, overlayId);

    }



    static Transform EnsureChild(Transform parent, string childName)

    {

        var child = parent.Find(childName);

        if (child != null)

        {

            return child;

        }



        child = new GameObject(childName).transform;

        child.SetParent(parent, false);

        EditorUtility.SetDirty(parent.gameObject);

        return child;

    }



    static CommonCheckCond MakeNoneCond()

    {

        return ParseCond("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}");

    }



    static CommonCheckCond MakeSiteLevelCond(int siteId, int minLevel)

    {

        var key = TownFacilityCondKeys.BuildSiteLevelCond(siteId);

        return ParseCond(

            $"{{\"type\":2,\"param1\":{minLevel},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"{key}\",\"param6\":\"\"}}");

    }



    static CommonCheckCond ParseCond(string json)

    {

        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(json));

    }

}

#endif

