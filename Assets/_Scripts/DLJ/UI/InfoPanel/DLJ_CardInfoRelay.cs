using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 기존 카드 스크립트를 수정하지 않고 더블클릭한 카드 SO를 이벤트로 전달한다.
/// </summary>
[RequireComponent(typeof(KTH_HandCard))]
public sealed class DLJ_CardInfoRelay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Min(1)] private int requiredClickCount = 2;

    private KTH_HandCard _card;

    private void Awake()
    {
        _card = GetComponent<KTH_HandCard>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (eventData.clickCount != requiredClickCount)
            return;

        if (_card == null || _card.CardData == null)
        {
            Debug.LogWarning("[DLJ_CardInfoRelay] 카드 SO를 가져올 수 없습니다.", this);
            return;
        }

        DLJ_InfoPanelEvents.RaiseCardDoubleClicked(_card.CardData);
    }
}
