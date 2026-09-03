using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 전투 승리 뒤 보드를 뒤집어 뒷면을 드러내는 연출을 진행한다.
    ///
    /// 순서: 전투 연출 대기 → 보드 입력 차단 → 남은 기물 정리 → 보드 회전 → 보상 앵커 노출.
    ///
    /// 카메라는 건드리지 않는다. Camera.main은 LDY_CameraShake가 런타임에 스스로 붙어서
    /// localPosition을 흔들고 DLJ의 TestCameraMove도 같은 카메라를 트윈하는 공유 자원이라,
    /// 여기서 위치나 회전을 잡으면 서로 밀어낸다. 그래서 도는 쪽은 카메라가 아니라 보드다.
    ///
    /// 씬 배선: rewardAnchor만 연결하면 된다. 나머지 참조는 비워두면 Awake에서 씬을 뒤져 채운다.
    ///   · rewardAnchor — 보상 quad가 붙을 빈 오브젝트. (3.5, 1.3, 1.8) / rotation (55, 0, 0)
    ///                    보드 루트의 자식이 되면 안 된다(Awake에서 검사한다).
    ///
    /// 이 컴포넌트를 앵커와 같은 오브젝트에 붙여도 된다. 앵커를 감출 때 앵커 자신이 아니라
    /// 자식만 끄기 때문에, 디렉터가 스스로를 꺼서 사라지는 일은 없다.
    ///
    /// 호출은 LSO_StageFlow.ClearStage()가 한다. 씬에서 이벤트를 걸 필요 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_BoardFlipDirector : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private LDY_BoardManager board;

        [Tooltip("이동·공격·디졸브가 끝나기를 기다릴 대상. 비워두면 씬에서 찾는다.\n" +
                 "LDY_TestScene은 KTH_GameEndManager의 turnManager가 비어 있어 그쪽 대기가 통째로 건너뛰어진다.\n" +
                 "그래서 여기서 한 번 더 기다린다.")]
        [SerializeField] private LDY_TurnManager turnManager;

        [Tooltip("비워두면 씬에서 찾는다.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Tooltip("회전이 도는 동안 꺼둘 선택 컨트롤러. 비워두면 씬에서 찾는다.")]
        [SerializeField] private LDY_SelectionController selectionController;

        [Tooltip("위 둘 말고 더 꺼둘 것이 있으면 여기에. 보통은 비워둔다.\n" +
                 "GameObject를 끌어다 놓으면 그 오브젝트의 첫 번째 Behaviour가 잡히므로, " +
                 "컴포넌트를 직접 집어서 넣을 것.")]
        [SerializeField] private Behaviour[] additionalInputBehaviours;

        [Header("보상 앵커")]
        [Tooltip("보상 quad가 붙을 빈 오브젝트. KTH가 이 오브젝트의 자식으로 quad를 붙인다.\n" +
                 "권장 좌표 (3.5, 1.3, 1.8) · rotation (55, 0, 0) = 카메라와 같은 회전.\n" +
                 "로컬 +X가 화면 오른쪽, +Y가 화면 위, +Z가 화면 안쪽이 된다.")]
        [SerializeField] private Transform rewardAnchor;

        [Tooltip("켜면 회전이 끝날 때까지 앵커를 꺼둔다. 전투 중에 보상 quad가 보이는 것을 막는다.")]
        [SerializeField] private bool hideAnchorUntilFlip = true;

        [Header("회전")]
        [Tooltip("보드 중심에서 위아래로 얼마나 떨어진 곳을 축이 지나갈지.\n" +
                 "타일 중심 높이(-0.05)에 두면 보드가 제자리에서 앞뒤 면만 맞바뀐다.")]
        [SerializeField] private float pivotHeightOffset = -0.05f;

        [Tooltip("월드 기준 회전축. X축이면 카메라 쪽으로 앞뒤로 넘어간다.")]
        [SerializeField] private Vector3 flipAxis = Vector3.right;

        [Tooltip("180 = 완전히 뒤집어 같은 자리에 뒷면을 놓는다.\n" +
                 "145 = 뒷면이 카메라를 정면으로 마주 본다(90 + 카메라 피치 55).")]
        [SerializeField] private float flipAngle = 180f;

        [SerializeField, Min(0.05f)] private float flipDuration = 1f;
        [SerializeField] private Ease flipEase = Ease.InOutCubic;

        [Tooltip("되돌릴 때 왔던 길을 거꾸로 되짚을지.\n" +
                 "\n" +
                 "켜면 뒤집힐 때와 반대 방향으로 돈다. 넘어갔다 되넘어오는 모양이다.\n" +
                 "끄면 같은 방향으로 계속 돌아 한 바퀴를 채운다.\n" +
                 "\n" +
                 "도착하는 자세는 어느 쪽이든 같다. 보이는 방향만 다르다.")]
        [SerializeField] private bool reverseRetraces = true;

        [Header("남은 기물")]
        [Tooltip("보드를 돌리기 전에 살아남은 기물을 줄여서 없앤다. 0이면 즉시 끈다.")]
        [SerializeField, Min(0f)] private float pieceHideDuration = 0.35f;

        [SerializeField] private Ease pieceHideEase = Ease.InBack;

        [Tooltip("기물이 다 사라진 뒤 보드가 돌기 시작하기까지의 뜸(초).\n" +
                 "\n" +
                 "0이면 사라지자마자 돈다. 두 동작이 붙어 한 덩어리로 보인다.\n" +
                 "빈 판을 한 박자 보여주고 싶으면 0.2~0.5 정도를 준다.")]
        [SerializeField, Min(0f)] private float hideToFlipDelay;

        [Header("타이밍")]
        [Tooltip("회전을 시작하기 전에 전투 연출이 끝나기를 기다리는 상한(초). 멈춤 방지선이다.")]
        [SerializeField, Min(0f)] private float combatWaitTimeout = 3f;

        [Tooltip("회전이 끝난 뒤 보상이 뜨기까지의 뜸. 뒷면을 한 박자 보여준다.")]
        [SerializeField, Min(0f)] private float revealHold = 0.25f;

        [Header("반응")]
        [Tooltip("보드가 다 돌아간 순간. 앵커를 켜기 전이다.\n" +
                 "쿵 하는 소리나 카메라 흔들림처럼 착지에 붙는 것을 여기 건다.")]
        [SerializeField] private UnityEvent onFlipped;

        [Tooltip("Reveal Hold까지 끝나 연출이 완전히 마무리됐을 때.\n" +
                 "이 시점에 보상 앵커가 켜져 있고 입력은 닫힌 채다.")]
        [SerializeField] private UnityEvent onFinished;

        [Tooltip("보상이 끝나고 보드가 앞면으로 되돌아왔을 때.\n" +
                 "다음 스테이지의 기물 배치가 이 뒤에 온다.")]
        [SerializeField] private UnityEvent onReversed;

        [Header("전역 잠금")]
        [Tooltip("LSO_WillSelection의 전역 보드 잠금까지 쓸지.\n" +
                 "켜면 LSO_WillPanel이 전체 화면 검은 디머를 페이드인한다(LSO_Test 씬에 그 패널이 있다).\n" +
                 "보드가 뒤집히는 장면이 가려지므로 기본은 꺼둔다. 입력 차단은 컴포넌트를 꺼서 처리한다.")]
        [SerializeField] private bool useGlobalInteractionLock = false;

        private readonly LDY_BoardFlipMotion _motion = new();
        private readonly LDY_BoardPieceHider _pieceHider = new();

        private LDY_BoardInputGate _gate;
        private Coroutine _routine;

        /// <summary>
        /// 갈 때 쓴 회전축이 지나간 지점. 되돌릴 때 그대로 다시 쓴다.
        ///
        /// 다시 계산하면 안 된다. BoardManager.BoardCenter는 귀퉁이 칸(boardOrigin)의
        /// 월드 좌표에 (half, 0, half)를 그냥 더한 값이라 보드의 회전을 보지 않는다.
        /// 보드가 뒤집히면 귀퉁이는 축 반대편으로 넘어가는데 오프셋은 월드 기준 그대로라,
        /// 되돌릴 때 계산되는 "중심"이 갈 때의 중심과 아예 다른 점이 된다.
        /// 그 점을 축으로 돌면 보드가 엉뚱한 데로 휘둘린다.
        /// </summary>
        private Vector3 _flipPivot;

        /// <summary>연출이 도는 중인지. 기다리는 쪽(LSO_StageFlow)이 이 값을 본다.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>보상 quad가 붙을 자리. 보상 UI 쪽에서 물어볼 수 있게 열어둔다.</summary>
        public Transform RewardAnchor => rewardAnchor;

        // =========================================================
        // Unity
        // =========================================================

        private void Awake()
        {
            if (board == null)
                board = FindFirstObjectByType<LDY_BoardManager>();

            if (turnManager == null)
                turnManager = FindFirstObjectByType<LDY_TurnManager>();

            if (cardPlacer == null)
                cardPlacer = FindFirstObjectByType<LDY_CardPlacer>();

            if (selectionController == null)
                selectionController = FindFirstObjectByType<LDY_SelectionController>();

            WarnIfAnchorRidesTheBoard();
        }

        /// <summary>
        /// 앵커 감추기는 Awake가 아니라 Start에서 한다.
        /// 앵커 밑에 놓인 것이 Awake에서 자식을 더 만들 수 있기 때문이다.
        /// Awake에서 감추면 그 뒤에 생긴 자식이 켜진 채로 남는다.
        /// 모든 Awake가 끝난 뒤인 Start 시점에는 자식이 다 모여 있다.
        /// </summary>
        private void Start()
        {
            if (hideAnchorUntilFlip)
                SetAnchorContentVisible(false);
        }

        private void OnDisable()
        {
            // 연출 도중에 꺼지면 보드가 뒤집힌 채로, 기물이 사라진 채로 남는다. 원래대로 돌려놓는다.
            if (IsPlaying) Abort();
        }

        /// <summary>
        /// 앵커에 놓인 것들을 보이거나 감춘다.
        ///
        /// 앵커 자신은 절대 끄지 않는다. 디렉터가 앵커와 같은 오브젝트에 붙어 있는 경우
        /// 스스로를 꺼버리게 되고, 그러면 밖에서 FindFirstObjectByType이
        /// (기본값이 비활성 제외라) 디렉터를 못 찾아 연출이 통째로 건너뛰어진다.
        /// 실제로 그렇게 배선돼서 회전이 한 번도 돌지 않은 적이 있다.
        /// </summary>
        private void SetAnchorContentVisible(bool visible)
        {
            if (rewardAnchor == null) return;

            for (int i = 0; i < rewardAnchor.childCount; i++)
                rewardAnchor.GetChild(i).gameObject.SetActive(visible);
        }

        /// <summary>
        /// 회전 중에 꺼둘 입력 컴포넌트를 모은다.
        ///
        /// 예전에는 Behaviour[] 하나로 인스펙터에서 받았는데, LDY_SelectionController가
        /// LDY_BoardManager와 같은 오브젝트에 있어서 GameObject를 끌어다 놓으면
        /// 엉뚱하게 BoardManager가 잡혔다. 이제는 타입을 지정해 직접 찾는다.
        /// </summary>
        private List<Behaviour> CollectInputBehaviours()
        {
            var gated = new List<Behaviour>();

            // 호버 연출은 여기서 끄지 않는다. 예전에는 이 오브젝트에 함께 붙어 있었지만
            // 지금은 기물마다 따로 들고 있어서 여기서 모을 수가 없다.
            // 대신 LDY_BoardPieceHider가 기물을 끄고, 그때 각자의 OnDisable이 원위치로 돌려놓는다.
            if (selectionController != null)
                gated.Add(selectionController);

            if (cardPlacer != null)
                gated.Add(cardPlacer);

            if (additionalInputBehaviours != null)
            {
                foreach (Behaviour behaviour in additionalInputBehaviours)
                {
                    if (behaviour != null && !gated.Contains(behaviour))
                        gated.Add(behaviour);
                }
            }

            return gated;
        }

        /// <summary>
        /// 앵커가 보드 루트 밑에 있으면 보드와 함께 돌아가 버린다.
        /// 그러면 회전 각도를 바꿀 때마다 보상 배치가 따라 틀어져서, 앵커를 따로 둔 의미가 없어진다.
        /// </summary>
        private void WarnIfAnchorRidesTheBoard()
        {
            if (rewardAnchor == null || board == null) return;

            Transform boardRoot = board.BoardRoot;
            if (boardRoot == null || !rewardAnchor.IsChildOf(boardRoot)) return;

            Debug.LogWarning(
                $"[LDY_BoardFlipDirector] 보상 앵커 '{rewardAnchor.name}'가 보드 루트 '{boardRoot.name}' 아래에 있습니다. " +
                "보드와 함께 회전하므로 보드 밖으로 꺼내 주세요.", rewardAnchor);
        }

        // =========================================================
        // 재생
        // =========================================================

        /// <summary>
        /// 연출을 시작한다. 이미 돌고 있거나 배선이 빠져 있으면 아무 일도 하지 않는다.
        /// 시작하지 못하면 IsPlaying이 false로 남으므로, 기다리는 쪽은 그대로 다음 단계로 넘어간다.
        /// </summary>
        public void Play()
        {
            if (IsPlaying) return;

            if (board == null)
            {
                Debug.LogWarning(
                    "[LDY_BoardFlipDirector] BoardManager가 없어 회전 연출을 건너뜁니다.", this);
                return;
            }

            if (board.BoardRoot == null)
            {
                Debug.LogWarning(
                    "[LDY_BoardFlipDirector] BoardManager의 boardOrigin이 비어 있어 돌릴 대상이 없습니다.", this);
                return;
            }

            // 이미 뒤집혀 있으면 먼저 처음 자세로 되돌린다.
            //
            // 회전은 "부를 때의 자세"를 시작점으로 잡는다. 뒤집힌 채로 또 돌리면
            // 180°가 한 번 더 얹혀 360°(제자리)가 되고, 처음 자세를 기억하던 값도
            // 뒤집힌 자세로 덮여서 되돌리기까지 망가진다.
            // 언제 불러도 같은 연출이 같은 자리에서 시작하도록 여기서 한 번 정리한다.
            if (IsFlipped) ResetToStart();

            IsPlaying = true;
            _routine = StartCoroutine(Co_Play());
        }

        /// <summary>보드가 돌아간 채로 남아 있는지. 되돌릴 것이 있는지와 같은 뜻이다.</summary>
        public bool IsFlipped => _motion.HasMoved;

        /// <summary>
        /// 뒤집힌 보드를 연출로 되돌린다. 보상이 끝난 뒤 다음 스테이지로 넘어갈 때 쓴다.
        ///
        /// Abort의 ResetToStart와 다르다. 그쪽은 순간이동으로 물리는 취소용이고,
        /// 이쪽은 같은 시간·이징으로 천천히 돌아온다.
        ///
        /// 기물은 되살리지 않는다. 숨겨둔 것은 지나간 스테이지의 기물이고,
        /// 다음 스테이지는 자기 기물을 새로 놓는다(LDY_StageDirector).
        /// 여기서 되살리면 죽은 기물이 다시 서 있는 판 위에 새 기물이 겹친다.
        ///
        /// 앵커는 다시 감춘다. 보상 상자가 뒤집힌 면에 붙어 있으므로
        /// 되돌아오면 보이면 안 된다.
        /// </summary>
        public IEnumerator PlayReverse()
        {
            if (board == null || board.BoardRoot == null) yield break;

            // 돌아간 적이 없으면 되돌릴 것도 없다.
            if (!IsFlipped) yield break;

            if (hideAnchorUntilFlip)
                SetAnchorContentVisible(false);

            Transform boardRoot = board.BoardRoot;

            // 갈 때 쓴 축 지점을 그대로 쓴다. 다시 계산하면 다른 점이 나온다(_flipPivot 주석 참고).
            Vector3 pivot = _flipPivot;

            // 되짚으면 왔던 길을 거꾸로, 이어 돌면 같은 방향으로 한 바퀴를 채운다.
            // 어느 쪽이든 도착하는 자세는 같다.
            float angle = reverseRetraces ? -flipAngle : flipAngle;

            yield return _motion.Rotate(
                boardRoot, pivot, flipAxis, angle, flipDuration, flipEase, boardRoot.gameObject);

            // 제자리로 돌아왔다. 되돌릴 것이 없다고 표시해야 다음 Play가
            // "아직 뒤집혀 있다"고 보고 엉뚱한 기준점으로 튀지 않는다.
            _motion.MarkRestored();

            onReversed?.Invoke();
        }

        /// <summary>
        /// 연출을 중단하고 시작 전 상태로 되돌린다.
        /// 기다리는 쪽이 상한 시간을 넘겼을 때, 그리고 이 컴포넌트가 꺼질 때 불린다.
        /// </summary>
        public void Abort()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            ResetToStart();

            IsPlaying = false;
        }

        /// <summary>
        /// 보드·기물·입력·앵커를 연출 시작 전 상태로 되돌린다.
        /// 중단할 때도, 다시 재생하기 직전에도 같은 자리에서 출발하도록 쓴다.
        /// </summary>
        private void ResetToStart()
        {
            _motion.Restore();
            _pieceHider.Restore();
            _gate?.Open();

            if (hideAnchorUntilFlip)
                SetAnchorContentVisible(false);
        }

        private IEnumerator Co_Play()
        {
            // 1. 마지막 공격의 복귀 애니메이션과 사망 디졸브가 끝나기를 기다린다.
            //    여기서 기다리지 않으면 기물이 녹는 도중에 보드가 돌기 시작한다.
            yield return Co_WaitForCombatAnimations();

            // 2. 보드 입력을 막는다. 선택이 걸려 있었다면 이 시점에 풀린다.
            _gate = new LDY_BoardInputGate(cardPlacer, CollectInputBehaviours(), useGlobalInteractionLock);
            _gate.Close();

            // 3. 살아남은 기물을 정리한다. 부모가 갈려 있어 보드와 같이 돌릴 수 없다.
            yield return _pieceHider.Hide(CollectSurvivingPieces(), pieceHideDuration, pieceHideEase);

            // 3-1. 빈 판을 한 박자 보여준다.
            //      Realtime인 이유는 기물을 줄이는 트윈도 timeScale을 무시하기 때문이다.
            //      한쪽만 스케일 시간이면 유언 연출이 시간을 쥐는 구간에서 뜸이 늘어난다.
            if (hideToFlipDelay > 0f)
                yield return new WaitForSecondsRealtime(hideToFlipDelay);

            // 4. 보드를 뒤집는다.
            //    축이 지나간 지점을 남겨둔다. 되돌릴 때 같은 점을 써야 왔던 길로 돌아온다.
            Transform boardRoot = board.BoardRoot;
            _flipPivot = board.BoardCenter + Vector3.up * pivotHeightOffset;

            yield return _motion.Rotate(
                boardRoot, _flipPivot, flipAxis, flipAngle, flipDuration, flipEase, boardRoot.gameObject);

            // 4-1. 다 돌았다. 착지에 붙는 것들이 이 신호를 받는다.
            onFlipped?.Invoke();

            // 5. 드러난 자리에 보상이 놓일 것들을 켠다.
            SetAnchorContentVisible(true);

            if (revealHold > 0f)
                yield return new WaitForSecondsRealtime(revealHold);

            // 보드는 뒤집힌 채, 기물은 숨겨진 채로 남는다. 입력은 다시 열지 않는다.
            // 전역 잠금만 풀어 다음 씬으로 새지 않게 한다.
            _gate.Seal();

            _routine = null;
            IsPlaying = false;

            // 상태를 다 정리한 뒤에 알린다. 여기 걸린 쪽이 IsPlaying을 물어볼 수 있고,
            // 그때 아직 true면 "아직 도는 중"으로 잘못 읽는다.
            onFinished?.Invoke();
        }

        /// <summary>
        /// 이동·공격·디졸브가 하나라도 재생 중이면 기다린다.
        /// 끝나지 않는 상황에서 영영 멈추는 쪽이 잘리는 것보다 나쁘므로 상한을 둔다.
        /// </summary>
        private IEnumerator Co_WaitForCombatAnimations()
        {
            if (turnManager == null) yield break;

            float deadline = Time.unscaledTime + combatWaitTimeout;

            while (turnManager != null && turnManager.IsAnimating())
            {
                if (Time.unscaledTime >= deadline)
                {
                    Debug.LogWarning(
                        $"[LDY_BoardFlipDirector] 전투 연출이 {combatWaitTimeout:0.#}초 안에 끝나지 않아 " +
                        "기다리지 않고 보드를 돌립니다.", this);
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>격자에 아직 남아 있는 기물. 죽은 기물은 LDY_DeathHandler가 이미 격자에서 지웠다.</summary>
        private List<LDY_Animal> CollectSurvivingPieces()
        {
            var survivors = new List<LDY_Animal>();
            if (board == null) return survivors;

            survivors.AddRange(board.GetAllByTeam(LDY_Team.Player));
            survivors.AddRange(board.GetAllByTeam(LDY_Team.Enemy));
            return survivors;
        }

        // =========================================================
        // 디버그 (에디터 전용)
        // =========================================================

#if UNITY_EDITOR
        [Header("디버그 — 에디터 전용")]
        [Tooltip("전투 없이 회전 연출만 즉시 재생한다. 각도·시간·앵커 좌표를 맞출 때 쓴다.\n" +
                 "전체 흐름(회전 → 보상 → 맵 복귀)까지 보려면 KTH_StageScene에서 Play를 시작하고 " +
                 "KTH_TestClearButton을 쓸 것.")]
        [SerializeField] private bool enableDebugHotkey = true;

        [Tooltip("F5·F7·F8·F9는 LDY_SaveDebugHotkeys가 쓰고 있다.")]
        [SerializeField] private Key debugPlayKey = Key.F10;

        [Tooltip("되돌리기 키. 회전을 원래대로 물려 같은 씬에서 여러 번 확인한다.")]
        [SerializeField] private Key debugResetKey = Key.F11;

        private void Update()
        {
            if (!enableDebugHotkey || Keyboard.current == null) return;

            if (debugPlayKey != Key.None && Keyboard.current[debugPlayKey].wasPressedThisFrame)
            {
                Debug.Log("[LDY_BoardFlipDirector] 디버그 재생", this);
                Play();
                return;
            }

            if (debugResetKey != Key.None && Keyboard.current[debugResetKey].wasPressedThisFrame)
            {
                Debug.Log("[LDY_BoardFlipDirector] 디버그 되돌리기", this);
                Abort();
            }
        }
#endif
    }
}
