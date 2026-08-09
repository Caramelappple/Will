using System;
using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

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

/// <summary>
/// 해금 요소 매니저. 조립만 한다.
///
///   보상 테이블  KTH_RewardTableSO   (에셋)
///   확률 뽑기    KTH_RewardRoller    (static)
///   해금 상태    KTH_UnlockState     (순수 C#)
///
/// 이 클래스가 하는 일은 셋을 이어 붙이고 결과를 알리는 것뿐이다.
/// </summary>
public class KTH_Reward : MonoBehaviour
{
    public static KTH_Reward Instance { get; private set; }

    [Header("스테이지별 해금 테이블")]
    [SerializeField] private KTH_RewardTableSO rewardTable;

    /// <summary>보상이 지급됐을 때. 보상 UI가 이걸 받아 화면에 띄운다.</summary>
    public event Action<KTH_UnlockResult> Unlocked;

    /// <summary>해금 상태. 저장 담당자는 Export/Import만 쓰면 된다.</summary>
    public KTH_UnlockState Unlocks { get; } = new();

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

        if (rewardTable == null)
            Debug.LogError($"{name}: 보상 테이블(KTH_RewardTableSO)이 연결되지 않았습니다.", this);

        // 소환할 때 고를 수 있는 유언을 이쪽에서 공급한다.
        // 소환 코드가 해금 시스템을 직접 참조하지 않게 함수만 건네준다.
        LSO_WillSelection.UnlockedWillsProvider = GetUnlockedWillList;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;

        if (LSO_WillSelection.UnlockedWillsProvider == GetUnlockedWillList)
            LSO_WillSelection.UnlockedWillsProvider = null;
    }

    /// <summary>
    /// 해금된 유언 목록. 매번 새 리스트를 만들지 않도록 캐시해두고 해금될 때만 다시 만든다.
    /// </summary>
    private List<LSO_WillType> _unlockedWillCache;
    private int _unlockedWillCacheCount = -1;

    private IReadOnlyList<LSO_WillType> GetUnlockedWillList()
    {
        if (_unlockedWillCacheCount != Unlocks.Wills.Count)
        {
            _unlockedWillCache = new List<LSO_WillType>(Unlocks.Wills);
            _unlockedWillCacheCount = _unlockedWillCache.Count;
        }

        return _unlockedWillCache;
    }

    //==================================================
    // 스테이지 리워드 지급
    //==================================================

    public KTH_UnlockResult UnlockByStage(int chapter, int stage)
    {
        KTH_UnlockResult result = new();

        KTH_StageRewardData data = GetStageRewardData(chapter, stage);

        if (data == null)
        {
            Log($"Stage {stage}에 등록된 리워드가 없습니다.");
            return result;
        }

        RollPieces(data, result);
        RollWills(data, result);

        // 아무것도 못 뽑았어도 알린다. UI가 "받을 게 없다"를 보여줄 수 있어야 한다.
        Unlocked?.Invoke(result);

        return result;
    }

    private void RollPieces(KTH_StageRewardData data, KTH_UnlockResult result)
    {
        List<KTH_RewardPoolEntry> picked = KTH_RewardRoller.PickMany(
            data.possiblePieces,
            data.pieceCount,
            entry => entry.weight,
            entry => string.IsNullOrEmpty(entry.animalName)
                     || Unlocks.IsPieceUnlocked(entry.animalName));

        foreach (KTH_RewardPoolEntry entry in picked)
        {
            if (!Unlocks.UnlockPiece(entry.animalName)) continue;

            result.newlyUnlockedPieces.Add(entry.animalName);
            Log($"새 기물 해금 : {entry.animalName}");
        }
    }

    private void RollWills(KTH_StageRewardData data, KTH_UnlockResult result)
    {
        List<KTH_WillRewardPoolEntry> picked = KTH_RewardRoller.PickMany(
            data.possibleWills,
            data.willCount,
            entry => entry.weight,
            entry => Unlocks.IsWillUnlocked(entry.willType));

        foreach (KTH_WillRewardPoolEntry entry in picked)
        {
            if (!Unlocks.UnlockWill(entry.willType)) continue;

            result.newlyUnlockedWills.Add(entry.willType);
            Log($"새 유언 해금 : {entry.willType}");
        }
    }

    //==================================================
    // 조회 (기존 호출부 호환용)
    //==================================================

    public KTH_StageRewardData GetStageRewardData(int chapter, int stage)
    {
        return rewardTable != null ? rewardTable.Find(chapter, stage) : null;
    }

    public bool UnlockPiece(string animalName) => Unlocks.UnlockPiece(animalName);

    public bool UnlockPiece(LSO_AnimalSO animal) =>
        animal != null && Unlocks.UnlockPiece(animal.animalName);

    public bool IsPieceUnlocked(string animalName) => Unlocks.IsPieceUnlocked(animalName);

    public bool IsPieceUnlocked(LSO_AnimalSO animal) =>
        animal != null && Unlocks.IsPieceUnlocked(animal.animalName);

    public bool UnlockWill(LSO_WillType willType) => Unlocks.UnlockWill(willType);

    public bool IsWillUnlocked(LSO_WillType willType) => Unlocks.IsWillUnlocked(willType);

    public IReadOnlyCollection<string> GetUnlockedPieces() => Unlocks.Pieces;

    public IReadOnlyCollection<LSO_WillType> GetUnlockedWills() => Unlocks.Wills;

    public void ResetUnlocks()
    {
        Unlocks.Clear();
        Log("모든 해금 데이터 초기화");
    }

    // 릴리즈 빌드에서는 통째로 사라진다. 호출부도 같이 제거된다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string message)
    {
        Debug.Log($"[Reward] {message}");
    }
}
