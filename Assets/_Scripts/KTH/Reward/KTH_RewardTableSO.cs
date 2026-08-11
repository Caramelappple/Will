using _Scripts.LSO.Deck.Data; // LSO_CardSO 네임스페이스
using _Scripts.LSO.Will;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확률 뽑기용 카드 항목 (LSO_CardSO 직접 등록)
/// </summary>
[System.Serializable]
public class KTH_RewardPoolEntry
{
    [Tooltip("카드 ScriptableObject 에셋")]
    public LSO_CardSO pieceSO; // 타입을 LSO_AnimalSO -> LSO_CardSO로 변경

    [Tooltip("가중치 (값이 클수록 뽑힐 확률 높음)")]
    public float weight = 1f;
}

/// <summary>
/// 확률 뽑기용 유언 항목
/// </summary>
[System.Serializable]
public class KTH_WillRewardPoolEntry
{
    [Tooltip("유언 ScriptableObject 에셋")]
    public DLJ_WillDataSO willSO;

    [Tooltip("가중치")]
    public float weight = 1f;
}

/// <summary>
/// 스테이지별 해금 데이터 (카드 / 유언) - 확률 기반 풀
/// </summary>
[System.Serializable]
public class KTH_StageRewardData
{
    [Header("챕터")]
    public int chapter;

    [Header("스테이지")]
    public int stage;

    [Tooltip("이 스테이지에서 뽑을 카드 개수.")]
    [Min(0)] public int pieceCount = 1;

    [Tooltip("이 스테이지에서 뽑을 유언 개수.")]
    [Min(0)] public int willCount = 1;

    [Header("카드 후보")]
    public List<KTH_RewardPoolEntry> possiblePieces = new();

    [Header("유언 후보")]
    public List<KTH_WillRewardPoolEntry> possibleWills = new();
}

/// <summary>
/// 스테이지별 보상 테이블 (SO 에셋)
/// </summary>
[CreateAssetMenu(fileName = "KTH_RewardTable", menuName = "KTH/Reward Table")]
public class KTH_RewardTableSO : ScriptableObject
{
    [SerializeField] private List<KTH_StageRewardData> stages = new();

    /// <summary>편집기에서만 목록 자체를 다룬다. 런타임에는 Find로 조회할 것.</summary>
    public List<KTH_StageRewardData> Stages => stages;

    public KTH_StageRewardData Find(int chapter, int stage)
    {
        return stages.Find(x => x != null && x.chapter == chapter && x.stage == stage);
    }
}