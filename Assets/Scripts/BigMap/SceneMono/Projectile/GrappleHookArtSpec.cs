// split_hook_body.png 默认链节尺寸（材质贴图读不到时的 fallback）
public static class GrappleHookArtSpec
{
    public const float DefaultPixelsPerUnit = 100f;
    public const float DefaultChainLinkTileWidthPx = 80f;
    public const float DefaultChainLinkTileHeightPx = 182f;
    public static float CalcTilesPerUnit(float tileWidthPx, float pixelsPerUnit = DefaultPixelsPerUnit)
    {
        if (tileWidthPx <= 1e-4f)
        {
            tileWidthPx = DefaultChainLinkTileWidthPx;
        }

        return pixelsPerUnit / tileWidthPx;
    }

    public static float CalcLineWidth(float tileHeightPx, float pixelsPerUnit = DefaultPixelsPerUnit)
    {
        if (tileHeightPx <= 1e-4f)
        {
            tileHeightPx = DefaultChainLinkTileHeightPx;
        }

        return tileHeightPx / pixelsPerUnit;
    }
}
