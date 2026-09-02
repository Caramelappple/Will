using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 기존 카드 스크립트를 수정하지 않고 선택한 카드 SO를 DLJ 인포창에 전달한다.
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

        if (eventData.clickCount < requiredClickCount)
            return;

        if (_card == null || _card.CardData == null)
        {
            Debug.LogWarning("[DLJ_CardInfoRelay] 카드 SO를 가져올 수 없습니다.", this);
            return;
        }

        if (DLJ_InfoPanel.Instance == null)
        {
            Debug.LogWarning("[DLJ_CardInfoRelay] 씬에 DLJ_InfoPanel이 없습니다.", this);
            return;
        }

        DLJ_InfoPanel.Instance.Show(_card.CardData);
    }
}
