using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiPrefabMigrationBuilder
{
    const string Root = "Assets/Resources/UI/Prefabs/PlayerProgressionHubPanelSub";
    const string SecretBaseRoot = "Assets/Resources/UI/Prefabs";
    const string FontGuid = "b15d44825abceaf499ea193f3376bb2c";

    [MenuItem("Tools/UI/Rebuild Cult Panel Prefab")]
    public static void RebuildCultPanel()
    {
        Directory.CreateDirectory(Root);
        BuildCultNodePrefab();
        BuildCultSeatNodePrefab();
        BuildCultSeatLayouts();
        BuildCultConnectionPrefab();
        BuildCult();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CultPanel prefab rebuilt.");
    }

    [MenuItem("Tools/UI/Rebuild Priority Progression Prefabs")]
    public static void Rebuild()
    {
        Directory.CreateDirectory(Root);
        BuildCultNodePrefab();
        BuildCultSeatNodePrefab();
        BuildCultSeatLayouts();
        BuildCultConnectionPrefab();
        BuildJingYuanWorkstation();
        BuildJingYuanWarehouse();
        BuildJingYuanCarried();
        BuildJingYuanPool();
        WireSecretBaseWarehouseButton();
        WirePlayerBagJingYuanButton();
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
        Text("Quality", detail.transform, "Quality: -", 15, Vector2.zero, Vector2.one, new Vector2(16, -96), new Vector2(-16, -126));
        Text("Type", detail.transform, "请选择装备槽", 20, Vector2.zero, Vector2.one, new Vector2(16, -16), new Vector2(-16, -54));
        Text("Level", detail.transform, "等级：", 15, Vector2.zero, Vector2.one, new Vector2(16, -62), new Vector2(-16, -92));
        Text("Concentration", detail.transform, "浓度：", 15, Vector2.zero, Vector2.one, new Vector2(16, -96), new Vector2(-16, -126));
        Text("ShelfLife", detail.transform, "保质期：", 15, Vector2.zero, Vector2.one, new Vector2(16, -130), new Vector2(-16, -160));
        Text("MainEffect", detail.transform, "主词条：", 15, Vector2.zero, Vector2.one, new Vector2(16, -164), new Vector2(-16, -214));
        Text("ExtraAffix", detail.transform, "额外词条：", 15, Vector2.zero, Vector2.one, new Vector2(16, -218), new Vector2(-16, -268));
        Text("Location", detail.transform, "所在位置：", 15, Vector2.zero, Vector2.one, new Vector2(16, -272), new Vector2(-16, -302));
        Text("Renewal", detail.transform, "Renewal: -", 14, Vector2.zero, Vector2.one, new Vector2(16, 78), new Vector2(-16, 104));
        Button("RenewButton", detail.transform, "Renew", 0, 1);
        var renewButton = detail.transform.Find("RenewButton").GetComponent<RectTransform>();
        renewButton.anchorMin = new Vector2(0, 0); renewButton.anchorMax = new Vector2(1, 0); renewButton.offsetMin = new Vector2(16, 18); renewButton.offsetMax = new Vector2(-16, 64);
        Text("TuneStatus", detail.transform, "Tune donor: -", 13, Vector2.zero, Vector2.one, new Vector2(16, 136), new Vector2(-16, 160));
        var boostToggle = Child(detail.transform, "ResidueBoostToggle");
        boostToggle.AddComponent<Toggle>();
        SetRect(boostToggle, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 106), new Vector2(-16, 132));
        Text("Label", boostToggle.transform, "Use residue for +20% success", 13, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(0, 0));
        Button("TuneButton", detail.transform, "Tune", 0, 1);
        var tuneButton = detail.transform.Find("TuneButton").GetComponent<RectTransform>();
        tuneButton.anchorMin = new Vector2(0, 0); tuneButton.anchorMax = new Vector2(1, 0); tuneButton.offsetMin = new Vector2(16, 166); tuneButton.offsetMax = new Vector2(-16, 210);
        Save(root, "JingYuanTunePanel.prefab");
    }

    static void BuildJingYuanWorkstation()
    {
        var root = Panel("JingYuanTunePanel", "d4f708135ce86d209b3c5e7f8192a3b4");
        var built = Child(root, "BuiltRoot");
        Image("Background", built, new Color(.055f, .065f, .09f, .98f), true);
        Text("Title", built, "Jing Yuan Pool - Tune", 26, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(0, -62));
        Text("EquipmentHeader", built, "Equipped Essences", 15, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -70), new Vector2(0, -96));
        var slots = Child(built, "EquipSlots"); SetRect(slots, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -190), new Vector2(-28, -102));
        var slotLayout = slots.AddComponent<HorizontalLayoutGroup>(); slotLayout.spacing = 12; slotLayout.childForceExpandWidth = false; slotLayout.childForceExpandHeight = true;
        Text("SourceHeader", built, "Essence Source", 15, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -214), new Vector2(0, -240));
        var source = Image("SourcePanel", built, new Color(.09f, .1f, .14f, 1), false); SetRect(source.rectTransform, new Vector2(0, 0), new Vector2(.62f, 1), new Vector2(28, 20), new Vector2(-12, -250));
        var tabs = Child(source.transform, "SourceTabs"); SetRect(tabs, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -42), new Vector2(-8, -8));
        Button("TemporaryTab", tabs.transform, "Carried 0/0", 0, 0); SetRect(tabs.transform.Find("TemporaryTab").GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(.5f, 1), Vector2.zero, Vector2.zero);
        Button("WarehouseTab", tabs.transform, "Warehouse 0/50", 0, 0); SetRect(tabs.transform.Find("WarehouseTab").GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Text("SourceHint", source.transform, "Click to equip. Use Donor to select a tuning material.", 12, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -70), new Vector2(-12, -44));
        var candidate = Image("CandidatePanel", source.transform, new Color(.07f, .08f, .11f, 1), false); SetRect(candidate.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -78));
        var scroll = candidate.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        var viewport = Child(candidate.transform, "Viewport"); SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); viewport.AddComponent<RectMask2D>();
        var content = Child(viewport, "Content"); SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -8), new Vector2(-8, 8));
        var grid = content.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(190, 82); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content.GetComponent<RectTransform>();
        var detail = Image("DetailPanel", built, new Color(.09f, .1f, .14f, 1), false); SetRect(detail.rectTransform, new Vector2(.62f, 0), Vector2.one, new Vector2(12, 20), new Vector2(-28, -250));
        Text("Type", detail.transform, "Select an equipment slot", 20, Vector2.zero, Vector2.one, new Vector2(16, -46), new Vector2(-16, -16));
        Text("Quality", detail.transform, "Quality: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -80), new Vector2(-16, -52));
        Text("Level", detail.transform, "Level: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -112), new Vector2(-16, -84));
        Text("Concentration", detail.transform, "Concentration: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -144), new Vector2(-16, -116));
        Text("ShelfLife", detail.transform, "Shelf life: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -176), new Vector2(-16, -148));
        Text("MainEffect", detail.transform, "Main effect: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -208), new Vector2(-16, -180));
        Text("ExtraAffix", detail.transform, "Extra affix: -", 14, Vector2.zero, Vector2.one, new Vector2(16, -246), new Vector2(-16, -214));
        Text("Location", detail.transform, "Location: -", 13, Vector2.zero, Vector2.one, new Vector2(16, -278), new Vector2(-16, -252));
        Text("Renewal", detail.transform, "Renewal: -", 13, Vector2.zero, Vector2.one, new Vector2(16, 142), new Vector2(-16, 168));
        Button("RenewButton", detail.transform, "Renew", 0, 1); var renew = detail.transform.Find("RenewButton").GetComponent<RectTransform>(); renew.anchorMin = new Vector2(0, 0); renew.anchorMax = new Vector2(1, 0); renew.offsetMin = new Vector2(16, 82); renew.offsetMax = new Vector2(-16, 124);
        Text("TuneStatus", detail.transform, "Donor: -  Success: -", 13, Vector2.zero, Vector2.one, new Vector2(16, 42), new Vector2(-16, 70));
        var boost = Child(detail.transform, "ResidueBoostToggle"); boost.AddComponent<Toggle>(); SetRect(boost, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 12), new Vector2(-16, 40)); Text("Label", boost.transform, "Use residue to improve success", 13, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(0, 0));
        Button("TuneButton", detail.transform, "Tune", 0, 1); var tune = detail.transform.Find("TuneButton").GetComponent<RectTransform>(); tune.anchorMin = new Vector2(0, 0); tune.anchorMax = new Vector2(1, 0); tune.offsetMin = new Vector2(16, -42); tune.offsetMax = new Vector2(-16, 2);
        Save(root, "JingYuanTunePanel.prefab");
    }

    static void BuildJingYuanWarehouse()
    {
        var root = new GameObject("SecretBaseJingYuanWarehousePanel", typeof(RectTransform), typeof(CanvasGroup));
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.AddComponent<My.UI.JingYuanWarehousePanel>();
        var built = Child(root, "BuiltRoot"); SetRect(built, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image("Background", built, new Color(.055f, .065f, .09f, .98f), true);
        Text("Title", built, "Premium Essence Warehouse", 26, new Vector2(.06f, .9f), new Vector2(.7f, .98f), Vector2.zero, Vector2.zero);
        Text("Capacity", built, "Warehouse 0/50", 15, new Vector2(.06f, .84f), new Vector2(.7f, .9f), Vector2.zero, Vector2.zero);
        var close = Child(built, "CloseButton"); close.AddComponent<Image>().color = new Color(.18f, .12f, .18f, 1); close.AddComponent<Button>();
        SetRect(close, new Vector2(.9f, .9f), new Vector2(.98f, .98f), Vector2.zero, Vector2.zero);
        Text("Label", close.transform, "X", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var scroll = Image("Scroll", built, new Color(.09f, .1f, .14f, 1), false);
        SetRect(scroll.rectTransform, new Vector2(.06f, .08f), new Vector2(.94f, .82f), Vector2.zero, Vector2.zero);
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>(); scrollRect.horizontal = false; scrollRect.vertical = true;
        var viewport = Child(scroll.transform, "Viewport"); SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); viewport.AddComponent<RectMask2D>();
        var content = Child(viewport, "Content"); SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -8), new Vector2(-8, 8));
        var grid = content.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(210, 96); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scrollRect.viewport = viewport.GetComponent<RectTransform>(); scrollRect.content = content.GetComponent<RectTransform>();
        var template = Child(built, "CellTemplate"); template.SetActive(false); template.AddComponent<Image>().color = new Color(.14f, .18f, .22f, 1); template.AddComponent<Button>();
        SetRect(template, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(210, 96));
        var icon = Image("Icon", template.transform, Color.white, false); SetRect(icon.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 24), new Vector2(64, 88));
        Text("Label", template.transform, "Premium Essence", 13, new Vector2(0, 0), new Vector2(1, 1), new Vector2(76, 8), new Vector2(-8, -8));
        PrefabUtility.SaveAsPrefabAsset(root, $"{SecretBaseRoot}/SecretBaseJingYuanWarehousePanel.prefab");
        Object.DestroyImmediate(root);
    }

    static void BuildJingYuanCarried()
    {
        var root = new GameObject("JingYuanCarriedPanel", typeof(RectTransform), typeof(CanvasGroup));
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.AddComponent<My.UI.JingYuanCarriedPanel>();
        var built = Child(root, "BuiltRoot"); SetRect(built, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image("Background", built, new Color(.055f, .065f, .09f, .98f), true);
        Text("Title", built, "随身精元", 26, new Vector2(.06f, .9f), new Vector2(.7f, .98f), Vector2.zero, Vector2.zero);
        Text("Capacity", built, "随身精元 0/10", 15, new Vector2(.06f, .84f), new Vector2(.7f, .9f), Vector2.zero, Vector2.zero);
        Text("Overflow", built, "随身精元未超限", 14, new Vector2(.06f, .78f), new Vector2(.86f, .84f), Vector2.zero, Vector2.zero);
        var close = Child(built, "CloseButton"); close.AddComponent<Image>().color = new Color(.18f, .12f, .18f, 1); close.AddComponent<Button>();
        SetRect(close, new Vector2(.9f, .9f), new Vector2(.98f, .98f), Vector2.zero, Vector2.zero); Text("Label", close.transform, "X", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var scroll = Image("Scroll", built, new Color(.09f, .1f, .14f, 1), false); SetRect(scroll.rectTransform, new Vector2(.06f, .08f), new Vector2(.94f, .75f), Vector2.zero, Vector2.zero);
        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>(); scrollRect.horizontal = false; scrollRect.vertical = true;
        var viewport = Child(scroll.transform, "Viewport"); SetRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); viewport.AddComponent<RectMask2D>();
        var content = Child(viewport, "Content"); SetRect(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -8), new Vector2(-8, 8));
        var grid = content.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(220, 86); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scrollRect.viewport = viewport.GetComponent<RectTransform>(); scrollRect.content = content.GetComponent<RectTransform>();
        var template = Child(built, "CellTemplate"); template.SetActive(false); template.AddComponent<Image>().color = new Color(.14f, .18f, .22f, 1); template.AddComponent<Button>(); SetRect(template, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(220, 86));
        var icon = Image("Icon", template.transform, Color.white, false); SetRect(icon.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 20), new Vector2(64, 78));
        Text("Label", template.transform, "Premium Essence", 13, new Vector2(0, 0), new Vector2(1, 1), new Vector2(76, 8), new Vector2(-74, -8));
        var equip = Child(template.transform, "EquipButton"); equip.AddComponent<Image>().color = new Color(.28f, .16f, .38f, 1); equip.AddComponent<Button>(); SetRect(equip, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-66, 8), new Vector2(-8, 34)); Text("Label", equip.transform, "装备", 11, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        PrefabUtility.SaveAsPrefabAsset(root, $"{SecretBaseRoot}/JingYuanCarriedPanel.prefab"); Object.DestroyImmediate(root);
    }

    static void BuildJingYuanPool()
    {
        var root = new GameObject("JingYuanPoolPanel", typeof(RectTransform), typeof(CanvasGroup));
        SetRect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.AddComponent<My.UI.JingYuanPoolPanel>();
        var built = Child(root, "BuiltRoot"); SetRect(built, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image("Background", built, new Color(.055f, .065f, .09f, .98f), true);
        Text("Title", built, "精元池", 30, new Vector2(.1f, .78f), new Vector2(.9f, .9f), Vector2.zero, Vector2.zero);
        Text("Description", built, "在古老火团的供奉中调和精元", 15, new Vector2(.1f, .69f), new Vector2(.9f, .77f), Vector2.zero, Vector2.zero);
        var tune = Child(built, "TuneButton"); tune.AddComponent<Image>().color = new Color(.28f, .16f, .38f, 1); tune.AddComponent<Button>();
        SetRect(tune, new Vector2(.18f, .42f), new Vector2(.82f, .57f), Vector2.zero, Vector2.zero); Text("Label", tune.transform, "调精", 20, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var warehouse = Child(built, "WarehouseButton"); warehouse.AddComponent<Image>().color = new Color(.16f, .22f, .3f, 1); warehouse.AddComponent<Button>();
        SetRect(warehouse, new Vector2(.18f, .25f), new Vector2(.82f, .4f), Vector2.zero, Vector2.zero); Text("Label", warehouse.transform, "精元仓库", 20, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var close = Child(built, "CloseButton"); close.AddComponent<Image>().color = new Color(.18f, .12f, .18f, 1); close.AddComponent<Button>();
        SetRect(close, new Vector2(.88f, .86f), new Vector2(.96f, .94f), Vector2.zero, Vector2.zero); Text("Label", close.transform, "X", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var serialized = new SerializedObject(root.GetComponent<My.UI.JingYuanPoolPanel>());
        serialized.FindProperty("tuneButton").objectReferenceValue = tune.GetComponent<Button>();
        serialized.FindProperty("warehouseButton").objectReferenceValue = warehouse.GetComponent<Button>();
        serialized.FindProperty("closeButton").objectReferenceValue = close.GetComponent<Button>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, $"{SecretBaseRoot}/JingYuanPoolPanel.prefab");
        Object.DestroyImmediate(root);
    }

    static void WireSecretBaseWarehouseButton()
    {
        const string path = "Assets/Resources/UI/Prefabs/SecretBaseHudPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var panel = root.GetComponent<My.UI.SecretBaseHudPanel>();
        var parent = root.transform.Find("TopRightBar") ?? root.transform;
        var button = parent.Find("BtnJingYuanWarehouse");
        if (button == null)
        {
            var go = Child(parent, "BtnJingYuanWarehouse");
            go.AddComponent<Image>().color = new Color(.16f, .12f, .18f, 1); go.AddComponent<Button>();
            SetRect(go, new Vector2(.42f, 0), new Vector2(.7f, 1), Vector2.zero, Vector2.zero);
            Text("Text", go.transform, "精元仓库", 14, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            button = go.transform;
        }
        var serialized = new SerializedObject(panel);
        serialized.FindProperty("btnJingYuanWarehouse").objectReferenceValue = button.GetComponent<Button>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void WirePlayerBagJingYuanButton()
    {
        const string path = "Assets/Resources/UI/Prefabs/PlayerBag.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        var panel = root.GetComponent<My.UI.Bag.PlayerBagUIPanel>();
        if (panel == null) { PrefabUtility.UnloadPrefabContents(root); return; }
        var button = root.transform.Find("JingYuanButton");
        if (button == null)
        {
            var go = Child(root.transform, "JingYuanButton"); go.AddComponent<Image>().color = new Color(.22f, .14f, .3f, 1); go.AddComponent<Button>();
            SetRect(go, new Vector2(.72f, .9f), new Vector2(.88f, .98f), Vector2.zero, Vector2.zero); Text("Label", go.transform, "精元", 13, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            button = go.transform;
        }
        var serialized = new SerializedObject(panel); serialized.FindProperty("JingYuanButton").objectReferenceValue = button.GetComponent<Button>(); serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }

    static void BuildCult()
    {
        var root = Panel("CultPanel", "c7a1e8f24b3d4e6a9f0c1d2e3a4b5c6d");
        AddComponent(root, "a2b3c4d5e6f74899aabbccddee001122");
        AddComponent(root, "d8b2f9e35c4e5f7b0a1d2e3f4b5c6d7e");
        Image("Background", root, new Color(.06f, .04f, .08f, .96f), true);

        var overview = Child(root, "OverviewRoot");
        SetRect(overview, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddComponent(overview, "c04e15ed305f2284a9bf2905542526ff");
        Image("OverviewBackground", overview, new Color(.07f, .045f, .075f, .98f), true);
        Text("OverviewTitle", overview, "????", 30, new Vector2(.08f, .78f), new Vector2(.92f, .9f), Vector2.zero, Vector2.zero);
        Text("OverviewSubtitle", overview, "????????????????", 15, new Vector2(.08f, .72f), new Vector2(.92f, .79f), Vector2.zero, Vector2.zero);
        var cards = Child(overview, "OverviewCards"); SetRect(cards, new Vector2(.08f, .35f), new Vector2(.92f, .68f), Vector2.zero, Vector2.zero);
        OverviewCard(cards, "FaithCard", "??", "?????????", "FaithValue", 0);
        OverviewCard(cards, "DoctrineCard", "????", "??????????", "DoctrineValue", 1);
        OverviewCard(cards, "SeatCard", "?????", "????????", "SeatValue", 2);
        OverviewCard(cards, "SeatTechCard", "????", "????????", "SeatTechValue", 3);
        var influence = Image("InfluenceCard", overview, new Color(.11f, .07f, .12f, .96f), false);
        SetRect(influence.rectTransform, new Vector2(.08f, .12f), new Vector2(.92f, .29f), Vector2.zero, Vector2.zero);
        Text("InfluenceLabel", influence.transform, "????", 18, new Vector2(.05f, .56f), new Vector2(.4f, .9f), Vector2.zero, Vector2.zero);
        Text("InfluenceValue", influence.transform, "???", 28, new Vector2(.55f, .28f), new Vector2(.95f, .82f), Vector2.zero, Vector2.zero);
        Text("InfluenceHint", influence.transform, "???????????????????????", 13, new Vector2(.05f, .12f), new Vector2(.8f, .42f), Vector2.zero, Vector2.zero);

        Text("FaithText", root, "??", 20, new Vector2(.04f, .9f), new Vector2(.68f, .97f), Vector2.zero, Vector2.zero);
        var tree = Child(root, "TreeRoot"); SetRect(tree, new Vector2(.03f, .1f), new Vector2(.68f, .88f), Vector2.zero, Vector2.zero);
        tree.AddComponent<CanvasGroup>();
        var connections = Child(tree, "Connections");
        SetRect(connections, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        connections.transform.SetAsFirstSibling();
        AddCultTechNodes(tree);
        var seats = Child(root, "SeatRoot"); SetRect(seats, new Vector2(.04f, .12f), new Vector2(.96f, .86f), Vector2.zero, Vector2.zero);
        var seatTemplate = Child(seats, "SeatCardTemplate");
        seatTemplate.SetActive(false);
        seatTemplate.AddComponent<Image>().color = new Color(.2f, .12f, .2f, .98f);
        seatTemplate.AddComponent<Button>();
        SetRect(seatTemplate, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-56, -195), new Vector2(56, 195));
        var seatPortrait = Image("Portrait", seatTemplate, Color.white, false);
        SetRect(seatPortrait.rectTransform, new Vector2(.12f, .2f), new Vector2(.88f, .92f), Vector2.zero, Vector2.zero);
        Text("Label", seatTemplate, "??", 14, new Vector2(.05f, .04f), new Vector2(.95f, .17f), Vector2.zero, Vector2.zero);
        var seatStatus = Image("StatusIcon", seatTemplate, Color.white, false);
        SetRect(seatStatus.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(-6, -6));
        Text("SeatHeader", root, "????", 20, new Vector2(.2f, .87f), new Vector2(.68f, .93f), Vector2.zero, Vector2.zero);
        Text("SeatDescription", root, "????", 13, new Vector2(.2f, .82f), new Vector2(.68f, .87f), Vector2.zero, Vector2.zero);
        Button("BackToSeatsButton", root, "????", 0);
        SetRect(root.transform.Find("BackToSeatsButton").gameObject, new Vector2(.02f, .87f), new Vector2(.16f, .93f), Vector2.zero, Vector2.zero);
        Button("PreviousSeatButton", root, "<", 0);
        SetRect(root.transform.Find("PreviousSeatButton").gameObject, new Vector2(.16f, .87f), new Vector2(.2f, .93f), Vector2.zero, Vector2.zero);
        Button("NextSeatButton", root, ">", 0);
        SetRect(root.transform.Find("NextSeatButton").gameObject, new Vector2(.68f, .87f), new Vector2(.72f, .93f), Vector2.zero, Vector2.zero);
        Detail(root, "DetailArea", .72f, "????");

        var tabs = Child(root, "CultTabs");
        SetRect(tabs, new Vector2(.02f, .94f), new Vector2(.68f, .995f), Vector2.zero, Vector2.zero);
        Button("OverviewTab", tabs, "????", 0, 3);
        Button("DoctrineTab", tabs, "????", 1, 3);
        Button("AncientSeatTab", tabs, "?????", 2, 3);
        Button("CloseButton", root, "X", 0);
        Save(root, "CultPanel.prefab");
    }

    static void BuildCultConnectionPrefab()
    {
        var root = new GameObject("CultTechConnection_View", typeof(RectTransform), typeof(Image));
        var image = root.GetComponent<Image>();
        image.color = new Color(.62f, .28f, .4f, .7f);
        image.raycastTarget = false;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 3f);
        Save(root, "CultTechConnection_View.prefab");
    }

    static void BuildCultNodePrefab()
    {
        var root = new GameObject("CultTechNode_View", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(118f, 118f);
        root.GetComponent<Image>().color = new Color(.11f, .07f, .17f, .98f);
        root.AddComponent<My.UI.CultTech.CultTechNodeBinder>();
        root.AddComponent<My.UI.CultTech.CultTechNodeView>();
        root.AddComponent<My.UI.CultTech.CultTechNodeHoverView>();

        var selection = Image("SelectionFrame", root.transform, new Color(.95f, .55f, 1f, .9f), false);
        SetRect(selection.rectTransform, Vector2.zero, Vector2.one, new Vector2(-4, -4), new Vector2(4, 4));
        selection.gameObject.SetActive(false);
        var icon = Image("NodeIcon", root.transform, Color.white, false);
        SetRect(icon.rectTransform, new Vector2(.18f, .34f), new Vector2(.82f, .9f), Vector2.zero, Vector2.zero);
        var spritePath = "Assets/Resources/UI/Cult/Generated/CultNode_Core.png";
        EnsureSprite(spritePath);
        icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        Text("Title", root.transform, "????", 13, new Vector2(.05f, .18f), new Vector2(.95f, .34f), Vector2.zero, Vector2.zero);
        Text("Level", root.transform, "Lv0/1", 11, new Vector2(.05f, .05f), new Vector2(.42f, .17f), Vector2.zero, Vector2.zero);
        var action = Child(root.transform, "ActionButton");
        SetRect(action, new Vector2(.5f, .05f), new Vector2(.95f, .17f), Vector2.zero, Vector2.zero);
        action.AddComponent<Image>().color = new Color(.28f, .16f, .38f, .98f);
        action.AddComponent<Button>();
        Text("Label", action.transform, "??", 10, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var hoverTip = Image("HoverTip", root.transform, new Color(.055f, .035f, .08f, .98f), false);
        SetRect(hoverTip.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(30f, 8f), new Vector2(290f, 104f));
        hoverTip.raycastTarget = false;
        hoverTip.gameObject.SetActive(false);
        Text("Title", hoverTip.transform, "????", 14, new Vector2(.06f, .64f), new Vector2(.94f, .92f), Vector2.zero, Vector2.zero);
        Text("Summary", hoverTip.transform, "????", 11, new Vector2(.06f, .3f), new Vector2(.94f, .62f), Vector2.zero, Vector2.zero);
        Text("Status", hoverTip.transform, "??", 11, new Vector2(.06f, .06f), new Vector2(.94f, .27f), Vector2.zero, Vector2.zero);
        Save(root, "CultTechNode_View.prefab");
    }

    static void BuildCultSeatNodePrefab()
    {
        const string seatRoot = Root + "/CultSeats";
        Directory.CreateDirectory(seatRoot);
        var root = new GameObject("CultSeatTechNode_View", typeof(RectTransform), typeof(Image), typeof(Button));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(156f, 108f);
        root.GetComponent<Image>().color = new Color(.12f, .08f, .18f, .98f);
        root.AddComponent<My.UI.CultTech.CultSeatTechNodeBinder>();
        root.AddComponent<My.UI.CultTech.CultSeatTechNodeView>();
        var selection = Image("SelectionFrame", root.transform, new Color(.95f, .55f, 1f, .9f), false);
        SetRect(selection.rectTransform, Vector2.zero, Vector2.one, new Vector2(-4, -4), new Vector2(4, 4));
        selection.gameObject.SetActive(false);
        var icon = Image("NodeIcon", root.transform, Color.white, false);
        SetRect(icon.rectTransform, new Vector2(.34f, .48f), new Vector2(.66f, .92f), Vector2.zero, Vector2.zero);
        Text("Title", root.transform, "???", 13, new Vector2(.05f, .24f), new Vector2(.95f, .45f), Vector2.zero, Vector2.zero);
        Text("Level", root.transform, "Lv0/1", 11, new Vector2(.05f, .06f), new Vector2(.45f, .22f), Vector2.zero, Vector2.zero);
        Text("SeatHint", root.transform, "?????", 10, new Vector2(.42f, .06f), new Vector2(.95f, .22f), Vector2.zero, Vector2.zero);
        SaveAt(root, $"{seatRoot}/CultSeatTechNode_View.prefab");
    }

    static void BuildCultSeatLayouts()
    {
        const string seatRoot = Root + "/CultSeats";
        Directory.CreateDirectory(seatRoot);
        var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{seatRoot}/CultSeatTechNode_View.prefab");
        if (nodePrefab == null) { Debug.LogError("CultSeatTechNode_View prefab is missing."); return; }
        BuildCultSeatLayout(1, new[] { 20, 21, 22, 23, 24 }, nodePrefab);
        BuildCultSeatLayout(2, new[] { 120, 121, 122, 123, 124 }, nodePrefab);
    }

    static void BuildCultSeatLayout(int seatId, int[] nodeIds, GameObject nodePrefab)
    {
        var root = new GameObject($"Seat_{seatId}_Layout", typeof(RectTransform));
        var rootRect = root.GetComponent<RectTransform>();
        SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        for (var i = 0; i < nodeIds.Length; i++)
        {
            var node = (GameObject)PrefabUtility.InstantiatePrefab(nodePrefab, root.transform);
            node.name = $"Seat_{seatId}_Node_{nodeIds[i]}";
            node.GetComponent<My.UI.CultTech.CultSeatTechNodeBinder>().SetNodeIdForEditor(nodeIds[i]);
            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            var angle = 90f + i * 72f;
            var radians = angle * Mathf.Deg2Rad;
            rect.anchoredPosition = new Vector2(Mathf.Cos(radians) * 250f, Mathf.Sin(radians) * 250f);
        }
        SaveAt(root, $"{Root}/CultSeats/Seat_{seatId}_Layout.prefab");
    }

    static void AddCultTechNodes(GameObject tree)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/CultTechNode_View.prefab");
        if (prefab == null)
        {
            Debug.LogError("CultTechNode_View prefab is missing.");
            return;
        }

        int[] inner = { 1, 10, 11, 12, 13 };
        int[] outer = { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34 };
        for (int i = 0; i < inner.Length; i++)
        {
            AddCultTechNodeInstance(prefab, tree.transform, inner[i], 180f, 90f + i * 72f);
        }

        for (int i = 0; i < outer.Length; i++)
        {
            AddCultTechNodeInstance(prefab, tree.transform, outer[i], 330f, 76f + i * 24f);
        }
    }

    static void AddCultTechNodeInstance(GameObject prefab, Transform parent, int nodeId, float radius, float angle)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = $"CultTechNode_{nodeId}";
        var binder = instance.GetComponent<My.UI.CultTech.CultTechNodeBinder>();
        binder.SetNodeIdForEditor(nodeId);
        var rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        float radians = angle * Mathf.Deg2Rad;
        rect.anchoredPosition = new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
    }

    static void EnsureSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
    }

    static void OverviewCard(GameObject parent, string name, string title, string hint, string valueName, int index)
    {
        var card = Image(name, parent, new Color(.11f, .07f, .12f, .96f), false);
        SetRect(card.rectTransform, new Vector2(index * .255f, 0), new Vector2(index * .255f + .235f, 1), Vector2.zero, Vector2.zero);
        Text("Title", card.transform, title, 16, new Vector2(.1f, .68f), new Vector2(.9f, .9f), Vector2.zero, Vector2.zero);
        Text(valueName, card.transform, "0", 30, new Vector2(.1f, .28f), new Vector2(.9f, .66f), Vector2.zero, Vector2.zero);
        Text("Hint", card.transform, hint, 11, new Vector2(.1f, .08f), new Vector2(.9f, .26f), Vector2.zero, Vector2.zero);
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

    static void Button(string name, Transform parent, string label, int index, int count = 2)
    {
        var go = Child(parent, name); var image = go.AddComponent<Image>(); image.color = new Color(.16f, .12f, .18f, 1); go.AddComponent<Button>(); SetRect(go, new Vector2(index / (float)count, 0), new Vector2((index + 1) / (float)count - .01f, 1), Vector2.zero, Vector2.zero); Text("Label", go.transform, label, 16, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }
    static void Button(string name, GameObject parent, string label, int index, int count = 2) => Button(name, parent.transform, label, index, count);

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
        SaveAt(root, $"{Root}/{fileName}");
    }

    static void SaveAt(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }
}
