using System.Collections.Generic;
using UnityEngine;

public class KTH_HandCardLayout : MonoBehaviour
{
    [Header("Arc Layout Settings")]
    [Tooltip("카드 간의 horizontal 간격 (값이 작을수록 카드가 많이 겹침)")]
    [SerializeField] private float cardSpacing = 75f;  // [수정] 촘촘한 오버랩을 위해 축소

    [Tooltip("중앙과 양 끝 카드의 Y축 높이 차이 (곡선 휘어짐 정도)")]
    [SerializeField] private float arcHeight = 6f;      // [수정] 완만한 포물선을 위해 축소

    [Tooltip("양 끝 카드의 최대 Z축 기울기 각도")]
    [SerializeField] private float maxRotation = 6f;    // [수정] 자연스러운 각도를 위해 완화

    private readonly List<KTH_HandCard> handCards = new List<KTH_HandCard>();

    public void AddCard(KTH_HandCard card)
    {
        if (card == null || handCards.Contains(card)) return;

        handCards.Add(card);
        UpdateHandLayout();
    }

    public void RemoveCard(KTH_HandCard card)
    {
        if (handCards.Remove(card))
        {
            UpdateHandLayout();
        }
    }

    public void UpdateHandLayout(float duration = 0.35f)
    {
        int count = handCards.Count;
        if (count == 0) return;

        float centerIndex = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float offset = i - centerIndex;

            // 1. X좌표: 좁은 간격으로 촘촘히 배치
            float posX = offset * cardSpacing;

            // 2. Y좌표: 완만하게 아래로 처지는 포물선
            float posY = -Mathf.Pow(offset, 2) * arcHeight;

            // 3. Z회전: 과하지 않은 미세한 부채꼴 기울기
            float zRotation = (count > 1) ? -offset * (maxRotation / Mathf.Max(1f, centerIndex)) : 0f;

            Vector3 targetLocalPos = new Vector3(posX, posY, 0f);

            // 카드 렌더링 순서(겹침) 보정: 왼쪽에서 오른쪽으로 자연스럽게 포개지도록 SiblingIndex 설정
            handCards[i].transform.SetSiblingIndex(i);

            // 카드 위치/회전 이동 요청
            handCards[i].MoveToHandPosition(targetLocalPos, zRotation, duration);
        }
    }
}