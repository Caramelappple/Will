using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 전투 씬의 종료 조건을 관리한다.
///
/// 적 전멸:
///     적이 전부 죽는 순간 즉시 클리어한다.
///     단, Health의 IsDestroyed 갱신 타이밍을 고려하여
///     한 프레임 뒤에 최종 확인한다.
///
/// 아군 전멸:
///     턴 변경 시점에 패배 여부를 확인한다.
/// </summary>
public class KTH_GameEndManager : MonoBehaviour
{
    private readonly List<LDY_Animal> _enemies = new();
    private readonly List<LDY_Animal> _allies = new();

    private bool _isGameEnded;
    private Coroutine _enemyClearCheckCoroutine;

    [Header("턴 매니저 참조")]
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("보스 승리 시 연출용 (선택)")]
    [SerializeField] private UnityEvent onBossClear;

    [Header("일반 배틀 클리어 시 연출용 (선택)")]
    [SerializeField] private UnityEvent onBattleClear;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        RegisterEnemies();
        RegisterAllies();

        // Inspector에 연결하지 않았다면 자동 검색
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<LDY_TurnManager>();
        }

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += HandleTurnChanged;
        }
        else
        {
            Debug.LogWarning(
                "[KTH_GameEndManager] LDY_TurnManager를 찾지 못했습니다."
            );
        }

        // 게임 시작 직후 상태 확인
        CheckGameClear();
        CheckGameOver();
    }


    private void OnDestroy()
    {
        if (_enemyClearCheckCoroutine != null)
        {
            StopCoroutine(_enemyClearCheckCoroutine);
            _enemyClearCheckCoroutine = null;
        }

        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= HandleTurnChanged;
        }

        UnregisterEnemies();
        UnregisterAllies();
    }


    // =========================================================
    // 턴 변경
    // =========================================================

    private void HandleTurnChanged(LDY_Team currentTurn)
    {
        if (_isGameEnded)
            return;

        Debug.Log(
            $"[KTH_GameEndManager] 턴 전환 감지 → 현재 턴: {currentTurn}"
        );

        // 적 전멸은 이미 즉시 검사하지만
        // 안전을 위해 턴 변경 시에도 한 번 확인
        CheckGameClear();

        // 아군 전멸은 턴 변경 시 검사
        CheckGameOver();
    }


    // =========================================================
    // 외부 등록
    // =========================================================

    /// <summary>
    /// 동적으로 생성된 아군을 등록한다.
    /// </summary>
    public void RegisterAllyExternally(LDY_Animal ally)
    {
        if (ally == null)
            return;

        if (ally.team != LDY_Team.Player)
            return;

        if (ally.health == null)
            return;

        RegisterAlly(ally);
    }


    /// <summary>
    /// 동적으로 생성된 적을 등록한다.
    /// </summary>
    public void RegisterEnemyExternally(LDY_Animal enemy)
    {
        if (enemy == null)
            return;

        if (enemy.team != LDY_Team.Enemy)
            return;

        if (enemy.health == null)
            return;

        RegisterEnemy(enemy);
    }


    // =========================================================
    // 초기 등록
    // =========================================================

    private void RegisterEnemies()
    {
        LDY_Animal[] animals = FindObjectsByType<LDY_Animal>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (LDY_Animal animal in animals)
        {
            if (animal == null)
                continue;

            if (animal.team != LDY_Team.Enemy)
                continue;

            if (animal.health == null)
                continue;

            RegisterEnemy(animal);
        }
    }


    private void RegisterAllies()
    {
        LDY_Animal[] animals = FindObjectsByType<LDY_Animal>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (LDY_Animal animal in animals)
        {
            if (animal == null)
                continue;

            if (animal.team != LDY_Team.Player)
                continue;

            if (animal.health == null)
                continue;

            RegisterAlly(animal);
        }
    }


    // =========================================================
    // 등록
    // =========================================================

    private void RegisterEnemy(LDY_Animal enemy)
    {
        if (_enemies.Contains(enemy))
            return;

        _enemies.Add(enemy);

        enemy.health.OnDamage += HandleEnemyDamaged;

        Debug.Log(
            $"[KTH_GameEndManager] 적 등록: {enemy.name} " +
            $"(총 적 수: {_enemies.Count})"
        );
    }


    private void RegisterAlly(LDY_Animal ally)
    {
        if (_allies.Contains(ally))
            return;

        _allies.Add(ally);

        ally.health.OnDamage += HandleAllyDamaged;

        Debug.Log(
            $"[KTH_GameEndManager] 아군 등록: {ally.name} " +
            $"(총 아군 수: {_allies.Count})"
        );
    }


    // =========================================================
    // 적 피격
    // =========================================================

    /// <summary>
    /// 적이 데미지를 받는 순간 호출된다.
    ///
    /// 바로 CheckGameClear()를 하지 않고 한 프레임 기다린다.
    /// Damage 이벤트 발생 시점에는 아직 IsDestroyed가
    /// 갱신되지 않았을 수 있기 때문이다.
    /// </summary>
    private void HandleEnemyDamaged(DamageResultData result)
    {
        if (_isGameEnded)
            return;

        if (_enemyClearCheckCoroutine != null)
        {
            StopCoroutine(_enemyClearCheckCoroutine);
        }

        _enemyClearCheckCoroutine = StartCoroutine(
            CheckGameClearNextFrame()
        );
    }


    private IEnumerator CheckGameClearNextFrame()
    {
        yield return null;

        _enemyClearCheckCoroutine = null;

        if (_isGameEnded)
            yield break;

        CheckGameClear();
    }


    // =========================================================
    // 아군 피격
    // =========================================================

    /// <summary>
    /// 아군 피격 시에는 즉시 패배 처리하지 않는다.
    /// 턴 변경 시점에서 CheckGameOver()가 최종 판단한다.
    /// </summary>
    private void HandleAllyDamaged(DamageResultData result)
    {
        if (_isGameEnded)
            return;

        Debug.Log(
            "[KTH_GameEndManager] 아군 피격 → 턴 종료 후 패배 여부 검사"
        );
    }


    // =========================================================
    // 승리 판정
    // =========================================================

    private void CheckGameClear()
    {
        if (_isGameEnded)
            return;

        if (_enemies.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_GameEndManager] 등록된 적이 없습니다."
            );

            return;
        }

        // 살아있는 적이 하나라도 있으면 클리어하지 않는다.
        for (int i = 0; i < _enemies.Count; i++)
        {
            LDY_Animal enemy = _enemies[i];

            // GameObject가 Destroy되었으면 죽은 것으로 취급
            if (enemy == null)
                continue;

            // Health가 없으면 검사에서 제외
            if (enemy.health == null)
                continue;

            // 아직 살아있는 적이 있음
            if (!enemy.health.IsDestroyed)
            {
                return;
            }
        }

        // 모든 적이 죽음
        Debug.Log(
            "[KTH_GameEndManager] ★ 모든 Enemy 사망 확인 → 즉시 클리어"
        );

        ClearStage();
    }


    // =========================================================
    // 패배 판정
    // =========================================================

    private void CheckGameOver()
    {
        if (_isGameEnded)
            return;

        // 동적으로 생성된 아군이 있을 수 있으므로 다시 검색
        RegisterAllies();

        if (_allies.Count == 0)
        {
            // 아직 아군이 배치되지 않은 상태
            return;
        }

        // 살아있는 아군이 하나라도 있으면 계속
        for (int i = 0; i < _allies.Count; i++)
        {
            LDY_Animal ally = _allies[i];

            if (ally == null)
                continue;

            if (ally.health == null)
                continue;

            if (!ally.health.IsDestroyed)
            {
                return;
            }
        }

        // 모든 아군 사망
        FailStage();
    }


    // =========================================================
    // 클리어
    // =========================================================

    private void ClearStage()
    {
        if (_isGameEnded)
            return;

        _isGameEnded = true;

        if (_enemyClearCheckCoroutine != null)
        {
            StopCoroutine(_enemyClearCheckCoroutine);
            _enemyClearCheckCoroutine = null;
        }

        if (LDY_MapManager.Instance == null)
        {
            Debug.LogError(
                "[KTH_GameEndManager] LDY_MapManager.Instance를 찾을 수 없습니다."
            );

            return;
        }

        LDY_NodeType stageType = ResolveCurrentStageType();

        Debug.Log(
            $"[KTH_GameEndManager] 스테이지 클리어 타입: {stageType}"
        );

        switch (stageType)
        {
            case LDY_NodeType.Boss:
                ClearBossStage();
                break;

            case LDY_NodeType.Battle:
            default:
                ClearBattleStage();
                break;
        }
    }


    private void ClearBattleStage()
    {
        Debug.Log(
            "[KTH_GameEndManager] ★ 일반 배틀 스테이지 클리어!"
        );

        onBattleClear?.Invoke();

        LDY_MapManager.Instance.CompleteActiveNodeAndReturnToMap();
    }


    private void ClearBossStage()
    {
        Debug.Log(
            "[KTH_GameEndManager] ★ 보스 스테이지 클리어!"
        );

        onBossClear?.Invoke();

        // 보스는 KTH_ChapterClearManager에게 넘긴다.
        // 다음 챕터가 있으면 MapScene으로 돌아가고,
        // 마지막 챕터면 완전 클리어 처리를 한다.
        if (KTH_ChapterClearManager.Instance == null)
        {
            Debug.LogError(
                "[KTH_GameEndManager] " +
                "KTH_ChapterClearManager를 찾을 수 없습니다."
            );

            return;
        }

        KTH_ChapterClearManager.Instance.HandleBossClear();
    }


    // =========================================================
    // 패배
    // =========================================================

    private void FailStage()
    {
        if (_isGameEnded)
            return;

        _isGameEnded = true;

        Debug.Log(
            "[KTH_GameEndManager] ★ 모든 Ally가 사망했습니다. 스테이지 패배!"
        );

        if (LDY_MapManager.Instance == null)
        {
            Debug.LogError(
                "[KTH_GameEndManager] LDY_MapManager.Instance를 찾을 수 없습니다."
            );

            return;
        }

        LDY_MapManager.Instance.FailActiveNodeAndReturnToMap();
    }


    // =========================================================
    // 현재 스테이지 타입
    // =========================================================

    private LDY_NodeType ResolveCurrentStageType()
    {
        LDY_MapManager mapManager = LDY_MapManager.Instance;

        if (mapManager == null)
            return LDY_NodeType.Battle;

        int activeIndex = mapManager.ActiveNodeIndex;

        if (activeIndex < 0 ||
            activeIndex >= mapManager.Nodes.Count)
        {
            Debug.LogWarning(
                "[KTH_GameEndManager] " +
                "ActiveNodeIndex가 유효하지 않아 Battle로 취급합니다."
            );

            return LDY_NodeType.Battle;
        }

        return mapManager.Nodes[activeIndex].type;
    }


    // =========================================================
    // 이벤트 해제
    // =========================================================

    private void UnregisterEnemies()
    {
        foreach (LDY_Animal enemy in _enemies)
        {
            if (enemy != null && enemy.health != null)
            {
                enemy.health.OnDamage -= HandleEnemyDamaged;
            }
        }

        _enemies.Clear();
    }


    private void UnregisterAllies()
    {
        foreach (LDY_Animal ally in _allies)
        {
            if (ally != null && ally.health != null)
            {
                ally.health.OnDamage -= HandleAllyDamaged;
            }
        }

        _allies.Clear();
    }
}