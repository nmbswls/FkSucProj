using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 重建精元池面板控件（模板装配，不在运行时 new UI）
public static class JingYuanPoolPanelBuilder
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/JingYuanPoolPanel.prefab";

    [MenuItem("Tools/UI/Rebuild JingYuan Pool Panel")]
    public static void Rebuild()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var panel = root.GetComponent<JingYuanPoolPanel>() ?? root.AddComponent<JingYuanPoolPanel>();
            var built = root.transform.Find("BuiltRoot");
            if (built == null)
            {
                var go = new GameObject("BuiltRoot", typeof(RectTransform));
                go.transform.SetParent(root.transform, false);
                built = go.transform;
                Stretch(built as RectTransform);
            }

            EnsureBackground(built);
            var title = EnsureText(built, "Title", "精元池", 28, new Vector2(0.1f, 0.88f), new Vector2(0.82f, 0.98f), TextAlignmentOptions.MidlineLeft);
            var info = EnsureText(built, "InfoText", "信息", 16, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.86f), TextAlignmentOptions.TopLeft);
            var status = EnsureText(built, "StatusText", "", 14, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.42f), TextAlignmentOptions.MidlineLeft);

            var oldDesc = built.Find("Description");
            if (oldDesc != null) oldDesc.gameObject.SetActive(false);

            var upgrade = EnsureButton(built, "UpgradeButton", "升级", new Vector2(0.06f, 0.26f), new Vector2(0.22f, 0.34f));
            var deposit = EnsureButton(built, "DepositButton", "存入精元", new Vector2(0.24f, 0.26f), new Vector2(0.40f, 0.34f));
            var withdraw = EnsureButton(built, "WithdrawButton", "取出精元", new Vector2(0.42f, 0.26f), new Vector2(0.58f, 0.34f));
            var decompose = EnsureButton(built, "DecomposeButton", "分解为残精", new Vector2(0.60f, 0.26f), new Vector2(0.78f, 0.34f));
            var ritual = EnsureButton(built, "RitualButton", "池畔泄欲", new Vector2(0.80f, 0.26f), new Vector2(0.96f, 0.34f));
            var tune = EnsureButton(built, "TuneButton", "调精", new Vector2(0.10f, 0.08f), new Vector2(0.36f, 0.16f));
            var warehouse = EnsureButton(built, "WarehouseButton", "精华仓库", new Vector2(0.38f, 0.08f), new Vector2(0.64f, 0.16f));
            var close = EnsureButton(built, "CloseButton", "关闭", new Vector2(0.66f, 0.08f), new Vector2(0.92f, 0.16f));

            var so = new SerializedObject(panel);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("infoText").objectReferenceValue = info;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("upgradeButton").objectReferenceValue = upgrade;
            so.FindProperty("depositButton").objectReferenceValue = deposit;
            so.FindProperty("withdrawButton").objectReferenceValue = withdraw;
            so.FindProperty("decomposeButton").objectReferenceValue = decompose;
            so.FindProperty("ritualButton").objectReferenceValue = ritual;
            so.FindProperty("tuneButton").objectReferenceValue = tune;
            so.FindProperty("warehouseButton").objectReferenceValue = warehouse;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("JingYuanPoolPanel rebuilt.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void EnsureBackground(Transform built)
    {
        var t = built.Find("Background");
        GameObject go;
        if (t == null)
        {
            go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(built, false);
            go.transform.SetAsFirstSibling();
        }
        else go = t.gameObject;

        Stretch(go.GetComponent<RectTransform>());
        var img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(0.055f, 0.065f, 0.09f, 0.98f);
    }

    static TextMeshProUGUI EnsureText(
        Transform root,
        string name,
        string text,
        int size,
        Vector2 min,
        Vector2 max,
        TextAlignmentOptions align)
    {
        var t = root.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
        }
        else go = t.gameObject;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = new Color(0.9f, 0.86f, 0.92f, 1f);
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    static Button EnsureButton(Transform root, string name, string label, Vector2 min, Vector2 max)
    {
        var t = root.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            Stretch(labelGo.GetComponent<RectTransform>());
        }
        else go = t.gameObject;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(0.28f, 0.16f, 0.38f, 1f);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
        return go.GetComponent<Button>();
    }
}
