using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO.Boss;
using UnityEngine;

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

        [Header("연출")]
        [Tooltip("돌진으로 한 칸을 지나는 데 걸리는 시간. 작을수록 빠르다.\n" +
                 "일반 기물의 이동 속도(LDY_MoveSystem의 moveDuration)와 무관하게 이 값이 쓰인다.")]
        [SerializeField, Min(0.005f)] private float chargeSecondsPerTile = 0.07f;

        [Tooltip("짧은 돌진이 한 번 튀고 마는 것을 막는 하한.")]
        [SerializeField, Min(0f)] private float minChargeDuration = 0.15f;

        [Tooltip("돌진의 가속 곡선. 뒤로 갈수록 가팔라야 '달려들어 박는' 느낌이 난다.\n" +
                 "끝값을 1보다 크게 만들면 목적지를 살짝 지나쳤다가 돌아온다.")]
        [SerializeField] private AnimationCurve chargeEasing = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 2f));

        [Tooltip("밀려나는 기물이 한 칸 미끄러지는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float pushDuration = 0.2f;

        private LDY_Animal _animal;
        private LSO_BossPhase _phase;

        /// <summary>현재 페이즈. LSO_BossPhase가 원본이다.</summary>
        public int Phase => BossPhase != null ? BossPhase.CurrentPhase : 1;

        /// <summary>지금 페이즈에서 쓸 돌진 수치.</summary>
        public LDY_BullChargeRule Rule => Phase >= 2 ? phaseTwo : phaseOne;

        public AnimationCurve ChargeEasing => chargeEasing;

        /// <summary>이만큼 달릴 때 연출에 쓸 시간.</summary>
        public float ChargeDuration(int distance)
        {
            return Mathf.Max(minChargeDuration, Mathf.Max(1, distance) * chargeSecondsPerTile);
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
            if (_phase != null)
                _phase.OnPhaseChange -= LogPhaseChange;
        }

        internal void RaiseChargeResolved(IReadOnlyList<Vector3Int> deathTiles)
        {
            ChargeResolved?.Invoke(deathTiles);
        }

        /// <summary>
        /// 밀려난 기물을 새 칸까지 미끄러뜨린다.
        ///
        /// LDY_BoardManager.Move는 격자와 pos만 고치고 모델은 건드리지 않는다(연출은 부른 쪽 몫이다).
        /// 특성은 MonoBehaviour가 아니라 코루틴을 돌릴 수 없으므로 여기서 대신 돌려준다.
        /// </summary>
        internal void PlayPush(LDY_Animal pushed, Vector3 targetWorldPos)
        {
            if (pushed == null) return;

            Transform t = pushed.modelTransform;
            if (t == null) return;

            if (pushDuration <= 0f)
            {
                t.position = targetWorldPos;
                return;
            }

            StartCoroutine(PushVisual(t, targetWorldPos));
        }

        private IEnumerator PushVisual(Transform t, Vector3 targetWorldPos)
        {
            Vector3 startPos = t.position;
            float elapsed = 0f;

            while (elapsed < pushDuration)
            {
                // 밀려난 기물은 충돌 피해로 그 자리에서 죽을 수 있다.
                // 확인하지 않으면 파괴된 Transform에 값을 써서 예외가 난다.
                // (LDY_MoveSystem.MoveVisual이 같은 이유로 같은 검사를 한다.)
                if (t == null) yield break;

                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / pushDuration);
                yield return null;
            }

            if (t != null)
                t.position = targetWorldPos;
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
