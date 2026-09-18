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
/// 패배:
///     **양초가 다 닳으면** 진다(DLJ_PlayerHealth). 기물을 다 잃어도
///     양초가 남아 있으면 계속한다.
/// </summary>
public class KTH_GameEndManager : MonoBehaviour
{
    private readonly List<LDY_Animal> _enemies = new();
    private readonly List<LDY_Animal> _allies = new();

    /// <summary>
    /// 이 전투가 끝났는지.
    ///
    /// 정적인 이유는 **판이 끝났다는 사실을 밖에서도 물어야 하기 때문이다.**
    /// 손패를 뽑는 쪽(KTH_StartCardSet)이 그렇다 — 적이 마지막 아군을 잡으면서
    /// 같이 죽으면 적 턴이 끝나며 내 턴이 오고, 그 신호만 보고 카드를 뽑는다.
    /// 판은 이미 끝났는데 손패가 한 번 더 채워진다.
    ///
    /// 값을 들고 있는 곳은 여기 하나다. 보는 쪽은 복사해두지 말고 그때그때 물을 것.
    /// 전투 씬에 이 관리자는 하나뿐이라 정적으로 두어도 주체가 갈리지 않는다.
    /// </summary>
    public static bool IsBattleOver { get; private set; }

    private bool _isGameEnded
    {
        get => IsBattleOver;
        set => IsBattleOver = value;
    }

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

    [Header("멈춤 방지")]
    [Tooltip("클리어를 넘기기 전에 사망 연출이 끝나기를 기다리는 상한(초).\n" +
             "\n" +
             "기다림이 영영 안 끝나는 쪽이 연출이 잘리는 쪽보다 나쁘므로 상한을 둔다.")]
    [SerializeField, Min(0.5f)] private float deathAnimationWaitTimeout = 5f;

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
        // 정적 값이라 씬을 다시 열어도 지난 판의 "끝났다"가 남아 있을 수 있다.
        // (Enter Play Mode Options 로 도메인 재로드를 끄면 특히 그렇다)
        // 여기서 한 번 지워야 새 판이 끝난 판으로 시작하지 않는다.
        _isGameEnded = false;

        SubscribeStageDirector();

