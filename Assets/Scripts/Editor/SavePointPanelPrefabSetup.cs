#if UNITY_EDITOR
using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 重建 SavePointPanel 预制体
public static class SavePointPanelPrefabSetup
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/SavePointPanel.prefab";

    [MenuItem("Tools/SavePoint/Setup SavePointPanel Prefab")]
    public static void SetupFromMenu()
    {
        if (BuildPrefab())
        {
            Debug.Log("[SavePointPanelPrefabSetup] Prefab rebuilt: " + PrefabPath);
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
        var root = new GameObject("SavePointPanel", typeof(RectTransform), typeof(CanvasGroup));
        var panel = root.AddComponent<SavePointPanel>();
        StretchFull(root.GetComponent<RectTransform>());

        var main = CreateChild(root.transform, "Main");
        SetRectCenter(main, 520f, 360f);
        AddImage(main, new Color(0.12f, 0.13f, 0.16f, 0.98f));

        var titleGo = CreateChild(main.transform, "Title");
        SetRectAnchor(titleGo, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
        var titleTmp = AddTmp(titleGo, "存档点", 24);

        var statusGo = CreateChild(main.transform, "Status");
        SetRectAnchor(statusGo, new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
        var statusTmp = AddTmp(statusGo, "Saving...", 18);

        var vaultRoot = CreateChild(main.transform, "Vault");
        SetRectAnchor(vaultRoot, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.66f), Vector2.zero, Vector2.zero);
        AddImage(vaultRoot, new Color(0.16f, 0.17f, 0.22f, 1f));

        var vaultTitleGo = CreateChild(vaultRoot.transform, "VaultTitle");
        SetRectAnchor(vaultTitleGo, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        AddTmp(vaultTitleGo, "保险箱 · 欲望碎片", 18);

        var carriedGo = CreateChild(vaultRoot.transform, "CarriedCount");
        SetRectAnchor(carriedGo, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.74f), Vector2.zero, Vector2.zero);
        var carriedTmp = AddTmp(carriedGo, "携带：0", 16);

        var quotaGo = CreateChild(vaultRoot.transform, "Quota");
        SetRectAnchor(quotaGo, new Vector2(0.04f, 0.4f), new Vector2(0.96f, 0.56f), Vector2.zero, Vector2.zero);
        var quotaTmp = AddTmp(quotaGo, "本次额度：0 / 50", 16);

        var feedbackGo = CreateChild(vaultRoot.transform, "DepositFeedback");
        SetRectAnchor(feedbackGo, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.38f), Vector2.zero, Vector2.zero);
        var feedbackTmp = AddTmp(feedbackGo, "", 14);

        var btnDeposit = CreateButton(vaultRoot.transform, "BtnDeposit", "存入（全部可用）", new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.18f));
        var btnClose = CreateButton(main.transform, "BtnClose", "关闭", new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.14f));

        WirePanel(
            panel,
            root.GetComponent<CanvasGroup>(),
            btnClose.GetComponent<Button>(),
            statusTmp,
            vaultRoot,
            carriedTmp,
            quotaTmp,
            btnDeposit.GetComponent<Button>(),
            feedbackTmp);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return true;
    }

    static void WirePanel(
        SavePointPanel panel,
        CanvasGroup canvasGroup,
        Button closeButton,
        TextMeshProUGUI statusText,
        GameObject vaultSectionRoot,
        TextMeshProUGUI carriedCountText,
        TextMeshProUGUI quotaText,
        Button depositButton,
        TextMeshProUGUI depositFeedbackText)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = "SavePointPanel";
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("statusText").objectReferenceValue = statusText;
        so.FindProperty("vaultSectionRoot").objectReferenceValue = vaultSectionRoot;
        so.FindProperty("carriedCountText").objectReferenceValue = carriedCountText;
        so.FindProperty("quotaText").objectReferenceValue = quotaText;
        so.FindProperty("depositButton").objectReferenceValue = depositButton;
        so.FindProperty("depositFeedbackText").objectReferenceValue = depositFeedbackText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateChild(parent, name);
        SetRectAnchor(go, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        AddImage(go, new Color(0.25f, 0.28f, 0.35f, 1f));
        go.AddComponent<Button>();
        var labelGo = CreateChild(go.transform, "Label");
        StretchFull(labelGo.GetComponent<RectTransform>());
        AddTmp(labelGo, label, 16);
        return go;
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

    static void SetRectAnchor(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        if (anchorMin != anchorMax)
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    static Image AddImage(GameObject go, Color color)
    {
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = color;
        if (go.GetComponent<Button>() == null)
        {
            img.raycastTarget = false;
        }

        return img;
    }

    static TextMeshProUGUI AddTmp(GameObject go, string text, int fontSize)
    {
        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }
}
#endif
