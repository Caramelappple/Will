using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class KTH_HandCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private TextMeshProUGUI power;

    [Header("Select Animation Settings")]
    [Tooltip("선택 시 위로 올라오는 Y 높이")]
    [SerializeField] private float selectOffsetY = 150f;
    [Tooltip("선택 시 크기 확대 비율")]
    [SerializeField] private float selectScale = 1.25f;
    [Tooltip("선택 애니메이션 지속 시간")]
    [SerializeField] private float selectDuration = 0.25f;

    [Header("Discard Animation Settings")]
    [SerializeField] private float discardDuration = 0.35f;

    private LSO_CardSO cardData;
    private bool isSelected;

    // 원래 상태 저장용
    private int originalSiblingIndex;
    private Vector3 originalLocalPos;
    private float originalZRotation;

    private static KTH_HandCard currentSelectedCard;

    public LSO_CardSO CardData => cardData;

    public event Action<KTH_HandCard> OnCardClicked;

    public void Setup(LSO_CardSO data)
    {
        cardData = data;
    }

    private void Start()
    {
        SettingUi();
    }

    public void SettingUi()
    {
        cardImage.sprite = cardData.Image;
        title.text = cardData.Animal.animalName;
        cost.text = $"{cardData.Animal.cost}";
        power.text = $"{cardData.Animal.damage}";
        outlineImage.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[{name}] OnPointerClick 호출됨");

        if (currentSelectedCard != null && currentSelectedCard != this)
        {
            currentSelectedCard.SetSelected(false);
        }

        if (isSelected)
        {
            SetSelected(false);
            currentSelectedCard = null;
            KTH_InfoPanl.Instance.CancleInfoPanl();
        }
        else
        {
            SetSelected(true);
            currentSelectedCard = this;
            KTH_InfoPanl.Instance.StartInfoPanl(cardData, this);
        }

        OnCardClicked?.Invoke(this);
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        outlineImage.gameObject.SetActive(isSelected);

        transform.DOKill();

        if (isSelected)
        {
            // 1. 현재 렌더링 순서 저장 및 UI 최상단으로 이동
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();

            // 2. 중앙(X=0), 위로 올라옴(Y=selectOffsetY), 정방향 회전(Z=0), 크기 확대 연출
            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOLocalMove(new Vector3(0f, selectOffsetY, 0f), selectDuration).SetEase(Ease.OutBack))
               .Join(transform.DOLocalRotate(Vector3.zero, selectDuration).SetEase(Ease.OutCubic))
               .Join(transform.DOScale(Vector3.one * selectScale, selectDuration).SetEase(Ease.OutBack));
        }
        else
        {
            // 1. 원래 렌더링 순서로 복원
            transform.SetSiblingIndex(originalSiblingIndex);

            // 2. 원래 손패 위치, 회전, 스케일(1,1,1)로 복원
            Sequence seq = DOTween.Sequence();
            seq.Join(transform.DOLocalMove(originalLocalPos, selectDuration).SetEase(Ease.OutCubic))
               .Join(transform.DOLocalRotate(new Vector3(0, 0, originalZRotation), selectDuration).SetEase(Ease.OutCubic))
               .Join(transform.DOScale(Vector3.one, selectDuration).SetEase(Ease.OutCubic));
        }
    }

    public void SetSpawnPosition(Vector3 worldPos)
    {
        transform.position = worldPos;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public void MoveToHandPosition(Vector3 localPos, float zRotation, float duration = 0.35f)
    {
        // 선택되어 있는 동안에는 레이아웃 재정렬로 위치가 덮어씌워지지 않도록 함
        originalLocalPos = localPos;
        originalZRotation = zRotation;

        if (isSelected) return;

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOLocalMove(localPos, duration).SetEase(Ease.OutCubic))
                .Join(transform.DOLocalRotate(new Vector3(0, 0, zRotation), duration).SetEase(Ease.OutCubic));
    }

    public void PlayDrawAnimation(Vector3 targetLocalPos, float targetZRotation, float duration = 0.4f)
    {
        originalLocalPos = targetLocalPos;
        originalZRotation = targetZRotation;

        transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOLocalMove(targetLocalPos, duration).SetEase(Ease.OutCubic))
                .Join(transform.DOLocalRotate(new Vector3(0, 0, targetZRotation), duration).SetEase(Ease.OutCubic))
                .Join(transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 소환이 확정된 카드를 손패에서 제거하고 재정렬한다.
    /// discardPile이 주어지면 그쪽으로 날아가며 사라지고, 없으면 즉시 파괴된다.
    /// KTH_InfoPanl의 Select 버튼(보드 배치 완료) 콜백에서 호출된다.
    /// </summary>
    public void ConsumeAndRearrange(KTH_DiscardCardUI discardPile = null)
    {
        transform.DOKill();

        if (currentSelectedCard == this)
            currentSelectedCard = null;

        // 손패 목록에서는 즉시 제거하고 재정렬 (남은 카드들이 바로 자리를 채우도록)
        if (KTH_HandCardLayout.Instance != null)
            KTH_HandCardLayout.Instance.RemoveCard(this);

        if (discardPile != null && discardPile.DiscardCardTransform != null)
        {
            PlayDiscardAnimation(discardPile);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 버린 카드 더미 위치로 날아가며 축소·회전·페이드되다가 파괴된다.
    /// </summary>
    private void PlayDiscardAnimation(KTH_DiscardCardUI discardPile)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        // 날아가는 동안 클릭 등 입력 차단
        cg.blocksRaycasts = false;

        // 버린 카드 더미와 동일한 부모로 옮겨서 좌표계를 맞춘다 (worldPositionStays: true로 현재 화면 위치는 유지)
        Transform discardParent = discardPile.DiscardCardTransform.parent;
        transform.SetParent(discardParent, worldPositionStays: true);

        // 목표는 버린 카드 더미의 로컬 좌표
        Vector3 targetLocalPos = discardPile.DiscardCardTransform.localPosition;

        float randomTilt = UnityEngine.Random.Range(-30f, 30f);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOLocalMove(targetLocalPos, discardDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOScale(Vector3.zero, discardDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, randomTilt), discardDuration, RotateMode.FastBeyond360));

        seq.OnComplete(() =>
        {
            discardPile.AddToDiscardPile(cardData);
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        transform.DOKill();

        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }
    }
}