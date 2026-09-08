using UnityEngine;

// 아무 오브젝트에나 붙여서 쓴다. 이 오브젝트(Anchor)의 Scale/Rotation이 바뀌면(인스펙터
// 타이핑이든 씬 뷰 기즈모 드래그든) 그 값을 인스펙터에서 지정한 target 트랜스폼에 그대로
// 반영해서, target의 크기/회전이 실시간으로 따라오게 한다.
// 씬 뷰 기즈모 드래그는 OnValidate를 호출하지 않는다(인스펙터 필드 입력에만 반응함) -
// 그래서 매 프레임 값이 바뀌었는지만 가볍게 비교해서 바뀐 경우에만 반영한다. 대상이 몇 개
// 안 되는 상황이라 이 비교 비용은 무시할 수준이다.
// [ExecuteAlways]로 플레이 중이 아닌 에디터에서 드래그할 때도 바로 반영되게 한다.
[ExecuteAlways]
public class KTH_AnchorSync : MonoBehaviour
{
    [Tooltip("이 오브젝트의 Scale/Rotation을 반영받을 대상. 비워두면 부모 트랜스폼을 쓴다.")]
    [SerializeField] private Transform target;

    private Vector3 lastScale;
    private Quaternion lastRotation;

    // 시작 시점에 한 번 강제로 다르게 만들어서, OnEnable에서 바로 Apply()가 한 번은
    // 실행되게 한다 - 그래야 씬 로드 시점에 이미 저장돼 있던 Anchor 값도 target에 반영된다.
    private void OnEnable()
    {
        lastScale = Vector3.zero;
        lastRotation = default;

        Apply();
    }

    private void Update()
    {
        Apply();
    }

    private void Apply()
    {
        if (transform.localScale == lastScale && transform.localRotation == lastRotation)
        {
            return;
        }

        var syncTarget = target != null ? target : transform.parent;

        if (syncTarget == null)
        {
            Debug.LogWarning(
                "[KTH_AnchorSync] target도 없고 부모 트랜스폼도 없어 값을 반영하지 못합니다.",
                this
            );

            return;
        }

        syncTarget.localScale = transform.localScale;
        syncTarget.localRotation = transform.localRotation;

        lastScale = transform.localScale;
        lastRotation = transform.localRotation;
    }
}
