using UnityEngine;

namespace _Scripts.LSO.UI.Feedback
{
    /// <summary>
    /// 거부 신호를 콘솔로 내보내는 구독자.
    ///
    /// LSO_RejectSignal 은 지금까지 아무도 듣지 않았다. 부르는 쪽은 있는데
    /// 받는 쪽이 없어서, 왜 카드를 못 뽑았는지가 화면에도 콘솔에도 안 나왔다.
    /// 우선 콘솔로만 흘려보내 "신호가 오긴 오는가"를 눈으로 볼 수 있게 한다.
    ///
    /// ── 이 로그의 성격 ───────────────────────────────────────
    /// 여기 찍히는 것은 <b>버그가 아니라 규칙대로 돌아간 결과다.</b>
    /// 덱이 비었거나, 드로우를 다 썼거나, 손패가 찼거나.
    ///
    /// 원래 LSO_RejectSignal 을 만든 이유가 이런 정상 상태를 콘솔 경고에서
    /// 빼내는 것이었다(LSO_RejectSignal 주석 참고). 정상 플레이에서 경고가
    /// 쌓이면 진짜 배선 경고가 그 아래로 밀려 안 보이기 때문이다.
    ///
    /// 그래서 경고로 낼지 일반 로그로 낼지를 인스펙터에서 고르게 해뒀다.
    /// 콘솔이 덮이기 시작하면 As Warning 을 끄면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 카드 흔들기·소리·화면 문구를 붙일 때가 오면 이 컴포넌트를 지우지 말고
    /// 그 옆에 나란히 두면 된다. 신호 하나에 구독자는 몇이든 붙는다.
    /// </summary>
    public sealed class LSO_RejectLogger : MonoBehaviour
    {
        [Tooltip("체크하면 LogWarning, 풀면 Log 로 찍는다.\n" +
                 "\n" +
                 "여기 찍히는 것은 규칙대로 돌아간 결과지 잘못이 아니다.\n" +
                 "정상 플레이에서 경고가 쌓여 진짜 배선 경고를 덮기 시작하면 풀 것.")]
        [SerializeField] private bool asWarning = true;

        [Tooltip("붙은 오브젝트 이름을 앞에 붙인다. 씬에 여러 개 둬서 구분해야 할 때만.")]
        [SerializeField] private bool includeObjectName;

        /// <summary>
        /// 씬에 둘 이상 있으면 같은 거부가 여러 줄로 찍힌다.
        /// 로그가 두 배로 나오는 것을 "신호가 두 번 왔다"로 오해하기 쉬워서 짚어둔다.
        /// </summary>
        private static LSO_RejectLogger _first;

        private void OnEnable()
        {
            if (_first == null)
            {
                _first = this;
            }
            else if (_first != this)
            {
                Debug.LogWarning(
                    $"{name}: LSO_RejectLogger 가 씬에 둘 이상 있습니다. " +
                    $"같은 거부가 여러 줄로 찍힙니다 — 먼저 켜진 것은 '{_first.name}' 입니다.", this);
            }

            LSO_RejectSignal.Rejected += HandleRejected;
        }

        private void OnDisable()
        {
            LSO_RejectSignal.Rejected -= HandleRejected;

            if (_first == this) _first = null;
        }

        private void HandleRejected(LSO_RejectReason reason)
        {
            string message = includeObjectName
                ? $"[{name}] {MessageOf(reason)}"
                : MessageOf(reason);

            // 로그를 클릭했을 때 이 오브젝트가 하이라이트되도록 this 를 넘긴다.
            if (asWarning) Debug.LogWarning(message, this);
            else Debug.Log(message, this);
        }

        /// <summary>
        /// 거부 이유를 사람이 읽는 문구로.
        ///
        /// 화면 토스트를 붙일 때가 오면 이 문구는 여기서 빼내야 한다 —
        /// 콘솔과 화면이 각자 문구를 들고 있으면 고칠 때 두 곳을 맞춰야 한다.
        /// 그때는 LSO_WillText 처럼 UI/Text 아래에 창구를 하나 두고 둘 다 거기를 보게 할 것.
        /// 지금은 읽는 곳이 여기뿐이라 그대로 둔다.
        /// </summary>
        private static string MessageOf(LSO_RejectReason reason)
        {
            switch (reason)
            {
                case LSO_RejectReason.NotEnoughCost:
                    return "코스트가 모자랍니다.";

                case LSO_RejectReason.NotEnoughActionPoint:
                    return "행동력이 모자랍니다.";

                case LSO_RejectReason.InvalidTile:
                    return "놓을 수 없는 칸입니다.";

                case LSO_RejectReason.NotYourTurn:
                    return "지금은 내 턴이 아닙니다.";

                case LSO_RejectReason.Locked:
                    return "아직 잠긴 기능입니다.";

                case LSO_RejectReason.NoDrawsLeft:
                    return "이번 턴에는 더 뽑을 수 없습니다.";

                case LSO_RejectReason.DeckEmpty:
                    return "덱이 비었습니다. 적 턴이 시작되면 버린 더미가 돌아옵니다.";

                case LSO_RejectReason.HandFull:
                    return "손패가 가득 찼습니다.";

                // 이유를 안 붙이고 Raise 한 곳이 있다는 뜻이다. 어느 이유인지
                // 모르면 화면에 뭘 띄울지도 못 정하므로 숫자까지 같이 남긴다.
                default:
                    return $"거부됐지만 이유가 붙어 있지 않습니다. (LSO_RejectReason.{reason})";
            }
        }
    }
}
