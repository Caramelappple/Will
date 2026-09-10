using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 커서가 올라가 있거나 선택된 동안 자리를 옮기고, 둘 다 해제되면 돌아온다.
    ///
    /// 클릭 연출과 달리 "한 번 재생"이 아니라 상태다.
    /// 커서가 머무는 내내 옮긴 자리에 있어야 하므로 되돌아오는 트윈을 붙이지 않는다.
    ///
    /// 옮기는 것은 localPosition이다. 부모가 돌아가 있어도 방향이 함께 따라간다.
    /// "원래 자리"는 완전히 복귀한 뒤 다시 떠오르기 시작할 때 읽는다.
    /// 복귀 중 재진입은 기존 기준을 유지해 높이가 쌓이지 않게 한다.
    /// Awake/Configure 시점에 미리 캐싱해두지 않는 이유는, 이 컴포넌트가 런타임에 붙는
    /// 대상(예: 보드 기물)이 그 이후에 다른 곳(격자 배치 등)에 의해 최종 위치로
    /// 옮겨질 수 있기 때문이다. 자리를 "정하는" 주체는 여전히 하나(그 다른 곳)이고,
    /// 이 컴포넌트는 그 결과 위에서 상대적으로 들썩이기만 한다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverMoveEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Header("대상")]
        [Tooltip("비워두면 자기 자신을 옮긴다.\n" +
                 "콜라이더가 같이 움직여 커서에서 벗어나는 것이 문제라면,\n" +
                 "콜라이더는 이 오브젝트에 두고 자식 비주얼을 여기 연결할 것.")]
        [SerializeField] private Transform target;

        [Header("연출 설정")]
        [Tooltip("원래 자리에서 얼마나 옮길지. 대상의 로컬 기준이다.\n" +
                 "3D 월드는 0.05~0.2, UI(RectTransform)는 픽셀 단위라 10~20이 보인다.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("커서가 올라갈 때 걸리는 시간")]
        [SerializeField, Min(0f)] private float enterDuration = 0.1f;

        [Tooltip("커서가 벗어날 때 걸리는 시간. 조금 길게 두면 부드럽다.")]
        [SerializeField, Min(0f)] private float exitDuration = 0.16f;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [SerializeField] private Ease easeEnter = Ease.OutQuad;
        [SerializeField] private Ease easeExit = Ease.OutQuad;

        [Header("추가 애니메이션 (선택)")]
        [Tooltip("비워두면 아무 일도 안 한다. 기물에서 뜨는 동작과 같이 재생할 " +
                 "추가 연출(KTH)이 있을 때만 연결한다.")]
        [SerializeField] private KTH_PiecesHoveringAnimation extraAnimation;

        private Transform _target;
        private Vector3 _originalPosition;
        private bool _isOffset;
        private bool _pointerInside;
        private bool _selected;
        private bool _liftRequested;
        private int _suspendCount;
        private Tween _tween;

        private void Awake()
        {
            _target = target != null ? target : transform;

            // 인스펙터에서 안 걸어뒀으면 같은 오브젝트에서 자동으로 찾는다.
            if (extraAnimation == null) extraAnimation = GetComponent<KTH_PiecesHoveringAnimation>();
        }

        /// <summary>
        /// 옮길 대상과 연출 값을 코드로 정한다. 런타임에 붙일 때 쓴다.
        ///
        /// "원래 자리"는 여기서 캐싱하지 않는다. Install 순서상 이 호출은
        /// (예: LSO_AnimalFactory -> LDY_BoardManager.Place처럼) 대상이 최종 위치로
        /// 옮겨지기 "전"에 일어날 수 있어서, 여기서 잡아두면 아직 자리 잡기 전의
        /// 좌표가 원래 자리로 굳어버린다. 대신 RefreshLift에서 처음 떠오를 때 읽고,
        /// 완전히 복귀할 때까지 같은 기준을 유지한다.
        ///
        /// 인스펙터로 이미 맞춰둔 것을 덮어쓰게 되므로, 부르는 쪽이
        /// "아직 안 붙어 있을 때만" 부르는 것을 전제로 한다.
        /// </summary>
        public void Configure(Transform newTarget, LSO_HoverMoveTuning tuning)
        {
            KillTween();

            // 이미 옮겨둔 상태에서 대상이 바뀌면 지난 대상이 들린 채로 남는다.
            if (_isOffset && _target != null) _target.localPosition = _originalPosition;

            _isOffset = false;
            _liftRequested = false;
            target = newTarget;
            _target = newTarget != null ? newTarget : transform;

            offset = tuning.offset;
            enterDuration = tuning.enterDuration;
            exitDuration = tuning.exitDuration;
            easeEnter = tuning.easeEnter;
            easeExit = tuning.easeExit;
            ignoreTimeScale = tuning.ignoreTimeScale;
        }

        public void OnHoverEnter()
        {
            _pointerInside = true;
            RefreshLift();
        }

        public void OnHoverExit()
        {
            _pointerInside = false;
            RefreshLift();
        }

        /// <summary>선택 상태는 포인터와 별도로 유지한다. 선택 컨트롤러가 갱신한다.</summary>
        public void SetSelected(bool selected)
        {
            _selected = selected;
            RefreshLift();
        }

        private void RefreshLift()
        {
            if (_suspendCount > 0 || !isActiveAndEnabled || _target == null) return;

            bool lift = _pointerInside || _selected;
            if (_liftRequested == lift) return;
            _liftRequested = lift;

            if (lift)
            {
                // 내려가는 중 다시 호버되면 기존 기준을 유지해야 높이가 누적되지 않는다.
                if (!_isOffset) _originalPosition = _target.localPosition;
                _isOffset = true;
                MoveTo(_originalPosition + offset, enterDuration, easeEnter);
                extraAnimation?.PlayHoverEnter();
            }
            else if (_isOffset)
            {
                MoveTo(_originalPosition, exitDuration, easeExit);
                // 실제 복귀가 끝나기 전에는 기준 위치를 버리지 않는다.
                if (_tween != null)
                    _tween.OnComplete(() => { _isOffset = false; _tween = null; });
                else
                    _isOffset = false;
                extraAnimation?.PlayHoverExit();
            }
        }

        /// <summary>
        /// 외부(예: LDY_MoveSystem)가 이 트랜스폼을 다른 이유로(격자 이동 등) 옮기는 동안
        /// 호버 연출이 끼어들지 못하게 잠깐 쉬게 한다.
        ///
        /// LSO_ButtonHoverHandler.enabled를 껐다 켜는 방식은 쓰지 않는다. 그러면
        /// OnDisable -> OnHoverExit이 트윈으로 되돌아가고, 다시 켤 때 유니티 이벤트
        /// 시스템의 호버 상태 추적과 어긋나서 커서가 그대로 머물러 있어도 다시
        /// OnHoverEnter가 걸리는 등 예측하기 어려운 위치로 튄다.
        ///
        /// 대신 핸들러는 그대로 두고, 쉬는 동안 포인터/선택 상태만 기록한다.
        /// 쉬기 시작할 때 오프셋이 걸려 있었으면 트윈 없이
        /// 즉시 제자리로 돌려놓는다(트윈을 걸면 외부 이동과 자리를 다툰다).
        /// true/false 호출을 쌍으로 세어 마지막 연출이 끝난 뒤에만 호버를 재개한다.
        /// 커서가 남아 있거나 선택된 상태면 도착한 위치를 기준으로 다시 떠오른다.
        /// </summary>
        public void SetSuspended(bool suspended)
        {
            SetSuspended(suspended, restore: true);
        }

        /// <summary>
        /// restore를 false로 주면 쉬기 시작할 때 원래 자리로 되돌리지 않고 트윈만 멈춘다.
        /// 뜬 자리에서 그대로 재생해야 하는 외부 연출(예: 공격 포물선)이 있을 때 쓴다.
        /// 이 경우 자리를 되돌리는 책임은 호출한 쪽이 이후에 SetSelected(false) 등으로 진다.
        /// </summary>
        public void SetSuspended(bool suspended, bool restore)
        {
            if (suspended)
            {
                if (++_suspendCount == 1)
                {
                    if (restore) RestoreImmediate();
                    else KillTween();
                }
                return;
            }

            if (_suspendCount == 0) return;
            _suspendCount--;
            if (_suspendCount == 0) RefreshLift();
        }

        private void MoveTo(Vector3 position, float duration, Ease ease)
        {
            // 커서를 빠르게 들락거려도 트윈이 쌓이지 않게 이전 것을 먼저 끊는다.
            KillTween();

            // 꺼지는 중이면 트윈을 걸지 않는다.
            //
            // 기물이 SetActive(false)로 사라질 때 호버 핸들러가 이탈을 보내는데,
            // 그 트윈은 돌지 못한 채 남았다가 다시 켤 때 옮겨진 자리부터 시작한다.
            if (!isActiveAndEnabled || duration <= 0f)
            {
                _target.localPosition = position;
                return;
            }

            _tween = _target.DOLocalMove(position, duration)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        private void KillTween()
        {
            if (_tween == null) return;

            _tween.Kill();
            _tween = null;
        }

        private void OnDisable()
        {
            _pointerInside = false;
            _selected = false;
            // 커서가 올라간 채로 창이 닫히면 OnHoverExit이 오지 않아 옮겨진 자리에서 굳는다.
            RestoreImmediate();
        }

        private void RestoreImmediate()
        {
            KillTween();
            _liftRequested = false;

            // 실제로 호버 오프셋이 적용된 상태였을 때만 되돌린다.
            // 한 번도 호버되지 않은 상태에서 무조건 되돌리면, _originalPosition이
            // 아직 채워지지 않은 기본값(0,0,0) 등으로 자리가 튀어버릴 수 있다.
            if (_isOffset && _target != null)
            {
                _target.localPosition = _originalPosition;
                _isOffset = false;
            }
        }
    }
}
