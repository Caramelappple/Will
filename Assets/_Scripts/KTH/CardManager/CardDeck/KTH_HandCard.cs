using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class KTH_HandCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI cost;
    [SerializeField] private TextMeshProUGUI power;

    [Header("Select Animation Settings")]
    [Tooltip("선택 시 크기 확대 비율")]
    [SerializeField] private float selectScale = 1.25f;

    [Tooltip("선택 시 위로 올라가는 거리")]
    [SerializeField] private float selectMoveY = 30f;

    [Tooltip("선택 애니메이션 지속 시간")]
    [SerializeField] private float selectDuration = 0.25f;


    [Header("Discard Animation Settings")]
    [SerializeField] private float discardDuration = 0.35f;


    [Header("Hover Info Settings")]
    [SerializeField] private float infoHoverDelay = 0.5f;

    private Coroutine infoHoverCoroutine;
    
    private LSO_CardSO cardData;
    private bool isSelected;

    private int originalSiblingIndex;
    private Vector3 originalLocalPos;
    private float originalZRotation;

    private static KTH_HandCard currentSelectedCard;

    public LSO_CardSO CardData => cardData;

    public event Action<KTH_HandCard> OnCardClicked;

    public void Setup(LSO_CardSO data)
    {
        cardData = data;
        SettingUi();
    }

    private void Start()
    {
        if (cardData != null)
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
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSelectedCard != null) return;
        if (cardData == null) return;

        infoHoverCoroutine = StartCoroutine(ShowInfoAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHoverCoroutine();

        if (currentSelectedCard != null) return;

        if (KTH_InfoPanl.Instance != null)
            KTH_InfoPanl.Instance.CancleInfoPanl();
    }
    
    private System.Collections.IEnumerator ShowInfoAfterDelay()
    {
        yield return new WaitForSeconds(infoHoverDelay);

        // 기다리는 동안 다른 카드가 선택됐다면 취소
        if (currentSelectedCard != null) yield break;

        if (cardData == null) yield break;

        KTH_InfoPanl.Instance.StartInfoPanl(cardData, this);

        infoHoverCoroutine = null;
    }

    private void StopHoverCoroutine()
    {
        if (infoHoverCoroutine != null)
        {
            StopCoroutine(infoHoverCoroutine);
            infoHoverCoroutine = null;
        }
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

            if (KTH_InfoPanl.Instance != null)
                KTH_InfoPanl.Instance.CancleInfoPanl();
        }
        else
        {
            SetSelected(true);
            currentSelectedCard = this;

            if (KTH_InfoPanl.Instance != null)
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
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();

            Vector3 targetPos = originalLocalPos;
            targetPos.y += selectMoveY;

            Sequence seq = DOTween.Sequence();

            seq.Join(
                transform.DOLocalMove(targetPos, selectDuration)
                    .SetEase(Ease.OutBack)
            );

            seq.Join(
                transform.DOScale(Vector3.one * selectScale, selectDuration)
                    .SetEase(Ease.OutBack)
            );
        }
        else
        {
            transform.SetSiblingIndex(originalSiblingIndex);

            Sequence seq = DOTween.Sequence();

            seq.Join(
                transform.DOLocalMove(originalLocalPos, selectDuration)
                    .SetEase(Ease.OutCubic)
            );

            seq.Join(
                transform.DOLocalRotate(
                    new Vector3(0, 0, originalZRotation),
                    selectDuration
                ).SetEase(Ease.OutCubic)
            );

            seq.Join(
                transform.DOScale(Vector3.one, selectDuration)
                    .SetEase(Ease.OutCubic)
            );
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

    public void ConsumeAndRearrange(KTH_DiscardCardUI discardPile = null)
    {
        transform.DOKill();

        if (currentSelectedCard == this)
            currentSelectedCard = null;

        if (KTH_HandCardLayout.Instance != null)
            KTH_HandCardLayout.Instance.RemoveCard(this);

        if (discardPile != null && discardPile.DiscardCardTransform != null)
        {
            PlayDiscardAnimation(discardPile);
        }
        else
        {
            Debug.LogWarning($"[KTH_HandCard] discardPile이 비어있어 '{(cardData != null ? cardData.name : "Unknown")}' 카드가 버린 카드 더미에 기록되지 않고 파괴됩니다! Inspector 연결을 확인하세요.");
            Destroy(gameObject);
        }
    }

    private void PlayDiscardAnimation(KTH_DiscardCardUI discardPile)
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false;

        Transform discardParent = discardPile.DiscardCardTransform.parent;
        transform.SetParent(discardParent, worldPositionStays: true);

        Vector3 targetLocalPos = discardPile.DiscardCardTransform.localPosition;

        float randomTilt = UnityEngine.Random.Range(-30f, 30f);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOLocalMove(targetLocalPos, discardDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOScale(Vector3.zero, discardDuration).SetEase(Ease.InQuad));
        seq.Join(transform.DOLocalRotate(new Vector3(0f, 0f, randomTilt), discardDuration, RotateMode.FastBeyond360));
        seq.Join(cg.DOFade(0f, discardDuration * 0.85f));

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
    public static void DeselectCurrent()
    {
        if (currentSelectedCard == null) return;

        currentSelectedCard = null;

        if (KTH_InfoPanl.Instance != null)
            KTH_InfoPanl.Instance.CancleInfoPanl();
    }
}