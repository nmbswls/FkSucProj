using UnityEngine;

// 由 MainGameManager 创建；驱动场景内已装配的 MapCameraBoundsExtension
public class MapCameraBoundsController
{
    readonly MapCameraBoundsExtension _extension;

    public MapCameraBoundsController(MapCameraBoundsExtension extension)
    {
        _extension = extension;
    }

    public bool IsReady => _extension != null;

    public void Clear()
    {
        _extension?.ClearBounds();
    }

    public void Apply(Rect bounds)
    {
        if (_extension == null)
        {
            Debug.LogError("[MapCameraBounds] MapCameraBoundsExtension is missing on MainMapVCam.");
            return;
        }

        if (bounds.width <= 0f || bounds.height <= 0f)
        {
            return;
        }

        _extension.SetBounds(bounds);

        Debug.Log(
            $"MapCameraBoundsController: confine camera to [{bounds.xMin:F1},{bounds.yMin:F1}] - [{bounds.xMax:F1},{bounds.yMax:F1}]");
    }
}
