using System;
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

    [Header("Hand Settings")]
    [Tooltip("손패에 들고 있을 수 있는 최대 카드 수 (0 이하면 제한 없음)")]
    [SerializeField] private int maxHandSize = 10;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    private readonly List<KTH_HandCard> handCards = new List<KTH_HandCard>();

    /// <summary>
    /// 현재 손패에 있는 카드 수
    /// </summary>
    public int HandCount => handCards.Count;

    public int MaxHandSize
    {
        get => maxHandSize;
        set => maxHandSize = value;
    }

    /// <summary>
    /// 손패가 최대치에 도달했는지 여부 (maxHandSize가 0 이하면 항상 false)
    /// </summary>
    public bool IsFull => maxHandSize > 0 && handCards.Count >= maxHandSize;

    /// <summary>
    /// 손패 카드 수가 바뀔 때마다 호출됩니다. (현재 수, 최대 수)
    /// </summary>
    public event Action<int, int> OnHandCountChanged;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 카드를 추가할 수 있는 상태인지 (최대치 여부) 미리 확인합니다.
    /// </summary>
    public bool CanAddCard()
    {
        return !IsFull;
    }

    public bool AddCard(KTH_HandCard card)
    {
        if (card == null || handCards.Contains(card)) return false;

        if (IsFull)
        {
            Debug.LogWarning($"[KTH_HandCardLayout] 손패가 가득 찼습니다! ({handCards.Count}/{maxHandSize})");
            return false;
        }

        handCards.Add(card);
        UpdateHandLayout(card);
        OnHandCountChanged?.Invoke(handCards.Count, maxHandSize);
        return true;
    }

    public void RemoveCard(KTH_HandCard card)
    {
        if (handCards.Remove(card))
        {
            UpdateHandLayout(null);
            OnHandCountChanged?.Invoke(handCards.Count, maxHandSize);
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