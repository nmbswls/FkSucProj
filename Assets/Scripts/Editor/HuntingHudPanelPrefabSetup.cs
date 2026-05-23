#if UNITY_EDITOR
using My.Map.Hunting;
using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 构建 HuntingHudPanel 预制体（编辑器一次性生成，运行时不再动态拼 UI）。
/// </summary>
public static class HuntingHudPanelPrefabSetup
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/HuntingHudPanel.prefab";
    const string InteractPrefabPath = "Assets/Resources/UI/Prefabs/InteractMenu.prefab";

    [MenuItem("Tools/Map/Setup HuntingHudPanel Prefab")]
    public static void SetupFromMenu()
    {
        if (BuildPrefab())
        {
            Debug.Log("[HuntingHudPanelPrefabSetup] Prefab rebuilt: " + PrefabPath);
        }
    }

    public static void SetupBatch()
    {
        BuildPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static bool BuildPrefab()
    {
        var root = new GameObject("HuntingHudPanel", typeof(RectTransform), typeof(CanvasGroup));
        var panel = root.AddComponent<HuntingHudPanel>();
        StretchFull(root.GetComponent<RectTransform>());

        var markersGo = CreateChild(root.transform, "DesireCrystalHuntingMarkers");
        StretchFull(markersGo.GetComponent<RectTransform>());
        var markersComp = markersGo.AddComponent<DesireCrystalHuntingHudMarkers>();

        var detailGo = CreateChild(root.transform, "HuntingNpcDetail");
        SetRectCenter(detailGo, 220f, 100f);
        var detailView = detailGo.AddComponent<HuntingNpcDetailView>();

        var panelGo = CreateChild(detailGo.transform, "DetailPanel");
        StretchFull(panelGo.GetComponent<RectTransform>());
        AddImage(panelGo, new Color(0.08f, 0.08f, 0.1f, 0.85f));

        var nameGo = CreateChild(panelGo.transform, "NameText");
        SetRectCenter(nameGo, 200f, 26f);
        nameGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 22f);
        var nameText = AddTmp(nameGo, "NPC", 16);

        var willGo = CreateChild(panelGo.transform, "NpcWillText");
        SetRectCenter(willGo, 40f, 22f);
        willGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(72f, -6f);
        var willText = AddTmp(willGo, "", 14);

        var sjGo = CreateChild(panelGo.transform, "SJProgressBar");
        SetRectCenter(sjGo, 96f, 10f);
        sjGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-55f, -6f);
        var sjBar = AddFilledImage(sjGo, new Color(0.92f, 0.35f, 0.2f, 0.95f));

        RectTransform executeRt = TryCloneExecuteHint(detailGo.transform);
        if (executeRt == null)
        {
            var executeGo = CreateChild(detailGo.transform, "ExecuteHint");
            SetRectCenter(executeGo, 100f, 28f);
            executeGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 52f);
            AddTmp(executeGo, "点击处决", 13);
            executeGo.SetActive(false);
            executeRt = executeGo.GetComponent<RectTransform>();
        }

        WireDetailView(detailView, detailGo.GetComponent<RectTransform>(), nameText, sjBar, willText, executeRt);
        WirePanel(panel, root.GetComponent<CanvasGroup>(), detailView, markersComp, markersGo.GetComponent<RectTransform>());

        detailGo.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return true;
    }

    static RectTransform TryCloneExecuteHint(Transform detailRoot)
    {
        var interactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InteractPrefabPath);
        var hMode = interactPrefab != null ? interactPrefab.transform.Find("HModeExecute") : null;
        if (hMode == null)
        {
            return null;
        }

        var clone = Object.Instantiate(hMode.gameObject, detailRoot);
        clone.name = "ExecuteHint";
        var rt = clone.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, 56f);
        clone.SetActive(false);
        return rt;
    }

    static void WireDetailView(
        HuntingNpcDetailView view,
        RectTransform detailRoot,
        TextMeshProUGUI nameText,
        Image sjBar,
        TextMeshProUGUI willText,
        RectTransform executeRt)
    {
        view.DetailRoot = detailRoot;
        view.NameText = nameText;
        view.SJProgressBar = sjBar;
        view.NpcWillText = willText;
        view.ExecuteHintRoot = executeRt;
        view.ExecuteHintText = executeRt != null
            ? executeRt.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
    }

    static void WirePanel(
        HuntingHudPanel panel,
        CanvasGroup canvasGroup,
        HuntingNpcDetailView detailView,
        DesireCrystalHuntingHudMarkers markers,
        RectTransform markersParent)
    {
        var markersSo = new SerializedObject(markers);
        markersSo.FindProperty("markersParent").objectReferenceValue = markersParent;
        markersSo.ApplyModifiedPropertiesWithoutUndo();

        var so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = HuntingHudPanel.PanelIdConst;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("npcDetail").objectReferenceValue = detailView;
        so.FindProperty("crystalMarkers").objectReferenceValue = markers;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void SetRectCenter(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static Image AddImage(GameObject go, Color color)
    {
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Image AddFilledImage(GameObject go, Color color)
    {
        var img = AddImage(go, color);
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillAmount = 0f;
        return img;
    }

    static TextMeshProUGUI AddTmp(GameObject go, string text, int fontSize)
    {
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
