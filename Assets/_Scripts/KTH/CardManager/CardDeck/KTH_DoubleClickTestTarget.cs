using UnityEngine;

/// <summary>
/// 테스트용: KTH_HandCard.OnCardDoubleClicked 이벤트를 구독해서
/// 카드가 더블클릭되면 지정한 오브젝트를 활성화(SetActive(true))한다.
/// 씬의 아무 빈 오브젝트에 붙이고 targetObject만 인스펙터에서 연결하면 됨.
/// </summary>
public class KTH_DoubleClickTestTarget : MonoBehaviour
{
    [Tooltip("카드가 더블클릭됐을 때 활성화할 대상 오브젝트")]
    [SerializeField] private GameObject targetObject;

    [Tooltip("체크하면 더블클릭 시 targetObject.SetActive(false) -> true 로 강제 리셋")]
    [SerializeField] private bool forceReactivate = true;

    private void OnEnable()
    {
        KTH_HandCard.OnCardDoubleClicked += HandleCardDoubleClicked;
        KTH_HandCard.OnCardDoubleClickCancelled += HandleCardDoubleClickCancelled;
    }

    private void OnDisable()
    {
        KTH_HandCard.OnCardDoubleClicked -= HandleCardDoubleClicked;
        KTH_HandCard.OnCardDoubleClickCancelled -= HandleCardDoubleClickCancelled;
    }

    private void HandleCardDoubleClicked(KTH_HandCard doubleClickedCard)
    {
        if (targetObject == null)
        {
            Debug.LogWarning(
                $"[{nameof(KTH_DoubleClickTestTarget)}] targetObject가 연결되어 있지 않습니다."
            );
            return;
        }

        Debug.Log(
            $"[{nameof(KTH_DoubleClickTestTarget)}] 더블클릭 감지: {doubleClickedCard.name} -> {targetObject.name} 활성화"
        );

        if (forceReactivate && targetObject.activeSelf)
        {
            targetObject.SetActive(false);
        }

        targetObject.SetActive(true);
    }

    private void HandleCardDoubleClickCancelled(KTH_HandCard cancelledCard)
    {
        if (targetObject == null)
        {
            return;
        }

        Debug.Log(
            $"[{nameof(KTH_DoubleClickTestTarget)}] 더블클릭 취소: {cancelledCard.name} -> {targetObject.name} 비활성화"
        );

        targetObject.SetActive(false);
    }
}
