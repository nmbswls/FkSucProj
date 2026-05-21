#if UNITY_EDITOR
using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 重建 SecretBaseNpcHubPanel 预制体（修复手写 YAML 缺 CanvasRenderer 等问题）
public static class SecretBaseNpcHubPanelPrefabSetup
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/SecretBaseNpcHubPanel.prefab";

    [MenuItem("Tools/SecretBase/Setup SecretBaseNpcHubPanel Prefab")]
    public static void SetupFromMenu()
    {
        if (BuildPrefab())
        {
            Debug.Log("[SecretBaseNpcHubPanelPrefabSetup] Prefab rebuilt: " + PrefabPath);
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
        var root = new GameObject("SecretBaseNpcHubPanel", typeof(RectTransform), typeof(CanvasGroup));
        var panel = root.AddComponent<SecretBaseNpcHubPanel>();
        StretchFull(root.GetComponent<RectTransform>());

        var main = CreateChild(root.transform, "Main");
        SetRectCenter(main, 560f, 360f);
        AddImage(main, new Color(0.12f, 0.13f, 0.16f, 0.98f));

        var hubRoot = CreateChild(main.transform, "HubRoot");
        StretchFull(hubRoot.GetComponent<RectTransform>());

        var portrait = CreateChild(hubRoot.transform, "Portrait");
        SetRectAnchor(portrait, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(160f, 220f));
        AddImage(portrait, new Color(0.35f, 0.45f, 0.55f, 1f));

        var nameGo = CreateChild(hubRoot.transform, "Name");
        SetRectAnchor(nameGo, new Vector2(0.35f, 0.75f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        AddTmp(nameGo, "NPC", 22);

        var favorGo = CreateChild(hubRoot.transform, "Favor");
        SetRectAnchor(favorGo, new Vector2(0.35f, 0.55f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
        AddTmp(favorGo, "Favor", 16);

        var btnTalk = CreateButton(hubRoot.transform, "BtnTalk", "交谈", new Vector2(0.4f, 0.28f), new Vector2(0.58f, 0.4f));
        var btnGift = CreateButton(hubRoot.transform, "BtnGift", "送礼", new Vector2(0.62f, 0.28f), new Vector2(0.8f, 0.4f));
        var btnClose = CreateButton(hubRoot.transform, "BtnClose", "关闭", new Vector2(0.85f, 0.85f), new Vector2(0.98f, 0.98f));

        var giftPicker = CreateChild(main.transform, "GiftPicker");
        StretchFull(giftPicker.GetComponent<RectTransform>());
        giftPicker.SetActive(false);

        var hintGo = CreateChild(giftPicker.transform, "Hint");
        SetRectAnchor(hintGo, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);
        var hintTmp = AddTmp(hintGo, "选择要赠送的礼物", 16);

        var btnBack = CreateButton(giftPicker.transform, "BtnBack", "返回", new Vector2(0.02f, 0.9f), new Vector2(0.15f, 0.98f));

        var listGo = CreateChild(giftPicker.transform, "List");
        SetRectAnchor(listGo, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
        var vlg = listGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        listGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var template = CreateGiftCell(giftPicker.transform, "GiftCellTemplate");
        template.SetActive(false);

        var btnGive = CreateButton(giftPicker.transform, "BtnGive", "赠送", new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.14f));
        btnGive.GetComponent<Button>().interactable = false;

        WirePanel(panel, root.GetComponent<CanvasGroup>(), portrait.GetComponent<Image>(),
            nameGo.GetComponent<TextMeshProUGUI>(), favorGo.GetComponent<TextMeshProUGUI>(),
            btnTalk.GetComponent<Button>(), btnGift.GetComponent<Button>(), btnClose.GetComponent<Button>(),
            hubRoot, giftPicker,
            listGo.GetComponent<RectTransform>(), template.GetComponent<SecretBaseNpcGiftCell>(),
            btnGive.GetComponent<Button>(), hintTmp, btnBack.GetComponent<Button>());

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return true;
    }

    static void WirePanel(
        SecretBaseNpcHubPanel panel,
        CanvasGroup canvasGroup,
        Image portraitImage,
        TextMeshProUGUI nameText,
        TextMeshProUGUI favorText,
        Button btnTalk,
        Button btnGift,
        Button btnClose,
        GameObject hubRoot,
        GameObject giftPickerRoot,
        RectTransform giftListContent,
        SecretBaseNpcGiftCell giftCellTemplate,
        Button btnGiveGift,
        TextMeshProUGUI giftHintText,
        Button btnGiftBack)
    {
        var so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = SecretBaseNpcHubPanel.PanelIdConst;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("portraitImage").objectReferenceValue = portraitImage;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("favorText").objectReferenceValue = favorText;
        so.FindProperty("btnTalk").objectReferenceValue = btnTalk;
        so.FindProperty("btnGiftMode").objectReferenceValue = btnGift;
        so.FindProperty("btnClose").objectReferenceValue = btnClose;
        so.FindProperty("hubRoot").objectReferenceValue = hubRoot;
        so.FindProperty("giftPickerRoot").objectReferenceValue = giftPickerRoot;
        so.FindProperty("giftListContent").objectReferenceValue = giftListContent;
        so.FindProperty("giftCellTemplate").objectReferenceValue = giftCellTemplate;
        so.FindProperty("btnGiveGift").objectReferenceValue = btnGiveGift;
        so.FindProperty("giftHintText").objectReferenceValue = giftHintText;
        so.FindProperty("btnGiftBack").objectReferenceValue = btnGiftBack;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject CreateGiftCell(Transform parent, string name)
    {
        var go = CreateChild(parent, name);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(480f, 48f);
        AddImage(go, new Color(0.2f, 0.22f, 0.28f, 1f));
        go.AddComponent<Button>();

        var icon = CreateChild(go.transform, "Icon");
        SetRectAnchor(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(40f, 40f));
        AddImage(icon, Color.white);

        var nameGo = CreateChild(go.transform, "Name");
        SetRectAnchor(nameGo, new Vector2(0.12f, 0.2f), new Vector2(0.75f, 0.8f), Vector2.zero, Vector2.zero);
        AddTmp(nameGo, "item", 14);

        var countGo = CreateChild(go.transform, "Count");
        SetRectAnchor(countGo, new Vector2(0.78f, 0.2f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
        AddTmp(countGo, "1", 14);

        var cell = go.AddComponent<SecretBaseNpcGiftCell>();
        var cso = new SerializedObject(cell);
        cso.FindProperty("clickButton").objectReferenceValue = go.GetComponent<Button>();
        cso.FindProperty("icon").objectReferenceValue = icon.GetComponent<Image>();
        cso.FindProperty("nameText").objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        cso.FindProperty("countText").objectReferenceValue = countGo.GetComponent<TextMeshProUGUI>();
        cso.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = CreateChild(parent, name);
        SetRectAnchor(go, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        AddImage(go, new Color(0.25f, 0.28f, 0.35f, 1f));
        var btn = go.AddComponent<Button>();
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
            img.raycastTarget = go.name.StartsWith("Portrait") || go.name == "Icon";
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
