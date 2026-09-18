using Unity.Cinemachine;
using UnityEngine;

/// <summary>원본 카메라 설정 대신 해당 프레임의 렌즈 계산 결과에만 줌을 적용.</summary>
public sealed class DLJ_CorvoKingZoomExtension : CinemachineExtension
{
    [System.NonSerialized] public DLJ_CorvoKingCameraZoom Owner;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || Owner == null) return;
        var lens = state.Lens;
        lens.FieldOfView = Mathf.Clamp(lens.FieldOfView * Owner.LensRatio, 1f, 179f);
        lens.OrthographicSize = Mathf.Max(0.001f, lens.OrthographicSize * Owner.LensRatio);
        state.Lens = lens;
    }
}
