#if UNITY_EDITOR
using System.IO;
using My.Map.Logic;
using My.MapExport;
using UnityEngine;

// 单 chunk 外扩上下文：拼接邻块给 AI 参考，Import 时裁回中心区域
public static class MapPaintBackgroundContext
{
    public static int ComputeMarginPx(int slicePx, float expandRatio)
    {
        if (slicePx <= 0)
        {
            return 0;
        }

        float ratio = Mathf.Clamp(expandRatio, 0f, 0.49f);
        return Mathf.Max(0, Mathf.RoundToInt(slicePx * ratio));
    }

    public static int ComputeContextSize(int slicePx, float expandRatio)
    {
        return slicePx + ComputeMarginPx(slicePx, expandRatio) * 2;
    }

    public static Texture2D BuildChunkForAi(
        string mapName,
        MapPaintManifest manifest,
        ChunkCoord center,
        int slicePx,
        float expandRatio,
        Color maskColor,
        FilterMode filter)
    {
        int margin = ComputeMarginPx(slicePx, expandRatio);
        int total = slicePx + margin * 2;
        var output = CreateFilledTexture(total, total, maskColor);

        int chunkRadius = margin > 0 ? Mathf.CeilToInt(margin / (float)slicePx) : 0;
        for (int dy = -chunkRadius; dy <= chunkRadius; dy++)
        {
            for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
            {
                var coord = new ChunkCoord(center.X + dx, center.Y + dy);
                var chunkTex = LoadChunkReference(mapName, manifest, coord, slicePx, filter);
                if (chunkTex == null)
                {
                    continue;
                }

                int destX = margin + dx * slicePx;
                int destY = margin + dy * slicePx;
                BlitInto(output, chunkTex, destX, destY);
                Object.DestroyImmediate(chunkTex);
            }
        }

        output.Apply();
        return output;
    }

    public static Texture2D CropCenterFromContext(
        Texture2D contextTex,
        int slicePx,
        float expandRatio,
        FilterMode filter)
    {
        if (contextTex == null)
        {
            return null;
        }

        int margin = ComputeMarginPx(slicePx, expandRatio);
        int expected = slicePx + margin * 2;
        if (contextTex.width != expected || contextTex.height != expected)
        {
            var resampled = MapPaintBackgroundShared.ResampleTexture(contextTex, expected, expected, filter);
            var cropped = CropCenter(resampled, slicePx, margin);
            Object.DestroyImmediate(resampled);
            return cropped;
        }

        return CropCenter(contextTex, slicePx, margin);
    }

    // 优先 painted（含中心块已绘部分），其次 chunk 模板
    public static Texture2D LoadChunkReference(
        string mapName,
        MapPaintManifest manifest,
        ChunkCoord coord,
        int slicePx,
        FilterMode filter)
    {
        string paintedPath = MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord);
        if (File.Exists(paintedPath))
        {
            var painted = MapPaintBackgroundShared.LoadTextureFromAssetPath(paintedPath);
            return EnsureSize(painted, slicePx, filter);
        }

        string templatePath = MapPaintBackgroundShared.GetChunkTemplatePath(mapName, coord);
        if (File.Exists(templatePath))
        {
            var template = MapPaintBackgroundShared.LoadTextureFromAssetPath(templatePath);
            return EnsureSize(template, slicePx, filter);
        }

        return null;
    }

    static Texture2D CreateFilledTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var fill = new Color[width * height];
        for (int i = 0; i < fill.Length; i++)
        {
            fill[i] = color;
        }

        tex.SetPixels(fill);
        return tex;
    }

    static void BlitInto(Texture2D dest, Texture2D src, int destX, int destY)
    {
        int w = src.width;
        int h = src.height;
        int x0 = Mathf.Max(0, destX);
        int y0 = Mathf.Max(0, destY);
        int x1 = Mathf.Min(dest.width, destX + w);
        int y1 = Mathf.Min(dest.height, destY + h);
        if (x0 >= x1 || y0 >= y1)
        {
            return;
        }

        int srcX = x0 - destX;
        int srcY = y0 - destY;
        int copyW = x1 - x0;
        int copyH = y1 - y0;
        dest.SetPixels(x0, y0, copyW, copyH, src.GetPixels(srcX, srcY, copyW, copyH));
    }

    static Texture2D CropCenter(Texture2D src, int slicePx, int margin)
    {
        var cropped = new Texture2D(slicePx, slicePx, TextureFormat.RGBA32, false);
        cropped.SetPixels(src.GetPixels(margin, margin, slicePx, slicePx));
        cropped.Apply();
        return cropped;
    }

    static Texture2D EnsureSize(Texture2D tex, int slicePx, FilterMode filter)
    {
        if (tex == null)
        {
            return null;
        }

        if (tex.width == slicePx && tex.height == slicePx)
        {
            return tex;
        }

        var resampled = MapPaintBackgroundShared.ResampleTexture(tex, slicePx, slicePx, filter);
        Object.DestroyImmediate(tex);
        return resampled;
    }
}
#endif
