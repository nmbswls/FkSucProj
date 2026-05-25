#if UNITY_EDITOR
using My.UI;
using My.UI.BodyPart;
using My.UI.SkillLoadout;
using My.UI.Talent;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 养成 Hub 子页 prefab 拼装：所有 TMP 在编辑器内创建，运行时只做绑定与 Instantiate 模板。
public static class ProgressionHubUiPrefabSetup
{
    const string FontPath = "Assets/Fonts/MSYH SDF.asset";
    const string SkillPanelPath = "Assets/Resources/UI/Prefabs/PlayerProgressionHubPanelSub/SkillLoadoutPanel.prefab";
    const string GearPanelPath = "Assets/Resources/UI/Prefabs/PlayerProgressionHubPanelSub/PlayerGearEquipPanel.prefab";
    const string TalentNodePath = "Assets/Resources/UI/Prefabs/PlayerProgressionHubPanelSub/TalentNode_View.prefab";

    static TMP_FontAsset _font;

    static TMP_FontAsset HubFont =>
        _font != null ? _font : (_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));

    [MenuItem("Tools/ProgressionHub/Setup Hub UI Prefabs")]
    public static void SetupFromMenu()
    {
        SetupSkillLoadoutPanel();
        SetupPlayerGearEquipPanel();
        SetupTalentNodeView();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProgressionHubUiPrefabSetup] Hub UI prefabs updated.");
    }

    public static void SetupBatch()
    {
        SetupFromMenu();
    }

    static void SetupSkillLoadoutPanel()
    {
        var root = PrefabUtility.LoadPrefabContents(SkillPanelPath);
        if (root == null)
        {
            return;
        }

        var window = root.transform.Find("BuiltRoot/Window");
        if (window == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        EnsureChild(window, "LearnTitle", out var learnTitleGo);
        var learnTitleLe = learnTitleGo.GetComponent<LayoutElement>() ?? learnTitleGo.AddComponent<LayoutElement>();
        learnTitleLe.preferredHeight = 24f;
        var learnTitleTmp = learnTitleGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(learnTitleGo, "技能学习", 18);
        learnTitleTmp.text = "技能学习";

        EnsureChild(window, "LearnSection", out var learnSectionGo);
        var learnSectionLe = learnSectionGo.GetComponent<LayoutElement>() ?? learnSectionGo.AddComponent<LayoutElement>();
        learnSectionLe.preferredHeight = 120f;
        var learnSectionImg = learnSectionGo.GetComponent<Image>() ?? learnSectionGo.AddComponent<Image>();
        learnSectionImg.color = new Color(0.1f, 0.09f, 0.14f, 0.55f);
        var learnScroll = learnSectionGo.GetComponent<ScrollRect>() ?? learnSectionGo.AddComponent<ScrollRect>();
        learnScroll.horizontal = false;
        learnScroll.vertical = true;

        EnsureChild(learnSectionGo.transform, "Viewport", out var learnViewportGo);
        AddImage(learnViewportGo, Color.white);
        if (learnViewportGo.GetComponent<Mask>() == null)
        {
            learnViewportGo.AddComponent<Mask>();
        }
        StretchFull(learnViewportGo.GetComponent<RectTransform>());

        EnsureChild(learnViewportGo.transform, "Content", out var learnContentGo);
        var learnContentVlg = learnContentGo.GetComponent<VerticalLayoutGroup>() ?? learnContentGo.AddComponent<VerticalLayoutGroup>();
        learnContentVlg.spacing = 4f;
        learnContentVlg.childControlHeight = true;
        learnContentVlg.childForceExpandHeight = false;
        learnContentVlg.padding = new RectOffset(6, 6, 6, 6);
        learnScroll.viewport = learnViewportGo.GetComponent<RectTransform>();
        learnScroll.content = learnContentGo.GetComponent<RectTransform>();

        EnsureChild(learnContentGo.transform, "LearnRow_Template", out var learnRowGo);
        learnRowGo.SetActive(false);
        BuildSkillLearnRow(learnRowGo);

        var poolScroll = window.Find("PoolScroll");
        if (poolScroll != null)
        {
            EnsureChild(poolScroll, "PoolEmptyHint", out var poolHintGo);
            StretchFull(poolHintGo.GetComponent<RectTransform>());
            var poolHintTmp = poolHintGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(poolHintGo, "暂无已学技能，请先在上方学习。", 16);
            poolHintTmp.text = "暂无已学技能，请先在上方学习。";
            poolHintTmp.alignment = TextAlignmentOptions.Center;
            poolHintTmp.color = new Color(0.75f, 0.72f, 0.82f, 0.9f);
            poolHintGo.SetActive(false);
        }

        var tabs = window.Find("Tabs");
        if (tabs != null)
        {
            learnTitleGo.transform.SetSiblingIndex(tabs.GetSiblingIndex() + 1);
            learnSectionGo.transform.SetSiblingIndex(tabs.GetSiblingIndex() + 2);
        }

        EnsurePassiveBar(window);
        PrefabUtility.SaveAsPrefabAsset(root, SkillPanelPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void EnsurePassiveBar(Transform window)
    {
        var passiveRow = window.Find("PassiveBarRow");
        if (passiveRow != null)
        {
            return;
        }

        var barRow = window.Find("BarRow");
        var template = barRow != null ? barRow.Find("Slot_3") : null;
        if (template == null)
        {
            return;
        }

        EnsureChild(window, "PassiveBarLabel", out var labelGo);
        var labelLe = labelGo.GetComponent<LayoutElement>() ?? labelGo.AddComponent<LayoutElement>();
        labelLe.preferredHeight = 24f;
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(labelGo, "被动技能（右键槽位卸下）", 18);
        labelTmp.text = "被动技能（右键槽位卸下）";

        EnsureChild(window, "PassiveBarRow", out var rowGo);
        var rowLe = rowGo.GetComponent<LayoutElement>() ?? rowGo.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 92f;
        var rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>() ?? rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;

        for (int i = 0; i < My.Player.PlayerSkillSystem.PassiveSlotCount; i++)
        {
            var clone = Object.Instantiate(template.gameObject, rowGo.transform);
            clone.name = "PassiveSlot_" + i;
            var view = clone.GetComponent<SkillSlotView>();
            if (view != null)
            {
                view.slotKind = SkillLoadoutSlotKind.Passive;
                view.SlotIndex = i;
            }
        }

        if (barRow != null)
        {
            labelGo.transform.SetSiblingIndex(barRow.GetSiblingIndex() + 1);
            rowGo.transform.SetSiblingIndex(barRow.GetSiblingIndex() + 2);
        }
    }

    static void BuildSkillLearnRow(GameObject rowGo)
    {
        var rt = rowGo.GetComponent<RectTransform>() ?? rowGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 36f);
        var le = rowGo.GetComponent<LayoutElement>() ?? rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;
        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>() ?? rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(4, 4, 2, 2);

        EnsureChild(rowGo.transform, "Title", out var titleGo);
        var titleLe = titleGo.GetComponent<LayoutElement>() ?? titleGo.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(titleGo, "技能名", 15);

        EnsureChild(rowGo.transform, "Reason", out var reasonGo);
        var reasonLe = reasonGo.GetComponent<LayoutElement>() ?? reasonGo.AddComponent<LayoutElement>();
        reasonLe.preferredWidth = 120f;
        var reasonTmp = reasonGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(reasonGo, string.Empty, 12);
        reasonTmp.alignment = TextAlignmentOptions.MidlineRight;
        reasonTmp.color = new Color(0.75f, 0.7f, 0.78f, 1f);

        EnsureChild(rowGo.transform, "LearnBtn", out var btnGo);
        var btnLe = btnGo.GetComponent<LayoutElement>() ?? btnGo.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 72f;
        btnLe.preferredHeight = 28f;
        AddImage(btnGo, new Color(0.28f, 0.45f, 0.62f, 1f));
        var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.75f);
        btn.colors = colors;

        EnsureChild(btnGo.transform, "Text", out var btnTextGo);
        StretchFull(btnTextGo.GetComponent<RectTransform>());
        var btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(btnTextGo, "学习", 14);
        btnTmp.alignment = TextAlignmentOptions.Center;

        var view = rowGo.GetComponent<SkillLearnEntryView>() ?? rowGo.AddComponent<SkillLearnEntryView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("TitleText").objectReferenceValue = titleTmp;
        vso.FindProperty("ReasonText").objectReferenceValue = reasonTmp;
        vso.FindProperty("LearnButton").objectReferenceValue = btn;
        vso.FindProperty("LearnButtonText").objectReferenceValue = btnTmp;
        vso.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupPlayerGearEquipPanel()
    {
        var root = PrefabUtility.LoadPrefabContents(GearPanelPath);
        if (root == null)
        {
            return;
        }

        var window = root.transform.Find("BuiltRoot/Window");
        if (window == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        for (int i = window.childCount - 1; i >= 0; i--)
        {
            var child = window.GetChild(i);
            if (child.name != "Header")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        StretchFull(window.GetComponent<RectTransform>());

        EnsureChild(window, "BodyRow", out var bodyRowGo);
        var bodyLe = bodyRowGo.GetComponent<LayoutElement>() ?? bodyRowGo.AddComponent<LayoutElement>();
        bodyLe.flexibleHeight = 1f;
        bodyLe.minHeight = 420f;
        var bodyHlg = bodyRowGo.GetComponent<HorizontalLayoutGroup>() ?? bodyRowGo.AddComponent<HorizontalLayoutGroup>();
        bodyHlg.spacing = 12f;
        bodyHlg.childAlignment = TextAnchor.UpperCenter;
        bodyHlg.childControlWidth = true;
        bodyHlg.childControlHeight = true;
        bodyHlg.childForceExpandWidth = true;
        bodyHlg.childForceExpandHeight = true;

        EnsureChild(bodyRowGo.transform, "LeftPanel", out var leftGo);
        var leftLe = leftGo.GetComponent<LayoutElement>() ?? leftGo.AddComponent<LayoutElement>();
        leftLe.flexibleWidth = 0.42f;
        leftLe.minWidth = 260f;
        StretchFull(leftGo.GetComponent<RectTransform>());

        EnsureChild(leftGo.transform, "Portrait", out var portraitGo);
        StretchFull(portraitGo.GetComponent<RectTransform>());
        var portraitImg = portraitGo.GetComponent<Image>() ?? AddImage(portraitGo, new Color(0.18f, 0.16f, 0.24f, 1f));
        portraitImg.preserveAspect = true;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/Prefabs/CloseupTexture/closeup_gc.png");
        if (sprite != null)
        {
            portraitImg.sprite = sprite;
        }

        EnsureChild(leftGo.transform, "HotspotRoot", out var hotspotRootGo);
        StretchFull(hotspotRootGo.GetComponent<RectTransform>());
        var hotspotViews = new BodyPartHotspotView[4];
        var hotspotLayout = new (cfg.demo.EBodyPart part, Vector2 min, Vector2 max)[]
        {
            (cfg.demo.EBodyPart.Mouth, new Vector2(0.35f, 0.72f), new Vector2(0.65f, 0.88f)),
            (cfg.demo.EBodyPart.Breast, new Vector2(0.28f, 0.52f), new Vector2(0.72f, 0.68f)),
            (cfg.demo.EBodyPart.Womb, new Vector2(0.32f, 0.34f), new Vector2(0.68f, 0.5f)),
            (cfg.demo.EBodyPart.Tail, new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.24f)),
        };
        for (int i = 0; i < hotspotLayout.Length; i++)
        {
            var layout = hotspotLayout[i];
            EnsureChild(hotspotRootGo.transform, "Hotspot_" + layout.part, out var hotspotGo);
            SetRectAnchor(hotspotGo, layout.min, layout.max, Vector2.zero, Vector2.zero);
            hotspotViews[i] = BuildHotspot(hotspotGo, layout.part);
        }

        EnsureChild(bodyRowGo.transform, "RightPanel", out var rightGo);
        var rightLe = rightGo.GetComponent<LayoutElement>() ?? rightGo.AddComponent<LayoutElement>();
        rightLe.flexibleWidth = 0.58f;
        var rightVlg = rightGo.GetComponent<VerticalLayoutGroup>() ?? rightGo.AddComponent<VerticalLayoutGroup>();
        rightVlg.spacing = 8f;
        rightVlg.childControlHeight = true;
        rightVlg.childForceExpandHeight = false;
        rightVlg.childControlWidth = true;
        rightVlg.childForceExpandWidth = true;

        EnsureChild(rightGo.transform, "DetailHeader", out var headerGo);
        var headerVlg = headerGo.GetComponent<VerticalLayoutGroup>() ?? headerGo.AddComponent<VerticalLayoutGroup>();
        headerVlg.spacing = 4f;
        var detailTitle = CreateLabel(headerGo.transform, "Title", 20);
        var detailLevel = CreateLabel(headerGo.transform, "Level", 15);
        var detailGearPoint = CreateLabel(headerGo.transform, "GearPoint", 15);

        var localStatsContent = CreateScrollSection(rightGo.transform, "LocalStats", "部位属性", 110f, out _);
        var equippedContent = CreateScrollSection(rightGo.transform, "Equipped", "已装备", 130f, out _);
        var candidateContent = CreateScrollSection(rightGo.transform, "Candidates", "可装备", 160f, out _);

        EnsureChild(localStatsContent, "InfoRow_Template", out var infoTemplateGo);
        infoTemplateGo.SetActive(false);
        BuildInfoRow(infoTemplateGo);

        EnsureChild(candidateContent, "ActionRow_Template", out var actionTemplateGo);
        actionTemplateGo.SetActive(false);
        BuildActionRow(actionTemplateGo);

        var panel = root.GetComponent<PlayerGearEquipPanel>();
        if (panel != null)
        {
            var so = new SerializedObject(panel);
            so.FindProperty("portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("hotspots").arraySize = hotspotViews.Length;
            for (int i = 0; i < hotspotViews.Length; i++)
            {
                so.FindProperty("hotspots").GetArrayElementAtIndex(i).objectReferenceValue = hotspotViews[i];
            }

            so.FindProperty("detailTitle").objectReferenceValue = detailTitle;
            so.FindProperty("detailLevel").objectReferenceValue = detailLevel;
            so.FindProperty("detailGearPoint").objectReferenceValue = detailGearPoint;
            so.FindProperty("localStatsContent").objectReferenceValue = localStatsContent;
            so.FindProperty("equippedContent").objectReferenceValue = equippedContent;
            so.FindProperty("candidateContent").objectReferenceValue = candidateContent;
            so.FindProperty("infoRowTemplate").objectReferenceValue = infoTemplateGo.GetComponent<GearEquipRowView>();
            so.FindProperty("actionRowTemplate").objectReferenceValue = actionTemplateGo.GetComponent<GearEquipRowView>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, GearPanelPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static BodyPartHotspotView BuildHotspot(GameObject go, cfg.demo.EBodyPart part)
    {
        var highlight = go.GetComponent<Image>() ?? AddImage(go, new Color(0.2f, 0.18f, 0.28f, 0.55f));
        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        btn.targetGraphic = highlight;
        var colors = btn.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.75f);
        btn.colors = colors;

        EnsureChild(go.transform, "Label", out var labelGo);
        StretchFull(labelGo.GetComponent<RectTransform>());
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(labelGo, part.ToString(), 16);
        labelTmp.alignment = TextAlignmentOptions.Center;

        var view = go.GetComponent<BodyPartHotspotView>() ?? go.AddComponent<BodyPartHotspotView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("PartId").enumValueIndex = (int)part;
        vso.FindProperty("ClickButton").objectReferenceValue = btn;
        vso.FindProperty("HighlightImage").objectReferenceValue = highlight;
        vso.FindProperty("LabelText").objectReferenceValue = labelTmp;
        vso.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    static void BuildInfoRow(GameObject rowGo)
    {
        var le = rowGo.GetComponent<LayoutElement>() ?? rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 24f;
        EnsureChild(rowGo.transform, "Title", out var titleGo);
        StretchFull(titleGo.GetComponent<RectTransform>());
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(titleGo, string.Empty, 13);
        titleTmp.color = new Color(0.85f, 0.82f, 0.92f, 1f);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var view = rowGo.GetComponent<GearEquipRowView>() ?? rowGo.AddComponent<GearEquipRowView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("TitleText").objectReferenceValue = titleTmp;
        vso.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildActionRow(GameObject rowGo)
    {
        var le = rowGo.GetComponent<LayoutElement>() ?? rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 32f;
        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>() ?? rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = false;

        EnsureChild(rowGo.transform, "Title", out var titleGo);
        var titleLe = titleGo.GetComponent<LayoutElement>() ?? titleGo.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(titleGo, string.Empty, 13);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        EnsureChild(rowGo.transform, "Hint", out var hintGo);
        var hintLe = hintGo.GetComponent<LayoutElement>() ?? hintGo.AddComponent<LayoutElement>();
        hintLe.preferredWidth = 90f;
        var hintTmp = hintGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(hintGo, string.Empty, 11);
        hintTmp.alignment = TextAlignmentOptions.MidlineRight;
        hintTmp.color = new Color(0.72f, 0.68f, 0.78f, 1f);

        EnsureChild(rowGo.transform, "ActionBtn", out var btnGo);
        var btnLe = btnGo.GetComponent<LayoutElement>() ?? btnGo.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 64f;
        btnLe.preferredHeight = 26f;
        AddImage(btnGo, new Color(0.28f, 0.45f, 0.62f, 1f));
        var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.75f);
        btn.colors = colors;

        EnsureChild(btnGo.transform, "Text", out var btnTextGo);
        StretchFull(btnTextGo.GetComponent<RectTransform>());
        var btnTmp = btnTextGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(btnTextGo, "装备", 13);
        btnTmp.alignment = TextAlignmentOptions.Center;

        var view = rowGo.GetComponent<GearEquipRowView>() ?? rowGo.AddComponent<GearEquipRowView>();
        var vso = new SerializedObject(view);
        vso.FindProperty("TitleText").objectReferenceValue = titleTmp;
        vso.FindProperty("HintText").objectReferenceValue = hintTmp;
        vso.FindProperty("ActionButton").objectReferenceValue = btn;
        vso.FindProperty("ActionButtonText").objectReferenceValue = btnTmp;
        vso.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupTalentNodeView()
    {
        var root = PrefabUtility.LoadPrefabContents(TalentNodePath);
        if (root == null)
        {
            return;
        }

        var unlockButton = root.GetComponentInChildren<Button>(true);
        Transform btnRoot = unlockButton != null ? unlockButton.transform : root.transform;
        EnsureChild(btnRoot, "BtnLabel", out var btnLabelGo);
        SetRectAnchor(btnLabelGo, new Vector2(0f, 0f), new Vector2(1f, 0.35f), Vector2.zero, Vector2.zero);
        var btnLabelTmp = btnLabelGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(btnLabelGo, "解锁", 11);
        btnLabelTmp.alignment = TextAlignmentOptions.Center;

        var view = root.GetComponent<TalentTreeNodeView>();
        if (view != null)
        {
            var level = root.transform.Find("Root/Level")?.GetComponent<TextMeshProUGUI>();
            var so = new SerializedObject(view);
            so.FindProperty("levelText").objectReferenceValue = level;
            so.FindProperty("unlockButtonText").objectReferenceValue = btnLabelTmp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, TalentNodePath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static Transform CreateScrollSection(Transform parent, string name, string title, float height, out Transform content)
    {
        EnsureChild(parent, name, out var sectionGo);
        var sectionLe = sectionGo.GetComponent<LayoutElement>() ?? sectionGo.AddComponent<LayoutElement>();
        sectionLe.preferredHeight = height + 28f;
        var sectionVlg = sectionGo.GetComponent<VerticalLayoutGroup>() ?? sectionGo.AddComponent<VerticalLayoutGroup>();
        sectionVlg.spacing = 4f;
        sectionVlg.childControlHeight = true;
        sectionVlg.childForceExpandHeight = false;

        EnsureChild(sectionGo.transform, "SectionTitle", out var titleGo);
        var titleLe = titleGo.GetComponent<LayoutElement>() ?? titleGo.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 22f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>() ?? AddTmp(titleGo, title, 15);
        titleTmp.text = title;

        EnsureChild(sectionGo.transform, "Scroll", out var scrollGo);
        var scrollLe = scrollGo.GetComponent<LayoutElement>() ?? scrollGo.AddComponent<LayoutElement>();
        scrollLe.preferredHeight = height;
        AddImage(scrollGo, new Color(0.1f, 0.09f, 0.14f, 0.6f));
        var scroll = scrollGo.GetComponent<ScrollRect>() ?? scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;

        EnsureChild(scrollGo.transform, "Viewport", out var viewportGo);
        AddImage(viewportGo, Color.white);
        if (viewportGo.GetComponent<Mask>() == null)
        {
            viewportGo.AddComponent<Mask>();
        }
        StretchFull(viewportGo.GetComponent<RectTransform>());

        EnsureChild(viewportGo.transform, "Content", out var contentGo);
        var contentVlg = contentGo.GetComponent<VerticalLayoutGroup>() ?? contentGo.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 4f;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.padding = new RectOffset(4, 4, 4, 4);
        scroll.viewport = viewportGo.GetComponent<RectTransform>();
        scroll.content = contentGo.GetComponent<RectTransform>();
        content = contentGo.transform;
        return sectionGo.transform;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize)
    {
        EnsureChild(parent, name, out var go);
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10f;
        return go.GetComponent<TextMeshProUGUI>() ?? AddTmp(go, string.Empty, (int)fontSize);
    }

    static void EnsureChild(Transform parent, string name, out GameObject go)
    {
        var tr = parent.Find(name);
        if (tr != null)
        {
            go = tr.gameObject;
            return;
        }

        go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
    }

    static Image AddImage(GameObject go, Color color)
    {
        if (go.GetComponent<CanvasRenderer>() == null)
        {
            go.AddComponent<CanvasRenderer>();
        }
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI AddTmp(GameObject go, string text, int fontSize)
    {
        if (go.GetComponent<CanvasRenderer>() == null)
        {
            go.AddComponent<CanvasRenderer>();
        }
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (HubFont != null)
        {
            tmp.font = HubFont;
        }

        return tmp;
    }
}
#endif
