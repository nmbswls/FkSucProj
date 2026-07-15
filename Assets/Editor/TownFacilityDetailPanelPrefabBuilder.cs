using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TownFacilityDetailPanelPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/TownFacilityDetailPanel.prefab";
    const string ScriptGuid = "056745ae525017c4788c8bc754f7f42e";
    const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee";

    public static void Rebuild()
    {
        Build();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("TownFacilityDetailPanel prefab rebuilt with upgrade section.");
    }

    static void Build()
    {
        var root = Panel("TownFacilityDetailPanel");
        var panel = root.GetComponent<My.UI.Home.TownFacilityDetailPanel>();
        var card = Image("Content", root.transform, new Color(.1f, .12f, .16f, .98f), false);
        SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-300, -430), new Vector2(300, 430));

        var txtTitle = Text("Title", card.transform, "设施", 24, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -52), new Vector2(-16, -16));
        var txtLevel = Text("Level", card.transform, "Lv.1", 16, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -84), new Vector2(-16, -56));
        txtLevel.alignment = TextAlignmentOptions.MidlineRight;

        var statusCard = Image("StatusCard", card.transform, new Color(.14f, .17f, .22f, .95f), false);
        SetRect(statusCard.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -188), new Vector2(-16, -60));
        var txtStatusTitle = Text("StatusTitle", statusCard.transform, "当前等级", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -34), new Vector2(-12, -10));
        txtStatusTitle.fontStyle = FontStyles.Bold;
        var txtStatusDesc = Text("StatusDesc", statusCard.transform, "", 14, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -72), new Vector2(-12, -36));
        var txtDailyOutput = Text("DailyOutput", statusCard.transform, "每日产出", 14, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 10), new Vector2(-12, 34));
        txtDailyOutput.richText = true;

        var nextCard = Image("NextLevelCard", card.transform, new Color(.12f, .16f, .21f, .95f), false);
        SetRect(nextCard.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -332), new Vector2(-16, -196));
        var txtNextLevelHeader = Text("NextLevelHeader", nextCard.transform, "升级预览", 14, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -30), new Vector2(-12, -8));
        txtNextLevelHeader.color = new Color(.62f, .78f, .92f, 1f);
        var txtNextLevelTitle = Text("NextLevelTitle", nextCard.transform, "Lv.2", 15, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -58), new Vector2(-12, -34));
        txtNextLevelTitle.fontStyle = FontStyles.Bold;
        var txtNextLevelDesc = Text("NextLevelDesc", nextCard.transform, "", 13, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -88), new Vector2(-12, -60));
        var txtUpgradeCosts = Text("UpgradeCosts", nextCard.transform, "消耗", 13, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -150), new Vector2(-12, -92));
        txtUpgradeCosts.richText = true;
        var txtNextDailyOutput = Text("NextDailyOutput", nextCard.transform, "升级后产出", 13, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -178), new Vector2(-12, -154));
        txtNextDailyOutput.richText = true;
        var txtUpgradeHint = Text("UpgradeHint", nextCard.transform, "", 12, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 8), new Vector2(-12, 32));
        txtUpgradeHint.richText = true;

        var supervisorHeader = Text("SupervisorHeader", card.transform, "领头者", 16, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -364), new Vector2(-16, -336));
        var supervisorListRoot = Child(card.transform, "SupervisorList");
        SetRect(supervisorListRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -468), new Vector2(-16, -372));
        var supervisorLayout = supervisorListRoot.AddComponent<VerticalLayoutGroup>();
        supervisorLayout.spacing = 6;
        supervisorLayout.childControlHeight = true;
        supervisorLayout.childForceExpandHeight = false;
        supervisorLayout.childForceExpandWidth = true;
        var supervisorSlots = new My.UI.Home.TownFacilitySupervisorSlotView[2];
        for (int i = 0; i < supervisorSlots.Length; i++)
        {
            supervisorSlots[i] = BuildSupervisorSlot(supervisorListRoot.transform, $"SupervisorSlot_{i + 1}");
        }

        var helperHeader = Text("HelperHeader", card.transform, "帮工人手", 16, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -500), new Vector2(-16, -472));
        var workforceRow = Child(card.transform, "WorkforceRow");
        SetRect(workforceRow, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -540), new Vector2(-16, -504));
        var txtWorkforceValue = Text("WorkforceLabel", workforceRow.transform, "帮工 0/0", 16, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(160, 0));
        var sliderGo = Child(workforceRow.transform, "WorkforceSlider");
        SetRect(sliderGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(168, 0), Vector2.zero);
        var slider = sliderGo.AddComponent<Slider>();
        var sliderImg = sliderGo.AddComponent<Image>();
        sliderImg.color = new Color(.2f, .22f, .28f, 1f);
        slider.targetGraphic = sliderImg;
        var fill = Image("Fill", sliderGo.transform, new Color(.35f, .55f, .75f, 1f), true);
        slider.fillRect = fill.rectTransform;

        var renovationHeader = Text("RenovationHeader", card.transform, "改造项", 16, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -572), new Vector2(-16, -544));
        var renovationListRoot = Child(card.transform, "RenovationList");
        SetRect(renovationListRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -788), new Vector2(-16, -580));
        var renovationLayout = renovationListRoot.AddComponent<VerticalLayoutGroup>();
        renovationLayout.spacing = 6;
        renovationLayout.childControlHeight = true;
        renovationLayout.childForceExpandHeight = false;
        renovationLayout.childForceExpandWidth = true;
        var renovationSlots = new My.UI.Home.TownFacilityRenovationSlotView[3];
        for (int i = 0; i < renovationSlots.Length; i++)
        {
            renovationSlots[i] = BuildRenovationSlot(renovationListRoot.transform, $"RenovationSlot_{i + 1}");
        }

        var btnLearnGo = Child(card.transform, "BtnLearnRenovation");
        SetRect(btnLearnGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -832), new Vector2(-16, -796));
        var btnLearnImg = btnLearnGo.AddComponent<Image>();
        btnLearnImg.color = new Color(.24f, .38f, .52f, 1f);
        var btnLearnRenovation = btnLearnGo.AddComponent<Button>();
        btnLearnRenovation.targetGraphic = btnLearnImg;
        var txtLearnRenovation = Text("Label", btnLearnGo.transform, "学习", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var btnUpgradeGo = Child(card.transform, "BtnUpgrade");
        SetRect(btnUpgradeGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 56), new Vector2(-16, 100));
        var btnUpgradeImg = btnUpgradeGo.AddComponent<Image>();
        btnUpgradeImg.color = new Color(.24f, .46f, .34f, 1f);
        var btnUpgrade = btnUpgradeGo.AddComponent<Button>();
        btnUpgrade.targetGraphic = btnUpgradeImg;
        var txtUpgrade = Text("Label", btnUpgradeGo.transform, "升级", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var btnCloseGo = Child(card.transform, "Close");
        SetRect(btnCloseGo, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -48), new Vector2(-12, -12));
        var btnCloseImg = btnCloseGo.AddComponent<Image>();
        btnCloseImg.color = new Color(.35f, .2f, .2f, 1f);
        var btnClose = btnCloseGo.AddComponent<Button>();
        btnClose.targetGraphic = btnCloseImg;
        Text("Label", btnCloseGo.transform, "X", 18, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var txtDesc = Text("Desc", card.transform, "", 14, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -160), new Vector2(-16, -96));
        txtDesc.gameObject.SetActive(false);

        var so = new SerializedObject(panel);
        so.FindProperty("panelId").stringValue = "TownFacilityDetailPanel";
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("txtTitle").objectReferenceValue = txtTitle;
        so.FindProperty("txtLevel").objectReferenceValue = txtLevel;
        so.FindProperty("txtDesc").objectReferenceValue = txtDesc;
        so.FindProperty("statusCardRoot").objectReferenceValue = statusCard.gameObject;
        so.FindProperty("txtStatusTitle").objectReferenceValue = txtStatusTitle;
        so.FindProperty("txtStatusDesc").objectReferenceValue = txtStatusDesc;
        so.FindProperty("txtDailyOutput").objectReferenceValue = txtDailyOutput;
        so.FindProperty("nextLevelCardRoot").objectReferenceValue = nextCard.gameObject;
        so.FindProperty("txtNextLevelHeader").objectReferenceValue = txtNextLevelHeader;
        so.FindProperty("txtNextLevelTitle").objectReferenceValue = txtNextLevelTitle;
        so.FindProperty("txtNextLevelDesc").objectReferenceValue = txtNextLevelDesc;
        so.FindProperty("txtUpgradeCosts").objectReferenceValue = txtUpgradeCosts;
        so.FindProperty("txtNextDailyOutput").objectReferenceValue = txtNextDailyOutput;
        so.FindProperty("txtUpgradeHint").objectReferenceValue = txtUpgradeHint;
        so.FindProperty("txtSupervisorHeader").objectReferenceValue = supervisorHeader;
        so.FindProperty("supervisorSlots").arraySize = supervisorSlots.Length;
        for (int i = 0; i < supervisorSlots.Length; i++)
        {
            so.FindProperty("supervisorSlots").GetArrayElementAtIndex(i).objectReferenceValue = supervisorSlots[i];
        }
        so.FindProperty("txtHelperHeader").objectReferenceValue = helperHeader;
        so.FindProperty("txtWorkforceValue").objectReferenceValue = txtWorkforceValue;
        so.FindProperty("workforceSlider").objectReferenceValue = slider;
        so.FindProperty("txtRenovationHeader").objectReferenceValue = renovationHeader;
        so.FindProperty("renovationListRoot").objectReferenceValue = renovationListRoot.GetComponent<RectTransform>();
        so.FindProperty("renovationSlots").arraySize = renovationSlots.Length;
        for (int i = 0; i < renovationSlots.Length; i++)
        {
            so.FindProperty("renovationSlots").GetArrayElementAtIndex(i).objectReferenceValue = renovationSlots[i];
        }
        so.FindProperty("btnLearnRenovation").objectReferenceValue = btnLearnRenovation;
        so.FindProperty("txtLearnRenovation").objectReferenceValue = txtLearnRenovation;
        so.FindProperty("btnUpgrade").objectReferenceValue = btnUpgrade;
        so.FindProperty("txtUpgrade").objectReferenceValue = txtUpgrade;
        so.FindProperty("btnClose").objectReferenceValue = btnClose;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
    }

    static My.UI.Home.TownFacilitySupervisorSlotView BuildSupervisorSlot(Transform parent, string name)
    {
        var slotGo = Child(parent, name);
        SetRect(slotGo, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 48));
        var slotImg = slotGo.AddComponent<Image>();
        slotImg.color = new Color(.2f, .24f, .3f, .9f);
        var slotBtn = slotGo.AddComponent<Button>();
        slotBtn.targetGraphic = slotImg;
        var slotLabel = Text("Label", slotGo.transform, "选择监工", 16, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-8, -2));
        var slotDesc = Text("Desc", slotGo.transform, "点击指派", 13, new Vector2(0, 0), new Vector2(1, .5f), new Vector2(8, 2), new Vector2(-8, 0));
        slotDesc.color = new Color(.75f, .78f, .82f, 1f);
        var slotView = slotGo.AddComponent<My.UI.Home.TownFacilitySupervisorSlotView>();
        slotView.Button = slotBtn;
        slotView.Label = slotLabel;
        slotView.Desc = slotDesc;
        return slotView;
    }

    static My.UI.Home.TownFacilityRenovationSlotView BuildRenovationSlot(Transform parent, string name)
    {
        var slotGo = Child(parent, name);
        SetRect(slotGo, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 52));
        var slotImg = slotGo.AddComponent<Image>();
        slotImg.color = new Color(.2f, .24f, .3f, .9f);
        var slotBtn = slotGo.AddComponent<Button>();
        slotBtn.targetGraphic = slotImg;
        var slotLabel = Text("Label", slotGo.transform, "改造项", 16, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-8, -2));
        var slotDesc = Text("Desc", slotGo.transform, "描述", 13, new Vector2(0, 0), new Vector2(1, .5f), new Vector2(8, 2), new Vector2(-8, 0));
        slotDesc.color = new Color(.75f, .78f, .82f, 1f);
        var slotView = slotGo.AddComponent<My.UI.Home.TownFacilityRenovationSlotView>();
        slotView.Button = slotBtn;
        slotView.Label = slotLabel;
        slotView.Desc = slotDesc;
        return slotView;
    }

    static GameObject Panel(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        SetRect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddComponent(go, ScriptGuid);
        return go;
    }

    static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Image Image(string name, Transform parent, Color color, bool stretch)
    {
        var go = Child(parent, name);
        var image = go.AddComponent<Image>();
        image.color = color;
        if (stretch)
        {
            SetRect(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        return image;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = Child(parent, name);
        SetRect(go, min, max, offsetMin, offsetMax);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.enableWordWrapping = true;
        text.richText = true;
        text.raycastTarget = false;
        text.color = Color.white;
        ApplyFont(text);
        return text;
    }

    static void ApplyFont(TextMeshProUGUI text)
    {
        var path = AssetDatabase.GUIDToAssetPath(FontGuid);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null)
        {
            text.font = font;
        }
    }

    static void SetRect(GameObject go, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        SetRect(go.GetComponent<RectTransform>(), min, max, offsetMin, offsetMax);
    }

    static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static Component AddComponent(GameObject go, string guid)
    {
        var script = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(script))
        {
            return null;
        }

        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(script);
        return mono == null ? null : go.AddComponent(mono.GetClass());
    }
}
