using _Scripts.LSO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확률 뽑기용 기물 항목 (기물 이름 식별용)
///
/// </summary>
[System.Serializable]
public class KTH_RewardPoolEntry
{
    [Tooltip("기물의 animalName (예: Crow, Raven, Rook...)")]
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

    [Tooltip("이 스테이지에서 뽑을 기물 개수.")]
    [Min(0)] public int pieceCount = 1;

    [Tooltip("이 스테이지에서 뽑을 유언 개수.")]
    [Min(0)] public int willCount = 1;

    [Header("기물 후보 (animalName 기준)")]
    public List<KTH_RewardPoolEntry> possiblePieces = new();

    [Header("유언 후보")]
    public List<KTH_WillRewardPoolEntry> possibleWills = new();
}

/// <summary>
/// 스테이지별 보상 테이블. 순수 설정 데이터라 씬이 아니라 에셋으로 둔다.
///
/// 씬에 묻어두면 밸런싱을 고칠 때마다 씬 파일이 바뀌어 머지 충돌이 잦고,
/// 다른 씬에서 같은 테이블을 재사용할 수도 없다.
///
/// /// SO로 수정해 관리가 쉽도록 하였다
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
