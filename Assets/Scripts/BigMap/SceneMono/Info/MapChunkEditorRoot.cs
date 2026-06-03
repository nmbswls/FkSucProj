using My;
using My.MapExport;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor 场景根配置：挂于 Variant 场景 AreaRoot（如 Main_Area_01），与 WorldAreaRoot 同级
public class MapChunkEditorRoot : MonoBehaviour
{
    public string SceneName;
    public Texture2D SourceTexture;
    public float ChunkWorldSize = 32f;
    public float TexturePPU = 32f;
    public Vector2 ChunkOrigin;

    public Transform StaticPrefabRoot;

    [Header("GridRoot 3D Collision Export")]
    [Tooltip("关闭时仅导出 GridRoot 结构（Tilemap 等），不烘焙 3D BoxCollider。合并与性能完善前建议保持关闭。")]
    public bool ExportGridRoot3DCollision = false;
    public float GridRootCollisionThickness = 0.3f;
    public string GridRootCollisionLayer = "Wall";

    public int SlicePixelSize => MapChunkUtility.ComputeSlicePixelSize(ChunkWorldSize, TexturePPU);

    public Vector2Int SourceTextureSize
    {
        get
        {
            if (SourceTexture == null)
            {
                return Vector2Int.zero;
            }

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(SourceTexture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                return new Vector2Int(w, h);
            }
#endif
            return new Vector2Int(SourceTexture.width, SourceTexture.height);
        }
    }

    public Vector2Int ImportedTextureSize
    {
        get
        {
            if (SourceTexture == null)
            {
                return Vector2Int.zero;
            }

            return new Vector2Int(SourceTexture.width, SourceTexture.height);
        }
    }

    void Reset()
    {
        ChunkWorldSize = GameConsts.ChunkCellSize;
        TexturePPU = 32f;
        ChunkOrigin = Vector2.zero;
        SceneName = gameObject.scene.name;

        var staticRoot = transform.Find("StaticRoot");
        if (staticRoot != null)
        {
            StaticPrefabRoot = staticRoot;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (ChunkWorldSize <= 0f)
        {
            return;
        }

        var size = SourceTextureSize;
        if (size.x <= 0 || size.y <= 0)
        {
            size = new Vector2Int(256, 256);
        }

        int slicePx = SlicePixelSize;
        int cols = Mathf.Max(1, Mathf.CeilToInt(size.x / (float)slicePx));
        int rows = Mathf.Max(1, Mathf.CeilToInt(size.y / (float)slicePx));

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
        for (int cy = 0; cy < rows; cy++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                var min = MapChunkUtility.ChunkWorldMin(new My.Map.Logic.ChunkCoord(cx, cy), ChunkOrigin, ChunkWorldSize);
                var center = min + new Vector3(ChunkWorldSize * 0.5f, ChunkWorldSize * 0.5f, 0f);
                Gizmos.DrawWireCube(center, new Vector3(ChunkWorldSize, ChunkWorldSize, 0.05f));
            }
        }
    }
#endif
}
