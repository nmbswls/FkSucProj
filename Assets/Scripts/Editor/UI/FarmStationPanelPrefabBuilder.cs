#if UNITY_EDITOR

using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 生成农业小站面板 prefab（编辑器拼装一次，运行时只加载）
public static class FarmStationPanelPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/UI/Prefabs/FarmStationPanel.prefab";

    static readonly string[] CropIds =
    {
        "crop_wheat", "crop_cabbage", "crop_potato", "crop_berry", "crop_herb",
    };

    [InitializeOnLoadMethod]
    static void EnsurePrefabExists()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Build();
            }
        };
    }

    [MenuItem("Tools/UI/Build FarmStationPanel Prefab")]
    public static void Build()
    {
        EnsureFolder("Assets/Resources/UI/Prefabs");

        var root = new GameObject("FarmStationPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        var rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 460f);
        root.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.15f, 0.95f);

        var panel = root.AddComponent<FarmStationPanel>();
        var adapter = root.AddComponent<FarmProduceWarehouseLootAdapter>();
        var canvasGroup = root.GetComponent<CanvasGroup>();

        CreateLabel(root.transform, "Title", "农业小站", new Vector2(0f, 200f), 22);
        var status = CreateLabel(root.transform, "Status", "", new Vector2(0f, 165f), 14);
        CreateLabel(root.transform, "WfLabel", "派工收割人数", new Vector2(-110f, 120f), 14);
        var wfInput = CreateInput(root.transform, "WfInput", new Vector2(80f, 120f));
        CreateLabel(root.transform, "PlanHeader", "自动播种规划（尽量靠近）", new Vector2(0f, 70f), 15);

        var rows = new FarmStationPlanRowView[CropIds.Length];
        for (int i = 0; i < CropIds.Length; i++)
        {
            float y = 30f - i * 36f;
            rows[i] = CreatePlanRow(root.transform, "Plan_" + i, CropIds[i], new Vector2(0f, y));
        }

        var openWh = CreateButton(root.transform, "OpenWh", "打开产物仓", new Vector2(-90f, -180f));
        var closeBtn = CreateButton(root.transform, "Close", "关闭", new Vector2(90f, -180f));

        var so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = FarmStationPanel.PanelIdConst;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("statusText").objectReferenceValue = status;
        so.FindProperty("workforceInput").objectReferenceValue = wfInput;
        so.FindProperty("openWarehouseButton").objectReferenceValue = openWh;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("warehouseAdapter").objectReferenceValue = adapter;
        var arr = so.FindProperty("planRows");
        arr.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built " + PrefabPath);
    }

    static FarmStationPlanRowView CreatePlanRow(Transform parent, string name, string cropId, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380f, 32f);

        var label = CreateLabel(go.transform, "Name", cropId, new Vector2(-110f, 0f), 14);
        var input = CreateInput(go.transform, "Target", new Vector2(80f, 0f));
        var row = go.AddComponent<FarmStationPlanRowView>();
        var so = new SerializedObject(row);
        so.FindProperty("cropId").stringValue = cropId;
        so.FindProperty("nameText").objectReferenceValue = label;
        so.FindProperty("targetInput").objectReferenceValue = input;
        so.ApplyModifiedPropertiesWithoutUndo();
        return row;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 pos, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380f, 28f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        return tmp;
    }

    static TMP_InputField CreateInput(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(100f, 30f);
        go.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.24f, 1f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6f, 2f);
        trt.offsetMax = new Vector2(-6f, -2f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 16;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        var input = go.AddComponent<TMP_InputField>();
        input.textComponent = tmp;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        return input;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(140f, 36f);
        go.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.42f, 1f);
        CreateLabel(go.transform, "L", label, Vector2.zero, 15);
        return go.GetComponent<Button>();
    }

    static void EnsureFolder(string assetFolder)
    {
        var parts = assetFolder.Split('/');
        var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(cur, parts[i]);
            }

            cur = next;
        }
    }
}

#endif
