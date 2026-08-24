using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("Arc Layout Settings")]
    [Tooltip("카드 간의 최대 horizontal 간격 (카드 수가 적을 때)")]
    [SerializeField] private float maxCardSpacing = 200f;

    [Tooltip("카드 간의 최소 horizontal 간격 (카드 수가 많을 때)")]
    [SerializeField] private float minCardSpacing = 60f;

    [Tooltip("손패 전체가 차지할 수 있는 최대 폭")]
    [SerializeField] private float maxHandWidth = 900f;

    [Tooltip("중앙과 양 끝 카드의 Y축 높이 차이 (곡선 휘어짐 정도)")]
    [SerializeField] private float arcHeight = 6f;

    [Tooltip("양 끝 카드의 최대 Z축 기울기 각도")]
    [SerializeField] private float maxRotation = 6f;

    [Header("Hand Settings")]
    [Tooltip("손패에 들고 있을 수 있는 최대 카드 수 (0 이하면 제한 없음)")]
    [SerializeField] private int maxHandSize = 10;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    [Header("Placement Mode Settings")]
    [Tooltip("체크하면 셀렉트 버튼을 눌렀을 때 핸드 컨테이너가 내려감. 체크 해제하면 절대 안 내려감.")]
    [SerializeField] private bool enableMoveDown = true;

    [Tooltip("배치 모드일 때 컨테이너가 내려가는 거리")]
    [SerializeField] private float placementMoveDownDistance = 150f;
    [Tooltip("컨테이너 내려가고/올라올 때 걸리는 시간")]
    [SerializeField] private float placementMoveDuration = 0.3f;

    private readonly List<KTH_HandCard> handCards = new List<KTH_HandCard>();
    private Vector3 originalContainerLocalPos;

    /// <summary>
    /// 현재 컨테이너가 실제로 내려가 있는 상태인지 (중복 호출 방지용 내부 추적값).
    /// Inspector에서 건드리는 값이 아님.
    /// </summary>
    private bool isCurrentlyDown;

    public int HandCount => handCards.Count;

    public int MaxHandSize
    {
        get => maxHandSize;
        set => maxHandSize = value;
    }

    public bool IsFull => maxHandSize > 0 && handCards.Count >= maxHandSize;

    public event Action<int, int> OnHandCountChanged;

    private void Awake()
    {
        Instance = this;
        originalContainerLocalPos = transform.localPosition;
        isCurrentlyDown = false;
    }

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

        // 새 카드가 들어오면 기존에 선택된 카드가 있을 경우 선택 해제
        KTH_HandCard.DeselectCurrent();

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

        float cardSpacing = maxCardSpacing;
        if (count > 1)
        {
            cardSpacing = Mathf.Min(maxCardSpacing, maxHandWidth / (count - 1));
            cardSpacing = Mathf.Max(cardSpacing, minCardSpacing);
        }

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

    /// <summary>
    /// 셀렉트 버튼을 눌러 배치 모드가 시작될 때 호출됨.
    /// enableMoveDown이 체크되어 있을 때만 실제로 내려감.
    /// </summary>
    public void MoveDownForPlacement()
    {
        if (!enableMoveDown) return;
        if (isCurrentlyDown) return;
        isCurrentlyDown = true;

        transform.DOKill();
        transform.DOLocalMoveY(
            originalContainerLocalPos.y - placementMoveDownDistance,
            placementMoveDuration
        ).SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// 배치 완료/취소되어 원래 자리로 돌아올 때 호출됨.
    /// </summary>
    public void MoveUpFromPlacement()
    {
        if (!isCurrentlyDown) return;
        isCurrentlyDown = false;

        transform.DOKill();
        transform.DOLocalMoveY(
            originalContainerLocalPos.y,
            placementMoveDuration
        ).SetEase(Ease.OutCubic);
    }
}