using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 전투 승리 뒤 보드를 뒤집어 뒷면을 드러내는 연출을 진행한다.
    ///
    /// 순서: 전투 연출 대기 → 보드 입력 차단 → 기물과 보상 앵커를 판에 태우기
    ///       → 보드 회전 → 기물 내려놓기.
    ///
    /// **판 위의 것은 판과 함께 돈다.** 켜고 끄지 않는다.
    /// 기물은 예전에 돌리기 전에 줄여서 없앴고, 보상 상자는 회전이 끝난 뒤에 켰다.
    /// 둘 다 판만 돌고 나머지는 그 자리에서 나타나거나 사라져 보였다.
    /// 태우고 내리는 것은 LSO_BoardRiders 가 맡는다.
    ///
    /// 기물과 앵커는 수명이 다르다.
    ///   기물   회전이 끝나면 바로 내려놓고 감춘다. 지나간 스테이지의 것이다.
    ///   앵커   보상이 끝나고 판이 되돌아올 때까지 실린 채로 남는다.
    ///
    /// 카메라는 건드리지 않는다. Camera.main은 LSO_CameraDirector가 시네머신으로 잡고
    /// DLJ의 TestCameraMove도 같은 카메라를 트윈하는 공유 자원이라,
    /// 여기서 위치나 회전을 잡으면 서로 밀어낸다. 그래서 도는 쪽은 카메라가 아니라 보드다.
    ///
    /// 화면 흔들림은 LSO_CameraImpulse(시네머신 Impulse)를 쓴다. 그쪽은 카메라를
    /// 직접 밀지 않고 신호를 보내므로 이 규칙과 어긋나지 않는다.
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

        [Tooltip("켜면 전투 중에는 앵커를 꺼둔다. 보상 quad가 판 위에 떠 있는 것을 막는다.\n" +
                 "\n" +
                 "회전이 시작될 때 켜진다. 판 뒷면에서 같이 돌아 올라오므로,\n" +
                 "회전이 끝난 뒤가 아니라 시작할 때 켜야 들어오는 것이 보인다.")]
        [SerializeField] private bool hideAnchorUntilFlip = true;

        [Header("회전축")]
        [Tooltip("축이 지나갈 자리를 잡아줄 오브젝트. **비워두면 보드 중심을 쓴다.**\n" +
                 "\n" +
                 "눈으로 보면서 맞추고 싶을 때 빈 오브젝트를 하나 만들어 끌어다 놓는다.\n" +
                 "그 오브젝트를 씬에서 옮기면 도는 축이 따라 옮겨진다.\n" +
                 "\n" +
                 "보드 루트의 자식으로 두면 안 된다. 판과 같이 돌아버려서\n" +
                 "되돌릴 때 축이 딴 데로 간다. 넣으면 검사해서 경고한다.")]
        [SerializeField] private Transform pivotSource;

        [Tooltip("그 자리에서 얼마나 옮길지. 월드 기준이다.\n" +
                 "\n" +
                 "Y = -0.05 는 타일 중심 높이다. 여기 두면 보드가 제자리에서\n" +
                 "앞뒤 면만 맞바뀐다.\n" +
                 "\n" +
                 "Z 를 앞으로 빼면 판이 카메라 쪽으로 넘어오면서 돌고,\n" +
                 "뒤로 밀면 저 안쪽에서 넘어간다. X 는 좌우로 치우친 축이 된다.")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, -0.05f, 0f);

        [Tooltip("월드 기준 축 방향. X축이면 카메라 쪽으로 앞뒤로 넘어간다.")]
        [SerializeField] private Vector3 flipAxis = Vector3.right;

        [Tooltip("켜면 씬 화면에 축이 지나가는 선을 그린다. 플레이 중이 아니어도 보인다.\n" +
                 "축을 옮겨보며 맞출 때 켜둘 것.")]
        [SerializeField] private bool drawPivotGizmo = true;

        [Header("회전")]
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
        [Tooltip("보드가 돌기 시작하기까지의 뜸(초).\n" +
                 "\n" +
                 "기물은 판에 실린 채 같이 돈다. 이 값은 판이 돌기 직전에 한 박자\n" +
                 "쉬는 시간이다. 0이면 전투가 끝나자마자 돈다.")]
        [FormerlySerializedAs("hideToFlipDelay")]
        [SerializeField, Min(0f)] private float beforeFlipDelay;

        [Tooltip("판이 다 돌아간 뒤 지난 스테이지 기물을 감출지.\n" +
                 "\n" +
                 "켜면 회전이 끝나는 즉시 끈다. 보상 화면에 지난 판의 흔적이 남지 않는다.\n" +
                 "\n" +
                 "끄면 판에 매달린 채로 둔다. 판이 기물을 가려준다면 이쪽이 더 자연스럽다.\n" +
                 "어느 쪽이든 다음 스테이지를 세울 때 파괴되므로 남아 쌓이지는 않는다.\n" +
                 "\n" +
                 "눈으로 보고 정할 것 — 기물이 판 실루엣 밖으로 삐져나오는지가 갈림길이다.")]
        [SerializeField] private bool hidePiecesAfterFlip = true;

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

        /// <summary>기물. 다 돌면 내려놓고 감춘다.</summary>
        private readonly LSO_BoardRiders _riders = new();

        /// <summary>
        /// 보상 앵커. 기물과 수명이 달라 따로 둔다.
        ///
        /// 기물은 회전이 끝나면 바로 치우지만, 앵커는 보상이 끝나고 판이 되돌아올 때까지
        /// 판에 실린 채로 남아야 한다. 한 목록에 섞으면 Settle이 앵커까지 꺼버린다.
        /// </summary>
        private readonly LSO_BoardRiders _anchorRider = new();

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

        /// <summary>
        /// 뒤집기 전 보드 루트의 자세. 격자 계산을 하려면 이 자세여야 한다.
        ///
        /// LDY_BoardManager.GridToWorld 는 boardOrigin.position 에 칸 크기를 더해서 좌표를 낸다.
        /// 그런데 boardOrigin 이 곧 회전하는 당사자라, 뒤집히면 축 반대편으로 넘어가면서
        /// position 자체가 달라진다. 그 상태로 기물을 놓으면 어긋난 원점 기준으로 놓인다.
        ///
        /// BoardCenter 가 같은 이유로 틀렸던 적이 있다(_flipPivot 주석 참고).
        /// 근본은 하나다 — **격자 계산은 판이 앞면일 때만 맞다.**
        /// </summary>
        private Vector3 _homePosition;

        private Quaternion _homeRotation;

        /// <summary>연출이 도는 중인지. 기다리는 쪽(LSO_StageFlow)이 이 값을 본다.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 앞면 회전이 완전히 끝났을 때. Reveal Hold까지 지난 뒤다.
        ///
        /// 인스펙터의 On Finished와 같은 시점이지만 이쪽은 코드용이다.
        /// **누가 돌렸든** 이 신호가 나가므로, 디버그 재생(F10)으로 돌려도
        /// 실제 클리어와 똑같이 보상이 이어진다.
        ///
        /// 되돌리기(PlayReverse)에서는 나가지 않는다. 그쪽은 보상이 끝난 뒤라
        /// 여기 걸린 쪽이 다시 시작하면 안 된다.
        /// </summary>
        public event System.Action Finished;

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
            //
            // 대신 입력을 막는 이 문(gate)이 닫히면 기물을 고를 수 없고,
            // 회전이 끝나면 LSO_BoardRiders가 기물을 꺼서 각자의 OnDisable이 원위치로 돌린다.
            // 회전이 도는 동안에는 기물이 판과 함께 움직이므로 커서가 얹힐 일이 거의 없다.
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
        /// 앵커가 처음부터 보드 루트 밑에 있으면 안 된다.
        ///
        /// 이제 앵커도 판과 함께 돌지만, **태우는 것은 연출이 할 일이다.**
        /// 회전 직전에 뒷면 자리로 옮겼다가 태우고, 되돌아오면 원래 부모로 내려놓는다
        /// (LSO_BoardRiders.AttachForArrival).
        ///
        /// 처음부터 보드 밑에 놓아두면 그 계산이 한 번 더 얹혀서 두 번 뒤집힌 자리로 간다.
        /// 씬에서는 **보상이 보이길 원하는 자리**에 보드 밖으로 놓을 것.
        /// </summary>
        private void WarnIfAnchorRidesTheBoard()
        {
            if (rewardAnchor == null || board == null) return;

            Transform boardRoot = board.BoardRoot;
            if (boardRoot == null || !rewardAnchor.IsChildOf(boardRoot)) return;

            Debug.LogWarning(
                $"[LDY_BoardFlipDirector] 보상 앵커 '{rewardAnchor.name}'가 보드 루트 '{boardRoot.name}' 아래에 있습니다. " +
                "판에 태우는 것은 연출이 알아서 하므로, 보상이 보이길 원하는 자리에 보드 밖으로 놓아 주세요. " +
                "여기 두면 뒷면 계산이 한 번 더 얹혀 두 번 뒤집힌 자리로 갑니다.", rewardAnchor);
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
        /// 판을 잠깐 앞면 자세로 돌려놓고 일을 시킨 뒤, 뒤집힌 자세로 되돌린다.
        ///
        /// **격자 계산은 판이 앞면일 때만 맞다.** LDY_BoardManager.GridToWorld 가
        /// boardOrigin.position 을 원점으로 쓰는데, 그 boardOrigin 이 곧 회전하는 당사자라
        /// 뒤집힌 동안에는 원점이 축 반대편으로 넘어가 있다.
        ///
        /// 그래서 다음 스테이지 기물을 미리 놓을 때처럼 뒤집힌 채로 좌표를 구해야 하면
        /// 이걸 거친다. 한 프레임 안에서 되돌렸다 놓으므로 화면에는 아무것도 비치지 않는다.
        ///
        /// 뒤집힌 적이 없으면 그냥 부른다. 그때는 지금 자세가 곧 앞면이다.
        /// </summary>
        public void RunAtHomePose(System.Action action)
        {
            if (action == null) return;

            Transform boardRoot = board != null ? board.BoardRoot : null;

            if (boardRoot == null || !IsFlipped)
            {
                action();
                return;
            }

            boardRoot.GetPositionAndRotation(out Vector3 flipped, out Quaternion flippedRotation);

            boardRoot.SetPositionAndRotation(_homePosition, _homeRotation);

            try
            {
                action();
            }
            finally
            {
                // 일이 도중에 터져도 판은 원래 있던 자리로 돌려놓는다.
                // 뒤집힌 자세로 못 돌아가면 다음 회전이 엉뚱한 데서 출발한다.
                boardRoot.SetPositionAndRotation(flipped, flippedRotation);
            }
        }

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
        /// 앵커는 판에 실린 채로 같이 돌아 내려간다. 화면 밖으로 나간 뒤에 감춘다.
        /// 돌기 전에 감추면 상자가 그 자리에서 사라져 버려서, 들어올 때와 나갈 때가
        /// 서로 다른 연출이 된다.
        /// </summary>
        public IEnumerator PlayReverse()
        {
            if (board == null || board.BoardRoot == null) yield break;

            // 돌아간 적이 없으면 되돌릴 것도 없다.
            if (!IsFlipped) yield break;

            Transform boardRoot = board.BoardRoot;

            // 갈 때 쓴 축 지점을 그대로 쓴다. 다시 계산하면 다른 점이 나온다(_flipPivot 주석 참고).
            Vector3 pivot = _flipPivot;

            // 되짚으면 왔던 길을 거꾸로, 이어 돌면 같은 방향으로 한 바퀴를 채운다.
            // 어느 쪽이든 도착하는 자세는 같다.
            float angle = reverseRetraces ? -flipAngle : flipAngle;

            // 판 위에 놓인 기물을 태운다. 다음 스테이지 기물을 미리 놓아둔 경우,
            // 놓인 자리를 도착점으로 삼아 뒷면에서 함께 실려 올라온다.
            //
            // 놓여 있지 않으면 아무 일도 하지 않으므로, 빈 판만 돌아오는 경우도 그대로 된다.
            _riders.AttachForArrival(CollectSurvivingPieces(), boardRoot, pivot, flipAxis, angle);

            yield return _motion.Rotate(
                boardRoot, pivot, flipAxis, angle, flipDuration, flipEase, boardRoot.gameObject);

            // 기물을 내려놓는다. 놓여 있던 자세와 부모로 정확히 돌아가고 호버도 되살아난다.
            _riders.Restore();

            // 앵커를 내려놓는다. 씬에 놓여 있던 자세와 부모로 정확히 돌아간다.
            // Restore 를 쓰는 것은 감추지 않고 되돌리기 때문이다 — 감추는 것은 그다음 줄에서 한다.
            _anchorRider.Restore();

            if (hideAnchorUntilFlip)
                SetAnchorContentVisible(false);

            // 제자리로 돌아왔다. 되돌릴 것이 없다고 표시해야 다음 Play가
            // "아직 뒤집혀 있다"고 보고 엉뚱한 기준점으로 튀지 않는다.
            _motion.MarkRestored();

            // 같은 씬에서 다음 전투를 시작하므로 회전 중 닫았던 입력도 되돌린다.
            _gate?.Open();

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
            _riders.Restore();

            // 앵커도 내려놓는다. 판에 실린 채로 두면 다음 재생이 뒷면 자리로 한 번 더
            // 옮겨서, 두 번 뒤집힌 자리로 간다.
            _anchorRider.Restore();

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

            // 3. 살아남은 기물을 판에 태운다. 부모가 갈려 있어 그냥 두면 보드만 돌고
            //    기물은 제자리에 떠 있는다. 겉보기는 변하지 않고 부모만 바뀐다.
            Transform boardRoot = board.BoardRoot;

            _riders.Attach(CollectSurvivingPieces(), boardRoot);

            // 3-1. 한 박자 쉰다.
            //      Realtime인 이유는 회전 트윈도 timeScale을 무시하기 때문이다.
            //      한쪽만 스케일 시간이면 유언 연출이 시간을 쥐는 구간에서 뜸이 늘어난다.
            if (beforeFlipDelay > 0f)
                yield return new WaitForSecondsRealtime(beforeFlipDelay);

            // 4. 보드를 뒤집는다. 기물이 판에 실려 같이 넘어간다.
            //    축이 지나간 지점과 뒤집기 전 자세를 남겨둔다.
            //    되돌릴 때 같은 점을 써야 왔던 길로 돌아오고,
            //    뒤집힌 동안 격자 계산을 하려면 앞면 자세를 되짚어야 한다.
            _flipPivot = ResolvePivot();

            boardRoot.GetPositionAndRotation(out _homePosition, out _homeRotation);

            // 4-0. 보상 상자도 태운다. 판 뒷면에서 같이 올라오게 하려는 것이다.
            //      씬에 놓인 자세를 "도착점"으로 보고 뒷면 자리로 옮겼다가 태우므로,
            //      배치는 지금까지처럼 보이고 싶은 자리에 그냥 놓으면 된다.
            //
            //      켜는 것도 여기서 한다. 회전이 끝난 뒤에 켜면 가운데에 띡 하고 나타난다.
            _anchorRider.AttachForArrival(rewardAnchor, boardRoot, _flipPivot, flipAxis, flipAngle);

            SetAnchorContentVisible(true);

            yield return _motion.Rotate(
                boardRoot, _flipPivot, flipAxis, flipAngle, flipDuration, flipEase, boardRoot.gameObject);

            // 4-1. 다 돌았다. 착지에 붙는 것들이 이 신호를 받는다.
            onFlipped?.Invoke();

            // 4-2. 지난 스테이지 기물을 정리한다.
            //      감출지는 Hide Pieces After Flip 이 정한다 — 판이 가려주면 안 감춰도 된다.
            //      어느 쪽이든 다음 스테이지가 세워질 때 파괴된다.
            //
            //      앵커는 내려놓지 않는다. 보상이 끝나고 판이 되돌아올 때까지 실린 채여야 한다.
            _riders.Settle(hidePiecesAfterFlip);

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
            Finished?.Invoke();
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
        // 회전축
        // =========================================================

        /// <summary>
        /// 이번에 돌 때 축이 지나갈 지점.
        ///
        /// 자리를 정하는 것은 여기 하나다. Pivot Source 가 있으면 그 오브젝트의 위치,
        /// 없으면 보드 중심을 기준으로 삼고, 거기에 Pivot Offset 을 더한다.
        ///
        /// 한 번 정한 값은 _flipPivot 에 남겨 되돌릴 때 그대로 쓴다.
        /// 다시 계산하면 안 되는 이유는 _flipPivot 주석에 적어뒀다.
        /// </summary>
        private Vector3 ResolvePivot()
        {
            if (pivotSource == null)
            {
                if (board != null) return board.BoardCenter + pivotOffset;

                Debug.LogWarning(
                    $"{name}: LDY_BoardManager가 없어 축을 이 오브젝트 자리에서 잡습니다. " +
                    "판이 엉뚱한 데로 휘둘리면 Board 를 연결하거나 Pivot Source 를 넣으세요.", this);

                return transform.position + pivotOffset;
            }

            WarnIfPivotRidesBoard();

            return pivotSource.position + pivotOffset;
        }

        /// <summary>
        /// 축 오브젝트가 판에 실려 같이 도는지 본다.
        ///
        /// 그러면 갈 때와 돌아올 때의 축이 달라진다 — 보드 원점(boardOrigin)으로 좌표를
        /// 계산했다가 두 번 물렸던 것과 같은 종류다. 근본은 하나다.
        /// **도는 것을 기준으로 도는 자리를 정하면 안 된다.**
        /// </summary>
        private void WarnIfPivotRidesBoard()
        {
            if (board == null) return;

            Transform boardRoot = board.BoardRoot;

            if (boardRoot == null) return;
            if (!pivotSource.IsChildOf(boardRoot)) return;

            Debug.LogWarning(
                $"{name}: 회전축 오브젝트 '{pivotSource.name}' 가 보드 루트의 자식입니다. " +
                "판과 같이 돌아서 되돌릴 때 축이 딴 데로 갑니다. 보드 밖으로 빼세요.", this);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 축이 지나가는 선을 씬 화면에 그린다. 옮겨보며 맞추라고 있는 것이다.
        ///
        /// 도는 중에는 이번에 실제로 쓰는 축(_flipPivot)을 그린다.
        /// 멈춰 있을 때는 지금 설정대로면 어디가 될지를 그린다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!drawPivotGizmo) return;

            Vector3 pivot = IsPlaying ? _flipPivot : PreviewPivot();

            if (flipAxis.sqrMagnitude <= Mathf.Epsilon) return;

            Vector3 half = flipAxis.normalized * 5f;

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(pivot - half, pivot + half);
            Gizmos.DrawSphere(pivot, 0.12f);
        }

        /// <summary>
        /// 멈춰 있을 때 보여줄 축 자리. 경고를 내지 않는 조용한 판이다.
        ///
        /// ResolvePivot 을 그대로 쓰면 화면을 다시 그릴 때마다 경고가 쏟아진다.
        /// 실제 경고는 돌기 시작할 때 한 번만 나가면 된다.
        /// </summary>
        private Vector3 PreviewPivot()
        {
            if (pivotSource != null) return pivotSource.position + pivotOffset;
            if (board != null) return board.BoardCenter + pivotOffset;

            return transform.position + pivotOffset;
        }
#endif

        // =========================================================
        // 디버그 (에디터 전용)
        // =========================================================

#if UNITY_EDITOR
        [Header("디버그 — 에디터 전용")]
        [Tooltip("전투 없이 회전 연출을 즉시 재생한다. 각도·시간·앵커 좌표를 맞출 때 쓴다.\n" +
                 "\n" +
                 "회전이 끝나면 실제 클리어와 똑같이 보상까지 이어진다.\n" +
                 "(LSO_StageFlow가 Finished 신호를 듣는다. 씬에 없으면 회전만 하고 끝난다)")]
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
