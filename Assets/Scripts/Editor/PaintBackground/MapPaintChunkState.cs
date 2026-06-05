#if UNITY_EDITOR
using System.IO;
using My.Map.Logic;
using My.MapExport;

// chunk 四态：Ready / Done / Stale / Stale+Painted
public static class MapPaintChunkState
{
    public enum DisplayState
    {
        Ready,
        Done,
        Stale,
        StalePainted,
    }

    public static bool HasPainted(string mapName, MapPaintChunkInfo info, ChunkCoord coord)
    {
        if (info != null && info.Source == ChunkPaintSource.UserPainted)
        {
            return true;
        }

        return File.Exists(MapPaintBackgroundShared.GetPaintedChunkPath(mapName, coord));
    }

    public static DisplayState Resolve(string mapName, MapPaintChunkInfo info, ChunkCoord coord)
    {
        bool painted = HasPainted(mapName, info, coord);
        bool stale = info != null && info.TemplateStale;
        if (stale && painted)
        {
            return DisplayState.StalePainted;
        }

        if (stale)
        {
            return DisplayState.Stale;
        }

        if (painted)
        {
            return DisplayState.Done;
        }

        return DisplayState.Ready;
    }

    public static string GetLabel(DisplayState state)
    {
        switch (state)
        {
            case DisplayState.Done:
                return "Done";
            case DisplayState.Stale:
                return "Stale";
            case DisplayState.StalePainted:
                return "Stale+Painted";
            default:
                return "Ready";
        }
    }
}
#endif
