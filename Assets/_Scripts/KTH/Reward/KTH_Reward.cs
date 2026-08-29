using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Reward;
using _Scripts.LSO.Will;

public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    /// <summary>
    /// Reload Domain을 끈 에디터에서는 static이 플레이를 멈춰도 살아남는다.
    /// 지난 플레이의 값이 남아 있으면 두 번째 실행부터 엉뚱하게 동작하므로,
    /// 씬이 로드되기 전에 직접 비운다. LDY_RunSeed와 같은 이유다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [Header("스테이지별 해금 테이블")]
    [SerializeField] private LSO_RewardTableSO rewardTable;

    public event Action<List<LSO_RewardOption>> RewardOptionsGenerated;
    public event Action<LSO_UnlockState> Unlocked;

    private List<LSO_RewardOption> currentOptions = new();
    private LSO_StageRewardData currentStageData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[KTH_Reward] Instance 생성");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // 보상 후보 생성
    // =========================================================
    public List<LSO_RewardOption> GenerateRewardOptions(int chapter, int stage)
    {
        Debug.Log($"🎁 [KTH_Reward] 보상 후보 생성: Chapter {chapter}, Stage {stage}");

        currentOptions.Clear();

        if (rewardTable == null)
        {
            Debug.LogError("[KTH_Reward] RewardTable이 할당되지 않았습니다!");
            return currentOptions;
        }

        LSO_StageRewardData stageData = rewardTable.Find(chapter, stage);

        if (stageData == null)
        {
            Debug.LogWarning($"[KTH_Reward] Chapter {chapter}, Stage {stage} 데이터를 찾을 수 없습니다.");
            return currentOptions;
        }

        currentStageData = stageData;
        int choiceCount = Mathf.Max(1, stageData.rewardChoiceCount);
        List<LSO_RewardOption> available = CreateAvailableRewards(stageData);

        if (available.Count == 0)
        {
            Debug.LogWarning("[KTH_Reward] 뽑을 수 있는 보상이 없습니다.");
            return currentOptions;
        }

        for (int i = 0; i < choiceCount; i++)
        {
            if (available.Count == 0) break;

            LSO_RewardOption selected = RollRandomReward(available);

            if (selected == null) break;

            currentOptions.Add(selected);
            available.Remove(selected);
        }

        Debug.Log($"🎁 [KTH_Reward] 보상 후보 {currentOptions.Count}개 생성");

        foreach (LSO_RewardOption option in currentOptions)
        {
            if (option == null) continue;
            Debug.Log($"  → {option.type} : {option.GetName()}");
        }

        RewardOptionsGenerated?.Invoke(currentOptions);
        return currentOptions;
    }

    // =========================================================
    // 카드 + 유언 후보 생성
    // =========================================================
    private List<LSO_RewardOption> CreateAvailableRewards(LSO_StageRewardData stageData)
    {
        List<LSO_RewardOption> result = new();

        // 카드
        if (stageData.possiblePieces != null)
        {
            foreach (LSO_RewardPoolEntry entry in stageData.possiblePieces)
            {
                if (entry == null || entry.pieceSO == null || entry.weight <= 0)
                    continue;

                result.Add(new LSO_RewardOption
                {
                    type = LSO_RewardType.Piece,
                    piece = entry.pieceSO
                });

                Debug.Log($"[KTH_Reward] 카드 후보 추가: {entry.pieceSO.name}");
            }
        }

        // 유언
        if (stageData.possibleWills != null)
        {
            foreach (LSO_WillRewardPoolEntry entry in stageData.possibleWills)
            {
                if (entry == null || entry.willSO == null || entry.weight <= 0)
                    continue;

                result.Add(new LSO_RewardOption
                {
                    type = LSO_RewardType.Will,
                    will = entry.willSO
                });

                Debug.Log($"[KTH_Reward] 유언 후보 추가: {entry.willSO.name}");
            }
        }

        return result;
    }

    // =========================================================
    // 가중치 랜덤
    // =========================================================
    private LSO_RewardOption RollRandomReward(List<LSO_RewardOption> available)
    {
        if (available == null || available.Count == 0) return null;

        float totalWeight = 0f;

        foreach (LSO_RewardOption option in available)
        {
            if (option == null) continue;
            totalWeight += GetRewardWeight(option);
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[KTH_Reward] 전체 가중치가 0입니다.");
            return null;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (LSO_RewardOption option in available)
        {
            if (option == null) continue;

            currentWeight += GetRewardWeight(option);

            if (randomValue <= currentWeight)
                return option;
        }

        return available[available.Count - 1];
    }

    private float GetRewardWeight(LSO_RewardOption option)
    {
        if (option == null || currentStageData == null) return 0f;

        if (option.type == LSO_RewardType.Piece)
        {
            if (currentStageData.possiblePieces == null) return 0f;

            foreach (LSO_RewardPoolEntry entry in currentStageData.possiblePieces)
            {
                if (entry != null && entry.pieceSO == option.piece)
                    return Mathf.Max(0f, entry.weight);
            }
        }
        else if (option.type == LSO_RewardType.Will)
        {
            if (currentStageData.possibleWills == null) return 0f;

            foreach (LSO_WillRewardPoolEntry entry in currentStageData.possibleWills)
            {
                if (entry != null && entry.willSO == option.will)
                    return Mathf.Max(0f, entry.weight);
            }
        }

        return 0f;
    }

    // =========================================================
    // 선택된 보상 지급
    // =========================================================
    public LSO_UnlockState ClaimReward(LSO_RewardOption selectedOption)
    {
        if (selectedOption == null)
        {
            Debug.LogError("[KTH_Reward] 선택된 보상이 NULL입니다.");
            return null;
        }

        Debug.Log($"🎁 [KTH_Reward] 보상 선택: {selectedOption.type}");

        // 카드 보상
        if (selectedOption.type == LSO_RewardType.Piece)
        {
            if (selectedOption.piece == null)
            {
                Debug.LogError("[KTH_Reward] 선택된 CardSO가 NULL입니다.");
                return null;
            }

            LSO_CardSO card = selectedOption.piece;
            Debug.Log($"🎴 선택된 CardSO: {card.name}");

            LSO_UnlockState rewardState = new LSO_UnlockState();

            if (card.Animal != null)
            {
                rewardState.UnlockPiece(card.Animal);
            }

            if (LSO_ItemLibraryManager.Instance == null)
            {
                Debug.LogError("[KTH_Reward] LSO_ItemLibraryManager.Instance가 NULL입니다!");
            }
            else
            {
                LSO_ItemLibraryManager.Instance.AddPieceToLibrary(card);
                Debug.Log($"📚 [KTH_Reward] ItemLibrary에 CardSO 저장 완료: {card.name}");
            }

            Unlocked?.Invoke(rewardState);
            currentOptions.Clear();
            return rewardState;
        }

        // 유언 보상
        if (selectedOption.type == LSO_RewardType.Will)
        {
            if (selectedOption.will == null)
            {
                Debug.LogError("[KTH_Reward] 선택된 WillSO가 NULL입니다.");
                return null;
            }

            DLJ_WillDataSO will = selectedOption.will;
            Debug.Log($"📜 선택된 WillSO: {will.name}");

            LSO_UnlockState rewardState = new LSO_UnlockState();
            rewardState.UnlockWill(will);

            if (LSO_ItemLibraryManager.Instance == null)
            {
                Debug.LogError("[KTH_Reward] LSO_ItemLibraryManager.Instance가 NULL입니다!");
            }
            else
            {
                LSO_ItemLibraryManager.Instance.AddWillToLibrary(will);
                Debug.Log($"📚 [KTH_Reward] ItemLibrary에 WillSO 저장 완료: {will.name}");
            }

            Unlocked?.Invoke(rewardState);
            currentOptions.Clear();
            return rewardState;
        }

        Debug.LogError($"[KTH_Reward] 알 수 없는 보상 타입: {selectedOption.type}");
        return null;
    }

    public List<LSO_RewardOption> GetCurrentOptions()
    {
        return currentOptions;
    }
}