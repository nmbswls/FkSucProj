#if UNITY_EDITOR

using My.Farm;
using UnityEditor;
using UnityEngine;

// 生成静态农田 prefab：脚本与格子预置在 prefab 上，供 MapScenePrefabProvider 分块加载
public static class FarmPlotPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/Prefab/Map/FarmPlot/home01_field_a.prefab";
    public const string ResourceKey = "Map/FarmPlot/home01_field_a";

    // 与 demo_tbfarmplot.home01_field_a.cells 对齐
    static readonly Vector2Int[] Cells =
    {
        new(0, 0), new(1, 0), new(2, 0),
        new(0, 1), new(1, 1), new(2, 1),
        new(0, 2), new(1, 2), new(2, 2),
        new(0, 4), new(1, 4), new(2, 4),
        new(0, 5), new(1, 5), new(2, 5),
    };

    const float CellSize = 1f;

    [MenuItem("Tools/Maps/Home 01/Build FarmPlot Prefab")]
    public static void Build()
    {
        EnsureFolder("Assets/Resources/Prefab/Map/FarmPlot");

        var root = new GameObject("home01_field_a");
        var provider = root.AddComponent<FarmPlotAreaProvider>();

        var visual = new GameObject("VisualRoot");
        visual.transform.SetParent(root.transform, false);

        // 底盘示意（可选）
        var ground = new GameObject("GroundHint");
        ground.transform.SetParent(visual.transform, false);
        ground.transform.localPosition = new Vector3(1.5f, 2.5f, 0f);
        var groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = WhiteSprite();
        groundSr.color = new Color(0.42f, 0.32f, 0.18f, 0.35f);
        groundSr.sortingOrder = 5;
        ground.transform.localScale = new Vector3(3.2f, 6.2f, 1f);

        var views = new FarmCropCellView[Cells.Length];
        for (int i = 0; i < Cells.Length; i++)
        {
            var c = Cells[i];
            var cellGo = new GameObject($"FarmCell_{c.x}_{c.y}");
            cellGo.transform.SetParent(visual.transform, false);
            cellGo.transform.localPosition = new Vector3((c.x + 0.5f) * CellSize, (c.y + 0.5f) * CellSize, 0f);
            cellGo.layer = LayerMask.NameToLayer("MapTarget");

            var sr = cellGo.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.color = new Color(0.45f, 0.35f, 0.2f, 0.55f);
            sr.sortingOrder = 20;
            cellGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

            var col = cellGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one * 0.9f;

            views[i] = cellGo.AddComponent<FarmCropCellView>();
        }

        var so = new SerializedObject(provider);
        so.FindProperty("plotId").stringValue = "home01_field_a";
        so.FindProperty("logicAreaIdOverride").stringValue = "homestead_01";
        so.FindProperty("visualRoot").objectReferenceValue = visual.transform;
        var arr = so.FindProperty("cellViews");
        arr.arraySize = views.Length;
        for (int i = 0; i < views.Length; i++)
        {
            arr.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built " + PrefabPath + " key=" + ResourceKey);
    }

    static Sprite _white;

    static Sprite WhiteSprite()
    {
        if (_white != null)
        {
            return _white;
        }

        var tex = Texture2D.whiteTexture;
        _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        return _white;
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
