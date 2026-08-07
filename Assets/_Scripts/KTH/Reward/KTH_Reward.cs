using _Scripts.LSO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확률 뽑기용 기물 항목 (기물 이름 식별용)
/// </summary>
[System.Serializable]
public class KTH_RewardPoolEntry
{
    [Tooltip("기물의 animalName (예: Dog, Cat, Bear...)")]
    public string animalName;

    [Tooltip("가중치 (값이 클수록 뽑힐 확률 높음)")]
    public float weight = 1f;
}

/// <summary>
/// 확률 뽑기용 유언 항목
/// </summary>
[System.Serializable]
public class KTH_WillRewardPoolEntry
{
    [Tooltip("유언 타입")]
    public LSO_WillType willType;

    [Tooltip("가중치")]
    public float weight = 1f;
}

/// <summary>
/// 스테이지별 해금 데이터 (기물 / 유언) - 확률 기반 풀
/// </summary>
[System.Serializable]
public class KTH_StageRewardData
{
    [Header("챕터")]
    public int chapter;

    [Header("스테이지")]
    public int stage;

    [Header("기물 후보 (animalName 기준)")]
    public List<KTH_RewardPoolEntry> possiblePieces = new();

    [Header("유언 후보")]
    public List<KTH_WillRewardPoolEntry> possibleWills = new();
}

/// <summary>
/// 해금 요소 매니저
/// </summary>
public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    [Header("스테이지별 해금 테이블")]
    public List<KTH_StageRewardData> stageRewardTable = new();

    [Header("현재까지 해금된 기물 이름 목록")]
    [SerializeField] private List<string> unlockedPieces = new();

    [Header("현재까지 해금된 유언 목록")]
    [SerializeField] private List<LSO_WillType> unlockedWills = new();

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

    //==================================================
    // 스테이지 리워드 지급 (확률 뽑기)
    //==================================================

    public KTH_UnlockResult UnlockByStage(int chapter, int stage)
    {
        KTH_UnlockResult result = new();

        KTH_StageRewardData data =
            stageRewardTable.Find(x => x.chapter == chapter && x.stage == stage);

        if (data == null)
        {
            Debug.Log($"[Reward] Stage {stage}에 등록된 리워드가 없습니다.");
            return result;
        }

        Debug.Log($"========== Stage {stage} Reward ==========");

        // 기물 1개 뽑기 (animalName 기준)
        string pickedPieceName = PickWeightedRandomPiece(data.possiblePieces, unlockedPieces);
        if (!string.IsNullOrEmpty(pickedPieceName) && UnlockPiece(pickedPieceName))
        {
            result.newlyUnlockedPieces.Add(pickedPieceName);
            Debug.Log($"새 기물 해금 : {pickedPieceName}");
        }
        else
        {
            Debug.Log("뽑을 수 있는 기물이 없습니다 (풀이 비었거나 모두 해금됨).");
        }

        // 유언 1개 뽑기
        LSO_WillType? pickedWill = PickWeightedRandomWill(data.possibleWills, unlockedWills);
        if (pickedWill.HasValue && UnlockWill(pickedWill.Value))
        {
            result.newlyUnlockedWills.Add(pickedWill.Value);
            Debug.Log($"새 유언 해금 : {pickedWill.Value}");
        }
        else
        {
            Debug.Log("뽑을 수 있는 유언이 없습니다 (풀이 비었거나 모두 해금됨).");
        }

        if (!result.HasAnyNewUnlock)
        {
            Debug.Log("새롭게 해금된 리워드가 없습니다.");
        }

        Debug.Log("==========================================");

        return result;
    }

    //==================================================
    // 가중치 랜덤 뽑기 (이미 해금된 항목은 후보에서 제외)
    //==================================================

    private string PickWeightedRandomPiece(
        List<KTH_RewardPoolEntry> pool,
        List<string> alreadyUnlocked)
    {
        if (pool == null || pool.Count == 0)
            return null;

        List<KTH_RewardPoolEntry> candidates = new();

        foreach (var entry in pool)
        {
            if (entry == null || string.IsNullOrEmpty(entry.animalName))
                continue;

            if (alreadyUnlocked.Contains(entry.animalName))
                continue;

            if (entry.weight <= 0f)
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var c in candidates)
            totalWeight += c.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var c in candidates)
        {
            cumulative += c.weight;

            if (roll <= cumulative)
                return c.animalName;
        }

        return candidates[candidates.Count - 1].animalName;
    }

    private LSO_WillType? PickWeightedRandomWill(
        List<KTH_WillRewardPoolEntry> pool,
        List<LSO_WillType> alreadyUnlocked)
    {
        if (pool == null || pool.Count == 0)
            return null;

        List<KTH_WillRewardPoolEntry> candidates = new();

        foreach (var entry in pool)
        {
            if (alreadyUnlocked.Contains(entry.willType))
                continue;

            if (entry.weight <= 0f)
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var c in candidates)
            totalWeight += c.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var c in candidates)
        {
            cumulative += c.weight;

            if (roll <= cumulative)
                return c.willType;
        }

        return candidates[candidates.Count - 1].willType;
    }

    //==================================================
    // 기물 (animalName 기반)
    //==================================================

    public bool UnlockPiece(string animalName)
    {
        if (string.IsNullOrEmpty(animalName))
            return false;

        if (unlockedPieces.Contains(animalName))
            return false;

        unlockedPieces.Add(animalName);
        return true;
    }

    public bool UnlockPiece(LSO_AnimalSO animal)
    {
        if (animal == null) return false;
        return UnlockPiece(animal.animalName);
    }

    public bool IsPieceUnlocked(string animalName)
    {
        return unlockedPieces.Contains(animalName);
    }

    public bool IsPieceUnlocked(LSO_AnimalSO animal)
    {
        if (animal == null) return false;
        return IsPieceUnlocked(animal.animalName);
    }

    public IReadOnlyList<string> GetUnlockedPieces()
    {
        return unlockedPieces;
    }

    //==================================================
    // 유언
    //==================================================

    public bool UnlockWill(LSO_WillType willType)
    {
        if (unlockedWills.Contains(willType))
            return false;

        unlockedWills.Add(willType);
        return true;
    }

    public bool IsWillUnlocked(LSO_WillType willType)
    {
        return unlockedWills.Contains(willType);
    }

    public IReadOnlyList<LSO_WillType> GetUnlockedWills()
    {
        return unlockedWills;
    }

    //==================================================
    // 조회 및 초기화
    //==================================================

    public KTH_StageRewardData GetStageRewardData(int chapter, int stage)
    {
        return stageRewardTable.Find(x => x.chapter == chapter && x.stage == stage);
    }

    public void ResetUnlocks()
    {
        unlockedPieces.Clear();
        unlockedWills.Clear();

        Debug.Log("[Reward] 모든 해금 데이터 초기화");
    }
}

/// <summary>
/// Unlock 결과
/// </summary>
public class KTH_UnlockResult
{
    public List<string> newlyUnlockedPieces = new();
    public List<LSO_WillType> newlyUnlockedWills = new();

    public bool HasAnyNewUnlock =>
        newlyUnlockedPieces.Count > 0 ||
        newlyUnlockedWills.Count > 0;
}