using My.MapExport;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor 场景根配置：挂于 *_Editor 场景 AreaRoot，运行时由 WorldAreaRoot 承接
public class MapChunkEditorRoot : MonoBehaviour
{
    // 对应 Luban AreaVariantInfo.scene_name（运行时场景名，如 Main_Area_01）
    [FormerlySerializedAs("SceneName")]
    public string MapVariantSceneName;

    public Vector2 ChunkOrigin;

    // GridRoot 下参与行走/逻辑高度采样的 Tilemap 层名（不含 Hole）
    public string[] GroundLayerNames;

    [Header("Painted Background")]
    public Rect PaintWorldRect;
    public string LastPaintManifestKey;

    public float ChunkWorldSize
    {
        get
        {
#if UNITY_EDITOR
            return MapChunkEditorSettings.GetOrCreate().EffectiveChunkWorldSize;
#else
            return My.GameConsts.ChunkCellSize;
#endif
        }
    }

    public Transform MapVariantRoot => MapVariantSceneHierarchy.ResolveMapVariantRoot(transform);

    void Reset()
    {
        MapVariantSceneName = ResolveDefaultMapVariantSceneName();
    }

    string ResolveDefaultMapVariantSceneName()
    {
        var sceneName = gameObject.scene.name;
        if (sceneName.EndsWith("_Editor"))
        {
            return sceneName.Substring(0, sceneName.Length - "_Editor".Length);
        }

        return sceneName;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var chunkWorldSize = ChunkWorldSize;
        if (chunkWorldSize <= 0f)
        {
            return;
        }

        if (PaintWorldRect.width <= 0f || PaintWorldRect.height <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.85f);
        var paintRect = PaintWorldRect;
        int paintCols = Mathf.Max(1, Mathf.CeilToInt(paintRect.width / chunkWorldSize));
        int paintRows = Mathf.Max(1, Mathf.CeilToInt(paintRect.height / chunkWorldSize));
        var minCoord = MapChunkUtility.WorldToChunk(
            new Vector2(paintRect.xMin, paintRect.yMin),
            ChunkOrigin,
            chunkWorldSize);
        for (int cy = 0; cy < paintRows; cy++)
        {
            for (int cx = 0; cx < paintCols; cx++)
            {
                var coord = new My.Map.Logic.ChunkCoord(minCoord.X + cx, minCoord.Y + cy);
                var min = MapChunkUtility.ChunkWorldMin(coord, ChunkOrigin, chunkWorldSize);
                var center = min + new Vector3(chunkWorldSize * 0.5f, chunkWorldSize * 0.5f, 0f);
                Gizmos.DrawWireCube(center, new Vector3(chunkWorldSize, chunkWorldSize, 0.05f));
            }
        }

        Gizmos.color = new Color(0.9f, 0.5f, 1f, 0.35f);
        var rectCenter = new Vector3(paintRect.center.x, paintRect.center.y, 0f);
        Gizmos.DrawWireCube(rectCenter, new Vector3(paintRect.width, paintRect.height, 0.02f));
    }
#endif
}
