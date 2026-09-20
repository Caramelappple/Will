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
            _placer = FindAnyObjectByType<LDY_CardPlacer>(FindObjectsInactive.Include);
            _attack = FindAnyObjectByType<LDY_AttackSystem>();
            _turn = FindAnyObjectByType<LDY_TurnManager>();
            _reward = FindAnyObjectByType<LSO_RewardBox>();
            _stage = FindAnyObjectByType<LDY_StageDirector>();
            foreach (var unit in FindObjectsByType<LDY_Animal>(FindObjectsSortMode.None))
            {
                if (unit.team != LDY_Team.Player) continue;
                _positions[unit] = unit.pos;
                if (unit.WillType == will) _willUnits.Add(unit);
            }
            if (_placer != null) _placer.Placed += OnPlaced;
            if (_attack != null) _attack.AttackCompleted += OnAttacked;
            if (_turn != null) _turn.OnTurnChanged += OnTurnChanged;
            if (_reward != null) _reward.OnFinished += OnRewardFinished;
            if (_stage != null) _stage.OnStageLoaded += OnStageLoaded;
            _routine = Runner.StartCoroutine(Wait());
        }

        protected override void OnDisarm()
        {
            if (_placer != null) _placer.Placed -= OnPlaced;
            if (_attack != null) _attack.AttackCompleted -= OnAttacked;
            if (_turn != null) _turn.OnTurnChanged -= OnTurnChanged;
            if (_reward != null) _reward.OnFinished -= OnRewardFinished;
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

        private void OnStageLoaded(LDY_StageSO stage)
        {
            if (condition == Condition.StageLoaded) _occurred = true;
        }

        private IEnumerator Wait()
        {
            // 직전 실습의 클릭이 다음 관문까지 통과하지 않게 한 프레임 양보한다.
            yield return null;
            while (!_occurred && !Satisfied()) yield return null;
            _routine = null;
            Pass();
        }

        private bool Satisfied()
        {
            switch (condition)
            {
                case Condition.InfoClosed:
                    var panel = DLJ_InfoPanel.Instance;
                    // 대본의 '아무 데나 클릭'은 정보창의 콜라이더 밖도 포함한다.
                    if (panel != null && !panel.IsHidden && Mouse.current != null &&
                        (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                        panel.Hide();
                    return panel != null && panel.IsHidden;
                case Condition.Moved:
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
                    var candle = FindAnyObjectByType<LSO_WillCandle>();
                    return candle != null && candle.Current == will;
                case Condition.WillPainted:
                    var card = KTH_HandCard.ConfirmedCard;
                    var mark = card != null ? card.GetComponentInChildren<LSO_CardWill>(true) : null;
                    return mark != null && mark.HasWill && mark.Will == will;
                case Condition.EnemyTurn:
                    return _turn != null && _turn.CurrentTurn == LDY_Team.Enemy;
                case Condition.WillDied:
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
