#if UNITY_EDITOR

using My.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 生成/刷新播种模式种子栏 prefab（编辑器拼装一次，运行时只加载）
public static class FarmSeedBarPanelPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/FarmSeedBarPanel.prefab";
    const string HumanBarPath = "Assets/Resources/UI/Prefabs/PlayerHumanItemBarPanel.prefab";

    [MenuItem("Tools/UI/Build FarmSeedBarPanel Prefab")]
    public static void Build()
    {
        var human = AssetDatabase.LoadAssetAtPath<GameObject>(HumanBarPath);
        if (human == null)
        {
            throw new System.Exception("Missing human item bar prefab: " + HumanBarPath);
        }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(human);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = "FarmSeedBarPanel";

        // 去掉人类栏脚本，挂播种栏
        var humanPanel = root.GetComponent<PlayerHumanItemBarPanel>();
        ItemBarCenterItemView center = null;
        if (humanPanel != null)
        {
            var so = new SerializedObject(humanPanel);
            center = so.FindProperty("_centerItemView")?.objectReferenceValue as ItemBarCenterItemView;
            Object.DestroyImmediate(humanPanel);
        }

        if (center == null)
        {
            center = root.GetComponentInChildren<ItemBarCenterItemView>(true);
        }

        // 隐藏消耗品轮盘（人类栏遗留）
        HideNamed(root.transform, "ConsumableWheel");
        HideNamed(root.transform, "ConsumableWheelParent");
        HideNamed(root.transform, "Wheel");

        var panel = root.GetComponent<FarmSeedBarPanel>() ?? root.AddComponent<FarmSeedBarPanel>();
        var canvasGroup = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();

        // 标题 / 提示
        var title = EnsureTmp(root.transform, "SeedBarTitle", new Vector2(0f, 56f), 18, "播种模式");
        var hint = EnsureTmp(root.transform, "SeedBarHint", new Vector2(0f, -70f), 14, "滚轮切换种子 · 左键播前方格 · X退出");

        // 5 个预置槽
        var slotsRoot = EnsureRect(root.transform, "SeedSlots", new Vector2(0f, -8f), new Vector2(520f, 48f));
        var slots = new FarmSeedSlotView[5];
        for (int i = 0; i < 5; i++)
        {
            slots[i] = EnsureSlot(slotsRoot, "SeedSlot_" + i, new Vector2(-176f + i * 88f, 0f));
        }

        var panelSo = new SerializedObject(panel);
        panelSo.FindProperty("panelId").stringValue = FarmSeedBarPanel.PanelIdConst;
        panelSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        panelSo.FindProperty("centerItemView").objectReferenceValue = center;
        panelSo.FindProperty("titleText").objectReferenceValue = title;
        panelSo.FindProperty("hintText").objectReferenceValue = hint;
        var slotsProp = panelSo.FindProperty("seedSlots");
        slotsProp.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
        {
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }

        panelSo.ApplyModifiedPropertiesWithoutUndo();

        // 根布局靠底中
        var rt = root.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(560f, 160f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built " + PrefabPath);
    }

    static void HideNamed(Transform root, string name)
    {
        var t = FindDeep(root, name);
        if (t != null)
        {
            t.gameObject.SetActive(false);
        }
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var f = FindDeep(root.GetChild(i), name);
            if (f != null)
            {
                return f;
            }
        }

        return null;
    }

    static RectTransform EnsureRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            t = go.transform;
        }

        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static TextMeshProUGUI EnsureTmp(Transform parent, string name, Vector2 pos, float size, string text)
    {
        var rt = EnsureRect(parent, name, pos, new Vector2(520f, 28f));
        var tmp = rt.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        }

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

    static FarmSeedSlotView EnsureSlot(Transform parent, string name, Vector2 pos)
    {
        var rt = EnsureRect(parent, name, pos, new Vector2(84f, 44f));
        var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.2f, 0.24f, 0.28f, 0.95f);
        var btn = rt.GetComponent<Button>() ?? rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var iconRt = EnsureRect(rt, "Icon", new Vector2(-24f, 0f), new Vector2(28f, 28f));
        var icon = iconRt.GetComponent<Image>() ?? iconRt.gameObject.AddComponent<Image>();
        icon.raycastTarget = false;

        var label = EnsureTmp(rt, "Label", new Vector2(14f, 0f), 12, "-");
        label.rectTransform.sizeDelta = new Vector2(52f, 36f);

        var markRt = EnsureRect(rt, "Selected", new Vector2(0f, 0f), new Vector2(84f, 44f));
        var markImg = markRt.GetComponent<Image>() ?? markRt.gameObject.AddComponent<Image>();
        markImg.color = new Color(1f, 1f, 1f, 0.12f);
        markImg.raycastTarget = false;
        markRt.gameObject.SetActive(false);

        var slot = rt.GetComponent<FarmSeedSlotView>() ?? rt.gameObject.AddComponent<FarmSeedSlotView>();
        var so = new SerializedObject(slot);
        so.FindProperty("button").objectReferenceValue = btn;
        so.FindProperty("background").objectReferenceValue = img;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("selectedMark").objectReferenceValue = markRt.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }
}

#endif
