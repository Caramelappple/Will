using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // ⭐ DOTween 네임스페이스

public class KTH_RewardChoiceUI : MonoBehaviour
{
    [Header("보상 UI 프리팹 및 부모")]
    [SerializeField] private KTH_RewardOptionUI rewardPrefab;
    [SerializeField] private Transform rewardCanvas; // 카드 컨테이너 빈 오브젝트

    [Header("제어할 UI Group")]
    [SerializeField] private CanvasGroup canvasGroup; // Fade 효과용 (없으면 자동 추가됨)
    [SerializeField] private Transform popUpPanel;    // Scale 팝업 효과를 줄 패널/창

    [Header("하단 보상 획득 버튼")]
    [SerializeField] private Button claimButton;

    private readonly List<KTH_RewardOptionUI> spawnedRewards = new();
    private KTH_RewardOptionUI currentlySelectedUI;
    private bool isClaimed = false;

    public static KTH_RewardChoiceUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (popUpPanel == null)
        {
            popUpPanel = transform;
        }

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClickClaimButton);
            claimButton.interactable = false;
        }
    }

    // =========================================================
    // 보상 UI 등장 애니메이션
    // =========================================================
    public void ShowRewards(List<KTH_RewardOption> options)
    {
        gameObject.SetActive(true);

        ClearRewards();
        isClaimed = false;
        currentlySelectedUI = null;

        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("[KTH_RewardChoiceUI] 표시할 보상이 없습니다.");
            return;
        }

        // 1. 카드 생성
        foreach (KTH_RewardOption option in options)
        {
            if (option == null) continue;

            KTH_RewardOptionUI rewardUI = Instantiate(rewardPrefab, rewardCanvas);
            rewardUI.SetReward(option, this);
            spawnedRewards.Add(rewardUI);
        }

        // Layout 즉시 강제 갱신
        if (rewardCanvas is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // 2. DOTween 등장 연출 시작
        PlayShowAnimation();
    }

    private void PlayShowAnimation()
    {
        // 이전 트윈 중단
        popUpPanel.DOKill();
        canvasGroup.DOKill();

        // 초기 상태 설정
        canvasGroup.alpha = 0f;
        popUpPanel.localScale = Vector3.one * 0.7f;

        // Sequence로 창 등장 후 카드 순차 연출
        Sequence showSequence = DOTween.Sequence();

        // 팝업 창 FadeIn + ScaleUp
        showSequence.Append(canvasGroup.DOFade(1f, 0.25f));
        showSequence.Join(popUpPanel.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));

        // 카드들 순차적으로 애니메이션 실행
        showSequence.AppendCallback(() =>
        {
            for (int i = 0; i < spawnedRewards.Count; i++)
            {
                spawnedRewards[i].PlaySpawnAnimation(i * 0.08f); // 0.08초 시차 등장
            }
        });
    }

    // =========================================================
    // 카드 선택 처리
    // =========================================================
    public void OnSelectCard(KTH_RewardOptionUI selectedUI)
    {
        if (isClaimed) return;

        foreach (var ui in spawnedRewards)
        {
            ui.SetSelected(ui == selectedUI);
        }

        currentlySelectedUI = selectedUI;

        if (claimButton != null)
        {
            claimButton.interactable = true;
        }
    }

    // =========================================================
    // 하단 [보상 획득] 클릭 시 퇴장 애니메이션
    // =========================================================
    private void OnClickClaimButton()
    {
        if (isClaimed || currentlySelectedUI == null) return;

        isClaimed = true;

        KTH_RewardOption selectedOption = currentlySelectedUI.Option;
        Debug.Log($"🎁 [KTH_RewardChoiceUI] 최종 보상 획득: {selectedOption.GetName()}");

        if (KTH_Reward.Instance != null)
        {
            KTH_Reward.Instance.ClaimReward(selectedOption);
        }

        // DOTween 퇴장 연출 후 UI 닫기
        PlayHideAnimation();
    }

    private void PlayHideAnimation()
    {
        popUpPanel.DOKill();
        canvasGroup.DOKill();

        Sequence hideSequence = DOTween.Sequence();

        hideSequence.Append(popUpPanel.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InBack));
        hideSequence.Join(canvasGroup.DOFade(0f, 0.2f));

        hideSequence.OnComplete(() =>
        {
            ClearRewards();
            gameObject.SetActive(false);
        });
    }

    private void ClearRewards()
    {
        foreach (KTH_RewardOptionUI reward in spawnedRewards)
        {
            if (reward != null)
            {
                reward.transform.DOKill();
                Destroy(reward.gameObject);
            }
        }

        spawnedRewards.Clear();
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(OnClickClaimButton);

        popUpPanel.DOKill();
        canvasGroup.DOKill();
    }
}