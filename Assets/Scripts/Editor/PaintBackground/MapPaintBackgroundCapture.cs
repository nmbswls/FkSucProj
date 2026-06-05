#if UNITY_EDITOR
using My.Map.Logic;
using My.MapExport;
using UnityEngine;
using UnityEngine.Tilemaps;

// 正交 Camera 逐 chunk 拍摄场景，作为 AI 参考模板（含 Tilemap + 静态 SpriteRenderer）
public static class MapPaintBackgroundCapture
{
    const float MaskMatchThreshold = 0.05f;

    public static Texture2D CaptureChunk(
        MapChunkEditorRoot root,
        ChunkCoord coord,
        float ppu,
        Color clearColor,
        out float coverageRatio)
    {
        coverageRatio = 0f;
        if (root == null)
        {
            return null;
        }

        var settings = MapChunkEditorSettings.GetOrCreate();
        int slicePx = MapChunkUtility.ComputeSlicePixelSize(root.ChunkWorldSize, ppu);
        var chunkMin = MapChunkUtility.ChunkWorldMin(coord, root.ChunkOrigin, root.ChunkWorldSize);
        var center = chunkMin + new Vector3(root.ChunkWorldSize * 0.5f, root.ChunkWorldSize * 0.5f, settings.PaintCaptureCameraZ);

        var camGo = new GameObject("MapPaintCaptureCamera")
        {
            hideFlags = HideFlags.HideAndDontSave,
        };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = root.ChunkWorldSize * 0.5f;
        cam.aspect = 1f;
        cam.transform.position = center;
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = clearColor;
        cam.cullingMask = settings.PaintCaptureLayerMask;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = Mathf.Abs(settings.PaintCaptureCameraZ) + root.ChunkWorldSize + 50f;
        cam.enabled = false;
        cam.allowMSAA = false;
        cam.allowHDR = false;

        // Linear 项目需 sRGB RT，否则 ReadPixels 得到线性值写入 PNG 会与 Scene 观感不一致
        var readWrite = QualitySettings.activeColorSpace == ColorSpace.Linear
            ? RenderTextureReadWrite.sRGB
            : RenderTextureReadWrite.Linear;
        var rt = RenderTexture.GetTemporary(slicePx, slicePx, 24, RenderTextureFormat.ARGB32, readWrite);
        rt.filterMode = FilterMode.Point;
        rt.antiAliasing = 1;

        var prevActive = RenderTexture.active;
        var prevTarget = cam.targetTexture;
        Texture2D tex = null;

        try
        {
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            tex = new Texture2D(slicePx, slicePx, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.ReadPixels(new Rect(0, 0, slicePx, slicePx), 0, 0);
            tex.Apply();
            coverageRatio = ComputeCoverage(tex, clearColor);
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(camGo);
        }

        return tex;
    }

    public static Rect ComputeBoundsFromCaptureLayers(MapChunkEditorRoot root)
    {
        if (root == null)
        {
            return default;
        }

        int layerMask = MapChunkEditorSettings.GetOrCreate().PaintCaptureLayerMask.value;
        var scene = root.gameObject.scene;
        if (!scene.IsValid())
        {
            return root.PaintWorldRect;
        }

        bool hasBounds = false;
        var bounds = new Bounds();

        foreach (var rootGo in scene.GetRootGameObjects())
        {
            foreach (var sr in rootGo.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!IsLayerIncluded(sr.gameObject.layer, layerMask) || sr.sprite == null || !sr.enabled)
                {
                    continue;
                }

                Encapsulate(ref bounds, ref hasBounds, sr.bounds);
            }

            foreach (var tr in rootGo.GetComponentsInChildren<TilemapRenderer>(true))
            {
                if (!IsLayerIncluded(tr.gameObject.layer, layerMask) || !tr.enabled)
                {
                    continue;
                }

                var tm = tr.GetComponent<Tilemap>();
                if (tm == null)
                {
                    continue;
                }

                tm.CompressBounds();
                var cellBounds = tm.cellBounds;
                if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0)
                {
                    continue;
                }

                foreach (var pos in cellBounds.allPositionsWithin)
                {
                    if (tm.GetTile(pos) == null)
                    {
                        continue;
                    }

                    Encapsulate(ref bounds, ref hasBounds, tm.GetCellCenterWorld(pos));
                }
            }
        }

        if (!hasBounds)
        {
            return root.PaintWorldRect;
        }

        var rect = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        return MapChunkUtility.SnapWorldRectToChunkGrid(rect, root.ChunkOrigin, root.ChunkWorldSize);
    }

    static bool IsLayerIncluded(int layer, int layerMask)
    {
        return (layerMask & (1 << layer)) != 0;
    }

    static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Vector3 worldPoint)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(worldPoint, Vector3.zero);
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(worldPoint);
        }
    }

    static void Encapsulate(ref Bounds bounds, ref bool hasBounds, Bounds target)
    {
        if (!hasBounds)
        {
            bounds = target;
            hasBounds = true;
        }
        else
        {
            bounds.Encapsulate(target);
        }
    }

    static float ComputeCoverage(Texture2D tex, Color maskColor)
    {
        if (tex == null)
        {
            return 0f;
        }

        var pixels = tex.GetPixels();
        int covered = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (IsMaskPixel(pixels[i], maskColor))
            {
                continue;
            }

            covered++;
        }

        return covered / (float)pixels.Length;
    }

    static bool IsMaskPixel(Color pixel, Color maskColor)
    {
        if (pixel.a < 0.01f)
        {
            return true;
        }

        return Mathf.Abs(pixel.r - maskColor.r) < MaskMatchThreshold &&
               Mathf.Abs(pixel.g - maskColor.g) < MaskMatchThreshold &&
               Mathf.Abs(pixel.b - maskColor.b) < MaskMatchThreshold;
    }
}
#endif
