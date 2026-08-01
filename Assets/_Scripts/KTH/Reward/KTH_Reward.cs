using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지별 해금 데이터 (기물 / 유언)
/// 인스펙터에서 스테이지마다 무엇이 해금되는지 지정
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
/// 해금 요소 매니저 - 스테이지 클리어 시 새로운 기물 / 새로운 유언을 해금
/// </summary>
public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    [Header("스테이지별 해금 테이블 (고정 데이터)")]
    public List<KTH_StageRewardData> stageRewardTable = new List<KTH_StageRewardData>();

    [Header("현재까지 해금된 기물 ID")]
    [SerializeField] private List<string> unlockedPieces = new List<string>();

    [Header("현재까지 해금된 유언 ID")]
    [SerializeField] private List<string> unlockedWills = new List<string>();

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
            return;
        }
    }

    // ─────────────────────────────────────────────
    // 스테이지 클리어 시 해금 처리
    // ─────────────────────────────────────────────

    /// <summary>
    /// 해당 스테이지에 지정된 해금 요소(기물/유언)를 전부 해금 처리.
    /// 새로 해금된 기물/유언 목록을 반환 (UI 알림 등에 사용).
    /// </summary>
    public KTH_UnlockResult UnlockByStage(int stageNumber)
    {
        KTH_UnlockResult result = new KTH_UnlockResult();

        KTH_StageRewardData data = stageRewardTable.Find(d => d.stageNumber == stageNumber);
        if (data == null) return result; // 해당 스테이지에 정의된 해금 요소 없음

        foreach (string pieceId in data.unlockedPieceIds)
        {
            if (UnlockPiece(pieceId))
            {
                result.newlyUnlockedPieces.Add(pieceId);
            }
        }

        foreach (string willId in data.unlockedWillIds)
        {
            if (UnlockWill(willId))
            {
                result.newlyUnlockedWills.Add(willId);
            }
        }

        return result;
    }

    // ─────────────────────────────────────────────
    // 기물 해금
    // ─────────────────────────────────────────────

    /// <summary>기물 해금. 새로 해금되면 true, 이미 해금되어 있으면 false</summary>
    public bool UnlockPiece(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId)) return false;
        if (unlockedPieces.Contains(pieceId)) return false;

        unlockedPieces.Add(pieceId);
        return true;
    }

    /// <summary>해당 기물이 해금되어 있는지 여부</summary>
    public bool IsPieceUnlocked(string pieceId)
    {
        return unlockedPieces.Contains(pieceId);
    }

    /// <summary>해금된 기물 전체 목록 (읽기 전용)</summary>
    public IReadOnlyList<string> GetUnlockedPieces()
    {
        return unlockedPieces;
    }

    // ─────────────────────────────────────────────
    // 유언 해금
    // ─────────────────────────────────────────────

    /// <summary>유언 해금. 새로 해금되면 true, 이미 해금되어 있으면 false</summary>
    public bool UnlockWill(string willId)
    {
        if (string.IsNullOrEmpty(willId)) return false;
        if (unlockedWills.Contains(willId)) return false;

        unlockedWills.Add(willId);
        return true;
    }

    /// <summary>해당 유언이 해금되어 있는지 여부</summary>
    public bool IsWillUnlocked(string willId)
    {
        return unlockedWills.Contains(willId);
    }

    /// <summary>해금된 유언 전체 목록 (읽기 전용)</summary>
    public IReadOnlyList<string> GetUnlockedWills()
    {
        return unlockedWills;
    }

    // ─────────────────────────────────────────────
    // 조회 헬퍼
    // ─────────────────────────────────────────────

    /// <summary>특정 스테이지에서 해금될 예정인 데이터 조회 (미리보기 등에 사용)</summary>
    public KTH_StageRewardData GetStageRewardData(int stageNumber)
    {
        return stageRewardTable.Find(d => d.stageNumber == stageNumber);
    }

    // ─────────────────────────────────────────────
    // 초기화 (필요 시 사용)
    // ─────────────────────────────────────────────

    public void ResetUnlocks()
    {
        unlockedPieces.Clear();
        unlockedWills.Clear();
    }
}

/// <summary>UnlockByStage 호출 결과 - 새로 해금된 항목만 담김</summary>
public class KTH_UnlockResult
{
    public List<string> newlyUnlockedPieces = new List<string>();
    public List<string> newlyUnlockedWills = new List<string>();

    public bool HasAnyNewUnlock => newlyUnlockedPieces.Count > 0 || newlyUnlockedWills.Count > 0;
}