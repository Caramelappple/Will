using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.LSO.UI.Panel;
using _Scripts.LSO.Will.Candle;

// 3D 전환 메모:
// 이 스크립트는 원래도 RectTransform이 아니라 transform.DOLocalMove / DOLocalRotate를
// 쓰고 있어서 좌표 계산 자체는 그대로 3D에서도 동작한다.
// 유일하게 UI 전용이던 부분은 렌더 순서를 정하던 transform.SetAsLastSibling()이라
// 그 부분만 KTH_HandCard.BringToFront() (내부적으로 SpriteRenderer.sortingOrder 조정)로 바꿨다.
//
// 아래 Spacing/Width/Distance 값들은 이제 "픽셀"이 아니라 월드 스페이스 유닛이라
// 원래 UI 픽셀 값(200, 800, 60...)을 그대로 두면 카드 크기 기준으로 터무니없이 커진다.
// 카드 폭이 대략 1유닛인 걸 기준으로 값들을 다시 잡아뒀으니, 실제 카드 프리팹 크기에 맞춰
// 인스펙터에서 다시 조정해서 쓰면 된다.
//
// handTiltAngle (신규):
// UI에서는 카드가 항상 화면을 정면으로 봐서 회전이 Z축(부채꼴 기울기) 하나면 충분했지만,
// 3D에서는 손패 카드가 살짝 눕는 각도(X축)도 표현할 수 있어야 자연스럽다.
// 그래서 KTH_HandCard의 회전 관련 API를 float(Z만) -> Vector3(X+Z)로 바꾸고,
// 부채꼴로 펼쳐질 때만 X축에 handTiltAngle을 적용한다.
// 선택/배치 중앙으로 모일 때는 항상 Vector3.zero로 세워진다 (원래 로직 그대로).
public partial class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LSO_WillPanel willPanel;

    [Header("Piece Placement (LDY_CardPlacer 연동)")]
    [Tooltip("카드를 확정했을 때 실제 기물 배치를 시작할 대상. LDY_CardPlacer는 이 스크립트에서 건드리지 않고 공개 API만 호출한다.")]
    [SerializeField] private LDY_CardPlacer cardPlacer;

    /// <summary>
    /// 지금 배치를 시작해둔 카드. 없으면 null.
    ///
    /// LDY_CardPlacer 는 카드 오브젝트가 아니라 카드 데이터(LSO_CardSO)만 쥐고 있어서
    /// "누가 시작한 세션인가"를 스스로 답할 수 없다. 같은 종류 카드가 두 장이면
    /// 데이터가 같아 구분도 안 된다. 그 답을 여기서 들고 있는다.
    /// </summary>
    private KTH_HandCard placingCard;

    [Tooltip("배치가 끝난 카드를 버릴 더미. 비워두면 그냥 카드 오브젝트만 반납/파괴한다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Header("Arc Layout Settings")]
    [SerializeField] private float maxCardSpacing = 1.2f;
    [SerializeField] private float minCardSpacing = 0.5f;
    [SerializeField] private float maxHandWidth = 6f;
    [SerializeField] private float arcHeight = 0.4f;
    [SerializeField] private float maxRotation = 12f;

    [Header("Card Depth (겹칠 때 앞뒤)")]
    [Tooltip("어느 카드가 앞에 올지.\n" +
             "\n" +
             "카드가 불투명 메쉬라 sortingOrder가 안 먹는다. 앞뒤는 카메라와의 거리로만\n" +
             "정해지므로, 앞에 둘 카드를 실제로 카메라 쪽으로 당긴다.")]
    [SerializeField] private CardLayoutCalculator.DepthOrder depthOrder =
        CardLayoutCalculator.DepthOrder.LeftFirst;

    [Tooltip("카드 한 장마다 벌릴 깊이. 0이면 전부 같은 깊이에 놓여 앞뒤가 뒤죽박죽이 된다.\n" +
             "\n" +
             "**Max Hand Size 를 곱한 값이 카드 프리팹의 KTH_CardSorting.Front Z Offset\n" +
             "(기본 0.05)보다 작아야 한다.** 넘으면 뒤쪽 손패가 선택된 카드보다 앞으로 나온다.")]
    /// <summary>
    /// 카드 사이의 앞뒤 간격.
    ///
    /// ── 카드 안에서 띄운 거리보다 커야 한다 ───────────────────
    /// 카드는 평평한 한 장이 아니다. 그림·유언 아이콘 같은 자식이 카드 면에서
    /// 조금씩 앞으로 나와 있다(Hand_Card 프리팹 기준 0.018 · 0.02).
    ///
    /// 간격이 그보다 작으면 옆 카드의 그림이 이 카드를 뚫고 나온다. 카드 두 장이
    /// 서로 파고든 상태라, 무엇을 앞에 두든 겹쳐 보인다.
    ///
    /// 예전 기본값 0.005 는 아이콘이 나온 거리의 4분의 1이었다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 간격을 키워도 틈으로 보이지 않는다 — 시선 방향으로 떼기 때문이다(아래 DepthAxis).
    /// 카드에 무언가를 더 앞으로 빼면 이 값도 같이 올려야 한다.
    /// </summary>
    [SerializeField, Min(0f)] private float depthStep = 0.03f;

    [Tooltip("앞뒤 간격을 카메라 시선 방향으로 줄지.\n" +
             "\n" +
             "끄면 예전처럼 손패의 로컬 -Z 로 뗀다. 손패가 카메라를 정면으로\n" +
             "보고 있으면 둘이 같으므로 차이가 없다.\n" +
             "\n" +
             "손패를 눕혀 놓았다면 켜는 편이 낫다. 시선과 어긋난 방향으로 떼면\n" +
             "뗀 거리가 그대로 카드 사이의 틈으로 보인다.")]
    [SerializeField] private bool depthAlongView = true;

    [Tooltip("시선 방향을 물어볼 카메라. 비워두면 Camera.main 을 쓴다.")]
    [SerializeField] private Camera depthCamera;

    /// <summary>카메라를 못 찾았다고 이미 알렸는지. 매 프레임 내면 콘솔이 덮인다.</summary>
    private bool warnedMissingDepthCamera;

    /// <summary>
    /// 앞에 올 카드를 당길 방향. 손패 기준 로컬 좌표다.
    ///
    /// **앞뒤를 정하는 방향은 여기 하나다.** 부채꼴도, 배치 모드의 재배치도,
    /// 고른 카드를 앞으로 빼는 KTH_CardSorting 도 전부 이 값을 쓴다.
    /// 한 곳만 다른 방향으로 빼면 그 카드만 엉뚱한 쪽으로 튀어나온다.
    /// </summary>
    /// <summary>
    /// 고른 카드를 앞으로 뺄 거리.
    ///
    /// 부채꼴 전체가 차지하는 깊이보다 한 칸 더 나와야 **어느 자리의 카드를
    /// 골라도** 맨 앞에 선다. 고정값을 쓰면 손패가 길어졌을 때 맨 오른쪽 카드가
    /// 맨 왼쪽 카드를 못 넘어서서, 골랐는데도 남의 뒤에 깔린다.
    ///
    /// 그래서 간격에서 끌어낸다. depthStep 을 바꾸면 이 값도 따라온다 —
    /// 두 곳에 적어두면 한쪽만 고치고 지나가게 된다.
    /// </summary>
    public float FrontDepthDistance =>
        depthStep * Mathf.Max(1, maxHandSize);

    public Vector3 DepthAxis
    {
        get
        {
            if (!depthAlongView) return Vector3.back;

            Camera cam = depthCamera != null ? depthCamera : Camera.main;

            if (cam == null)
            {
                if (!warnedMissingDepthCamera)
                {
                    warnedMissingDepthCamera = true;

                    Debug.LogWarning(
                        "[KTH_HandCardLayout] 카메라를 찾지 못해 앞뒤 간격을 예전 방식(-Z)으로 줍니다. " +
                        "Depth Camera 를 꽂거나 MainCamera 태그를 확인하세요. " +
                        "(이 경고는 한 번만 나옵니다)",
                        this);
                }

                return Vector3.back;
            }

            // 회전만 쓴다. InverseTransformDirection 은 스케일까지 먹어서
            // 손패 컨테이너가 균등하지 않게 늘어나 있으면 방향이 틀어진다.
            Vector3 local =
                Quaternion.Inverse(transform.rotation) * -cam.transform.forward;

            return local.sqrMagnitude < 1e-6f ? Vector3.back : local.normalized;
        }
    }

    [Header("Hand Tilt (3D 전용)")]
    [Tooltip("손패에서 카드가 X축으로 얼마나 누워있을지. 0이면 완전히 세워짐, 값이 커질수록 뒤로 눕는다.")]
    [SerializeField] private float handTiltAngle = 20f;

    [Header("Organic Motion Settings")]
    [SerializeField] private float staggerDelay = 0.025f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 8;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    [Header("Selection Settings")]
    [Tooltip("카드 선택 해제/제거 시 손패가 원래대로 정리되는 애니메이션 시간")]
    [SerializeField] private float pushDuration = 0.28f;

    [Header("Placement Mode Settings")]
    [SerializeField] private bool enableMoveDown;
    [SerializeField] private float placementMoveDownDistance = 1f;
    [SerializeField] private float placementMoveDuration = 0.3f;
    [SerializeField] private float placementCenterGap = 1f;

    private readonly List<KTH_HandCard> handCards =
        new List<KTH_HandCard>();

    private Vector3 originalContainerLocalPos;
    private bool isCurrentlyDown;
    private KTH_HandCard selectedCard;

    // 카드 프리팹에 LSO_CardWill 이 없다는 경고를 이미 냈는지. SetupCard 참고.
    private bool warnedMissingCardWill;

    public int HandCount => handCards.Count;

    /// <summary>
    /// 손패에서 가장 싼 카드의 코스트. 낼 수 있는 카드가 하나도 없으면 -1.
    ///
    /// "코스트를 다 썼다"를 판단하는 쪽이 쓴다. 남은 코스트가 이 값보다 적으면
    /// 손패를 다 들고 있어도 더 낼 수 있는 것이 없다는 뜻이다.
    ///
    /// 손패는 여덟 장이 상한이라 그때그때 세도 값이 싸다. 따로 들고 있으면
    /// 카드가 오갈 때마다 맞춰야 하고, 한 번 어긋나면 알 방법이 없다.
    /// </summary>
    public int MinCardCost
    {
        get
        {
            int min = -1;

            for (int i = 0; i < handCards.Count; i++)
            {
                KTH_HandCard card = handCards[i];

                if (card == null || card.CardData == null || !card.CardData.IsValid) continue;

                int cost = card.CardData.Cost;

                if (min < 0 || cost < min) min = cost;
            }

            return min;
        }
    }

    public int MaxHandSize
    {
        get => maxHandSize;
        set => maxHandSize = value;
    }

    public bool IsFull =>
        maxHandSize > 0 &&
        handCards.Count >= maxHandSize;

    public event Action<int, int> OnHandCountChanged;

    private void Awake()
    {
        Instance = this;

        originalContainerLocalPos =
            transform.localPosition;
    }

    private void Update()
    {
        // 더블클릭으로 카드들이 내려가 있는 동안, 마우스 우클릭 한 번으로 그
        // 상태를 취소할 수 있게 한다. 활성화된 더블클릭이 없으면 CancelDoubleClick이
        // 알아서 아무 일도 하지 않고 false를 반환하므로 매 프레임 조건 없이 불러도 안전하다.
        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            bool wasActive = KTH_HandCard.CancelDoubleClick();

            // CancelActive()가 쓰는 PlayMoveUpAnimation은 각 카드가 들고 있는
            // OriginalLocalPosition으로 돌아가는데, 이 값이 그 순간 최신이
            // 아닐 수 있다(예: 부채꼴 재배치 애니메이션이 아직 안 끝난 도중이라
            // 새 자리로 갱신되기 전). 취소가 실제로 일어났다면 곧바로 손패
            // 재배치를 한 번 더 돌려서, 최신 계산값으로 무조건 맞춘다.
            //
            // 더블클릭은 항상 그 카드를 "확정(배치 모드)"까지 같이 켠다
            // (KTH_HandCard.OnPointerClick 참고). 그런데 여기서는 더블클릭이
            // 켠 "나머지 카드 내리기"만 취소하고 그 확정 상태는 그대로 두면,
            // 포커스 카드는 계속 중앙에 남고 나머지는 그 카드를 위해 자리를
            // 비워둔 부채꼴로만 남는다 - 우클릭으로 "취소"했는데도 손패가
            // 촘촘하게 다시 모이지 않고 계속 벌어져 보이는 원인이다.
            // 그래서 확정된 카드가 있으면 그 선택 상태까지 같이 취소한다.
            if (wasActive)
            {
                if (selectedCard != null && selectedCard.IsPlacementMode)
                {
                    selectedCard.CancelSelectionState();
                }

                // UpdateHandLayout / MoveToHandPositionWithDelay는 IsSelected인
                // 카드는 "다른 쪽에서 알아서 자리를 잡고 있다"고 보고 건너뛴다.
                // 그런데 호버 등 다른 경로로 "선택됨(들려 있음)" 상태가 된 카드가,
                // 여러 이벤트가 겹치는 순간(예: 마우스가 다른 카드로 넘어가는
                // 도중에 더블클릭이 겹침) KTH_HandCardLayout.selectedCard 갱신을
                // 놓치면 - 그 카드만 IsSelected가 true인 채로 영영 남아서
                // 재정렬 때마다 계속 건너뛰어지고, 혼자 제자리로 못 돌아온 채
                // 계속 떨어져 있게 된다. 우클릭 취소는 "손패를 확실히 원래대로"
                // 되돌리는 조작이므로, 여기서 남아있는 선택 상태를 전부 강제로
                // 정리해서 재정렬이 모든 카드를 빠짐없이 이동시키게 한다.
                for (int i = 0; i < handCards.Count; i++)
                {
                    KTH_HandCard stray = handCards[i];

                    if (stray != null && stray.IsSelected)
                    {
                        stray.CancelSelectionState();
                    }
                }

                UpdateHandLayout(null, pushDuration, false);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

}
