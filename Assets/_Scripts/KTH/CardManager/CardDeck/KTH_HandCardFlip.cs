using UnityEngine;

public class KTH_HandCardFlip : MonoBehaviour
{
    [Header("전환 대상")]
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;

    [Header("전환 기준")]
    [Tooltip("이 각도를 넘어가면 뒷면으로 전환한다 (0~180 기준, 보통 90)")]
    [SerializeField] private float flipThresholdAngle = 90f;

    [Tooltip("회전을 어떤 축 기준으로 판정할지 (기본 Y축 - 좌우로 뒤집는 카드)")]
    [SerializeField] private RotationAxis axis = RotationAxis.Y;

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    private bool _isShowingFront = true;

    private void Reset()
    {
        // 인스펙터에서 자동으로 Front/Back 찾아 연결 시도 (편의 기능)
        Transform frontTransform = transform.Find("Front");
        Transform backTransform = transform.Find("Back");

        if (frontTransform != null)
        {
            front = frontTransform.gameObject;
        }

        if (backTransform != null)
        {
            back = backTransform.gameObject;
        }
    }

    private void Awake()
    {
        ApplyState(true);
    }

    private void LateUpdate()
    {
        float rawAngle = GetRawAxisAngle();

        // 0~360을 -180~180 범위로 정규화
        float signedAngle = NormalizeAngle(rawAngle);

        // 절대각이 임계값을 넘으면 뒷면
        bool shouldShowFront =
            Mathf.Abs(signedAngle) < flipThresholdAngle;

        if (shouldShowFront != _isShowingFront)
        {
            ApplyState(shouldShowFront);
        }
    }

    private float GetRawAxisAngle()
    {
        Vector3 euler = transform.localEulerAngles;

        switch (axis)
        {
            case RotationAxis.X:
                return euler.x;
            case RotationAxis.Z:
                return euler.z;
            case RotationAxis.Y:
            default:
                return euler.y;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private void ApplyState(bool showFront)
    {
        _isShowingFront = showFront;

        if (front != null)
        {
            front.SetActive(showFront);
        }

        if (back != null)
        {
            back.SetActive(!showFront);
        }
    }

    /// <summary>
    /// 외부에서 강제로 앞/뒤 상태를 즉시 지정하고 싶을 때 사용.
    /// (예: 카드를 새로 생성/재사용할 때 항상 앞면부터 시작하도록)
    /// </summary>
    public void ForceShowFront()
    {
        ApplyState(true);
    }

    public void ForceShowBack()
    {
        ApplyState(false);
    }
}