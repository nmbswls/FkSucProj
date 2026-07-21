using My.UI.Cooking;
using My;
using My.Config;
using My.Player;
using My.Player.Bag;
using cfg.demo;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class CookingPanelPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/UI/Prefabs/CookingPanel.prefab";
    const string PanelBackgroundPath = "Assets/Resources/UI/CookingPanelBackground.png";
    const string FontPath = "Assets/Fonts/MSYH Cooking SDF.asset";
    const string SourceFontPath = "Assets/Fonts/MSYH.TTC";

    static TMP_FontAsset _font;

    [MenuItem("Tools/UI/Rebuild Cooking Panel")]
    public static void Rebuild()
    {
        _font = GetOrCreateCookingFont();
        EnsureCookingGlyphs();
        var root = Build();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Cooking] CookingPanel prefab rebuilt.");
    }

    static void EnsureCookingGlyphs()
    {
        if (_font == null)
        {
            throw new System.InvalidOperationException("Cooking UI font asset could not be created.");
        }
        if (CfgMgr.Cfgs == null) CfgMgr.LoadGameConfigs();
        var characters = new StringBuilder("烹饪固定配方全部可制作未解锁材料不足制作档次原料稀有度主类型风格无所需材料制作批次预计产出尚未解锁背包空间配置错误库存发生变化已回退无法完成当前没有料理选择常见少见稀有珍贵有限主食主菜汤羹甜点饮品拼盘家常丰盛甜味清爽精致节庆异域怀旧普通产物优质料理优质概率已选原料");
        var recipes = CfgMgr.Cfgs.TbCookingRecipe.DataList;
        for (int i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];
            characters.Append(recipe.DisplayName).Append(recipe.Description).Append(recipe.UnlockHint);
            var result = ItemCatalog.GetItemDef(recipe.ResultItemId);
            characters.Append(result?.DisplayName);
            var qualityResult = ItemCatalog.GetItemDef(recipe.QualityResultItemId);
            characters.Append(qualityResult?.DisplayName);
            for (int j = 0; j < recipe.IngredientItemIds.Count; j++)
            {
                characters.Append(ItemCatalog.GetItemDef(recipe.IngredientItemIds[j])?.DisplayName);
            }
        }
        if (!_font.TryAddCharacters(characters.ToString(), out string missing) && !string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning("[Cooking] MSYH font is missing glyphs: " + missing);
        }
        EditorUtility.SetDirty(_font);
    }

    static TMP_FontAsset GetOrCreateCookingFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (existing != null)
        {
            return existing;
        }

        var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (source == null)
        {
            throw new System.InvalidOperationException("Missing source font at " + SourceFontPath);
        }

        var created = TMP_FontAsset.CreateFontAsset(
            source,
            64,
            6,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);
        created.name = "MSYH Cooking SDF";
        created.isMultiAtlasTexturesEnabled = true;
        var atlas = created.atlasTexture;
        var material = created.material;
        AssetDatabase.CreateAsset(created, FontPath);
        if (atlas != null)
        {
            atlas.name = "MSYH Cooking SDF Atlas";
            AssetDatabase.AddObjectToAsset(atlas, created);
        }
        if (material != null)
        {
            material.name = "MSYH Cooking SDF Material";
            AssetDatabase.AddObjectToAsset(material, created);
        }
        AssetDatabase.SaveAssets();
        return created;
    }

    public static void RenderPreview()
    {
        CfgMgr.LoadGameConfigs();
        RenderPreviewAt(1440, 900, ".codex-tmp/cooking_panel_preview_1440x900.png");
        RenderPreviewAt(1024, 768, ".codex-tmp/cooking_panel_preview_1024x768.png");
        Debug.Log("[Cooking] CookingPanel previews rendered.");
    }

    public static void RebuildAndRenderPreview()
    {
        Rebuild();
        RenderPreview();
    }

    static void RenderPreviewAt(int width, int height, string outputPath)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraGo = new GameObject("PreviewCamera", typeof(Camera));
        var camera = cameraGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.03f, 0.03f, 1f);
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);

        var canvasGo = new GameObject("PreviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.matchWidthOrHeight = width / (float)height > 1.45f ? 1f : 0f;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
        var panel = instance.GetComponent<CookingPanel>();
        var inventory = BuildPreviewInventory();
        panel.Setup(new CookingPanel.OpenArgs { Inventory = inventory });
        panel.Show();
        Canvas.ForceUpdateCanvases();

        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        File.WriteAllBytes(Path.Combine(projectRoot, outputPath), texture.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(renderTexture);
    }

    static PlayerInventorySystem BuildPreviewInventory()
    {
        var inventory = new PlayerInventorySystem();
        inventory.MainBag.InitBag(EPlayerBagId.Default, 60, 0, EBagStorageLayout.Grid);
        inventory.MindFacetBag.InitBag(EPlayerBagId.Mind, 30, 0, EBagStorageLayout.Compact);
        inventory.WarehouseBag.InitBag(EPlayerBagId.Storage, 100, 0, EBagStorageLayout.Compact);
        foreach (var itemId in new[] { "produce_wheat", "produce_cabbage", "produce_potato", "produce_berry", "produce_herb", "mat_monster_meat", "loot_spice_pinch" })
        {
            inventory.GiveItemToPlayer(itemId, 8);
        }
        inventory.GiveItemToPlayer("mind_facet_01", 2);
        inventory.GiveItemToPlayer("mind_facet_03", 1);
        return inventory;
    }

    static GameObject Build()
    {
        var root = Go("CookingPanel", null, typeof(CanvasGroup), typeof(CookingPanel));
        Stretch(root.GetComponent<RectTransform>());
        var panel = root.GetComponent<CookingPanel>();
        var canvasGroup = root.GetComponent<CanvasGroup>();

        var dim = Image("Dim", root.transform, new Color(0.018f, 0.022f, 0.022f, 0.82f));
        Stretch(dim.rectTransform);

        var frame = Image("Frame", root.transform, new Color(0.075f, 0.086f, 0.084f, 1f));
        SetRect(frame.rectTransform, new Vector2(0.055f, 0.06f), new Vector2(0.945f, 0.94f), Vector2.zero, Vector2.zero);
        var panelBackground = AssetDatabase.LoadAssetAtPath<Sprite>(PanelBackgroundPath);
        if (panelBackground != null)
        {
            frame.sprite = panelBackground;
            frame.color = Color.white;
            frame.preserveAspect = false;
        }

        var topLine = Image("TopAccent", frame.transform, new Color(0.64f, 0.43f, 0.22f, 1f));
        SetRect(topLine.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), Vector2.zero);

        var title = Text("Title", frame.transform, "烹饪", 28, FontStyles.Bold, new Color(0.94f, 0.91f, 0.84f));
        SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(28, -64), new Vector2(0, -16));
        title.alignment = TextAlignmentOptions.MidlineLeft;

        var subtitle = Text("Subtitle", frame.transform, "固定配方", 14, FontStyles.Normal, new Color(0.55f, 0.61f, 0.58f));
        SetRect(subtitle.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(132, -58), new Vector2(0, -22));
        subtitle.alignment = TextAlignmentOptions.MidlineLeft;

        var close = Button("Close", frame.transform, new Color(0.16f, 0.18f, 0.18f, 1f), out _);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-68, -58), new Vector2(-24, -14));
        var closeText = Text("Label", close.transform, "×", 27, FontStyles.Normal, new Color(0.78f, 0.80f, 0.77f));
        Stretch(closeText.rectTransform);
        closeText.alignment = TextAlignmentOptions.Center;

        var divider = Image("HeaderDivider", frame.transform, new Color(0.20f, 0.22f, 0.21f, 1f));
        SetRect(divider.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -73), new Vector2(-24, -72));

        var left = Go("RecipeSection", frame.transform);
        SetRect(left.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0.37f, 1), new Vector2(24, 24), new Vector2(-12, -88));

        var filterRow = Go("FilterRow", left.transform, typeof(HorizontalLayoutGroup));
        SetRect(filterRow.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44), Vector2.zero);
        var filterLayout = filterRow.GetComponent<HorizontalLayoutGroup>();
        filterLayout.spacing = 8;
        filterLayout.childControlWidth = true;
        filterLayout.childForceExpandWidth = true;
        filterLayout.childControlHeight = true;

        var allFilter = Button("AllFilter", filterRow.transform, new Color(0.35f, 0.50f, 0.47f, 1f), out var allFilterBg);
        allFilter.AddComponent<LayoutElement>().preferredHeight = 38;
        var allText = Text("Label", allFilter.transform, "全部", 15, FontStyles.Bold, Color.white);
        Stretch(allText.rectTransform);
        allText.alignment = TextAlignmentOptions.Center;

        var craftableFilter = Button("CraftableFilter", filterRow.transform, new Color(0.16f, 0.18f, 0.19f, 1f), out var craftFilterBg);
        craftableFilter.AddComponent<LayoutElement>().preferredHeight = 38;
        var craftFilterText = Text("Label", craftableFilter.transform, "可制作", 15, FontStyles.Bold, Color.white);
        Stretch(craftFilterText.rectTransform);
        craftFilterText.alignment = TextAlignmentOptions.Center;

        var scroll = BuildScroll("RecipeScroll", left.transform, out var recipeContent);
        SetRect(scroll.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -54));
        var recipeLayout = recipeContent.gameObject.AddComponent<VerticalLayoutGroup>();
        recipeLayout.spacing = 7;
        recipeLayout.padding = new RectOffset(0, 5, 2, 2);
        recipeLayout.childControlHeight = true;
        recipeLayout.childForceExpandHeight = false;
        recipeLayout.childControlWidth = true;
        recipeLayout.childForceExpandWidth = true;
        var recipeFitter = recipeContent.gameObject.AddComponent<ContentSizeFitter>();
        recipeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var recipeTemplate = BuildRecipeTemplate(recipeContent);
        var emptyList = Text("EmptyList", left.transform, "当前没有可制作的料理", 15, FontStyles.Normal, new Color(0.52f, 0.56f, 0.54f));
        SetRect(emptyList.rectTransform, new Vector2(0, 0.38f), new Vector2(1, 0.62f), Vector2.zero, Vector2.zero);
        emptyList.alignment = TextAlignmentOptions.Center;
        emptyList.gameObject.SetActive(false);

        var verticalDivider = Image("VerticalDivider", frame.transform, new Color(0.20f, 0.22f, 0.21f, 1f));
        SetRect(verticalDivider.rectTransform, new Vector2(0.37f, 0), new Vector2(0.37f, 1), new Vector2(-1, 24), new Vector2(0, -88));

        var right = Go("DetailSection", frame.transform);
        SetRect(right.GetComponent<RectTransform>(), new Vector2(0.37f, 0), new Vector2(1, 1), new Vector2(24, 24), new Vector2(-28, -88));

        var dishIconBg = Image("DishIconBackground", right.transform, new Color(0.12f, 0.14f, 0.13f, 1f));
        SetRect(dishIconBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -148), new Vector2(132, -16));
        var dishIcon = Image("DishIcon", dishIconBg.transform, Color.white);
        SetRect(dishIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -14));
        dishIcon.preserveAspect = true;
        var dishIconFallback = Text("Fallback", dishIconBg.transform, "汤", 32, FontStyles.Bold, new Color(0.58f, 0.66f, 0.62f));
        Stretch(dishIconFallback.rectTransform);
        dishIconFallback.alignment = TextAlignmentOptions.Center;

        var dishName = Text("DishName", right.transform, "未选择料理", 25, FontStyles.Bold, new Color(0.94f, 0.91f, 0.84f));
        SetRect(dishName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(154, -54), new Vector2(0, -16));
        dishName.alignment = TextAlignmentOptions.MidlineLeft;

        var description = Text("Description", right.transform, string.Empty, 14, FontStyles.Normal, new Color(0.70f, 0.73f, 0.68f));
        SetRect(description.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(154, -126), new Vector2(0, -58));
        description.alignment = TextAlignmentOptions.TopLeft;

        var metaBg = Image("MetaBand", right.transform, new Color(0.095f, 0.11f, 0.105f, 1f));
        SetRect(metaBg.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(154, -150), new Vector2(0, -128));
        var levelText = MetaText("Level", metaBg.transform, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(10, 0), Vector2.zero);
        var rarityText = MetaText("Rarity", metaBg.transform, new Vector2(0.5f, 0), Vector2.one, new Vector2(10, 0), Vector2.zero);

        var taxonomyBg = Image("TaxonomyBand", right.transform, new Color(0.095f, 0.11f, 0.105f, 1f));
        SetRect(taxonomyBg.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(154, -184), new Vector2(0, -156));
        var primaryText = MetaText("PrimaryType", taxonomyBg.transform, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(10, 0), Vector2.zero);
        var styleText = MetaText("Style", taxonomyBg.transform, new Vector2(0.5f, 0), Vector2.one, new Vector2(10, 0), Vector2.zero);

        var ingredientTitle = Text("IngredientTitle", right.transform, "所需材料", 17, FontStyles.Bold, new Color(0.87f, 0.84f, 0.77f));
        SetRect(ingredientTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -224), new Vector2(0, -194));
        ingredientTitle.alignment = TextAlignmentOptions.MidlineLeft;

        var ingredientsScroll = BuildScroll("IngredientScroll", right.transform, out var ingredientContent);
        SetRect(ingredientsScroll.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 132), new Vector2(0, -232));
        var ingredientLayout = ingredientContent.gameObject.AddComponent<VerticalLayoutGroup>();
        ingredientLayout.spacing = 5;
        ingredientLayout.childControlHeight = true;
        ingredientLayout.childForceExpandHeight = false;
        ingredientLayout.childControlWidth = true;
        ingredientLayout.childForceExpandWidth = true;
        ingredientContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var ingredientTemplate = BuildIngredientTemplate(ingredientContent);

        var footerDivider = Image("FooterDivider", right.transform, new Color(0.20f, 0.22f, 0.21f, 1f));
        SetRect(footerDivider.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 119), new Vector2(0, 120));

        var batchLabel = Text("BatchLabel", right.transform, "制作批次", 14, FontStyles.Normal, new Color(0.62f, 0.66f, 0.63f));
        SetRect(batchLabel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 72), new Vector2(90, 110));
        batchLabel.alignment = TextAlignmentOptions.MidlineLeft;

        var minus = Button("DecreaseBatch", right.transform, new Color(0.16f, 0.18f, 0.18f, 1f), out _);
        SetRect(minus.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(94, 76), new Vector2(132, 112));
        var minusText = Text("Label", minus.transform, "−", 22, FontStyles.Bold, Color.white);
        Stretch(minusText.rectTransform);
        minusText.alignment = TextAlignmentOptions.Center;

        var batchText = Text("BatchValue", right.transform, "1", 18, FontStyles.Bold, new Color(0.93f, 0.89f, 0.81f));
        SetRect(batchText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(136, 76), new Vector2(188, 112));
        batchText.alignment = TextAlignmentOptions.Center;

        var plus = Button("IncreaseBatch", right.transform, new Color(0.16f, 0.18f, 0.18f, 1f), out _);
        SetRect(plus.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(192, 76), new Vector2(230, 112));
        var plusText = Text("Label", plus.transform, "+", 20, FontStyles.Bold, Color.white);
        Stretch(plusText.rectTransform);
        plusText.alignment = TextAlignmentOptions.Center;

        var outputText = Text("Output", right.transform, string.Empty, 14, FontStyles.Normal, new Color(0.64f, 0.70f, 0.65f));
        SetRect(outputText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(246, 76), new Vector2(-202, 112));
        outputText.alignment = TextAlignmentOptions.MidlineRight;

        var craft = Button("Craft", right.transform, new Color(0.36f, 0.52f, 0.43f, 1f), out _);
        SetRect(craft.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-184, 72), new Vector2(0, 114));
        var craftText = Text("Label", craft.transform, "制作", 18, FontStyles.Bold, Color.white);
        Stretch(craftText.rectTransform);
        craftText.alignment = TextAlignmentOptions.Center;

        var status = Text("Status", right.transform, string.Empty, 13, FontStyles.Normal, new Color(0.83f, 0.63f, 0.38f));
        SetRect(status.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 24), new Vector2(0, 64));
        status.alignment = TextAlignmentOptions.MidlineRight;

        var so = new SerializedObject(panel);
        Set(so, "panelId", Pid());
        Set(so, "canvasGroup", canvasGroup);
        Set(so, "closeButton", close.GetComponent<Button>());
        Set(so, "allFilterButton", allFilter.GetComponent<Button>());
        Set(so, "craftableFilterButton", craftableFilter.GetComponent<Button>());
        Set(so, "allFilterBackground", allFilterBg);
        Set(so, "craftableFilterBackground", craftFilterBg);
        Set(so, "recipeListRoot", recipeContent);
        Set(so, "recipeTemplate", recipeTemplate);
        Set(so, "emptyListText", emptyList);
        Set(so, "dishIcon", dishIcon);
        Set(so, "dishIconFallbackText", dishIconFallback);
        Set(so, "dishNameText", dishName);
        Set(so, "descriptionText", description);
        Set(so, "levelText", levelText);
        Set(so, "rarityText", rarityText);
        Set(so, "primaryTypeText", primaryText);
        Set(so, "styleText", styleText);
        Set(so, "ingredientListRoot", ingredientContent);
        Set(so, "ingredientTemplate", ingredientTemplate);
        Set(so, "decreaseBatchButton", minus.GetComponent<Button>());
        Set(so, "increaseBatchButton", plus.GetComponent<Button>());
        Set(so, "batchText", batchText);
        Set(so, "outputText", outputText);
        Set(so, "craftButton", craft.GetComponent<Button>());
        Set(so, "craftButtonText", craftText);
        Set(so, "statusText", status);
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static CookingRecipeListItem BuildRecipeTemplate(RectTransform parent)
    {
        var go = Go("RecipeTemplate", parent, typeof(Image), typeof(Button), typeof(LayoutElement), typeof(CookingRecipeListItem));
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.11f, 0.13f, 0.14f, 0.96f);
        go.GetComponent<Button>().targetGraphic = bg;
        go.GetComponent<LayoutElement>().preferredHeight = 82;
        var stripe = Image("Selection", go.transform, new Color(0.73f, 0.48f, 0.24f, 1f));
        SetRect(stripe.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(4, 0));
        var iconBg = Image("IconBackground", go.transform, new Color(0.08f, 0.09f, 0.09f, 1f));
        SetRect(iconBg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -30), new Vector2(72, 30));
        var icon = Image("Icon", iconBg.transform, Color.white);
        SetRect(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -7));
        icon.preserveAspect = true;
        var iconFallback = Text("Fallback", iconBg.transform, "汤", 20, FontStyles.Bold, new Color(0.56f, 0.64f, 0.60f));
        Stretch(iconFallback.rectTransform);
        iconFallback.alignment = TextAlignmentOptions.Center;
        var name = Text("Name", go.transform, "料理", 16, FontStyles.Bold, new Color(0.91f, 0.88f, 0.81f));
        SetRect(name.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(84, 0), new Vector2(-72, -8));
        name.alignment = TextAlignmentOptions.MidlineLeft;
        var meta = Text("Meta", go.transform, "Lv.1", 12, FontStyles.Normal, new Color(0.52f, 0.57f, 0.54f));
        SetRect(meta.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(84, 8), new Vector2(-72, 0));
        meta.alignment = TextAlignmentOptions.MidlineLeft;
        var state = Text("State", go.transform, "可制作", 12, FontStyles.Bold, Color.white);
        SetRect(state.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-72, 0), new Vector2(-10, 0));
        state.alignment = TextAlignmentOptions.Center;

        var so = new SerializedObject(go.GetComponent<CookingRecipeListItem>());
        Set(so, "button", go.GetComponent<Button>());
        Set(so, "background", bg);
        Set(so, "selectionBar", stripe);
        Set(so, "icon", icon);
        Set(so, "iconFallbackText", iconFallback);
        Set(so, "nameText", name);
        Set(so, "metaText", meta);
        Set(so, "stateText", state);
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);
        return go.GetComponent<CookingRecipeListItem>();
    }

    static CookingIngredientRow BuildIngredientTemplate(RectTransform parent)
    {
        var go = Go("IngredientTemplate", parent, typeof(Image), typeof(LayoutElement), typeof(CookingIngredientRow));
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.095f, 0.11f, 0.105f, 0.9f);
        go.GetComponent<LayoutElement>().preferredHeight = 52;
        var marker = Image("Shortage", go.transform, new Color(0.76f, 0.29f, 0.23f, 1f));
        SetRect(marker.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
        var icon = Image("Icon", go.transform, Color.white);
        SetRect(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -19), new Vector2(50, 19));
        icon.preserveAspect = true;
        var iconFallback = Text("Fallback", go.transform, "材", 16, FontStyles.Bold, new Color(0.54f, 0.62f, 0.58f));
        SetRect(iconFallback.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -19), new Vector2(50, 19));
        iconFallback.alignment = TextAlignmentOptions.Center;
        var name = Text("Name", go.transform, "材料", 14, FontStyles.Normal, new Color(0.85f, 0.83f, 0.77f));
        SetRect(name.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(62, 0), new Vector2(-126, 0));
        name.alignment = TextAlignmentOptions.MidlineLeft;
        var count = Text("Count", go.transform, "0 / 1", 14, FontStyles.Bold, Color.white);
        SetRect(count.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-120, 0), new Vector2(-14, 0));
        count.alignment = TextAlignmentOptions.MidlineRight;
        var so = new SerializedObject(go.GetComponent<CookingIngredientRow>());
        Set(so, "icon", icon);
        Set(so, "iconFallbackText", iconFallback);
        Set(so, "nameText", name);
        Set(so, "countText", count);
        Set(so, "shortageMarker", marker);
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);
        return go.GetComponent<CookingIngredientRow>();
    }

    static GameObject BuildScroll(string name, Transform parent, out RectTransform content)
    {
        var root = Go(name, parent, typeof(ScrollRect));
        var viewport = Go("Viewport", root.transform, typeof(Image), typeof(RectMask2D));
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
        var contentGo = Go("Content", viewport.transform);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        var scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24;
        return root;
    }

    static TextMeshProUGUI MetaText(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var text = Text(name, parent, string.Empty, 12, FontStyles.Normal, new Color(0.66f, 0.70f, 0.67f));
        SetRect(text.rectTransform, min, max, offsetMin, offsetMax);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    static GameObject Button(string name, Transform parent, Color color, out Image image)
    {
        var go = Go(name, parent, typeof(Image), typeof(Button));
        image = go.GetComponent<Image>();
        image.color = color;
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;
        return go;
    }

    static Image Image(string name, Transform parent, Color color)
    {
        var go = Go(name, parent, typeof(Image));
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string value, float size, FontStyles style, Color color)
    {
        var go = Go(name, parent, typeof(TextMeshProUGUI));
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = _font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    static GameObject Go(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        for (int i = 0; i < components.Length; i++) go.AddComponent(components[i]);
        return go;
    }

    static void Stretch(RectTransform rect)
    {
        SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static void Set(SerializedObject so, string property, Object value)
    {
        so.FindProperty(property).objectReferenceValue = value;
    }

    static void Set(SerializedObject so, string property, string value)
    {
        so.FindProperty(property).stringValue = value;
    }

    static string Pid() => CookingPanel.Pid;
}
