using Unity.Cinemachine;
using UnityEngine;

// Anchor 오브젝트에 붙인다. Anchor의 Scale/Rotation이 바뀌면(인스펙터 타이핑이든 씬 뷰
// 기즈모 드래그든) 그 값을 그대로 부모 트랜스폼(카드 루트)에 반영해서 카드 전체의
// 크기/회전이 실시간으로 따라오게 한다. KTH_HandCard 등 특정 컴포넌트에 의존하지 않고
// Anchor의 바로 위 부모(transform.parent)만 있으면 동작한다.
// 씬 뷰 기즈모 드래그는 OnValidate를 호출하지 않는다(인스펙터 필드 입력에만 반응함) -
// 그래서 매 프레임 값이 바뀌었는지만 가볍게 비교해서 바뀐 경우에만 반영한다. 손패 카드 수가
// 몇 장 안 돼서 이 비교 비용은 무시할 수준이다.
// [ExecuteAlways]로 플레이 중이 아닌 에디터에서 드래그할 때도 바로 반영되게 한다.
[ExecuteAlways]
public class KTH_HandCardAnchorSync : MonoBehaviour
{
    private Vector3 lastScale;
    private Quaternion lastRotation;



    private void OnEnable()
    {
        lastScale = transform.localScale;
        lastRotation = transform.localRotation;
    }

    private void Update()
    {
        if (transform.localScale == lastScale && transform.localRotation == lastRotation)
        {
            return;
        }

        var cardRoot = transform.parent;

        if (cardRoot == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardAnchorSync] 부모 트랜스폼이 없어 Anchor 값을 반영하지 못합니다.",
                this
            );

            return;
        }

        cardRoot.localScale = transform.localScale;
        cardRoot.localRotation = transform.localRotation;

        lastScale = transform.localScale;
        lastRotation = transform.localRotation;
    }
}
