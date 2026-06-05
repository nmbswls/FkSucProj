using Cinemachine;
using My.MapExport;
using UnityEngine;

// 在 Cinemachine 管线内裁剪相机，使玩家走到地图边缘时相机不再跟随越界（场景预挂在 MainMapVCam 上）
public class MapCameraBoundsExtension : CinemachineExtension
{
    internal Rect Bounds;
    internal bool Active;

    public void SetBounds(Rect bounds)
    {
        Bounds = bounds;
        Active = bounds.width > 0f && bounds.height > 0f;
    }

    public void ClearBounds()
    {
        Active = false;
        Bounds = default;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (!Active || stage != CinemachineCore.Stage.Body)
        {
            return;
        }

        if (!state.Lens.Orthographic)
        {
            return;
        }

        var current = state.CorrectedPosition;
        float halfH = state.Lens.OrthographicSize;
        float aspect = state.Lens.Aspect > 1e-4f
            ? state.Lens.Aspect
            : (Camera.main != null ? Camera.main.aspect : 16f / 9f);

        var clamped = MapChunkUtility.ClampOrthographicCenter(Bounds, halfH, aspect, current);
        state.PositionCorrection += clamped - current;
    }
}

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
