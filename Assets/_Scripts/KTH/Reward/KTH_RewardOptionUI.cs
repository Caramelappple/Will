using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Reward;
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

    private LSO_RewardOption rewardOption;
    private KTH_RewardChoiceUI owner;

    private Button cardButton;
    private RectTransform rectTransform;

    // LayoutGroup이 결정한 원래 위치
    private Vector2 basePosition;

    public LSO_RewardOption Option => rewardOption;

    private void Awake()
    {
        cardButton = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnClickCard);
        }

        if (highlightOutline != null)
        {
            highlightOutline.SetActive(false);
        }
    }

    public void SetReward(
        LSO_RewardOption option,
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

        if (option.type == LSO_RewardType.Piece)
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
                    nameText.text = will.WillType.ToString();
                }

                if (descriptionText != null)
                {
                    descriptionText.text = will.description;
                }

                if (iconImage != null)
                {
                    iconImage.sprite = will.icon;
                    iconImage.enabled = will.icon != null;
                }

                if (statText1 != null)
                {
                    statText1.text = $"피해량 : {will.DisplayDamage}";
                }

                if (statText2 != null)
                {
                    statText2.text = $"범위 : {will.DisplayRange}";
                }

                if (statText3 != null)
                {
                    statText3.text = $"지속시간 : {will.DisplayDuration}";
                }

                if (statText4 != null)
                {
                    statText4.text =
                        will.DisplayBuffAmount != 0
                            ? $"버프 : {will.DisplayBuffAmount}"
                            : "";
                }

                if (statText5 != null)
                {
                    statText5.text =
                        will.DisplayDebuffAmount != 0
                            ? $"디버프 : {will.DisplayDebuffAmount}"
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

        return "특성 : " +
               string.Join(", ", abilityTypes.Select(a => a.ToString()));
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

        rectTransform.DOKill();

        if (cardButton != null)
        {
            cardButton.interactable = false;
        }

        // 현재 위치를 원래 위치로 저장
        basePosition = rectTransform.anchoredPosition;

        // 아래에서 시작
        rectTransform.anchoredPosition =
            basePosition + Vector2.down * spawnMoveDistance;

        // 하나씩 원래 위치로 올라옴
        rectTransform
            .DOAnchorPos(basePosition, spawnDuration)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                rectTransform.anchoredPosition = basePosition;

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
        if (rewardOption == null || owner == null)
        {
            return;
        }

        // 클릭 연출
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

        rectTransform.anchoredPosition = basePosition;

        Sequence sequence = DOTween.Sequence();
        sequence.SetLink(gameObject);

        // 위로
        sequence.Append(
            rectTransform
                .DOAnchorPos(
                    basePosition +
                    Vector2.up * selectMoveDistance,
                    selectUpDuration)
                .SetEase(Ease.OutQuad)
        );

        // 다시 아래로
        sequence.Append(
            rectTransform
                .DOAnchorPos(
                    basePosition,
                    selectDownDuration)
                .SetEase(Ease.InQuad)
        );

        sequence.OnComplete(() =>
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = basePosition;
            }
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

        float targetScale = isSelected ? 1.08f : 1f;

        transform
            .DOScale(Vector3.one * targetScale, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
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