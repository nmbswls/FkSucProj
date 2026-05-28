using My;
using My.MapExport;
using UnityEngine;

// Editor 场景根配置：chunk 参数与静态物根节点（Tilemap 在导出时从场景解析）
public class MapChunkEditorRoot : MonoBehaviour
{
    public Texture2D SourceTexture;
    public float ChunkWorldSize = 32f;
    public float TexturePPU = 32f;
    public Vector2 ChunkOrigin;

    public Transform StaticPrefabRoot;

    public int SlicePixelSize => MapChunkUtility.ComputeSlicePixelSize(ChunkWorldSize, TexturePPU);

    public Vector2Int SourceTextureSize
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
