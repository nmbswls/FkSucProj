using cfg.demo;
using Cinemachine;
using My.Dungeon;
using My.MapExport;
using UnityEngine;

// 按地图逻辑范围裁剪 MainMapVCam 视野
public class MapCameraBoundsController : MonoBehaviour
{
    CinemachineVirtualCamera _vcam;
    CinemachineConfiner2D _confiner;
    GameObject _boundsGo;
    BoxCollider2D _boundsCollider;

    public void Bind(CinemachineVirtualCamera vcam)
    {
        _vcam = vcam;
    }

    public void Clear()
    {
        if (_confiner != null)
        {
            _confiner.enabled = false;
            _confiner.m_BoundingShape2D = null;
        }

        DestroyBoundsShape();
    }

    public void ApplyForCurrentMap(
        AreaOverlayStateInfo overlay,
        WorldAreaRoot worldRoot,
        MapChunkDatabase chunkDb)
    {
        Clear();

        if (_vcam == null || overlay == null)
        {
            return;
        }

        // 据点由 SecretBaseSceneRoot 自行控制相机
        if (overlay.IsSecretBase)
        {
            return;
        }

        if (!TryResolveLogicWorldRect(overlay, worldRoot, chunkDb, out var rect))
        {
            return;
        }

        ApplyRect(rect);
    }

    static bool TryResolveLogicWorldRect(
        AreaOverlayStateInfo overlay,
        WorldAreaRoot worldRoot,
        MapChunkDatabase chunkDb,
        out Rect rect)
    {
        rect = default;

        if (worldRoot != null && worldRoot.HasLogicWorldRectOverride)
        {
            rect = worldRoot.LogicWorldRectOverride;
            return true;
        }

        if (chunkDb != null)
        {
            rect = chunkDb.ResolveLogicWorldRect();
            if (rect.width > 0f && rect.height > 0f)
            {
                return true;
            }
        }

        if (DungeonPresentation.IsProceduralOverlay(overlay))
        {
            var result = DungeonSession.GetLastResult(overlay.Id);
            var grid = worldRoot != null ? worldRoot.Grid : null;
            return TryGetDungeonLogicWorldRect(result, grid, out rect);
        }

        return false;
    }

    static bool TryGetDungeonLogicWorldRect(DungeonGenerationResult result, Grid grid, out Rect rect)
    {
        rect = default;
        if (result?.WalkableCells == null || result.WalkableCells.Count == 0 || grid == null)
        {
            return false;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        foreach (var cell in result.WalkableCells)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        if (minX == int.MaxValue)
        {
            return false;
        }

        var cellSize = grid.cellSize;
        var halfCell = new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f);
        var sw = grid.GetCellCenterWorld(new Vector3Int(minX, minY, 0)) - halfCell;
        var ne = grid.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0)) + halfCell;
        rect = Rect.MinMaxRect(sw.x, sw.y, ne.x, ne.y);
        return rect.width > 0f && rect.height > 0f;
    }

    void ApplyRect(Rect rect)
    {
        EnsureConfiner();
        EnsureBoundsShape(rect);

        _confiner.m_BoundingShape2D = _boundsCollider;
        _confiner.m_Damping = 0f;
        _confiner.m_MaxWindowSize = Mathf.Max(_vcam.m_Lens.OrthographicSize * 1.5f, _vcam.m_Lens.OrthographicSize + 0.01f);
        _confiner.InvalidateCache();
        _confiner.enabled = true;

        Debug.Log(
            $"MapCameraBoundsController: confine camera to [{rect.xMin:F1},{rect.yMin:F1}] - [{rect.xMax:F1},{rect.yMax:F1}]");
    }

    void EnsureConfiner()
    {
        if (_confiner != null)
        {
            return;
        }

        _confiner = _vcam.GetComponent<CinemachineConfiner2D>();
        if (_confiner == null)
        {
            _confiner = _vcam.gameObject.AddComponent<CinemachineConfiner2D>();
        }
    }

    void EnsureBoundsShape(Rect rect)
    {
        if (_boundsGo == null)
        {
            _boundsGo = new GameObject("MapCameraBoundsShape");
            _boundsGo.hideFlags = HideFlags.HideAndDontSave;
            _boundsGo.transform.SetParent(transform, false);
            _boundsCollider = _boundsGo.AddComponent<BoxCollider2D>();
            _boundsCollider.isTrigger = true;
        }

        _boundsGo.transform.position = Vector3.zero;
        _boundsCollider.offset = rect.center;
        _boundsCollider.size = new Vector2(rect.width, rect.height);
    }

    void DestroyBoundsShape()
    {
        if (_boundsGo == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_boundsGo);
        }
        else
        {
            DestroyImmediate(_boundsGo);
        }

        _boundsGo = null;
        _boundsCollider = null;
    }
}
