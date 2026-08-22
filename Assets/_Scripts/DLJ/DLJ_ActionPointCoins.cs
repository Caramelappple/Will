using System.Collections.Generic;
using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 행동력의 소모/회복 상태를 동전 뒤집기로 표시한다.
/// Coins 리스트의 앞쪽 동전부터 행동력 1포인트씩 대응한다.
/// </summary>
public class DLJ_ActionPointCoins : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LDY_ActionPointManager actionPoints;
    [SerializeField] private LDY_TurnManager turnManager;
    [SerializeField] private List<Transform> coins = new List<Transform>();

    [Header("Flip Animation")]
    [SerializeField, Min(0f)] private float flipDuration = 0.25f;
    [SerializeField, Min(0f)] private float consumeFlipInterval = 0.08f;
    [SerializeField] private float spentRotationZ = 180f;
    [SerializeField] private Ease flipEase = Ease.InOutSine;

    private readonly Queue<CoinState> _availableCoins = new Queue<CoinState>();
    private readonly Stack<CoinState> _spentCoins = new Stack<CoinState>();
    private readonly Dictionary<Transform, Tween> _coinTweens = new Dictionary<Transform, Tween>();
    private readonly List<CoinState> _coinStates = new List<CoinState>();

    private LDY_ActionPointManager _subscribedActionPoints;
    private LDY_TurnManager _subscribedTurnManager;
    private bool _coinsInitialized;
    private bool _hasVisualState;

    private sealed class CoinState
    {
        public readonly Transform Transform;
        public readonly Vector3 ReadyEulerAngles;

        public CoinState(Transform coin)
        {
            Transform = coin;
            ReadyEulerAngles = coin.localEulerAngles;
        }
    }

        private void Awake()
        {
            InitializeCoins();
        }

        private void OnEnable()
        {
            TryBindTurnManager();
            TryBindActionPoints();
        }

        private void Start()
        {
            // 실행 순서 때문에 Awake/OnEnable에서 singleton이 아직 준비되지 않은 경우를 보완한다.
            TryBindTurnManager();
            TryBindActionPoints();
        }

        private void OnDisable()
        {
            UnbindActionPoints();
            UnbindTurnManager();
            KillCoinTweens();
        }

        private void TryBindTurnManager()
        {
            if (_subscribedTurnManager != null) return;

            if (turnManager == null)
                turnManager = FindFirstObjectByType<LDY_TurnManager>();

            if (turnManager == null) return;

            _subscribedTurnManager = turnManager;
            _subscribedTurnManager.OnTurnChanged += HandleTurnChanged;
        }

        private void UnbindTurnManager()
        {
            if (_subscribedTurnManager == null) return;

            _subscribedTurnManager.OnTurnChanged -= HandleTurnChanged;
            _subscribedTurnManager = null;
        }

        private void InitializeCoins()
        {
            if (_coinsInitialized) return;

            _coinStates.Clear();
            foreach (Transform coin in coins)
            {
                if (coin != null)
                    _coinStates.Add(new CoinState(coin));
            }

            _coinsInitialized = true;
        }

        private void TryBindActionPoints()
        {
            if (_subscribedActionPoints != null) return;

            if (actionPoints == null)
                actionPoints = LDY_ActionPointManager.instance;

            if (actionPoints == null) return;

            _subscribedActionPoints = actionPoints;
            _subscribedActionPoints.OnActionPointsChanged += HandleActionPointsChanged;

            if (!_hasVisualState)
            {
                bool isEnemyTurn = turnManager != null && turnManager.CurrentTurn == LDY_Team.Enemy;
                int initialCurrent = isEnemyTurn
                    ? _subscribedActionPoints.Max
                    : _subscribedActionPoints.Current;
                SynchronizeImmediately(initialCurrent, _subscribedActionPoints.Max);
            }
            else if (turnManager == null || turnManager.CurrentTurn == LDY_Team.Player)
            {
                SynchronizeImmediately(_subscribedActionPoints.Current, _subscribedActionPoints.Max);
            }
        }

        private void UnbindActionPoints()
        {
            if (_subscribedActionPoints == null) return;

            _subscribedActionPoints.OnActionPointsChanged -= HandleActionPointsChanged;
            _subscribedActionPoints = null;
        }

        private void HandleActionPointsChanged(int current, int max)
        {
            // 적 턴의 AP 리셋과 소모는 플레이어 코인 UI에 반영하지 않는다.
            if (turnManager != null && turnManager.CurrentTurn == LDY_Team.Enemy)
                return;

            int targetSpentCount = GetTargetSpentCount(current, max);

            if (targetSpentCount > _spentCoins.Count)
            {
                int coinsToSpend = targetSpentCount - _spentCoins.Count;
                for (int i = 0; i < coinsToSpend && _availableCoins.Count > 0; i++)
                {
                    CoinState coin = _availableCoins.Dequeue();
                    _spentCoins.Push(coin);
                    AnimateCoin(coin, true, i * consumeFlipInterval);
                }
            }
            else
            {
                // AP가 한 번에 가득 차는 턴 전환도 모두 같은 프레임에 시작한다.
                while (_spentCoins.Count > targetSpentCount)
                {
                    CoinState coin = _spentCoins.Pop();
                    AnimateCoin(coin, false, 0f);
                }

                // Stack에서 되돌린 동전이 큐의 맨 뒤로 밀리면 다음 차감 순서가 깨진다.
                // 직렬화된 Coins 순서를 기준으로 사용 가능 큐를 다시 만든다.
                RebuildAvailableQueue(targetSpentCount);
            }
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            if (team != LDY_Team.Player || actionPoints == null) return;

            // 적 턴 동안 멈춰 둔 코인을 다음 플레이어 턴 시작 시 한꺼번에 복구한다.
            HandleActionPointsChanged(actionPoints.Current, actionPoints.Max);
        }

        private void SynchronizeImmediately(int current, int max)
        {
            InitializeCoins();
            KillCoinTweens();
            _availableCoins.Clear();
            _spentCoins.Clear();

            int targetSpentCount = GetTargetSpentCount(current, max);
            for (int i = 0; i < _coinStates.Count; i++)
            {
                CoinState coin = _coinStates[i];
                bool isSpent = i < targetSpentCount;
                coin.Transform.localEulerAngles = GetTargetEulerAngles(coin, isSpent);

                if (isSpent)
                    _spentCoins.Push(coin);
                else
                    _availableCoins.Enqueue(coin);
            }

            _hasVisualState = true;
        }

        private int GetTargetSpentCount(int current, int max)
        {
            return Mathf.Clamp(max - current, 0, _coinStates.Count);
        }

        private void RebuildAvailableQueue(int spentCount)
        {
            _availableCoins.Clear();
            for (int i = spentCount; i < _coinStates.Count; i++)
                _availableCoins.Enqueue(_coinStates[i]);
        }

        private void AnimateCoin(CoinState coin, bool isSpent, float delay)
        {
            if (_coinTweens.TryGetValue(coin.Transform, out Tween runningTween))
                runningTween.Kill();

            Tween tween = coin.Transform
                .DOLocalRotate(
                    GetTargetEulerAngles(coin, isSpent),
                    flipDuration,
                    RotateMode.FastBeyond360)
                .SetDelay(delay)
                .SetEase(flipEase)
                .SetLink(gameObject);

            _coinTweens[coin.Transform] = tween;
            tween.OnKill(() =>
            {
                if (_coinTweens.TryGetValue(coin.Transform, out Tween trackedTween) && trackedTween == tween)
                    _coinTweens.Remove(coin.Transform);
            });
        }

        private Vector3 GetTargetEulerAngles(CoinState coin, bool isSpent)
        {
            return coin.ReadyEulerAngles + (isSpent ? Vector3.right * spentRotationZ : Vector3.zero);
        }

        private void KillCoinTweens()
        {
            Tween[] tweens = new Tween[_coinTweens.Count];
            _coinTweens.Values.CopyTo(tweens, 0);
            _coinTweens.Clear();

            foreach (Tween tween in tweens)
                tween?.Kill();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            flipDuration = Mathf.Max(0f, flipDuration);
            consumeFlipInterval = Mathf.Max(0f, consumeFlipInterval);
        }
#endif
    }
