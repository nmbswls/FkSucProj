#if UNITY_EDITOR
using My.Map.Hunting;
using My.UI;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 HuntingNpcDetail 写入 OverworldHUD 预制体。Unity 已打开本项目时请用菜单执行（勿 batchmode）。
/// </summary>
public static class HuntingNpcDetailPrefabSetup
{
    const string HudPrefabPath = "Assets/Resources/UI/Prefabs/OverworldHUD.prefab";
    const string InteractPrefabPath = "Assets/Resources/UI/Prefabs/InteractMenu.prefab";
    const string ExecuteHintChildName = "ExecuteHint";

    [MenuItem("Tools/Map/Setup HuntingNpcDetail On OverworldHUD")]
    public static void SetupFromMenu()
    {
        if (Setup())
        {
            Debug.Log("[HuntingNpcDetailPrefabSetup] OverworldHUD prefab updated. Save project if needed.");
        }
        else
        {
            Debug.LogWarning("[HuntingNpcDetailPrefabSetup] Setup skipped or failed.");
        }
    }

    public static void SetupBatch()
    {
        Setup();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static bool Setup()
    {
        var hudRoot = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        if (hudRoot == null)
        {
            Debug.LogError("[HuntingNpcDetailPrefabSetup] Cannot load " + HudPrefabPath);
            return false;
        }

        try
        {
            var hudPanel = hudRoot.GetComponent<OverworldHUDPanel>();
            if (hudPanel == null)
            {
                Debug.LogError("[HuntingNpcDetailPrefabSetup] OverworldHUDPanel missing.");
                return false;
            }

            var old = hudRoot.transform.Find("HuntingNpcDetail");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var view = HuntingNpcDetailUiBuilder.BuildUnder(hudRoot.transform);
            if (view == null)
            {
                return false;
            }

            TryReplaceExecuteHintWithInteractArt(view);

            var so = new SerializedObject(hudPanel);
            so.FindProperty("HuntingNpcDetail").objectReferenceValue = view;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(hudRoot, HudPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(hudRoot);
        }
    }

    static void TryReplaceExecuteHintWithInteractArt(HuntingNpcDetailView view)
    {
        var interactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractPrefabPath);
        var hMode = interactPrefab != null ? interactPrefab.transform.Find("HModeExecute") : null;
        if (hMode == null || view.DetailRoot == null || view.ExecuteHintRoot == null)
        {
            return;
        }

        Object.DestroyImmediate(view.ExecuteHintRoot.gameObject);
        var clone = Object.Instantiate(hMode.gameObject, view.DetailRoot);
        clone.name = ExecuteHintChildName;
        var rt = clone.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, 56f);
        clone.SetActive(false);
        view.ExecuteHintRoot = rt;
        view.ExecuteHintText = clone.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
    }
}
#endif
