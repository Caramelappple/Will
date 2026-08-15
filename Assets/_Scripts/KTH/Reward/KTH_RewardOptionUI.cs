using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Will;

[RequireComponent(typeof(Button))]
public class KTH_RewardOptionUI : MonoBehaviour
{
    [Header("UI 연결 - 공통")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject highlightOutline;

    [Header("UI 연결 - 스탯 (기물/유언 공용)")]
    [Tooltip("기물/유언 둘 다 항목이 5개라 텍스트 5칸을 공용으로 재사용한다.\n" +
             "기물: ATK / HP / 이동 / 사거리 / 특성\n" +
             "유언: 피해량 / 범위 / 지속시간 / 버프 / 디버프")]
    [SerializeField] private GameObject statRoot;
    [SerializeField] private TMP_Text statText1;
    [SerializeField] private TMP_Text statText2;
    [SerializeField] private TMP_Text statText3;
    [SerializeField] private TMP_Text statText4;
    [SerializeField] private TMP_Text statText5;

    [Header("등장 연출")]
    [Tooltip("카드가 아래에서 올라오는 거리")]
    [SerializeField] private float spawnMoveDistance = 25f;

    [Tooltip("등장 이동 시간")]
    [SerializeField] private float spawnDuration = 0.25f;

    [Header("선택 연출")]
    [SerializeField] private float selectMoveDistance = 50f;
    [SerializeField] private float selectUpDuration = 0.12f;
    [SerializeField] private float selectDownDuration = 0.18f;

    private KTH_RewardOption rewardOption;
    private KTH_RewardChoiceUI owner;

    private Button cardButton;
    private RectTransform rectTransform;

    // LayoutGroup이 결정한 원래 위치
    private Vector2 basePosition;

    public KTH_RewardOption Option => rewardOption;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnClickCard);
        }
    }

    public void SetReward(
        KTH_RewardOption option,
        KTH_RewardChoiceUI ownerUI)
    {
        rewardOption = option;
        owner = ownerUI;

        if (option == null)
        {
            return;
        }

        // ==========================================
        // 기물
        // ==========================================

        if (option.type == KTH_RewardType.Piece)
        {
            if (statRoot != null)
            {
                statRoot.SetActive(true);
            }

            if (option.piece != null &&
                option.piece.Animal != null)
            {
                var animal = option.piece.Animal;

                if (nameText != null)
                {
                    nameText.text = animal.animalName;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = animal.description;
                }

                if (iconImage != null)
                {
                    // 일러스트는 AnimalSO가 아니라 CardSO(option.piece)의 Image에 있음.
                    Sprite cardImage = option.piece.Image;
                    iconImage.sprite = cardImage;
                    iconImage.enabled = cardImage != null;
                }

                if (statText1 != null)
                {
                    statText1.text = $"ATK {animal.damage}";
                }

                if (statText2 != null)
                {
                    statText2.text = $"HP {animal.maxHealth}";
                }

                if (statText3 != null)
                {
                    statText3.text = $"이동 {animal.MoveRange}";
                }

                if (statText4 != null)
                {
                    // LDY_RangeType enum을 그대로 텍스트로 노출.
                    // 화면에 보여줄 한글 라벨이 따로 있다면 매핑 함수로 교체.
                    statText4.text = $"사거리 {animal.range}";
                }

                if (statText5 != null)
                {
                    statText5.text = BuildTraitText(animal.AbilityTypes);
                }
            }
            else
            {
                if (nameText != null)
                {
                    nameText.text = "알 수 없는 기물";
                }

                if (descriptionText != null)
                {
                    descriptionText.text = "";
                }

                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }

                if (statText1 != null) statText1.text = "ATK -";
                if (statText2 != null) statText2.text = "HP -";
                if (statText3 != null) statText3.text = "이동 -";
                if (statText4 != null) statText4.text = "사거리 -";
                if (statText5 != null) statText5.text = "특성: -";
            }
        }

        // ==========================================
        // 유언
        // ==========================================

        else
        {
            if (statRoot != null)
            {
                statRoot.SetActive(true);
            }

            DLJ_WillDataSO will = option.will;

            if (will != null)
            {
                if (nameText != null)
                {
                    // NOTE: 이름은 willType 이넘 값을 그대로 사용.
                    // 한글 표시명이 필요하면 이넘 -> 한글 매핑으로 교체.
                    nameText.text = will.willType.ToString();
                }

                if (descriptionText != null)
                {
                    // NOTE: DLJ_WillDataSO에 description 필드가 추가된다는 전제.
                    descriptionText.text = will.description;
                }

                if (iconImage != null)
                {
                    // NOTE: DLJ_WillDataSO에 icon(Sprite) 필드가 추가된다는 전제.
                    iconImage.sprite = will.icon;
                    iconImage.enabled = will.icon != null;
                }

                if (statText1 != null)
                {
                    statText1.text = $"피해량 : {will.damage}";
                }

                if (statText2 != null)
                {
                    statText2.text = $"범위 : {will.range}";
                }

                if (statText3 != null)
                {
                    statText3.text = $"지속시간 : {will.duration}";
                }

                // NOTE: buffAmount / debuffAmount도 DLJ_WillDataSO에 추가된다는 전제.
                // 값이 0이면 해당 항목 자체가 없는 유언으로 보고 텍스트를 비움.
                if (statText4 != null)
                {
                    statText4.text =
                        will.buffAmount != 0
                            ? $"버프 : {will.buffAmount}"
                            : "";
                }

                if (statText5 != null)
                {
                    statText5.text =
                        will.debuffAmount != 0
                            ? $"디버프 : {will.debuffAmount}"
                            : "";
                }
            }
            else
            {
                if (nameText != null)
                {
                    nameText.text = "알 수 없는 유언";
                }

                if (descriptionText != null)
                {
                    descriptionText.text = "";
                }

                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }

                if (statText1 != null) statText1.text = "피해량 -";
                if (statText2 != null) statText2.text = "범위 -";
                if (statText3 != null) statText3.text = "지속시간 -";
                if (statText4 != null) statText4.text = "";
                if (statText5 != null) statText5.text = "";
            }
        }

        SetSelected(false);
    }

    // ==========================================
    // 특성 텍스트 조합
    // ==========================================

    private static string BuildTraitText(
        System.Collections.Generic.IReadOnlyList<LSO_AbilityType> abilityTypes)
    {
        if (abilityTypes == null || abilityTypes.Count == 0)
        {
            return "특성: 없음";
        }

        // NOTE: LSO_AbilityType이 enum이라 ToString()은 영문 enum 이름 그대로 나옴.
        // 화면에 한글로 보여주려면 enum -> 한글 이름 매핑 딕셔너리/함수로 교체.
        return "특성 : " + string.Join(", ", abilityTypes.Select(a => a.ToString()));
    }

    // ==========================================
    // 카드 등장 연출
    // ==========================================

    public void PlaySpawnAnimation(float delay)
    {
        if (rectTransform == null)
        {
            return;
        }

        // 기존 위치 이동 Tween 제거
        rectTransform.DOKill();

        if (cardButton != null)
        {
            cardButton.interactable = false;
        }

        // RewardChoiceUI에서 생성 직후
        // scale = 0으로 만들어 놓기 때문에 반드시 복구
        transform.localScale = Vector3.one;

        // LayoutGroup이 결정한 현재 위치를 기준 위치로 저장
        basePosition = rectTransform.anchoredPosition;

        // 처음에는 살짝 아래
        rectTransform.anchoredPosition =
            basePosition +
            Vector2.down * spawnMoveDistance;

        // 아래 → 원래 위치
        rectTransform
            .DOAnchorPos(
                basePosition,
                spawnDuration
            )
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 마지막 위치 확실하게 고정
                rectTransform.anchoredPosition =
                    basePosition;

                if (cardButton != null)
                {
                    cardButton.interactable = true;
                }
            });
    }

    // ==========================================
    // 카드 클릭
    // ==========================================

    private void OnClickCard()
    {
        if (rewardOption == null ||
            owner == null)
        {
            return;
        }

        // 클릭할 때 살짝 올라왔다가 내려오는 연출
        PlaySelectAnimation();

        // 실제 선택 처리
        owner.OnSelectCard(this);
    }

    // ==========================================
    // 선택 연출
    // ==========================================

    private void PlaySelectAnimation()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.DOKill();

        // 혹시 등장 연출 직후 클릭되는 경우를 대비해
        // 현재 위치가 basePosition과 다르더라도 기준 위치로 복귀
        rectTransform.anchoredPosition =
            basePosition;

        Sequence sequence = DOTween.Sequence();

        // 위로 살짝
        sequence.Append(
            rectTransform.DOAnchorPos(
                basePosition +
                Vector2.up * selectMoveDistance,
                selectUpDuration
            )
            .SetEase(Ease.OutQuad)
        );

        // 다시 원래 위치
        sequence.Append(
            rectTransform.DOAnchorPos(
                basePosition,
                selectDownDuration
            )
            .SetEase(Ease.InQuad)
        );

        sequence.OnComplete(() =>
        {
            rectTransform.anchoredPosition =
                basePosition;
        });
    }

    // ==========================================
    // 선택 상태
    // ==========================================

    public void SetSelected(bool isSelected)
    {
        if (highlightOutline != null)
        {
            highlightOutline.SetActive(isSelected);
        }

        DOTween.Kill(transform);

        float targetScale = isSelected ? 1.08f : 1.0f;

        transform
            .DOScale(Vector3.one * targetScale, 0.15f)
            .SetEase(Ease.OutQuad);
    }

    // ==========================================
    // 정리
    // ==========================================

    private void OnDestroy()
    {
        if (rectTransform != null)
        {
            rectTransform.DOKill();
        }

        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnClickCard);
        }
    }
}