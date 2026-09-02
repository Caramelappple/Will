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
    [Tooltip("맨 앞으로 나올 때 카메라 쪽(-Z 방향)으로 당겨줄 거리.")]
    [SerializeField] private float frontZOffset = 0.05f;

    private float originalLocalZ;
    private bool isFront;

    public void BringToFront()
    {
        if (isFront)
        {
            return;
        }

        originalLocalZ =
            transform.localPosition.z;

        Vector3 localPos =
            transform.localPosition;

        localPos.z =
            originalLocalZ - frontZOffset;

        transform.localPosition =
            localPos;

        isFront = true;
    }

    public void RestoreSorting()
    {
        if (!isFront)
        {
            return;
        }

        Vector3 localPos =
            transform.localPosition;

        localPos.z = originalLocalZ;

        transform.localPosition =
            localPos;

        isFront = false;
    }

    public bool IsFront =>
        isFront;

    private void OnDisable()
    {
        RestoreSorting();
    }
}
