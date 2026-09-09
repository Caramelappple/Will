using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Stage;
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
    private Coroutine _allyDefeatCheckCoroutine;

    [Header("턴 매니저 참조")]
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("스테이지 참조")]
    [Tooltip("판이 새로 세워질 때 명단을 다시 적으려고 본다. 비워두면 씬에서 찾는다.\n" +
             "\n" +
             "이게 없으면 첫 판의 기물만 명단에 남는다. 그 기물들은 다음 판이 세워질 때\n" +
             "파괴되므로, 명단이 통째로 유령이 되어 턴만 넘겨도 클리어로 읽힌다.")]
    [SerializeField] private LDY_StageDirector stageDirector;

    [Header("스테이지 클리어 시 연출용 (선택)")]
    [Tooltip("보스인지 일반인지 가르지 않는다. 그 판단은 LSO_StageProgression이 하고,\n" +
             "갈래 연출은 LSO_StageIntroDirector가 발행한다.")]
    [SerializeField] private UnityEvent onStageClear;

    [Header("패배 시 연출용 (선택)")]
    [SerializeField] private UnityEvent onDefeat;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        SubscribeStageDirector();

        RegisterEnemies();
        RegisterAllies();

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += HandleTurnChanged;
        }

        // 게임 시작 직후 상태 확인
        CheckGameClear();
        CheckGameOver();
    }


    private void OnDestroy()
    {
        StopPendingChecks();

        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= HandleTurnChanged;
        }

        if (stageDirector != null)
        {
            stageDirector.OnStageLoaded -= HandleStageLoaded;
        }

        UnregisterEnemies();
        UnregisterAllies();
    }


    // =========================================================
    // 스테이지가 새로 세워질 때
    // =========================================================

    /// <summary>
    /// 판이 새로 세워지는 것을 듣는다.
    ///
    /// 명단은 Start에서 한 번만 적혔는데, 스테이지를 세울 때마다
    /// LDY_BoardClearStep이 판 위의 기물을 전부 파괴한다.
    /// 다시 적지 않으면 명단에는 파괴된 참조만 남는다.
    /// </summary>
    private void SubscribeStageDirector()
    {
        if (stageDirector == null)
        {
            stageDirector = FindAnyObjectByType<LDY_StageDirector>();
        }

        if (stageDirector == null)
        {
            Debug.LogWarning(
                "[KTH_GameEndManager] LDY_StageDirector를 찾지 못해 " +
                "판이 새로 세워져도 적 명단을 다시 적지 못합니다. " +
                "명단이 낡으면 턴만 넘겨도 클리어로 읽힙니다.",
                this
            );

            return;
        }

        stageDirector.OnStageLoaded -= HandleStageLoaded;
        stageDirector.OnStageLoaded += HandleStageLoaded;
    }


    private void HandleStageLoaded(LDY_StageSO stage)
    {
        Debug.Log(
            $"[KTH_GameEndManager] 새 판 — 명단을 다시 적습니다. " +
            $"({(stage != null ? stage.stageName : "알 수 없음")})"
        );

        Refresh();
    }


    /// <summary>
    /// 지금 판을 기준으로 명단을 다시 적는다.
    ///
    /// 승패 판정도 함께 푼다. 지난 판에서 이겼다는 표시가 남아 있으면
    /// 다음 판은 아무리 잡아도 클리어되지 않는다.
    /// </summary>
    public void Refresh()
    {
        StopPendingChecks();

        UnregisterEnemies();
        UnregisterAllies();

        _isGameEnded = false;

        RegisterEnemies();
        RegisterAllies();
    }


    private void StopPendingChecks()
    {
        if (_enemyClearCheckCoroutine != null)
        {
            StopCoroutine(_enemyClearCheckCoroutine);
            _enemyClearCheckCoroutine = null;
        }

        if (_allyDefeatCheckCoroutine != null)
        {
            StopCoroutine(_allyDefeatCheckCoroutine);
            _allyDefeatCheckCoroutine = null;
        }
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

        // 아군 전멸은 턴 변경 시 검사한다.
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

        while (HasPlayingDeathAnimation(_enemies))
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

        // 마지막 기물이 쓰러지기도 전에 씬을 바꾸지 않는다.
        if (HasPlayingDeathAnimation(_enemies))
            return;

        if (_enemies.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_GameEndManager] 등록된 적이 없습니다."
            );

            return;
        }

        // 살았는지 죽었는지 실제로 확인할 수 있었던 적의 수.
        // 명단에 이름은 있는데 전부 확인이 안 되면, 다 잡아서가 아니라
        // 명단이 낡아서일 수 있다. 그 둘을 갈라야 한다.
        int checkedCount = 0;

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

            checkedCount++;

            // 아직 살아있는 적이 있음
            if (!enemy.health.IsDestroyed)
            {
                return;
            }
        }

        // 명단에 이름은 남았는데 하나도 확인하지 못했다.
        //
        // 잡아서 죽은 적은 죽는 순간 HandleEnemyDamaged가 잡아내므로 여기까지 오지 않는다.
        // 여기까지 왔다는 것은 명단이 낡았다는 뜻이다 — 판이 새로 세워지면서
        // 명단의 기물이 통째로 파괴됐고, 새로 놓인 적은 아무도 적히지 않았다.
        //
        // 이걸 클리어로 읽으면 살아있는 적을 눈앞에 두고 판이 넘어간다.
        if (checkedCount == 0)
        {
            Debug.LogWarning(
                $"[KTH_GameEndManager] 명단에 적 {_enemies.Count}기가 있지만 하나도 확인할 수 없습니다. " +
                "명단이 낡았습니다. 클리어로 치지 않습니다. " +
                "Stage Director 연결을 확인하세요 — 판이 새로 세워질 때 명단을 다시 적는 곳입니다.",
                this
            );

            return;
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

        if (HasPlayingDeathAnimation(_allies))
        {
            RequestGameOverCheck();
            return;
        }

        // 동적으로 생성된 아군이 있을 수 있으므로 다시 검색
        RegisterAllies();

        if (_allies.Count == 0)
        {
            // 아직 아군이 배치되지 않은 상태
            return;
        }

        // 적 쪽과 같은 이유로 확인된 수를 센다. CheckGameClear의 주석을 볼 것.
        // 이쪽이 더 위험하다 — 잘못 읽으면 클리어가 아니라 패배가 뜬다.
        int checkedCount = 0;

        // 살아있는 아군이 하나라도 있으면 계속
        for (int i = 0; i < _allies.Count; i++)
        {
            LDY_Animal ally = _allies[i];

            if (ally == null)
                continue;

            if (ally.health == null)
                continue;

            checkedCount++;

            if (!ally.health.IsDestroyed)
            {
                return;
            }
        }

        if (checkedCount == 0)
        {
            Debug.LogWarning(
                $"[KTH_GameEndManager] 명단에 아군 {_allies.Count}기가 있지만 하나도 확인할 수 없습니다. " +
                "명단이 낡았습니다. 패배로 치지 않습니다.",
                this
            );

            return;
        }

        // 모든 아군 사망
        FailStage();
    }

    private void RequestGameOverCheck()
    {
        if (_isGameEnded || _allyDefeatCheckCoroutine != null)
            return;

        _allyDefeatCheckCoroutine = StartCoroutine(CheckGameOverAfterDeathAnimation());
    }

    private IEnumerator CheckGameOverAfterDeathAnimation()
    {
        while (HasPlayingDeathAnimation(_allies))
            yield return null;

        _allyDefeatCheckCoroutine = null;
        CheckGameOver();
    }

    private static bool HasPlayingDeathAnimation(List<LDY_Animal> animals)
    {
        for (int i = 0; i < animals.Count; i++)
        {
            LDY_Animal animal = animals[i];
            if (animal == null) continue;

            DLJ_DeathAnimation animation = animal.GetComponent<DLJ_DeathAnimation>();
            if (animation != null && animation.IsPlaying)
                return true;
        }

        return false;
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

        StartCoroutine(Co_ClearStage());
    }

    /// <summary>
    /// 마지막 타격의 복귀 애니메이션(LDY_AttackSystem.StrikeOnce)이 아직 돌고 있으면
    /// 씬 전환/승리 판정 전달을 기다린다. 안 그러면 씬이 넘어가면서
    /// 트윈이 물고 있던 Transform이 파괴돼 DOTween 경고가 뜬다.
    /// </summary>
    private IEnumerator Co_ClearStage()
    {
        if (turnManager != null)
        {
            while (turnManager.IsAnimating())
                yield return null;
        }

        Debug.Log("[KTH_GameEndManager] ★ 스테이지 클리어!");

        // 보스인지 일반인지 여기서 가르지 않는다.
        // 그 판단은 진행(LSO_StageProgression)이 하고, 흐름은 LSO_StageFlow가 안다.
        // 두 곳에서 판단하면 어긋났을 때 어느 쪽이 맞는지 정할 수 없다.
        onStageClear?.Invoke();

        if (LSO_StageFlow.HasInstance)
        {
            LSO_StageFlow.Instance.ClearStage();
            yield break;
        }

        Debug.LogError(
            "[KTH_GameEndManager] LSO_StageFlow를 찾을 수 없어 클리어를 넘기지 못했습니다."
        );
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

        onDefeat?.Invoke();

        if (LSO_StageFlow.HasInstance)
        {
            LSO_StageFlow.Instance.Defeat();
            return;
        }

        Debug.LogError(
            "[KTH_GameEndManager] LSO_StageFlow를 찾을 수 없어 패배를 넘기지 못했습니다."
        );
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
