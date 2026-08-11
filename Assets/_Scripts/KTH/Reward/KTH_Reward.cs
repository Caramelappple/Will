using System;
using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO;
using _Scripts.LSO.Deck.Data; // LSO_CardSO
using _Scripts.LSO.Will;

public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    [Header("스테이지별 해금 테이블 참조")]
    [SerializeField] private KTH_RewardTableSO rewardTable;

    // 보상 해금 이벤트
    public event Action<KTH_UnlockState> Unlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 스테이지 클리어 시 보상 롤링 및 라이브러리 자동 저장
    /// </summary>
    public KTH_UnlockState UnlockByStage(int chapter, int stage)
    {
        Debug.Log($"🎁 [KTH_Reward] 스테이지 보상 해금 프로세스 시작: Chapter {chapter}, Stage {stage}");

        // 이번 스테이지에서 새로 해금된 보상만 담을 객체 생성
        KTH_UnlockState newRewardState = new KTH_UnlockState();

        // ItemLibraryManager에 넘겨줄 카드 목록을 별도로 수집합니다.
        List<LSO_CardSO> unlockedCards = new List<LSO_CardSO>();

        // 1. KTH_RewardTableSO에서 현재 스테이지 보상 데이터 조회
        if (rewardTable != null)
        {
            KTH_StageRewardData stageData = rewardTable.Find(chapter, stage);

            if (stageData != null)
            {
                // [기물/카드 가중치 확률 뽑기]
                for (int i = 0; i < stageData.pieceCount; i++)
                {
                    LSO_CardSO selectedCard = RollRandomPiece(stageData.possiblePieces);
                    if (selectedCard != null)
                    {
                        // 1) KTH_UnlockState에는 Animal 데이터 전달
                        if (selectedCard.Animal != null)
                        {
                            newRewardState.UnlockPiece(selectedCard.Animal);
                        }

                        // 2) ItemLibraryManager에 등록할 카드(LSO_CardSO) 저장 리스트에 추가
                        unlockedCards.Add(selectedCard);

                        Debug.Log($"🎲 [KTH_Reward] 기물/카드 획득: {selectedCard.name}");
                    }
                }

                // [유언(Will) 가중치 확률 뽑기]
                for (int i = 0; i < stageData.willCount; i++)
                {
                    DLJ_WillDataSO selectedWill = RollRandomWill(stageData.possibleWills);
                    if (selectedWill != null)
                    {
                        newRewardState.UnlockWill(selectedWill);
                        Debug.Log($"🎲 [KTH_Reward] 유언 획득: {selectedWill.name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [KTH_Reward] Chapter {chapter}, Stage {stage} 보상 데이터를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("🚨 [KTH_Reward] RewardTable이 인스펙터에 할당되지 않았습니다!");
        }

        // 2. ItemLibraryManager에 카드 및 유언 전달 (⭐ 이 부분이 핵심입니다!)
        if (ItemLibraryManager.Instance != null)
        {
            // 수집한 카드(LSO_CardSO)들을 도감 라이브러리에 등록
            if (unlockedCards.Count > 0)
            {
                ItemLibraryManager.Instance.AddPiecesToLibrary(unlockedCards);
            }

            // 유언(Will)들을 도감 라이브러리에 등록
            if (newRewardState.Wills != null && newRewardState.Wills.Count > 0)
            {
                List<DLJ_WillDataSO> willsList = new List<DLJ_WillDataSO>(newRewardState.Wills);
                ItemLibraryManager.Instance.AddWillsToLibrary(willsList);
            }
        }
        else
        {
            Debug.LogError("🚨 [KTH_Reward] ItemLibraryManager 인스턴스를 찾을 수 없습니다!");
        }

        Unlocked?.Invoke(newRewardState);
        return newRewardState;
    }

    // =========================================================================
    // 🎲 가중치(Weight) 기반 확률 뽑기 헬퍼 메서드
    // =========================================================================

    private LSO_CardSO RollRandomPiece(List<KTH_RewardPoolEntry> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in pool)
        {
            if (entry != null && entry.pieceSO != null && entry.weight > 0)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in pool)
        {
            if (entry == null || entry.pieceSO == null || entry.weight <= 0) continue;

            currentWeight += entry.weight;
            if (randomValue <= currentWeight)
            {
                return entry.pieceSO;
            }
        }

        return null;
    }

    private DLJ_WillDataSO RollRandomWill(List<KTH_WillRewardPoolEntry> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in pool)
        {
            if (entry != null && entry.willSO != null && entry.weight > 0)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in pool)
        {
            if (entry == null || entry.willSO == null || entry.weight <= 0) continue;

            currentWeight += entry.weight;
            if (randomValue <= currentWeight)
            {
                return entry.willSO;
            }
        }

        return null;
    }
}