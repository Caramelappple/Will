using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지별 해금 데이터 (기물 / 유언)
/// </summary>
[System.Serializable]
public class KTH_StageRewardData
{
    [Header("스테이지 번호")]
    public int stageNumber;

    [Header("이 스테이지에서 해금되는 기물 ID 목록")]
    public List<string> unlockedPieceIds = new List<string>();

    [Header("이 스테이지에서 해금되는 유언 ID 목록")]
    public List<string> unlockedWillIds = new List<string>();
}

/// <summary>
/// 해금 요소 매니저
/// </summary>
public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    [Header("스테이지별 해금 테이블")]
    public List<KTH_StageRewardData> stageRewardTable = new();

    [Header("현재까지 해금된 기물")]
    [SerializeField] private List<string> unlockedPieces = new();

    [Header("현재까지 해금된 유언")]
    [SerializeField] private List<string> unlockedWills = new();

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
    // 스테이지 리워드 지급
    //==================================================

    public KTH_UnlockResult UnlockByStage(int stageNumber)
    {
        KTH_UnlockResult result = new();

        KTH_StageRewardData data = stageRewardTable.Find(x => x.stageNumber == stageNumber);

        if (data == null)
        {
            Debug.Log($"[Reward] Stage {stageNumber}에 등록된 리워드가 없습니다.");
            return result;
        }

        Debug.Log($"========== Stage {stageNumber} Reward ==========");

        foreach (string pieceId in data.unlockedPieceIds)
        {
            if (UnlockPiece(pieceId))
            {
                result.newlyUnlockedPieces.Add(pieceId);
                Debug.Log($"새 기물 해금 : {pieceId}");
            }
        }

        foreach (string willId in data.unlockedWillIds)
        {
            if (UnlockWill(willId))
            {
                result.newlyUnlockedWills.Add(willId);
                Debug.Log($"새 유언 해금 : {willId}");
            }
        }

        if (!result.HasAnyNewUnlock)
        {
            Debug.Log("새롭게 해금된 리워드가 없습니다.");
        }

        Debug.Log("==========================================");

        return result;
    }

    //==================================================
    // 기물
    //==================================================

    public bool UnlockPiece(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId))
            return false;

        if (unlockedPieces.Contains(pieceId))
            return false;

        unlockedPieces.Add(pieceId);
        return true;
    }

    public bool IsPieceUnlocked(string pieceId)
    {
        return unlockedPieces.Contains(pieceId);
    }

    public IReadOnlyList<string> GetUnlockedPieces()
    {
        return unlockedPieces;
    }

    //==================================================
    // 유언
    //==================================================

    public bool UnlockWill(string willId)
    {
        if (string.IsNullOrEmpty(willId))
            return false;

        if (unlockedWills.Contains(willId))
            return false;

        unlockedWills.Add(willId);
        return true;
    }

    public bool IsWillUnlocked(string willId)
    {
        return unlockedWills.Contains(willId);
    }

    public IReadOnlyList<string> GetUnlockedWills()
    {
        return unlockedWills;
    }

    //==================================================
    // 조회
    //==================================================

    public KTH_StageRewardData GetStageRewardData(int stageNumber)
    {
        return stageRewardTable.Find(x => x.stageNumber == stageNumber);
    }

    //==================================================
    // 초기화
    //==================================================

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
    public List<string> newlyUnlockedWills = new();

    public bool HasAnyNewUnlock =>
        newlyUnlockedPieces.Count > 0 ||
        newlyUnlockedWills.Count > 0;
}