using UnityEngine;

// 3D 전환 메모:
// UI 계층에서는 "맨 앞으로"가 transform.SetAsLastSibling()이었다.
//
// sortingOrder는 빼기로 함:
// 카드가 opaque(불투명) PBR 메쉬라서 sortingOrder는 아무 효과가 없다.
// opaque 오브젝트는 그리기 순서와 상관없이 Z-버퍼(depth test)로만 어느 게
// 위에 그려질지 픽셀 단위로 정해지기 때문. sortingOrder는 transparent 렌더 큐
// (스프라이트, 알파블렌드 머티리얼 등)에서만 의미가 있다.
//
// 그래서 실제로 먹히는 방법은 "맨 앞" 카드를 카메라 쪽으로 살짝 당겨서
// 물리적으로 더 가깝게 만드는 것뿐이다. 그러면 Z-버퍼가 자연스럽게
// 이 카드를 위에 그려준다.
//
// (텍스트를 잠깐 껐다 켜는 방식도 시도해봤지만, 별도 고정 타이머로 움직이다 보니
// 카드를 빠르게 여러 번 선택/해제하면 타이머끼리 꼬여서 이상한 타이밍에 꺼지는
// 문제가 있었다. 애니메이션 완료 시점과 정확히 동기화되지 않는 한 불안정해서 뺐다.)
public class KTH_CardSorting : MonoBehaviour
{
    [Tooltip("맨 앞으로 나올 때 카메라 쪽으로 당겨줄 거리.\n" +
             "\n" +
             "어느 방향이 '카메라 쪽'인지는 손패(KTH_HandCardLayout.DepthAxis)가 정한다.\n" +
             "부채꼴의 앞뒤 간격과 같은 방향이어야 이 카드만 엉뚱한 쪽으로 튀지 않는다.")]
    [SerializeField] private float frontZOffset = 0.05f;

    /// <summary>
    /// 앞으로 빼면서 더한 값. 되돌릴 때 이만큼 뺀다.
    ///
    /// ── 왜 절대 좌표를 안 쓰나 ────────────────────────────────
    /// 예전에는 원래 Z를 통째로 기억했다가 그대로 되돌려 썼다. 그 사이에 손패가
    /// 재배치되면 카드의 제 자리가 바뀌는데, 되돌릴 때 **옛 값을 덮어써서**
    /// 앞뒤가 엉켰다.
    ///
    /// 더한 만큼만 빼면 그 사이에 자리가 어디로 옮겨갔든 상대적으로 맞는다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    private Vector3 appliedOffset;

    private bool isFront;

    /// <summary>
    /// 지금 앞으로 빼둔 값. 앞이 아니면 0이다.
    ///
    /// 선택 연출이 이 값을 알아야 한다. 연출이 "원래 자리"로만 트윈하면
    /// 방금 앞으로 뺀 것을 도로 끌어내려서, 고른 카드가 이웃 밑으로 가라앉는다.
    /// </summary>
    public Vector3 FrontOffset => isFront ? appliedOffset : Vector3.zero;

    /// <summary>
    /// 맨 앞으로 뺀다.
    /// </summary>
    /// <param name="distance">
    /// 뺄 거리. 안 넘기면 인스펙터 값(frontZOffset)을 쓴다.
    ///
    /// 손패는 넘긴다 — 부채꼴 전체가 차지하는 깊이보다 더 나와야 어느 자리의
    /// 카드를 골라도 맨 앞에 서기 때문이다. 그 거리는 손패가 안다
    /// (KTH_HandCardLayout.FrontDepthDistance).
    ///
    /// 버림 더미처럼 부채꼴이 아닌 곳은 안 넘기고 제 값을 쓴다.
    /// </param>
    public void BringToFront(float? distance = null)
    {
        if (isFront)
        {
            return;
        }

        appliedOffset = ResolveAxis() * (distance ?? frontZOffset);

        transform.localPosition += appliedOffset;

        isFront = true;
    }

    public void RestoreSorting()
    {
        if (!isFront)
        {
            return;
        }

        transform.localPosition -= appliedOffset;

        appliedOffset = Vector3.zero;
        isFront = false;
    }

    /// <summary>
    /// 앞으로 뺄 방향. 손패가 쓰는 것과 같은 값을 쓴다.
    ///
    /// 손패가 없는 곳에서도 이 컴포넌트를 쓸 수 있으므로,
    /// 못 찾으면 예전 방식인 로컬 -Z 로 돌아간다.
    /// </summary>
    private static Vector3 ResolveAxis()
    {
        KTH_HandCardLayout layout = KTH_HandCardLayout.Instance;

        return layout != null ? layout.DepthAxis : Vector3.back;
    }

    public bool IsFront =>
        isFront;

    private void OnDisable()
    {
        RestoreSorting();
    }
}
