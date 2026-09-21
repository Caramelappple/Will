using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 사용한 코스트 코인을 화면 왼쪽으로 밀어낸다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DLJ_CostCoinShaderEffect : MonoBehaviour, IDLJ_CostCoinSpendEffect
{
    [Header("Path")]
    [Tooltip("화면 좌표를 계산할 카메라. 비워두면 Main Camera를 사용한다.")]
    [SerializeField] private Camera screenCamera;

    [Tooltip("코인이 도착할 화면 X 위치. 0이 화면 왼쪽 끝이며 음수면 화면 밖이다.")]
    [SerializeField] private float offscreenViewportX = -0.1f;

    [Tooltip("카메라가 없을 때 대신 사용할 로컬 이동 거리.")]
    [SerializeField] private float fallbackLocalDistance = 6f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float exitDuration = 0.3f;

    [SerializeField] private AnimationCurve exitEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 0f));

    [Header("Playback")]
    [SerializeField] private bool ignoreTimeScale;

    private readonly Dictionary<Transform, Tween> _tweens = new Dictionary<Transform, Tween>();

    private Camera ExitCamera => screenCamera != null ? screenCamera : Camera.main;

    /// <summary>현재 위치에서 화면 왼쪽 바깥으로 이동한 뒤 완료를 알린다.</summary>
    public bool Play(Transform coin, Action onComplete = null)
    {
        if (coin == null || !coin.gameObject.activeInHierarchy)
            return false;

        StopAndReset(coin);

        Vector3 targetPosition;
        Camera cameraForExit = ExitCamera;
        if (cameraForExit != null)
        {
            Vector3 viewportPosition = cameraForExit.WorldToViewportPoint(coin.position);
            viewportPosition.x = offscreenViewportX;
            targetPosition = cameraForExit.ViewportToWorldPoint(viewportPosition);
        }
        else
        {
            // 테스트 씬처럼 Main Camera가 없는 경우에도 연출 자체는 빠뜨리지 않는다.
            targetPosition = coin.position - coin.TransformVector(Vector3.right * fallbackLocalDistance);
        }

        Tween tween = coin
            .DOMove(targetPosition, exitDuration)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(coin.gameObject)
            .OnComplete(() => onComplete?.Invoke());

        _tweens[coin] = tween;
        tween.OnKill(() =>
        {
            if (_tweens.TryGetValue(coin, out Tween tracked) && tracked == tween)
                _tweens.Remove(coin);
        });

        return true;
    }

    /// <summary>진행 중인 퇴장 연출을 중지한다. 슬롯 위치 복구는 DLJ_CostCoinSlot이 담당한다.</summary>
    public void StopAndReset(Transform coin)
    {
        if (coin == null || !_tweens.TryGetValue(coin, out Tween tween)) return;

        _tweens.Remove(coin);
        tween.Kill();
    }

    private void OnDisable()
    {
        Tween[] tweens = new Tween[_tweens.Count];
        _tweens.Values.CopyTo(tweens, 0);
        _tweens.Clear();

        foreach (Tween tween in tweens)
            tween?.Kill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fallbackLocalDistance = Mathf.Max(0f, fallbackLocalDistance);
        exitDuration = Mathf.Max(0.01f, exitDuration);

        if (exitEase == null || exitEase.length == 0)
            exitEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
#endif
}
