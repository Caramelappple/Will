using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Reward;
using _Scripts.LSO.Will;
using _Scripts.LSO.Will.Candle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>실습은 입력 자체가 아니라 게임에서 확인한 결과를 기다린다.</summary>
    [CreateAssetMenu(fileName = "Gate_Practice", menuName = "SO/Tutorial/Gate/실습 결과")]
    public sealed class LSO_GatePractice : LSO_TutorialGateSO
    {
        public enum Condition
        {
            InfoClosed, Placed, Moved, Attacked, WillSelected, WillPainted,
            EnemyTurn, WillDied, RewardReady, RewardOpened, RewardDealt,
            RewardFinished, StageLoaded
        }

        public Condition condition;
        public Vector3Int tile;
        public bool requireTile;
        public LSO_WillType will = LSO_WillType.Rage;
        public bool requireWill;

        private Coroutine _routine;
        private LDY_CardPlacer _placer;
        private LDY_AttackSystem _attack;
        private LDY_TurnManager _turn;
        private LSO_WillCandle _candle;
        private LSO_RewardBox _reward;
        private LDY_StageDirector _stage;
        private readonly Dictionary<LDY_Animal, Vector3Int> _positions = new();
        private readonly List<LDY_Animal> _willUnits = new();
        private bool _occurred;

        protected override void OnArm()
        {
            _occurred = false;
            _positions.Clear();
            _willUnits.Clear();
            // 튜토리얼 잠금이 컴포넌트를 먼저 꺼둔 프레임에도 관찰자를 찾아야 한다.
            // 활성 오브젝트만 찾으면 올바른 행동을 해도 완료 신호를 받지 못한다.
            _placer = FindAnyObjectByType<LDY_CardPlacer>(FindObjectsInactive.Include);
            _attack = FindAnyObjectByType<LDY_AttackSystem>(FindObjectsInactive.Include);
            _turn = FindAnyObjectByType<LDY_TurnManager>(FindObjectsInactive.Include);
            _candle = FindAnyObjectByType<LSO_WillCandle>(FindObjectsInactive.Include);
            BindReward();
            _stage = FindAnyObjectByType<LDY_StageDirector>(FindObjectsInactive.Include);
            foreach (var unit in FindObjectsByType<LDY_Animal>(FindObjectsSortMode.None))
            {
                if (unit.team != LDY_Team.Player) continue;
                _positions[unit] = unit.pos;
                if (unit.WillType == will) _willUnits.Add(unit);
            }
            if (_placer != null) _placer.Placed += OnPlaced;
            if (_attack != null) _attack.AttackCompleted += OnAttacked;
            if (_turn != null) _turn.OnTurnChanged += OnTurnChanged;
            if (_stage != null) _stage.OnStageLoaded += OnStageLoaded;
            _routine = Runner.StartCoroutine(Wait());
        }

        protected override void OnDisarm()
        {
            if (_placer != null) _placer.Placed -= OnPlaced;
            if (_attack != null) _attack.AttackCompleted -= OnAttacked;
            if (_turn != null) _turn.OnTurnChanged -= OnTurnChanged;
            if (_reward != null) _reward.OnFinished -= OnRewardFinished;
            _reward = null;
            if (_stage != null) _stage.OnStageLoaded -= OnStageLoaded;
            if (_routine != null && Runner != null) Runner.StopCoroutine(_routine);
            _routine = null;
            _positions.Clear();
            _willUnits.Clear();
        }

        private void OnPlaced(LDY_Animal unit)
        {
            if (condition == Condition.Placed && unit != null && unit.team == LDY_Team.Player &&
                (!requireTile || unit.pos == tile) && (!requireWill || unit.WillType == will))
                _occurred = true;
        }

        private void OnAttacked(LDY_Animal unit)
        {
            if (condition == Condition.Attacked && unit != null && unit.team == LDY_Team.Player &&
                (!requireWill || unit.WillType == will)) _occurred = true;
        }

        private void OnTurnChanged(LDY_Team team)
        {
            if (condition == Condition.EnemyTurn && team == LDY_Team.Enemy) _occurred = true;
        }

        private void OnRewardFinished(LSO_RewardOption option)
        {
            if (condition == Condition.RewardFinished && option != null) _occurred = true;
        }

        /// <summary>
        /// 전투 중 보상 상자는 보드 연출이 자식 오브젝트를 꺼서 감춘다.
        /// 활성 오브젝트만 찾으면 RewardReady 관문을 준비하는 순간에는 상자를 못 찾고,
        /// 이후 상자가 켜져도 null을 계속 들고 있어 튜토리얼이 영원히 멈춘다.
        ///
        /// 비활성 오브젝트까지 찾고, 런타임에 상자가 교체돼도 같은 구독을 옮긴다.
        /// </summary>
        private void BindReward()
        {
            LSO_RewardBox found = FindAnyObjectByType<LSO_RewardBox>(FindObjectsInactive.Include);

            if (_reward == found) return;

            if (_reward != null)
                _reward.OnFinished -= OnRewardFinished;

            _reward = found;

            if (_reward != null)
                _reward.OnFinished += OnRewardFinished;
        }

        private void OnStageLoaded(LDY_StageSO stage)
        {
            if (condition == Condition.StageLoaded) _occurred = true;
        }

        /// <summary>
        /// 결과를 관찰할 주체가 없으면 플레이어 행동으로 해결할 수 없는 관문이다.
        /// 배선 오류는 남기되 튜토리얼 전체를 영구 정지시키지는 않는다.
        /// </summary>
        private bool MissingRequiredObserver()
        {
            string missing = condition switch
            {
                Condition.Placed when _placer == null => nameof(LDY_CardPlacer),
                Condition.Attacked when _attack == null => nameof(LDY_AttackSystem),
                Condition.EnemyTurn when _turn == null => nameof(LDY_TurnManager),
                Condition.WillSelected when _candle == null => nameof(LSO_WillCandle),
                Condition.StageLoaded when _stage == null => nameof(LDY_StageDirector),
                _ => null
            };

            if (missing == null) return false;

            Debug.LogError(
                $"{name}: {condition} 관문에 필요한 {missing}을 찾지 못해 이 단계를 건너뜁니다.",
                this);
            return true;
        }

        private IEnumerator Wait()
        {
            // 직전 실습의 클릭이 다음 관문까지 통과하지 않게 한 프레임 양보한다.
            yield return null;

            if (MissingRequiredObserver())
            {
                _routine = null;
                Pass();
                yield break;
            }

            while (!_occurred)
            {
                // OnArm 때 상자가 아직 생성되지 않았거나 파괴된 경우만 다시 찾는다.
                // 정상적으로 찾은 뒤에는 매 프레임 씬 전체를 검색하지 않는다.
                if (_reward == null) BindReward();

                if (Satisfied()) break;

                yield return null;
            }
            _routine = null;
            Pass();
        }

        private bool Satisfied()
        {
            switch (condition)
            {
                case Condition.InfoClosed:
                    var panel = DLJ_InfoPanel.Instance;
                    // 정보창 자체가 없는 씬에서는 닫는 행동을 할 수 없다.
                    if (panel == null) return true;
                    // 대본의 '아무 데나 클릭'은 정보창의 콜라이더 밖도 포함한다.
                    if (!panel.IsHidden && Mouse.current != null &&
                        (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                        panel.Hide();
                    return panel.IsHidden;
                case Condition.Moved:
                    // 감시할 아군이 없다면 이동 결과는 앞으로도 생기지 않는다.
                    if (_positions.Count == 0) return true;
                    foreach (var entry in _positions)
                        if (entry.Key != null && entry.Key.pos != entry.Value &&
                            (!requireTile || entry.Key.pos == tile) &&
                            (!requireWill || entry.Key.WillType == will))
                        {
                            var movement = FindAnyObjectByType<LDY_MoveSystem>();
                            if (movement == null || !movement.IsMoving(entry.Key)) return true;
                        }
                    return false;
                case Condition.WillSelected:
                    return _candle != null && _candle.Current == will;
                case Condition.WillPainted:
                    var card = KTH_HandCard.ConfirmedCard;
                    var mark = card != null ? card.GetComponentInChildren<LSO_CardWill>(true) : null;
                    return mark != null && mark.HasWill && mark.Will == will;
                case Condition.EnemyTurn:
                    return _turn != null && _turn.CurrentTurn == LDY_Team.Enemy;
                case Condition.WillDied:
                    // 대상이 Arm 직전에 죽었거나 잘못된 전투 데이터로 존재하지 않는 경우.
                    // 이미 사라진 대상을 기다리며 영원히 멈추지 않는다.
                    if (_willUnits.Count == 0) return true;
                    foreach (var unit in _willUnits)
                        if (unit == null || (unit.health != null && unit.health.IsDestroyed))
                            return _turn == null || !_turn.IsAnimating();
                    return false;
                case Condition.RewardReady: return _reward != null && _reward.HasBegun && !_reward.IsBusy;
                case Condition.RewardOpened: return _reward != null && _reward.IsOpened;
                case Condition.RewardDealt: return _reward != null && _reward.IsSelecting;
                default: return false;
            }
        }
    }
}
