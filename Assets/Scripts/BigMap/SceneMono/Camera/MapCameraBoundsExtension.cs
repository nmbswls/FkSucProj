using Cinemachine;
using My.MapExport;
using UnityEngine;

// 在 Cinemachine 管线内裁剪相机，使玩家走到地图边缘时相机不再跟随越界（场景预挂在 MainMapVCam 上）
[DisallowMultipleComponent]
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
