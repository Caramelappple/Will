using System.Collections.Generic;
using UnityEngine;

public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("Arc Layout Settings")]
    [Tooltip("카드 간의 horizontal 간격 (값이 작을수록 카드가 많이 겹침)")]
    [SerializeField] private float cardSpacing = 75f;

    [Tooltip("중앙과 양 끝 카드의 Y축 높이 차이 (곡선 휘어짐 정도)")]
    [SerializeField] private float arcHeight = 6f;

    [Tooltip("양 끝 카드의 최대 Z축 기울기 각도")]
    [SerializeField] private float maxRotation = 6f;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    private readonly List<KTH_HandCard> handCards = new List<KTH_HandCard>();

    private void Awake()
    {
        Instance = this;
    }

    public void AddCard(KTH_HandCard card)
    {
        if (card == null || handCards.Contains(card)) return;

        handCards.Add(card);
        UpdateHandLayout(card);
    }

    public void RemoveCard(KTH_HandCard card)
    {
        if (handCards.Remove(card))
        {
            UpdateHandLayout(null);
        }
    }

    public void UpdateHandLayout(KTH_HandCard newlyDrawnCard = null, float duration = 0.35f)
    {
        int count = handCards.Count;
        if (count == 0) return;

        float centerIndex = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float offset = i - centerIndex;

            float posX = offset * cardSpacing;
            float posY = -Mathf.Pow(offset, 2) * arcHeight;
            float zRotation = (count > 1) ? -offset * (maxRotation / Mathf.Max(1f, centerIndex)) : 0f;

            Vector3 targetLocalPos = new Vector3(posX, posY, 0f);

            KTH_HandCard card = handCards[i];
            card.transform.SetSiblingIndex(i);

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(targetLocalPos, zRotation, drawDuration);
            }
            else
            {
                card.MoveToHandPosition(targetLocalPos, zRotation, duration);
            }
        }
    }
}