        RegisterEnemies();
        RegisterAllies();

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += HandleTurnChanged;
        }

        // 양초가 꺼지는 순간 바로 안다. 턴이 바뀔 때까지 기다리면
        // 마지막 양초가 꺼진 뒤에도 한참 더 두는 것처럼 보인다.
        if (DLJ_PlayerHealth.Instance != null)
            DLJ_PlayerHealth.Instance.OnPlayerDeath += HandlePlayerDeath;

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

        if (DLJ_PlayerHealth.Instance != null)
            DLJ_PlayerHealth.Instance.OnPlayerDeath -= HandlePlayerDeath;

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

        // 양초는 꺼지는 순간 알지만, 놓친 경우를 위해 여기서도 한 번 본다.
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

        // 전멸을 읽는 규칙은 AllDead 하나다. 이기는 조건과 지는 조건이 같은 함수를
        // 봐야 한 쪽만 고쳤을 때 서로 다른 말을 하지 않는다.
        if (!AllDead(_enemies, out int checkedCount))
            return;

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

        // ── 양초가 다 닳았으면 이긴 것이 아니다 ───────────────────
        // 아군이 몇 기 남았는지는 보지 않는다. 기물을 다 잃어도 양초가 남아
        // 있으면 아직 진 것이 아니다.
        //
        // 다만 그 반대는 봐야 한다. 적이 마지막 아군을 잡으면서 같이 죽으면
        // 양초가 그 자리에서 깎이는데(LDY_DeathHandler.DamagePlayerFor),
        // 적 전멸은 죽는 즉시 검사하고 양초 쪽은 연출을 기다렸다 검사한다.
        // 그래서 클리어가 먼저 달려 _isGameEnded 를 세우고, 다 닳은 양초를
        // 눈앞에 두고 보상 카드가 나갔다.
        //
        // 이기고 지는 것이 한 순간에 겹치면 지는 쪽이 이긴다.
        // ─────────────────────────────────────────────────────────
        DLJ_PlayerHealth playerHealth = DLJ_PlayerHealth.Instance;

        if (playerHealth != null && playerHealth.IsDead)
        {
            Debug.Log(
                "[KTH_GameEndManager] 적은 전멸했지만 양초가 다 닳았습니다 → 패배로 칩니다."
            );

            FailStage();
            return;
        }

        // 모든 적이 죽음
        Debug.Log(
            "[KTH_GameEndManager] ★ 모든 Enemy 사망 확인 → 즉시 클리어"
        );

        ClearStage();
    }

    /// <summary>
    /// 명단이 전부 죽었는지.
    /// </summary>
    /// <param name="checkedCount">
    /// 살았는지 죽었는지 **실제로 확인할 수 있었던** 수.
    ///
    /// 0이면 다 죽어서가 아니라 명단이 낡아서다 — 판이 새로 세워지며 기물이
    /// 통째로 파괴됐는데 아무도 다시 적히지 않은 경우다. 그걸 전멸로 읽으면
    /// 살아있는 편을 눈앞에 두고 판이 끝난다. 부르는 쪽이 이 값을 볼 것.
    /// </param>
    private static bool AllDead(List<LDY_Animal> animals, out int checkedCount)
    {
        checkedCount = 0;

        for (int i = 0; i < animals.Count; i++)
        {
            LDY_Animal animal = animals[i];

            // 파괴된 것은 죽은 것으로 친다.
            if (animal == null) continue;
            if (animal.health == null) continue;

            checkedCount++;

            if (!animal.health.IsDestroyed) return false;
        }

        return true;
    }


    // =========================================================
    // 패배 판정
    // =========================================================

    /// <summary>
    /// 패배했는지 본다.
    ///
    /// ── 기준이 바뀌었다 ───────────────────────────────────────
    /// 예전에는 아군이 전멸하면 졌다. 지금은 **양초가 다 닳을 때** 진다.
    ///
    /// 기물을 다 잃어도 양초가 남아 있으면 계속한다. 그래서 적이 마지막 아군을
    /// 잡은 뒤 유언에 휘말려 죽는 판은 그냥 클리어다 — 잃은 것은 기물이지
    /// 판이 아니다.
    ///
    /// 세는 곳은 DLJ_PlayerHealth 하나다. 여기서 양초를 따로 세지 않는다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    private void CheckGameOver()
    {
        if (_isGameEnded)
            return;

        DLJ_PlayerHealth health = DLJ_PlayerHealth.Instance;

        if (health == null)
        {
            if (!_warnedMissingPlayerHealth)
            {
                _warnedMissingPlayerHealth = true;

                Debug.LogWarning(
                    "[KTH_GameEndManager] DLJ_PlayerHealth를 찾지 못해 패배 판정을 못 합니다. " +
                    "양초가 다 닳아도 판이 끝나지 않습니다. (이 경고는 한 번만 나옵니다)",
                    this
                );
            }

            return;
        }

        if (!health.IsDead)
            return;

        // 마지막 기물이 쓰러지는 중이면 그것부터 보여준다.
        if (HasPlayingDeathAnimation(_allies) || HasPlayingDeathAnimation(_enemies))
        {
            RequestGameOverCheck();
            return;
        }

        FailStage();
    }

    private bool _warnedMissingPlayerHealth;

    /// <summary>마지막 양초가 꺼졌다. 연출이 끝나면 패배로 넘긴다.</summary>
    private void HandlePlayerDeath()
    {
        CheckGameOver();
    }

    private void RequestGameOverCheck()
    {
        if (_isGameEnded || _allyDefeatCheckCoroutine != null)
            return;

        _allyDefeatCheckCoroutine = StartCoroutine(CheckGameOverAfterDeathAnimation());
    }

    private IEnumerator CheckGameOverAfterDeathAnimation()
    {
        while (HasPlayingDeathAnimation(_allies) || HasPlayingDeathAnimation(_enemies))
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

        // ── 아군의 사망 연출도 기다린다 ───────────────────────────
        // IsAnimating은 이동·공격만 본다. CheckGameClear가 기다리는 사망 연출도
        // **적만** 본다. 그래서 마지막 적과 마지막 아군이 같이 죽으면, 아군이
        // 쓰러지는 도중에 클리어가 달려나가 판이 뒤집히고 보상이 올라온다.
        //
        // 일반 클리어에서는 아군이 멀쩡하니 생기지 않던 일이다. 여기서 한 번
        // 더 기다려 두 경우를 같은 모양으로 만든다.
        //
        // 상한을 두는 이유는 기다림이 영영 안 끝나는 쪽이 연출이 잘리는 쪽보다
        // 나쁘기 때문이다. 실제로 재생 표시가 굳어 클리어가 막힌 적이 있다.
        // ─────────────────────────────────────────────────────────
        float waitUntil = Time.unscaledTime + deathAnimationWaitTimeout;

        while (HasPlayingDeathAnimation(_allies) || HasPlayingDeathAnimation(_enemies))
        {
            if (Time.unscaledTime > waitUntil)
            {
                Debug.LogWarning(
                    "[KTH_GameEndManager] 사망 연출이 " +
                    $"{deathAnimationWaitTimeout:0.#}초 안에 끝나지 않아 그대로 넘어갑니다. " +
                    "DLJ_DeathAnimation 의 재생 표시가 굳었을 수 있습니다.",
                    this);

                break;
            }

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
