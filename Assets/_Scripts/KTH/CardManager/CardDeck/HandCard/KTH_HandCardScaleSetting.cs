using UnityEngine;

// 카드 크기 관련 값만 담당한다. baseScale/anchorScale은 여기서만 정의하고,
// KTH_HandCard 등 다른 스크립트는 BaseScale만 읽어간다 - 크기를 정하는 주체를 하나로 유지하기 위해서다.
public class KTH_HandCardScaleSetting : MonoBehaviour
{
    [Tooltip("카드가 '정지 상태'일 때 기준이 되는 크기. 뽑기/선택/재정렬 등 모든 애니메이션이 원래대로 돌아올 때 이 값을 기준으로 삼는다.")]
    [SerializeField] private Vector3 baseScale = Vector3.one;

    [Tooltip("카드 그림/텍스트를 묶어둔 자식 트랜스폼(Anchor). 손패 정렬/선택 애니메이션이 건드리지 않는 자리라 여기 크기만 따로 조절해도 로직과 부딪히지 않는다.")]
    [SerializeField] private Transform visualAnchor;

    [Tooltip("visualAnchor의 크기. 카드 콜라이더 등 전체 틀은 그대로 두고 그림/텍스트만 키우거나 줄일 때 쓴다.")]
    [SerializeField] private Vector3 anchorScale = Vector3.one;

    public Vector3 BaseScale => baseScale;

    private void Awake()
    {
        ApplyAnchorScale();
    }

    // 인스펙터에서 baseScale/anchorScale 값을 바꿀 때마다(플레이 모드가 아니어도) 바로 반영해서
    // 에디터에서 결과를 보면서 조절할 수 있게 한다.
    private void OnValidate()
    {
        ApplyAnchorScale();
    }

    private void ApplyAnchorScale()
    {
        if (visualAnchor == null)
        {
            visualAnchor = transform.Find("Anchor");
        }

        if (visualAnchor == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardScaleSetting] visualAnchor(Anchor 자식)를 찾지 못해 anchorScale이 적용되지 않습니다.",
                this
            );

            return;
        }

        visualAnchor.localScale = anchorScale;
    }
}
