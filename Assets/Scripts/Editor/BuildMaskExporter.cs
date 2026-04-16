using My;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine;

public static class BuildMaskEditorMenu
{
    // 快速将场景中选中的 Tilemap 作为 ground 导出
    [MenuItem("Tools/Build Mask/Export From Selected Ground")]
    public static void ExportFromSelectedGround()
    {

        var ground = Selection.activeObject as GameObject;
        var comp = ground.GetComponent<Tilemap>();
        if (ground == null)
        {
            Debug.LogWarning("请选择一个 Ground Tilemap 后再执行。");
            return;
        }
        string path = EditorUtility.SaveFilePanelInProject(
            "Save BuildMask Asset",
            "BuildMask",
            "asset",
            "Choose location to save the BuildMask asset");
        if (string.IsNullOrEmpty(path)) return;

        var asset = BuildMaskExporter.ExportToAsset(path, comp);
        EditorGUIUtility.PingObject(asset);
        Debug.Log("BuildMask exported: " + path);
    }
}

public static class BuildMaskExporter
{
    public static BuildMaskAsset ExportToAsset(
        string assetPath,
        Tilemap ground,
        Tilemap blocked = null,
        Tilemap water = null,
        Tilemap occupiedLayer = null)
    {
        // 计算并集边界，避免层大小不一致
        BoundsInt bounds = UnionBounds(
            ground?.cellBounds ?? new BoundsInt(),
            blocked?.cellBounds ?? new BoundsInt(),
            water?.cellBounds ?? new BoundsInt(),
            occupiedLayer?.cellBounds ?? new BoundsInt()
        );

        int W = bounds.size.x;
        int H = bounds.size.y;
        int bitLen = (W * H + 7) / 8;

        var buildBits = new byte[bitLen];
        var occBits = new byte[bitLen];

        for (int ly = 0; ly < H; ly++)
        {
            for (int lx = 0; lx < W; lx++)
            {
                var cell = new Vector3Int(bounds.xMin + lx, bounds.yMin + ly, 0);
                bool hasGround = ground && ground.GetTile(cell) != null;
                bool isBlocked = blocked && blocked.GetTile(cell) != null;
                bool isWater = water && water.GetTile(cell) != null;
                bool isOccTile = occupiedLayer && occupiedLayer.GetTile(cell) != null;

                bool buildable = hasGround && !isBlocked && !isWater && !isOccTile;
                SetBit(buildBits, ly * W + lx, buildable);
                SetBit(occBits, ly * W + lx, isOccTile);
            }
        }

        var asset = ScriptableObject.CreateInstance<BuildMaskAsset>();
        asset.width = W;
        asset.height = H;
        asset.originX = bounds.xMin;
        asset.originY = bounds.yMin;
        asset.buildableBits = buildBits;
        asset.occupancyBits = occBits;

        // 创建或覆盖资源
        var existing = AssetDatabase.LoadAssetAtPath<BuildMaskAsset>(assetPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        else
        {
            existing.width = asset.width;
            existing.height = asset.height;
            existing.originX = asset.originX;
            existing.originY = asset.originY;
            existing.buildableBits = asset.buildableBits;
            existing.occupancyBits = asset.occupancyBits;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            asset = existing;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return asset;
    }

    private static void SetBit(byte[] bits, int idx, bool v)
    {
        int bi = idx >> 3;
        int mask = 1 << (idx & 7);
        if (v) bits[bi] |= (byte)mask;
        else bits[bi] &= (byte)~mask;
    }

    private static BoundsInt UnionBounds(params BoundsInt[] list)
    {
        bool has = false;
        int minX = 0, minY = 0, maxX = 0, maxY = 0;
        foreach (var b in list)
        {
            if (b.size == Vector3Int.zero) continue;
            if (!has) { has = true; minX = b.xMin; minY = b.yMin; maxX = b.xMax; maxY = b.yMax; }
            else
            {
                if (b.xMin < minX) minX = b.xMin;
                if (b.yMin < minY) minY = b.yMin;
                if (b.xMax > maxX) maxX = b.xMax;
                if (b.yMax > maxY) maxY = b.yMax;
            }
        }
        if (!has) return new BoundsInt(0, 0, 0, 0, 0, 0);
        return new BoundsInt(minX, minY, 0, maxX - minX, maxY - minY, 1);
    }
}