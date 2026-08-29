using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 메타 진행 - 유언 기록 매니저
/// 저장 정보: 유언 사용 횟수 / 특수 조합 발견 / 보스 처치 기록
/// </summary>
public class KTH_WillRecord : MonoBehaviour
{
    public static KTH_WillRecord Instance { get; private set; }

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

    [Header("유언 사용 횟수")]
    [SerializeField] private int totalWillUseCount = 0;

    [Header("특수 조합 발견 목록 (조합 ID)")]
    [SerializeField] private List<string> discoveredCombos = new List<string>();

    [Header("보스 처치 기록 (보스 ID)")]
    [SerializeField] private List<string> defeatedBosses = new List<string>();

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
    // 1. 유언 사용 횟수
    // ─────────────────────────────────────────────

    /// <summary>유언을 사용했을 때 호출 (사용 횟수 +1)</summary>
    public void AddWillUse()
    {
        totalWillUseCount++;
    }

    /// <summary>현재까지 누적된 유언 사용 횟수 반환</summary>
    public int GetWillUseCount()
    {
        return totalWillUseCount;
    }

    // ─────────────────────────────────────────────
    // 2. 특수 조합 발견
    // ─────────────────────────────────────────────

    /// <summary>
    /// 특수 조합을 새로 발견했을 때 호출.
    /// 이미 발견한 조합이면 무시하고 false 반환, 새로 등록되면 true 반환.
    /// </summary>
    public bool DiscoverCombo(string comboId)
    {
        if (string.IsNullOrEmpty(comboId)) return false;

        if (discoveredCombos.Contains(comboId))
            return false; // 이미 발견한 조합

        discoveredCombos.Add(comboId);
        return true;
    }

    /// <summary>해당 조합을 이미 발견했는지 여부</summary>
    public bool HasDiscoveredCombo(string comboId)
    {
        return discoveredCombos.Contains(comboId);
    }

    /// <summary>발견한 특수 조합 개수</summary>
    public int GetDiscoveredComboCount()
    {
        return discoveredCombos.Count;
    }

    /// <summary>발견한 특수 조합 목록 (읽기 전용 반환)</summary>
    public IReadOnlyList<string> GetDiscoveredCombos()
    {
        return discoveredCombos;
    }

    // ─────────────────────────────────────────────
    // 3. 보스 처치 기록
    // ─────────────────────────────────────────────

    /// <summary>
    /// 보스를 처치했을 때 호출.
    /// 최초 처치면 true, 이미 처치 기록이 있으면 false 반환.
    /// </summary>
    public bool RecordBossDefeat(string bossId)
    {
        if (string.IsNullOrEmpty(bossId)) return false;

        if (defeatedBosses.Contains(bossId))
            return false; // 이미 처치한 보스

        defeatedBosses.Add(bossId);
        return true;
    }

    /// <summary>해당 보스를 처치한 적이 있는지 여부</summary>
    public bool HasDefeatedBoss(string bossId)
    {
        return defeatedBosses.Contains(bossId);
    }

    /// <summary>처치한 보스 수</summary>
    public int GetDefeatedBossCount()
    {
        return defeatedBosses.Count;
    }

    /// <summary>처치한 보스 목록 (읽기 전용 반환)</summary>
    public IReadOnlyList<string> GetDefeatedBosses()
    {
        return defeatedBosses;
    }

    // ─────────────────────────────────────────────
    // 전체 초기화 (필요 시 사용)
    // ─────────────────────────────────────────────

    /// <summary>모든 유언 기록 초기화</summary>
    public void ResetRecord()
    {
        totalWillUseCount = 0;
        discoveredCombos.Clear();
        defeatedBosses.Clear();
    }
}