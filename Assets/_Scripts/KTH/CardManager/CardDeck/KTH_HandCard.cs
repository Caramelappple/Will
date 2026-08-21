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

    private LSO_CardSO cardData;
    private bool isSelected;

    // 현재 선택된 카드 하나만 기억 (전체 카드가 공유)
    private static KTH_HandCard currentSelectedCard;

    public LSO_CardSO CardData => cardData;

    // 카드가 클릭됐을 때 외부에 알리는 이벤트
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
            Debug.Log("클릭");
            SetSelected(false);
            currentSelectedCard = null;
            KTH_InfoPanl.Instance.CancleInfoPanl();
        }
        else
        {
            SetSelected(true);
            currentSelectedCard = this;
            KTH_InfoPanl.Instance.StartInfoPanl(cardData);
        }

        OnCardClicked?.Invoke(this);
    }

    public void SetSelected(bool value)
    {
        isSelected = value;
        outlineImage.gameObject.SetActive(isSelected);
    }

    /// <summary>
    /// 핸드 레이아웃에서 지정한 부채꼴 좌표로 부드럽게 이동합니다.
    /// </summary>
    public void MoveToHandPosition(Vector3 localPos, float zRotation, float duration = 0.35f)
    {
        transform.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.Join(transform.DOLocalMove(localPos, duration).SetEase(Ease.OutCubic))
                .Join(transform.DOLocalRotate(new Vector3(0, 0, zRotation), duration).SetEase(Ease.OutCubic));
    }

    private void OnDestroy()
    {
        transform.DOKill();

        // 파괴될 때 자신이 선택된 카드였다면 참조 정리
        if (currentSelectedCard == this)
        {
            currentSelectedCard = null;
        }
    }
}