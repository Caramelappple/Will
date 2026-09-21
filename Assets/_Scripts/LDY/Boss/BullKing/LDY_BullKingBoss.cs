using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.DLJ.Boss;
using _Scripts.LSO.Boss;
using _Scripts.LSO.Manager;
using _Scripts.LSO.Camera;
using UnityEngine;
using _Scripts.LSO.Reward;
using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 황소왕의 조정 가능한 수치와 돌진 1회의 결과를 보관한다.
    ///
    /// 보관과 전달만 한다. "몇 칸을 밀지", "누가 죽는지"는 특성이 정하고, 여기로는 결과만 들어온다.
    /// 까마귀왕의 LSO_CrowKingMemory, 여우왕의 DLJ_FoxKingBoss와 같은 자리다.
    ///
    /// 페이즈 값을 따로 들고 있지 않고 매번 LSO_BossPhase에게 묻는 것은 의도된 것이다.
    /// 특성마다 자기 phase 필드를 캐시해두면 같은 사실이 여러 벌 생기고,
    /// 어느 하나가 LSO_IPhaseAware 구현을 빠뜨리면 그 특성만 조용히 1페이즈로 남는다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LDY_Animal), typeof(LSO_BossPhase))]
    public sealed class LDY_BullKingBoss : MonoBehaviour
    {
        [Header("1페이즈 — 난동")]
        [SerializeField] private LDY_BullChargeRule phaseOne = new()
        {
            chargeRange = 4, collisionDamage = 3, wallDamage = 2, maxChainPush = 3
        };

        [Header("2페이즈 — 광란")]
        [SerializeField] private LDY_BullChargeRule phaseTwo = new()
        {
            chargeRange = 6, collisionDamage = 4, wallDamage = 3, maxChainPush = 5
        };

        [Header("분노의 연쇄 (2페이즈)")]
        [Tooltip("돌진으로 죽은 기물이 터뜨리는 피해.")]
        [SerializeField, Min(0)] private int rageChainDamage = 3;

        [Tooltip("터지는 범위의 반지름. 1이면 3×3이다.")]
        [SerializeField, Min(0)] private int rageChainRange = 1;

        [Tooltip("한 번의 돌진에서 터질 수 있는 최대 횟수.")]
        [SerializeField, Min(0)] private int maxRageChainPerCharge = 3;

        [Tooltip("분노의 연쇄가 황소왕 자신도 때린다. 기획서의 '아군과 적군 모두'를 그대로 따르면 켠다.\n" +
                 "황소왕은 방금 들이받은 기물 바로 옆에 서 있으므로 켜두면 자기 피해가 상당하다.")]
        [SerializeField] private bool rageChainHitsBullKing = true;

        [Header("연출 — 돌진")]
        [Tooltip("최대 거리를 꽉 채워 달릴 때의 시간. 짧은 돌진은 거리에 비례해 짧아진다.\n" +
                 "즉 이 값은 속도를 정한다 — 작을수록 빠르다.")]
        [SerializeField, Min(0.05f)] private float chargeDuration = 1f;

        [Tooltip("한두 칸짜리 돌진이 순간이동처럼 보이는 것을 막는 하한.")]
        [SerializeField, Min(0f)] private float minChargeDuration = 0.2f;

        [Tooltip("돌진의 가속 곡선. 뒤로 갈수록 가팔라야 '달려들어 박는' 느낌이 난다.\n" +
                 "끝값을 1보다 크게 만들면 목적지를 살짝 지나쳤다가 돌아온다.")]
        [SerializeField] private AnimationCurve chargeEasing = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 2f));

        [Tooltip("돌진을 시작할 때 낼 소리. 사운드 매니저가 씬에 없으면 조용히 넘어간다.")]
        [SerializeField] private SoundClipSO chargeSfx;

        [Tooltip("들이받은 뒤 자기 칸 중앙으로 돌아오는 시간.")]
        [SerializeField, Min(0.01f)] private float contactReturnDuration = 0.16f;

        [Header("연출 — 충돌")]
        [Tooltip("부딪혔을 때 화면을 흔드는 시간.")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.25f;

        [Tooltip("화면 흔들림 세기. 시네머신 Impulse로 나가므로 카메라의 Listener Gain에도 곱해진다.\n" +
                 "\n" +
                 "예전 방식(카메라를 직접 밀기)과 계산이 달라 같은 숫자여도 체감이 다르다.\n" +
                 "보면서 다시 맞출 것.")]
        [SerializeField, Min(0f)] private float shakeStrength = 0.15f;

        [Tooltip("이동 중 반복되는 미세 진동 한 번의 시간과 간격.")]
        [SerializeField, Min(0f)] private float chargeShakeDuration = 0.12f;

        [Tooltip("이동 중 미세 진동 세기. 충돌 진동보다 아주 약하게 사용한다.")]
        [SerializeField, Min(0f)] private float chargeShakeStrength = 0.015f;

        [Tooltip("기물이 한 개씩 튕겨 나가기 시작할 때의 화면 흔들림 시간.")]
        [SerializeField, Min(0f)] private float pushShakeDuration = 0.12f;

        [Tooltip("기물이 한 개씩 튕겨 나가기 시작할 때의 화면 흔들림 세기.")]
        [SerializeField, Min(0f)] private float pushShakeStrength = 0.08f;

        [Tooltip("마지막 기물이 보드 끝에 부딪힐 때 추가로 얹는 화면 흔들림 시간.")]
        [SerializeField, Min(0f)] private float boardEdgeShakeDuration = 0.18f;

        [Tooltip("마지막 기물이 보드 끝에 부딪힐 때 추가로 얹는 화면 흔들림 세기.")]
        [SerializeField, Min(0f)] private float boardEdgeShakeStrength = 0.12f;

        [Tooltip("한 칸을 튕겨 날아가 착지하는 시간. 다음 기물은 표면에 닿는 순간 반응한다.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.3f;

        [Tooltip("보드 끝이나 막힌 기물에 부딪힌 뒤 자기 자리로 튕겨 돌아오는 시간.")]
        [SerializeField, Min(0f)] private float fallDuration = 0.3f;

        [Tooltip("연쇄 충격 전달과 기물 반동의 시간 배율. 1.3이면 기존보다 30% 길게 재생한다.")]
        [SerializeField, Min(0.1f)] private float chainTimeMultiplier = 1.3f;

        [Tooltip("밀려나면서 떠오르는 높이. 0이면 바닥으로 미끄러지기만 한다.")]
        [SerializeField, Min(0f)] private float pushArcHeight = 0.35f;

        [Tooltip("밀려나지 못한 기물들의 공통 반동 기준 높이. 보드 끝에 직접 닿은 기물만 조절하려면 Board Edge Hop Multiplier를 사용한다.")]
        [SerializeField, Min(0f)] private float slamHopHeight = 0.5f;

        [Tooltip("보드 끝에 직접 부딪힌 기물의 반동 높이 배율. 다른 기물에 막힌 경우에는 적용하지 않는다.")]
        [SerializeField, Min(0f)] private float boardEdgeHopMultiplier = 1.5f;

        private LDY_Animal _animal;
        private LSO_BossPhase _phase;
        private LDY_TurnManager _cryTurnManager;
        private bool _criedThisTurn;

        private DLJ_BullImpactMotion _impactMotion;
        private Coroutine _collisionRoutine;
        private Coroutine _chargeShakeRoutine;
        public bool IsResolvingCollision { get; private set; }
        internal float ContactReturnDuration => contactReturnDuration;
        internal float ChainTimeMultiplier => Mathf.Max(0.1f, chainTimeMultiplier);
        internal float PushFlightDuration => riseDuration;
        internal float PushReturnDuration => fallDuration;
        internal float PushHeight => pushArcHeight;
        internal float BlockedHeight => slamHopHeight;
        internal float BoardEdgeHopMultiplier => Mathf.Max(0f, boardEdgeHopMultiplier);

        /// <summary>현재 페이즈. LSO_BossPhase가 원본이다.</summary>
        public int Phase => BossPhase != null ? BossPhase.CurrentPhase : 1;

        /// <summary>지금 페이즈에서 쓸 돌진 수치.</summary>
        public LDY_BullChargeRule Rule => Phase >= 2 ? phaseTwo : phaseOne;

        public AnimationCurve ChargeEasing => chargeEasing;

        /// <summary>
        /// 이만큼 달릴 때 연출에 쓸 시간.
        ///
        /// 거리에 비례시키는 이유는 짧은 돌진이 생겼기 때문이다.
        /// 거리와 무관하게 같은 시간을 쓰면 한 칸 돌진이 여섯 칸 돌진과 같은 시간을 끌어
        /// 굼뜨게 보이고, 속도가 거리마다 달라져 "일정하게 내달린다"가 깨진다.
        /// </summary>
        public float ChargeDuration(int distance)
        {
            int longest = Mathf.Max(1, Rule.chargeRange);
            float ratio = Mathf.Clamp01(Mathf.Max(1, distance) / (float)longest);

            return Mathf.Max(minChargeDuration, chargeDuration * ratio);
        }

        /// <summary>적 턴의 첫 돌진에서만 운다.</summary>
        public void PlayChargeCry()
        {
            LDY_TurnManager turnManager = GameManager.HasInstance ? GameManager.Instance.TurnManager : null;
            if (_cryTurnManager != turnManager)
            {
                if (_cryTurnManager != null) _cryTurnManager.OnTurnChanged -= HandleCryTurnChanged;
                _cryTurnManager = turnManager;
                _criedThisTurn = false;
                if (_cryTurnManager != null) _cryTurnManager.OnTurnChanged += HandleCryTurnChanged;
            }

            if (turnManager == null)
            {
                Debug.LogWarning($"{name}: 턴 매니저가 없어 황소왕 첫 돌진 울음을 재생할 수 없습니다.", this);
                return;
            }
            if (turnManager.CurrentTurn != LDY_Team.Enemy || _criedThisTurn) return;
            _criedThisTurn = true;

            if (chargeSfx == null)
            {
                _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.BullCry);
                return;
            }

            ServiceLocator.Get<IAudioService>()?.PlaySfx(chargeSfx);
        }

        private void HandleCryTurnChanged(LDY_Team team)
        {
            if (team == LDY_Team.Enemy) _criedThisTurn = false;
        }

        /// <summary>DLJ: 이동하는 동안만 작은 임펄스를 반복한다.</summary>
        public void ShakeOnChargeStart()
        {
            StopChargeShake();
            _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.BullMove,
                _Scripts.LSO.Sound.LSO_GameAudio.BullMovementChannel);
            if (!isActiveAndEnabled || chargeShakeDuration <= 0f || chargeShakeStrength <= 0f) return;
            _chargeShakeRoutine = StartCoroutine(ChargeShakeRoutine());
        }

        private IEnumerator ChargeShakeRoutine()
        {
            while (isActiveAndEnabled)
            {
                LSO_CameraImpulse.Shake(chargeShakeDuration, chargeShakeStrength);
                // 매 프레임 신호를 겹치지 않도록 한 임펄스가 끝난 뒤 다음 신호를 보낸다.
                yield return new WaitForSeconds(Mathf.Max(0.05f, chargeShakeDuration));
            }
        }

        internal void StopChargeShake()
        {
            _Scripts.LSO.Sound.LSO_GameAudio.Stop(_Scripts.LSO.Sound.LSO_GameAudio.BullMovementChannel);
            if (_chargeShakeRoutine != null) StopCoroutine(_chargeShakeRoutine);
            _chargeShakeRoutine = null;
        }

        /// <summary>
        /// 부딪힌 충격으로 화면을 흔든다.
        ///
        /// 시네머신 Impulse를 거친다. 예전에는 Camera.main을 직접 밀었는데,
        /// 같은 카메라를 LSO_CameraDirector가 시네머신으로 잡고 있어서 서로 밀어냈다.
        ///
        /// 흔들림이 안 보이면 카메라에 Cinemachine Impulse Listener 가 붙었는지 볼 것.
        /// 안 붙어 있으면 콘솔에 한 번 경고가 남는다.
        /// </summary>
        public void ShakeOnCollision()
        {
            LSO_CameraImpulse.Shake(shakeDuration, shakeStrength);
        }

        /// <summary>각 기물이 앞 기물에 맞아 튕겨 나가기 시작하는 순간의 흔들림.</summary>
        public void ShakeOnPiecePushed()
        {
            LSO_CameraImpulse.Shake(pushShakeDuration, pushShakeStrength);
        }

        /// <summary>밀려난 줄의 끝이 보드 끝에 막힐 때 추가로 얹는 흔들림.</summary>
        public void ShakeOnBoardEdgeCollision()
        {
            LSO_CameraImpulse.Shake(boardEdgeShakeDuration, boardEdgeShakeStrength);
        }

        public int RageChainDamage => rageChainDamage;
        public int RageChainRange => rageChainRange;
        public int MaxRageChainPerCharge => maxRageChainPerCharge;
        public bool RageChainHitsBullKing => rageChainHitsBullKing;

        /// <summary>마지막 돌진에서 밀려난 기물 수. 인스펙터 확인용이라 게임 규칙에는 쓰지 않는다.</summary>
        public int LastPushedCount { get; internal set; }

        /// <summary>마지막 돌진에서 죽은 기물 수. 위와 같이 확인용이다.</summary>
        public int LastKilledCount { get; internal set; }

        /// <summary>
        /// 돌진 한 번이 완전히 끝났을 때, 그 돌진으로 죽은 기물이 서 있던 칸을 알린다.
        ///
        /// 죽은 기물의 참조가 아니라 칸을 싣는 이유는 두 가지다.
        ///   1. 사망 처리가 끝나면 오브젝트가 파괴되어 참조가 죽는다.
        ///   2. 분노의 연쇄가 필요로 하는 건 "어디서 터지는가"뿐이다.
        ///
        /// 충돌 처리가 다 끝난 뒤 한 번만 나간다. 기물이 죽을 때마다 바로 알리면
        /// 아직 밀려나는 중인 줄 한가운데서 분노가 터져 밀어내기가 꼬인다.
        /// </summary>
        internal event Action<IReadOnlyList<Vector3Int>> ChargeResolved;

        private LSO_BossPhase BossPhase
        {
            get
            {
                // 특성은 LDY_Animal.Awake 안에서 만들어지므로, 이 컴포넌트의 Awake보다 먼저
                // Phase를 물어올 수 있다. 그래서 캐시는 미리 채우지 않고 처음 필요할 때 잡는다.
                if (_phase == null) _phase = GetComponent<LSO_BossPhase>();
                return _phase;
            }
        }

        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();
            WarnIfMoveRangeTooShort();
        }

        private void OnEnable()
        {
            if (BossPhase != null)
                BossPhase.OnPhaseChange += LogPhaseChange;
        }

        private void OnDisable()
        {
            if (_cryTurnManager != null) _cryTurnManager.OnTurnChanged -= HandleCryTurnChanged;
            _cryTurnManager = null;
            _criedThisTurn = false;
            StopChargeShake();
            if (_phase != null)
                _phase.OnPhaseChange -= LogPhaseChange;

            // 비활성화 시 Unity가 반복자의 finally를 보장하지 않으므로 직접 정리한다.
            if (_collisionRoutine != null) StopCoroutine(_collisionRoutine);
            _collisionRoutine = null;
            _impactMotion?.Dispose();
            _impactMotion = null;
            IsResolvingCollision = false;
        }

        internal void RaiseChargeResolved(IReadOnlyList<Vector3Int> deathTiles)
        {
            ChargeResolved?.Invoke(deathTiles);
        }

        // DLJ: 특성은 코루틴을 소유하지 못하므로 보스가 충돌 행동의 수명을 관리한다.
        internal void RunCollision(IEnumerator resolution)
        {
            if (!isActiveAndEnabled || IsResolvingCollision) return;
            _collisionRoutine = StartCoroutine(CollisionRoutine(resolution));
        }

        private IEnumerator CollisionRoutine(IEnumerator resolution)
        {
            IsResolvingCollision = true;
            try
            {
                yield return resolution;
            }
            finally
            {
                _impactMotion?.Dispose();
                _impactMotion = null;
                IsResolvingCollision = false;
                _collisionRoutine = null;
            }
        }

        internal DLJ_BullImpactMotion CreateImpactMotion(LDY_Animal bull, LDY_BoardManager board,
            IReadOnlyList<LDY_Animal> chain, Vector3Int direction)
        {
            _impactMotion?.Dispose();
            _impactMotion = new DLJ_BullImpactMotion(bull, board, chain, direction);
            return _impactMotion;
        }

        private void LogPhaseChange(int phase)
        {
            if (phase < 2) return;

            Debug.Log(
                $"[황소왕] 광란 — 돌진 {phaseTwo.chargeRange}칸 / 충돌 {phaseTwo.collisionDamage} / " +
                $"벽 {phaseTwo.wallDamage} / 연쇄 {phaseTwo.maxChainPush}기물, 분노의 연쇄 개방", this);
        }

        /// <summary>
        /// 이동 후보는 LDY_MoveSystem이 동물 데이터의 moveRange만큼만 만들어 준다.
        /// 그 값이 2페이즈 돌진 거리보다 짧으면 긴 돌진이 후보에조차 오르지 못해,
        /// 광란에 들어가도 돌진이 조용히 4칸에 머문다. 원인을 찾기 어려운 종류라 미리 짚어준다.
        /// </summary>
        private void WarnIfMoveRangeTooShort()
        {
            if (_animal == null) return;

            int longest = Mathf.Max(phaseOne.chargeRange, phaseTwo.chargeRange);
            if (_animal.MoveRange >= longest) return;

            Debug.LogWarning(
                $"{name}: 동물 데이터의 이동 칸 수가 {_animal.MoveRange}칸이라 최대 돌진 {longest}칸에 못 미칩니다. " +
                $"AnimalSO의 moveRange를 {longest} 이상으로 올릴 것.", this);
        }
    }
}
