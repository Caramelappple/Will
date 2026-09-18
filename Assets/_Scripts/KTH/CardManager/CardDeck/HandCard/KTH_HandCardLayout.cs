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
public class KTH_HandCardLayout : MonoBehaviour
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

    public void SetupCard(
        KTH_HandCard card,
        LSO_CardSO cardData)
    {
        if (card == null)
        {
            return;
        }

        // 카드 오브젝트는 풀에서 돌려쓴다. 지난 판에 붙인 유언이 그대로 남은 채로
        // 돌아오면, 방금 뽑은 카드가 이미 저주가 붙어 있는 것처럼 보이고
        // 그대로 소환된다. 새 카드 데이터를 넣는 이 자리에서 지운다.
        //
        // KTH_HandCard.ResetForPool 이 아니라 여기인 이유:
        // 버림 연출(KTH_DiscardAnimation) 경로는 카드를 Pool.Release 하지 않고
        // 버림 더미의 자식으로 부모만 바꿔 눌러앉힌다 - 그 경로에서는
        // ResetForPool 이 아예 안 불린다(KTH_HandCardDiscardHandler 주석 참고).
        // 반면 SetupCard 는 풀에서 왔든 새로 만들었든 손패로 들어오는 모든 카드가
        // 반드시 한 번 거친다.
        LSO_CardWill cardWill =
            card.GetComponentInChildren<LSO_CardWill>(true);

        if (cardWill != null)
        {
            cardWill.Clear();
        }
        else if (!warnedMissingCardWill)
        {
            // 카드 프리팹에 LSO_CardWill 이 없으면 유언을 아예 붙일 수 없다.
            // LSO_WillPainter 도 경고를 내지만 그쪽은 양초를 눌러야 나온다 -
            // 한 번도 안 눌러보면 배선이 빠진 채로 넘어간다. 그래서 여기서도 알린다.
            //
            // 다만 드로우할 때마다 불리는 자리라 매번 내면 콘솔이 덮인다.
            // 처음 한 번만 내고 그 뒤로는 침묵한다 - 배선이 빠졌다는 사실은
            // 한 줄이면 충분하고, 고치면 어차피 다시 안 나온다.
            warnedMissingCardWill = true;

            Debug.LogWarning(
                $"[KTH_HandCardLayout] 카드 '{card.name}' 에 LSO_CardWill 이 없어 " +
                "유언을 붙일 수 없습니다. 카드 프리팹에 그 컴포넌트를 붙여 주세요. " +
                "(이 경고는 한 번만 나옵니다)",
                card
            );
        }

        card.Setup(
            cardData,
            willPanel
        );
    }

    public bool CanAddCard()
    {
        return !IsFull;
    }

    public bool AddCard(KTH_HandCard card)
    {
        return AddCard(
            card,
            false
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        Vector3 spawnerWorldPos)
    {
        bool insertAtFront =
            spawnerWorldPos.x >= 0f;

        return AddCard(
            card,
            insertAtFront
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        bool insertAtFront)
    {
        if (card == null)
        {
            return false;
        }

        if (handCards.Contains(card))
        {
            return false;
        }

        if (IsFull)
        {
            return false;
        }

        KTH_HandCard.DeselectCurrent();

        // 버려졌다가 풀에서 재사용된 카드는 더블클릭 대상 목록에서 빠진 채로
        // 돌아온다(ResetForPool에서 뺐음). 손패에 다시 들어오는 이 시점에 다시
        // 등록한다.
        card.RegisterForDoubleClick();

        if (insertAtFront)
        {
            handCards.Insert(
                0,
                card
            );
        }
        else
        {
            handCards.Add(card);
        }

        // 확정 클릭(배치 시작/취소)을 여기서 받아서 LDY_CardPlacer로 연결한다.
        card.OnCardClicked -= HandleCardConfirmClicked;
        card.OnCardClicked += HandleCardConfirmClicked;

        UpdateHandLayout(card);

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );

        return true;
    }

    public void RemoveCard(
        KTH_HandCard card)
    {
        if (card != null)
        {
            card.OnCardClicked -= HandleCardConfirmClicked;
        }

        if (selectedCard == card)
        {
            selectedCard = null;
        }

        if (!handCards.Remove(card))
        {
            return;
        }

        UpdateHandLayout(
            null,
            pushDuration,
            false
        );

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );
    }

    /// <summary>
    /// 손패를 통째로 버린다. 스테이지를 깼을 때 부른다.
    ///
    /// ClearHand 와 다르다. 이쪽은 **화면에 보이는 버림 연출을 태운다** —
    /// 카드가 버림 더미로 날아간다. 판이 뒤집히기 전이라 플레이어가 본다.
    ///
    /// 목록을 복사해서 도는 이유는 ConsumeAndRearrange 가 안에서 RemoveCard 로
    /// 원본을 건드리기 때문이다. 돌면서 지우면 중간부터 건너뛴다.
    /// </summary>
    public void DiscardHand(KTH_DiscardCardUI discardPile)
    {
        selectedCard = null;

        KTH_HandCard.CancelDoubleClick();

        List<KTH_HandCard> snapshot = new List<KTH_HandCard>(handCards);

        foreach (KTH_HandCard card in snapshot)
        {
            if (card == null) continue;

            KTH_HandCardDiscardHandler.ConsumeAndRearrange(card, discardPile, null);
        }
    }

    /// <summary>
    /// 손패를 통째로 비운다. 새 판을 세울 때 부른다.
    ///
    /// 한 장씩 RemoveCard 로 지우지 않는 이유는, 그때마다 재배치 애니메이션이
    /// 돌아 남은 카드가 우르르 움직이기 때문이다. 어차피 다 없앨 것이라
    /// 목록을 먼저 비우고 한 번만 알린다.
    ///
    /// 버린 카드 더미로 보내지 않는다. 판이 바뀌면 덱을 처음 상태로 되돌리므로
    /// 더미에 넣어봐야 곧바로 다시 걷힌다 — 넣었다 빼는 연출만 헛돈다.
    /// </summary>
    public void ClearHand()
    {
        // 배치 모드나 선택 상태가 남아 있으면 사라진 카드를 계속 가리킨다.
        selectedCard = null;

        KTH_HandCard.CancelDoubleClick();

        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard card = handCards[i];

            if (card == null) continue;

            card.OnCardClicked -= HandleCardConfirmClicked;

            card.CancelSelectionState();
            card.transform.DOKill(true);

            KTH_HandCardDiscardHandler.ReleaseOrDestroy(card);
        }

        handCards.Clear();

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );
    }

    // =========================================================
    // Piece Placement (LDY_CardPlacer 연동)
    // =========================================================

    /// <summary>
    /// 카드를 클릭했을 때 호출된다. OnCardClicked는 "확정 클릭"과 "취소 클릭" 둘 다에서 불리는데,
    /// 이 시점에는 KTH_HandCard 내부에서 이미 상태를 바꿔놓은 뒤라 card.IsConfirmed로 구분할 수 있다.
    ///   - 확정 클릭 (배치 시작): IsConfirmed == true
    ///   - 취소 클릭 (배치 모드에서 다시 눌러서 취소): IsConfirmed == false
    /// </summary>
    private void HandleCardConfirmClicked(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 취소 클릭이면 여기서 할 일이 없다. 선택 해제는 KTH_HandCard 쪽에서 이미 처리했다.
        if (!card.IsConfirmed)
        {
            card.LogClick("취소 클릭이라 배치를 시작하지 않음");
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardLayout] LDY_CardPlacer가 연결되지 않아 기물 배치를 시작할 수 없습니다.",
                this
            );

            card.CancelSelectionState();

            return;
        }

        // ── 예전에 여기 있던 가드 ─────────────────────────────────
        // "이미 배치 중이면 아무것도 하지 않는다"가 있었다. 같은 확정 클릭에서
        // KTH_CardPlacementController.TryBeginPlacement 도 배치를 시작하던 때,
        // 두 곳이 서로를 덮는 것을 막으려고 둔 것이다.
        //
        // **그 컨트롤러는 이제 없다.** 배치를 시작하는 곳은 여기 하나뿐이다.
        // 그래서 그 가드는 지킬 것이 없어졌고, 대신 카드를 번갈아 누를 때
        // 앞 카드의 배치가 남아 있으면 그 뒤로 어떤 카드도 못 고르게 막았다.
        //
        // 지금은 아래 ExitPlacementMode 가 앞 세션을 확실히 물리고,
        // BeginPlacement 자체도 "이미 배치 중이면 취소하고 새로 시작"한다.
        // 막을 것이 아니라 이어받으면 되는 일이었다.
        // ─────────────────────────────────────────────────────────

        LSO_CardSO cardData =
            card.CardData;

        if (cardData == null)
        {
            card.CancelSelectionState();

            return;
        }

        // 유언 칸을 통째로 넘긴다. **값을 읽어서 넘기면 안 된다** —
        // 지금은 아직 양초로 안 붙였을 수 있고, 칸을 고르는 사이에 붙이기 때문이다.
        // 값은 실제로 놓는 순간 LDY_CardPlacer 가 읽는다.
        LSO_CardWill cardWill =
            card.GetComponentInChildren<LSO_CardWill>(true);

        // 앞 카드의 세션은 여기서 끝난다. 아래 BeginPlacement 가 그것을 취소하며
        // 콜백을 돌리는데, 그 안에서 placingCard 를 보고 판단하는 곳이 있으므로
        // 미리 비워 둔다. 새 값은 시작이 확정된 뒤에 적는다.
        placingCard = null;

        bool started =
            cardPlacer.BeginPlacement(
                cardData,
                LDY_Team.Player,
                cardWill: cardWill,
                onPlaced: animal =>
                {
                    if (placingCard == card) placingCard = null;

                    if (animal != null)
                    {
                        // 실제로 보드에 놓였을 때만 손패에서 빼고 버린다.
                        card.ConsumeAndRearrange(
                            discardPile
                        );
                    }
                    else
                    {
                        // 칸이 막혀있거나 실패한 경우 카드는 손패에 그대로 두고 선택만 푼다.
                        card.CancelSelectionState();
                    }
                },
                onCancelled: () =>
                {
                    if (placingCard == card) placingCard = null;

                    // 우클릭 등으로 배치를 취소하면 카드는 손패에 남기고 선택만 푼다.
                    card.CancelSelectionState();
                }
            );

        // 시작이 확정된 뒤에 적는다. BeginPlacement 안에서 앞 세션이 취소되며
        // 콜백이 도는데, 그 전에 적어두면 방금 적은 값을 그 콜백이 지운다.
        if (started) placingCard = card;

        card.LogClick(started
            ? "배치 시작됨 — 이제 칸을 누르면 놓인다"
            : "배치를 시작하지 못함 (내 턴이 아니거나 코스트 부족)");

        if (!started)
        {
            // 내 턴이 아니거나 코스트가 부족해서 아예 시작을 못 한 경우.
            card.CancelSelectionState();
        }
    }

    public void OnCardSelectionChanged(
        KTH_HandCard card,
        bool selected)
    {
        if (selected)
        {
            if (selectedCard != null &&
                selectedCard != card)
            {
                selectedCard.SetSelected(false);
            }

            selectedCard = card;

            return;
        }

        if (selectedCard == card)
        {
            selectedCard = null;

            UpdateHandLayout(
                null,
                pushDuration,
                false
            );
        }
    }

    public void EnterPlacementMode(
        KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 손패 목록에 없는 카드가 확정됐다는 뜻이다.
        //
        // 카드는 "지금 손패에 있다"를 두 곳에 남긴다 — handCards 와 더블클릭
        // 레지스트리(allHandCards). 둘이 어긋나면 카드는 확정된 것처럼 보이는데
        // 부채꼴 재배치에서는 빠져, 눌러도 아무 반응이 없는 것처럼 보인다.
        //
        // 조용히 돌아서면 그 어긋남이 영영 안 드러난다.
        if (!handCards.Contains(card))
        {
            Debug.LogWarning(
                $"[KTH_HandCardLayout] '{card.name}' 가 손패 목록에 없는데 확정됐습니다. " +
                "버려지는 중인 카드를 눌렀거나, 손패에서 뺄 때 선택이 안 풀린 것입니다.",
                card);

            card.CancelSelectionState();

            return;
        }

        // 더블클릭으로 내려가 있는 카드가 있으면 부채꼴 재배치와 자리를 다투게
        // 되므로, 배치 모드로 들어가기 전에 먼저 정리한다.
        KTH_HandCard.CancelDoubleClick();

        // 확정된 카드가 아닌데도 호버로 선택된 채 남아있는 다른 카드가 있으면,
        // 부채꼴로 흩어지는 동안 UpdateHandLayout이 그 카드의 이동만 건너뛴다
        // (card.IsSelected면 MoveToHandPositionWithDelay가 자리를 옮기지 않음).
        // 그 상태로 두면 나중에 배치를 취소해도 그 카드만 계속 엉뚱한 자리에 남는다.
        // 배치 모드에 들어가기 전에 미리 정리해서 그런 카드가 없게 한다.
        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard other = handCards[i];

            if (other == null || other == card)
            {
                continue;
            }

            if (other.IsSelected && !other.IsConfirmed)
            {
                other.CancelSelectionState();
            }
        }

        selectedCard = card;

        MoveSelectedCardToCenter();
    }

    /// <summary>
    /// 배치 모드에서 빠져나온다. 카드의 선택이 풀릴 때(KTH_HandCardSelectionController)
    /// 불린다.
    ///
    /// ── 왜 여기서 배치까지 물리는가 ───────────────────────────
    /// "지금 고른 카드"를 두 곳이 따로 들고 있었다.
    ///   · 손패는 currentConfirmed 로
    ///   · LDY_CardPlacer 는 _pendingCard 로
    ///
    /// 카드를 번갈아 누르면 손패 쪽만 새 카드로 바뀌고 배치 쪽은 앞 카드를
    /// 그대로 쥐고 있었다. 보드를 안 누르고 카드를 눌렀으니 그 세션은 끝날
    /// 일이 없다. 그 뒤로는 어떤 카드를 눌러도 "앞의 배치가 안 끝났다"로
    /// 막혔다 — 손패가 고장 난 것처럼 보이지만 물린 곳은 배치 쪽이었다.
    ///
    /// 그래서 선택이 풀리는 이 자리에서 배치도 같이 물린다. 두 값이 어긋날
    /// 틈을 없애는 것이 요점이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 이미 놓고 나온 경우에는 아무 일도 하지 않는다 — 그때는 LDY_CardPlacer 가
    /// 스스로 IsPlacing 을 내려놓은 뒤라 CancelPlacement 가 곧바로 돌아선다.
    /// </summary>
    /// <param name="card">
    /// 배치 모드에서 빠져나오는 카드.
    ///
    /// **이 카드가 시작한 배치일 때만 물린다.** 그냥 물리면 남이 시작한 세션까지
    /// 죽는다. 이를테면 카드를 한 장 뽑을 때 AddCard 가 DeselectCurrent 를 부르는데,
    /// 그때 방금 고른 카드의 배치가 함께 끊긴다 — 고른 적도 없는 카드 때문에
    /// 선택이 풀리는 것이라 원인을 짐작할 방법이 없다.
    ///
    /// null 이면 누가 부른지 모르는 경우라 그냥 물린다.
    /// </param>
    public void ExitPlacementMode(KTH_HandCard card = null)
    {
        selectedCard = null;

        bool mine = card == null || card == placingCard;

        if (mine && cardPlacer != null)
        {
            placingCard = null;

            cardPlacer.CancelPlacement();
        }

        UpdateHandLayout(
            null,
            placementMoveDuration,
            false
        );
    }

    private void MoveSelectedCardToCenter()
    {
        if (selectedCard == null)
        {
            return;
        }

        selectedCard.transform.DOKill();
        selectedCard.BringToFront();

        Sequence moveToCenterSequence =
            DOTween.Sequence();

        moveToCenterSequence.SetTarget(
            selectedCard.transform
        );

        // 정중앙으로 데려가되 **앞으로 뺀 만큼은 지킨다.**
        //
        // ── 왜 Vector3.zero 가 아닌가 ─────────────────────────────
        // 예전에는 그냥 원점으로 보냈다. 원점은 깊이가 0 이다. 그런데 부채꼴로
        // 흩어진 나머지 카드는 0.03 ~ 0.21 만큼 앞에 서 있다.
        //
        // 즉 BringToFront 로 앞으로 뺀 것을 이 트윈이 도로 끌어내려서,
        // 고른 카드가 손패에서 **제일 뒤**에 놓였다. 가운데 크게 뜬 카드 위로
        // 옆 카드의 그림과 글자가 덮이던 것이 그것이다.
        //
        // BringToFront 가 방금 더한 값을 그대로 목표에 싣는다. 얼마나 빼는지는
        // 손패가 정하므로(FrontDepthDistance) 여기서 다시 계산하지 않는다.
        // ─────────────────────────────────────────────────────────
        Vector3 centerTarget = selectedCard.FrontOffset;

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    centerTarget,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        // 위치는 정중앙이니 부채꼴 Z축 기울기는 0으로 되돌린다.
        // X축(handTiltAngle, 눕는 각도)은 그대로 유지한다.
        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOScale(
                    selectedCard.BaseScale *
                    selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.OnComplete(() =>
        {
            if (selectedCard == null)
            {
                return;
            }

            if (!selectedCard.IsPlacementMode)
            {
                return;
            }

            SpreadCardsAroundCenter();
        });
    }

    /// <summary>
    /// focalCard를 뺀 나머지 카드들이 focalCard를 기준으로 좌우 부채꼴로 벌어질 목표
    /// 위치/회전을 계산해서 애니메이션까지 실행한다.
    ///
    /// 배치 모드(포커스 카드가 중앙 Vector3.zero로 이동한 상태, anchorX = 0)와
    /// 호버(포커스 카드가 자기 자리에 그대로 있는 상태, anchorX = 그 자리의 X)
    /// 양쪽에서 같이 쓴다.
    ///
    /// 나머지 카드는 실제 손패상의 좌/우 순서가 아니라 "항상 절반씩 좌우로 균등 분배"한다.
    /// (가장자리 카드를 선택해도 나머지가 한쪽으로 쏠리지 않고 중앙 기준으로 고르게 펼쳐짐)
    /// </summary>
    private void ApplyFanAroundFocalCard(
        KTH_HandCard focalCard,
        float anchorX,
        float centerGap,
        float duration)
    {
        int count = handCards.Count;

        if (count <= 1 || focalCard == null)
        {
            return;
        }

        List<KTH_HandCard> otherCards =
            new List<KTH_HandCard>();

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null ||
                card == focalCard)
            {
                continue;
            }

            otherCards.Add(card);
        }

        int otherCount =
            otherCards.Count;

        if (otherCount == 0)
        {
            return;
        }

        int leftCount = otherCount / 2;
        int rightCount = otherCount - leftCount;

        // 나머지가 딱 1장일 때는 위 균등분배 공식이 항상 오른쪽으로 밀어버려서
        // 원래 왼쪽에 있던 카드를 선택해도 반대편으로 튀어 보인다.
        // 이 경우만 실제 손패 순서를 보고 원래 있던 쪽으로 보낸다.
        if (otherCount == 1)
        {
            int focalIndex =
                handCards.IndexOf(focalCard);

            int otherIndex =
                handCards.IndexOf(otherCards[0]);

            bool otherIsOnLeft =
                focalIndex >= 0 &&
                otherIndex >= 0 &&
                otherIndex < focalIndex;

            leftCount = otherIsOnLeft ? 1 : 0;
            rightCount = otherIsOnLeft ? 0 : 1;
        }

        float spacing =
            CalculatePlacementSpacing(otherCount + 1);

        for (int i = 0; i < otherCount; i++)
        {
            KTH_HandCard card =
                otherCards[i];

            int relativeIndex =
                i < leftCount
                    ? i - leftCount
                    : i - leftCount + 1;

            float targetX =
                relativeIndex * spacing;

            targetX +=
                relativeIndex < 0
                    ? -centerGap
                    : centerGap;

            targetX += anchorX;

            int sideCount =
                relativeIndex < 0
                    ? leftCount
                    : rightCount;

            float normalized =
                Mathf.Clamp01(
                    Mathf.Abs(relativeIndex) /
                    (float)Mathf.Max(1, sideCount)
                );

            float targetY =
                -normalized * normalized * arcHeight;

            float targetRotationZ =
                relativeIndex < 0
                    ? normalized * maxRotation
                    : -normalized * maxRotation;

            // 여기도 부채꼴이라 겹친다. 손패와 같은 규칙으로 앞뒤를 준다 —
            // 배치 모드에서만 순서가 달라지면 눈에 걸린다.
            //
            // i 를 쓴다. relativeIndex 는 가운데를 비운 값이라 음수가 섞여
            // 깊이가 앞뒤로 튄다.
            Vector3 depthOffset =
                DepthAxis *
                (CardLayoutCalculator.DepthRank(i, otherCount, depthOrder) * depthStep);

            Vector3 targetPos =
                new Vector3(targetX, targetY, 0f) + depthOffset;

            Vector3 targetRot =
                new Vector3(handTiltAngle, 0f, targetRotationZ);

            card.transform.DOKill();

            Sequence sequence =
                DOTween.Sequence();

            sequence.SetTarget(card.transform);

            sequence.Join(
                card.transform
                    .DOLocalMove(targetPos, duration)
                    .SetEase(moveEase)
            );

            sequence.Join(
                card.transform
                    .DOLocalRotate(targetRot, duration)
                    .SetEase(moveEase)
            );

            sequence.Join(
                card.transform
                    .DOScale(card.BaseScale, duration)
                    .SetEase(moveEase)
            );

            card.UpdateOriginalTransform(targetPos, targetRot);

            // 더블클릭으로 내려가 있는 카드는 "원래 자리"가 방금 새로 계산한
            // 부채꼴 자리로 갱신됐으니, 그 새 자리를 기준으로 내려간 오프셋을
            // 다시 적용한다. 그래야 부채꼴로도 벌어지고 내려간 채로도 있는
            // 두 효과가 같이 보인다.
            if (card.IsMovedDown)
            {
                card.RefreshMoveDownOffset();
            }
        }
    }

    /// <summary>
    /// 배치 모드: 선택된 카드는 중앙(Vector3.zero)으로, 나머지는 그 주위로 부채꼴 벌어짐.
    /// </summary>
    private void SpreadCardsAroundCenter()
    {
        if (selectedCard == null)
        {
            return;
        }

        ApplyFanAroundFocalCard(
            selectedCard,
            0f,
            placementCenterGap,
            placementMoveDuration
        );

        selectedCard.transform.DOKill();

        // 앞으로 빼는 것이 **먼저다.** 아래 트윈이 그 값을 목표에 실어야 한다.
        // 예전에는 이 줄이 맨 끝에 있었고 목표도 Vector3.zero 였다 —
        // 그래서 고른 카드가 부채꼴로 흩어진 이웃들보다 뒤에 놓였다.
        selectedCard.BringToFront();

        Sequence selectedSequence =
            DOTween.Sequence();

        selectedSequence.SetTarget(selectedCard.transform);

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalMove(selectedCard.FrontOffset, placementMoveDuration)
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOScale(
                    selectedCard.BaseScale * selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );
    }

    private float CalculatePlacementSpacing(
        int count)
    {
        if (count <= 1)
        {
            return maxCardSpacing;
        }

        float spacing =
            Mathf.Min(
                maxCardSpacing,
                maxHandWidth /
                (count - 1)
            );

        return Mathf.Max(
            minCardSpacing,
            spacing
        );
    }

    public void UpdateHandLayout(
        KTH_HandCard newlyDrawnCard = null,
        float duration = 0.35f,
        bool useStagger = true)
    {
        // 더블클릭으로 "내려가 있는" 카드가 있는 채로 손패 재배치가 일어나면,
        // 그 카드는 더블클릭 쪽 좌표(isMovedDown)와 이 재배치 쪽 좌표를 각각
        // 다른 시점에 따로 계산해서 서로 자리를 다투게 된다(취소 시 엉뚱한
        // 자리로 튐). 재배치가 일어나는 순간 더블클릭 상태를 먼저 정리해서
        // 자리를 정하는 주체를 이 재배치 하나로 되돌린다.
        KTH_HandCard.CancelDoubleClick();

        int count =
            handCards.Count;

        if (count == 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

            var transformData =
                CardLayoutCalculator
                    .CalculateCardTransform(
                        i,
                        count,
                        maxCardSpacing,
                        minCardSpacing,
                        maxHandWidth,
                        arcHeight,
                        maxRotation,
                        depthStep: depthStep,
                        depthOrder: depthOrder,
                        depthAxis: DepthAxis
                    );

            Vector3 targetPosition =
                transformData.LocalPosition;

            Vector3 targetRotation =
                new Vector3(
                    handTiltAngle,
                    0f,
                    transformData.ZRotation
                );

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPosition,
                    targetRotation,
                    drawDuration
                );

                card.UpdateOriginalTransform(
                    targetPosition,
                    targetRotation
                );

                continue;
            }

            card.UpdateOriginalTransform(
                targetPosition,
                targetRotation
            );

            if (!card.IsSelected)
            {
                float delay =
                    useStagger
                        ? i * staggerDelay
                        : 0f;

                card.MoveToHandPositionWithDelay(
                    targetPosition,
                    targetRotation,
                    duration,
                    delay,
                    moveEase
                );
            }
            else if (!card.IsConfirmed)
            {
                // 호버로만 들려있는(확정 전) 카드는 재배치를 건너뛰는데, 그대로 두면
                // 카드 수가 바뀌어 간격(cardSpacing)이 다시 계산될 때 예전 자리에
                // 계속 떠 있게 된다. 옆 카드가 새 간격으로 옮겨오면서 그 자리와
                // 겹치는 게 "손패 카드가 가끔 겹친다"는 증상의 원인이었다.
                // 방금 갱신한 새 OriginalLocalPosition 기준으로 들림 오프셋만
                // 다시 적용해서 새 슬롯 위로 옮긴다.
                card.RefreshSelectedOffset();
            }
        }

        if (selectedCard != null &&
            selectedCard.IsSelected)
        {
            selectedCard.BringToFront();
        }
    }

    public void MoveDownForPlacement()
    {
        if (!enableMoveDown ||
            isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = true;

        AnimateContainerY(
            originalContainerLocalPos.y -
            placementMoveDownDistance
        );
    }

    public void GatherCardsToCenter(
        float duration)
    {
        if (handCards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

            card.transform.DOKill();

            Sequence gatherSequence =
                DOTween.Sequence();

            gatherSequence.Join(
                card.transform
                    .DOLocalMove(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOLocalRotate(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOScale(
                        card.BaseScale,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );
        }
    }

    public void RestoreCardsFromCenter(
        float duration)
    {
        UpdateHandLayout(
            null,
            duration,
            false
        );
    }

    public void MoveUpFromPlacement()
    {
        if (!isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = false;

        AnimateContainerY(
            originalContainerLocalPos.y
        );
    }

    private void AnimateContainerY(
        float targetY)
    {
        transform.DOKill();

        transform
            .DOLocalMoveY(
                targetY,
                placementMoveDuration
            )
            .SetEase(Ease.OutCubic);
    }
}
