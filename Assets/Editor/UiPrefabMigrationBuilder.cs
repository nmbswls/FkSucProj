using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiPrefabMigrationBuilder
{
    const string Root = "Assets/Resources/UI/Prefabs/PlayerProgressionHubPanelSub";
    const string FontGuid = "b15d44825abceaf499ea193f3376bb2c";

    [MenuItem("Tools/UI/Rebuild Priority Progression Prefabs")]
    public static void Rebuild()
    {
        Directory.CreateDirectory(Root);
        BuildJingYuan();
        BuildCult();
        BuildHuman();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Priority progression prefabs rebuilt.");
    }

    static void BuildJingYuan()
    {
        var root = Panel("JingYuanTunePanel", "d4f708135ce86d209b3c5e7f8192a3b4");
        var built = Child(root, "BuiltRoot");
        Image("Background", built, new Color(.055f, .065f, .09f, .98f), true);
        Text("Title", built, "优质精华装备", 28, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(0, -70));
        Text("SlotLabel", built, "当前装备", 16, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -78), new Vector2(0, -110));
        var slots = Child(built, "EquipSlots"); SetRect(slots, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, -116), new Vector2(-28, -200));
        slots.AddComponent<HorizontalLayoutGroup>().spacing = 12;
        Text("CandidateLabel", built, "可装备精华", 16, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, 116), new Vector2(0, 84));
        var candidate = Image("CandidatePanel", built, new Color(.09f, .1f, .14f, 1), false); SetRect(candidate.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 20), new Vector2(-292, 106));
        var scroll = candidate.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        var viewport = Child(candidate.transform, "Viewport"); SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); viewport.AddComponent<RectMask2D>();
        var content = Child(viewport, "Content"); SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -8), new Vector2(-8, 8));
        var grid = content.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(170, 76); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content.GetComponent<RectTransform>();
        var detail = Image("DetailPanel", built, new Color(.09f, .1f, .14f, 1), false); SetRect(detail.rectTransform, new Vector2(.68f, 0), Vector2.one, new Vector2(12, 20), new Vector2(-28, 106));
        Text("Type", detail.transform, "请选择装备槽", 20, Vector2.zero, Vector2.one, new Vector2(16, -16), new Vector2(-16, -54));
        Text("Level", detail.transform, "等级：", 15, Vector2.zero, Vector2.one, new Vector2(16, -62), new Vector2(-16, -92));
        Text("Concentration", detail.transform, "浓度：", 15, Vector2.zero, Vector2.one, new Vector2(16, -96), new Vector2(-16, -126));
        Text("ShelfLife", detail.transform, "保质期：", 15, Vector2.zero, Vector2.one, new Vector2(16, -130), new Vector2(-16, -160));
        Text("MainEffect", detail.transform, "主词条：", 15, Vector2.zero, Vector2.one, new Vector2(16, -164), new Vector2(-16, -214));
        Text("ExtraAffix", detail.transform, "额外词条：", 15, Vector2.zero, Vector2.one, new Vector2(16, -218), new Vector2(-16, -268));
        Text("Location", detail.transform, "所在位置：", 15, Vector2.zero, Vector2.one, new Vector2(16, -272), new Vector2(-16, -302));
        Save(root, "JingYuanTunePanel.prefab");
    }

    static void BuildCult()
    {
        var root = Panel("CultPanel", "c7a1e8f24b3d4e6a9f0c1d2e3a4b5c6d");
        AddComponent(root, "a2b3c4d5e6f74899aabbccddee001122");
        Image("Background", root, new Color(.06f, .04f, .08f, .96f), true);
        Text("FaithText", root, "信仰", 20, new Vector2(.02f, .94f), new Vector2(.68f, .99f), Vector2.zero, Vector2.zero);
        var tree = Child(root, "TreeRoot"); SetRect(tree, new Vector2(.02f, .08f), new Vector2(.68f, .84f), Vector2.zero, Vector2.zero);
        var seats = Child(root, "SeatRoot"); SetRect(seats, new Vector2(.02f, .86f), new Vector2(.68f, .94f), Vector2.zero, Vector2.zero);
        Detail(root, "DetailArea", .72f, "解锁");
        var tabs = Child(root, "CultTabs"); SetRect(tabs, new Vector2(.02f, .94f), new Vector2(.68f, .995f), Vector2.zero, Vector2.zero); Button("DoctrineTab", tabs, "基础教团", 0); Button("AncientSeatTab", tabs, "古老者之座", 1);
        Button("CloseButton", root, "X", 0);
        var view = AddComponent(root, "d8b2f9e35c4e5f7b0a1d2e3f4b5c6d7e");
        Save(root, "CultPanel.prefab");
    }

    static void BuildHuman()
    {
        var root = Panel("HumanTechTreePanel", "6dba2fc4f8c4469cb4a176c0ba57e0b2");
        Image("Background", root, new Color(.05f, .07f, .1f, .96f), true);
        Text("DebugTipText", root, "", 14, new Vector2(.22f, .94f), new Vector2(.68f, .99f), Vector2.zero, Vector2.zero);
        var stages = Child(root, "StageNavigation"); SetRect(stages, new Vector2(.01f, .08f), new Vector2(.2f, .92f), Vector2.zero, Vector2.zero); stages.AddComponent<VerticalLayoutGroup>();
        Text("StageTitle", stages, "文明阶段", 20, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -80));
        Text("StageDescription", stages, "", 14, Vector2.zero, Vector2.one, new Vector2(8, -70), new Vector2(-8, -20));
        Text("StageProgress", stages, "", 13, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -4));
        var tree = Child(root, "TreeRoot"); SetRect(tree, new Vector2(.22f, 0), new Vector2(.68f, 1), new Vector2(8, 14), new Vector2(-8, -14));
        AddComponent(root, "a0f479c18aa94d15975f1c6c9b537ee3"); Detail(root, "DetailArea", .7f, ""); Button("CloseButton", root, "X", 0);
        Save(root, "HumanTechTreePanel.prefab");
    }

    static GameObject Panel(string name, string scriptGuid)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        SetRect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddComponent(go, scriptGuid); return go;
    }

    static GameObject Child(Transform parent, string name) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go; }
    static GameObject Child(GameObject parent, string name) => Child(parent.transform, name);

    static Image Image(string name, Transform parent, Color color, bool stretch)
    {
        var go = Child(parent, name); var image = go.AddComponent<Image>(); image.color = color; if (stretch) SetRect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); return image;
    }
    static Image Image(string name, GameObject parent, Color color, bool stretch) => Image(name, parent.transform, color, stretch);

    static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = Child(parent, name); SetRect(go, min, max, offsetMin, offsetMax); var text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.enableWordWrapping = true; text.raycastTarget = false; return text;
    }
    static TextMeshProUGUI Text(string name, GameObject parent, string value, float size, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) => Text(name, parent.transform, value, size, min, max, offsetMin, offsetMax);

    static void Button(string name, Transform parent, string label, int index)
    {
        var go = Child(parent, name); var image = go.AddComponent<Image>(); image.color = new Color(.16f, .12f, .18f, 1); go.AddComponent<Button>(); SetRect(go, new Vector2(index * .5f, 0), new Vector2(index * .5f + .46f, 1), Vector2.zero, Vector2.zero); Text("Label", go.transform, label, 16, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }
    static void Button(string name, GameObject parent, string label, int index) => Button(name, parent.transform, label, index);

    static void Detail(GameObject root, string name, float minX, string buttonLabel)
    {
        var detail = Image(name, root, new Color(.1f, .06f, .12f, .94f), false); SetRect(detail.rectTransform, new Vector2(minX, .06f), new Vector2(.98f, .9f), Vector2.zero, Vector2.zero);
        Text("DetailTitle", detail.transform, "", 22, new Vector2(.05f, .82f), new Vector2(.95f, .96f), Vector2.zero, Vector2.zero);
        Text("DetailBody", detail.transform, "", 15, new Vector2(.05f, .28f), new Vector2(.95f, .8f), Vector2.zero, Vector2.zero);
        Text("DetailStatusHint", detail.transform, "", 15, new Vector2(.05f, .16f), new Vector2(.95f, .26f), Vector2.zero, Vector2.zero);
        if (!string.IsNullOrEmpty(buttonLabel)) { var button = Child(detail.transform, "UnlockButton"); button.AddComponent<Image>(); button.AddComponent<Button>(); SetRect(button, new Vector2(.15f, .04f), new Vector2(.85f, .14f), Vector2.zero, Vector2.zero); Text("Label", button.transform, buttonLabel, 16, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); }
    }

    static void SetRect(GameObject go, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { SetRect(go.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax); }
    static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }

    static Component AddComponent(GameObject go, string guid)
    {
        var script = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(script)) return null;
        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(script);
        return mono == null ? null : go.AddComponent(mono.GetClass());
    }

    static void Save(GameObject root, string fileName)
    {
        var path = $"{Root}/{fileName}";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
