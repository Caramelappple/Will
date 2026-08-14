using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // ⭐ DOTween 네임스페이스

[RequireComponent(typeof(Button))]
public class KTH_RewardOptionUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private GameObject highlightOutline;

    private KTH_RewardOption rewardOption;
    private KTH_RewardChoiceUI owner;
    private Button cardButton;
    private CanvasGroup canvasGroup;

    public KTH_RewardOption Option => rewardOption;

    private void Awake()
    {
        cardButton = GetComponent<Button>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnClickCard);
        }
    }

    public void SetReward(KTH_RewardOption option, KTH_RewardChoiceUI ownerUI)
    {
        rewardOption = option;
        owner = ownerUI;

        if (nameText != null)
            nameText.text = option.GetName();

        if (typeText != null)
        {
            typeText.text = option.type == KTH_RewardType.Piece ? "기물" : "유언";
        }

        SetSelected(false);
    }

    // ⭐ 카드 생성 시 아래에서 위로 나타나는 애니메이션
    public void PlaySpawnAnimation(float delay)
    {
        transform.DOKill();

        canvasGroup.alpha = 0f;
        Vector3 defaultScale = Vector3.one;
        transform.localScale = defaultScale * 0.8f;

        Sequence seq = DOTween.Sequence();
        seq.SetDelay(delay);
        seq.Append(canvasGroup.DOFade(1f, 0.25f));
        seq.Join(transform.DOScale(defaultScale, 0.25f).SetEase(Ease.OutBack));
    }

    private void OnClickCard()
    {
        if (rewardOption == null || owner == null)
            return;

        // 클릭 시 톡 튀어 나오는 반응 연출
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 1f);

        owner.OnSelectCard(this);
    }

    public void SetSelected(bool isSelected)
    {
        if (highlightOutline != null)
        {
            highlightOutline.SetActive(isSelected);
        }

        // 선택 강조 시 카드 살짝 확대
        transform.DOKill();
        if (isSelected)
        {
            transform.DOScale(Vector3.one * 1.05f, 0.15f).SetEase(Ease.OutQuad);
        }
        else
        {
            transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutQuad);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (cardButton != null)
            cardButton.onClick.RemoveListener(OnClickCard);
    }
}