using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using System.Collections.Generic;
using UnityEngine;

public enum KTH_RewardType
{
    Piece,
    Will
}

/// <summary>
/// 플레이어에게 보여줄 보상 후보
/// </summary>
[System.Serializable]
public class KTH_RewardOption
{
    public KTH_RewardType type;

    public LSO_CardSO piece;
    public DLJ_WillDataSO will;

    public string GetName()
    {
        if (type == KTH_RewardType.Piece)
            return piece != null ? piece.name : "알 수 없는 기물";

        return will != null ? will.name : "알 수 없는 유언";
    }
}


/// <summary>
/// 확률 뽑기용 카드 항목
/// </summary>
[System.Serializable]
public class KTH_RewardPoolEntry
{
    public LSO_CardSO pieceSO;

    [Tooltip("값이 클수록 뽑힐 확률이 높음")]
    public float weight = 1f;
}


/// <summary>
/// 확률 뽑기용 유언 항목
/// </summary>
[System.Serializable]
public class KTH_WillRewardPoolEntry
{
    public DLJ_WillDataSO willSO;

    [Tooltip("값이 클수록 뽑힐 확률이 높음")]
    public float weight = 1f;
}


/// <summary>
/// 스테이지별 보상 데이터
/// </summary>
[System.Serializable]
public class KTH_StageRewardData
{
    [Header("챕터")]
    public int chapter;

    [Header("스테이지")]
    public int stage;

    [Header("보상 후보 개수")]
    [Min(1)]
    public int rewardChoiceCount = 3;

    [Header("카드 후보")]
    public List<KTH_RewardPoolEntry> possiblePieces = new();

    [Header("유언 후보")]
    public List<KTH_WillRewardPoolEntry> possibleWills = new();
}


/// <summary>
/// 스테이지별 보상 테이블
/// </summary>
[CreateAssetMenu(fileName = "KTH_RewardTable", menuName = "KTH/Reward Table")]
public class KTH_RewardTableSO : ScriptableObject
{
    [SerializeField]
    private List<KTH_StageRewardData> stages = new();

    public List<KTH_StageRewardData> Stages => stages;

    public KTH_StageRewardData Find(int chapter, int stage)
    {
        return stages.Find(
            x => x != null &&
                 x.chapter == chapter &&
                 x.stage == stage
        );
    }
}