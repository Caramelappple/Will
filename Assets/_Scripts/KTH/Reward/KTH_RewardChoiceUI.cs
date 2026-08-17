using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KTH_RewardChoiceUI : MonoBehaviour
{
    [Header("보상 UI 프리팹 및 부모")]
    [SerializeField] private KTH_RewardOptionUI rewardPrefab;
    [SerializeField] private Transform rewardCanvas;

    [Header("제어할 UI Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Transform popUpPanel;

    [Header("백그라운드로 취급할 이미지(버튼)")]
    [SerializeField] private Transform backgroundVisual;

    [Header("하단 보상 획득 버튼")]
    [SerializeField] private Button claimButton;

    [Header("배경 애니메이션")]
    [SerializeField] private float bgScaleDuration = 0.5f;

    [Header("카드 생성 설정")]
    [Tooltip("카드 프리팹이 생성되는 사이의 간격")]
    [SerializeField] private float cardInstantiateInterval = 0.05f;

    [Header("카드 등장 애니메이션")]
    [Tooltip("카드 등장 애니메이션이 시작되기 전 대기 시간")]
    [SerializeField] private float delayBeforeCardSpawn = 0.15f;

    [Tooltip("카드 팝업 등장 사이의 간격")]
    [SerializeField] private float cardSpawnInterval = 0.15f;

    private readonly List<KTH_RewardOptionUI> spawnedRewards = new();

    private KTH_RewardOptionUI currentlySelectedUI;

    private bool isClaimed = false;

    public event System.Action OnRewardResolved;

    private static KTH_RewardChoiceUI instance;

    public static KTH_RewardChoiceUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<KTH_RewardChoiceUI>(
                    FindObjectsInactive.Include
                );

                if (instance == null)
                {
                    var allObjects =
                        Resources.FindObjectsOfTypeAll<KTH_RewardChoiceUI>();

                    foreach (var obj in allObjects)
                    {
                        if (obj.gameObject.scene.isLoaded ||
                            obj.gameObject.hideFlags == HideFlags.None)
                        {
                            instance = obj;
                            break;
                        }
                    }
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        // 중복 인스턴스 제거
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad는 루트 오브젝트에서만 가능
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClickClaimButton);
            claimButton.interactable = false;
        }

        ResetUIState();

        gameObject.SetActive(false);
    }

    public void ShowRewards(List<KTH_RewardOption> options)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopAllCoroutines();

        ClearRewards();

        isClaimed = false;
        currentlySelectedUI = null;

        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        if (options == null || options.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_RewardChoiceUI] 표시할 보상이 없습니다."
            );

            gameObject.SetActive(false);

            OnRewardResolved?.Invoke();

            return;
        }

        // 배경 애니메이션 초기화
        Transform bgTarget =
            backgroundVisual != null
                ? backgroundVisual
                : popUpPanel;

        if (bgTarget != null)
        {
            bgTarget.DOKill();
            bgTarget.localScale = Vector3.zero;
        }

        // CanvasGroup 초기화
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        StartCoroutine(Co_ShowSequence(options));
    }

    private IEnumerator Co_ShowSequence(
        List<KTH_RewardOption> options)
    {
        Transform bgTarget =
            backgroundVisual != null
                ? backgroundVisual
                : popUpPanel;

        // ==========================================
        // 1. 배경 등장
        // ==========================================

        if (bgTarget != null)
        {
            Tweener bgTween = bgTarget
                .DOScale(Vector3.one, bgScaleDuration)
                .SetEase(Ease.OutBack);

            yield return bgTween.WaitForCompletion();
        }

        // ==========================================
        // 2. 카드 프리팹을 조금씩 간격을 두고 생성
        // ==========================================

        foreach (KTH_RewardOption option in options)
        {
            if (option == null)
                continue;

            KTH_RewardOptionUI rewardUI =
                Instantiate(rewardPrefab, rewardCanvas);

            rewardUI.SetReward(option, this);

            rewardUI.transform.localScale = Vector3.zero;

            spawnedRewards.Add(rewardUI);

            // 다음 프리팹 생성까지 대기
            if (cardInstantiateInterval > 0f)
            {
                yield return new WaitForSeconds(
                    cardInstantiateInterval
                );
            }
        }

        // ==========================================
        // 3. 레이아웃 갱신
        // ==========================================

        if (rewardCanvas is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                rectTransform
            );
        }

        // 레이아웃 반영
        yield return null;

        // ==========================================
        // 4. 카드 등장 전 대기
        // ==========================================

        if (delayBeforeCardSpawn > 0f)
        {
            yield return new WaitForSeconds(
                delayBeforeCardSpawn
            );
        }

        // ==========================================
        // 5. 카드 순차 팝업
        // ==========================================

        for (int i = 0; i < spawnedRewards.Count; i++)
        {
            KTH_RewardOptionUI rewardUI =
                spawnedRewards[i];

            if (rewardUI != null)
            {
                rewardUI.PlaySpawnAnimation(0f);
            }

            if (cardSpawnInterval > 0f)
            {
                yield return new WaitForSeconds(
                    cardSpawnInterval
                );
            }
        }
    }

    public void OnSelectCard(
        KTH_RewardOptionUI selectedUI)
    {
        if (isClaimed)
            return;

        foreach (KTH_RewardOptionUI ui in spawnedRewards)
        {
            if (ui != null)
            {
                ui.SetSelected(ui == selectedUI);
            }
        }

        currentlySelectedUI = selectedUI;

        if (claimButton != null)
        {
            claimButton.interactable = true;
        }
    }

    private void OnClickClaimButton()
    {
        if (isClaimed ||
            currentlySelectedUI == null)
        {
            return;
        }

        isClaimed = true;

        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        KTH_RewardOption selectedOption =
            currentlySelectedUI.Option;

        Debug.Log(
            $"🎁 [KTH_RewardChoiceUI] 최종 보상 획득: {selectedOption.GetName()}"
        );

        if (KTH_Reward.Instance != null)
        {
            KTH_Reward.Instance.ClaimReward(
                selectedOption
            );
        }

        PlayHideAnimation();
    }

    private void PlayHideAnimation()
    {
        Transform bgTarget =
            backgroundVisual != null
                ? backgroundVisual
                : popUpPanel;

        if (bgTarget != null)
        {
            bgTarget.DOKill();
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
        }

        Sequence hideSequence = DOTween.Sequence();

        foreach (KTH_RewardOptionUI rewardUI in spawnedRewards)
        {
            if (rewardUI != null)
            {
                hideSequence.Join(
                    rewardUI.transform
                        .DOScale(
                            Vector3.zero,
                            0.2f
                        )
                        .SetEase(Ease.InBack)
                );
            }
        }

        if (bgTarget != null)
        {
            hideSequence.Append(
                bgTarget
                    .DOScale(
                        Vector3.zero,
                        0.25f
                    )
                    .SetEase(Ease.InOutQuad)
            );
        }

        hideSequence.OnComplete(() =>
        {
            ClearRewards();

            gameObject.SetActive(false);

            OnRewardResolved?.Invoke();
        });
    }

    private void ResetUIState()
    {
        Transform bgTarget =
            backgroundVisual != null
                ? backgroundVisual
                : popUpPanel;

        if (bgTarget != null)
        {
            bgTarget.DOKill();
            bgTarget.localScale = Vector3.zero;
        }

        if (claimButton != null)
        {
            claimButton.interactable = false;
        }
    }

    private void ClearRewards()
    {
        foreach (KTH_RewardOptionUI reward in spawnedRewards)
        {
            if (reward != null)
            {
                DOTween.Kill(reward.transform);

                Destroy(reward.gameObject);
            }
        }

        spawnedRewards.Clear();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(
                OnClickClaimButton
            );
        }

        Transform bgTarget =
            backgroundVisual != null
                ? backgroundVisual
                : popUpPanel;

        if (bgTarget != null)
        {
            bgTarget.DOKill();
        }
    }
}