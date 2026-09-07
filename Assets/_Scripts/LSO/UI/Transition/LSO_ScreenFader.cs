using System.Collections;
using UnityEngine;

namespace _Scripts.LSO.UI.Transition
{
    /// <summary>
    /// 화면 전체를 덮는 암전 판.
    ///
    /// 씬이 바뀌어도 살아남아야 해서 DontDestroyOnLoad로 남는다.
    /// 그래서 반드시 하이어라키 최상단(루트) 오브젝트여야 한다.
    ///
    /// 프리팹 구성:
    ///   ScreenFader   Canvas(Screen Space - Overlay) + CanvasGroup + 이 스크립트
    ///   └── Black     Image (검은색, Raycast Target 꺼둘 것)
    ///
    /// timescale과 무관하게 돌기 때문에 일시정지 중 씬 전환도 정상 동작한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LSO_ScreenFader : MonoBehaviour, LSO_IScreenFader
    {
        public static LSO_IScreenFader Current { get; private set; }

        [Tooltip("화면을 가리는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float coverDuration = 0.25f;

        [Tooltip("화면을 걷는 데 걸리는 시간. 보통 가릴 때보다 조금 길게 두면 부드럽다.")]
        [SerializeField, Min(0f)] private float revealDuration = 0.35f;

        [Tooltip("켜면 게임을 검은 화면으로 시작해서 밝아진다.")]
        [SerializeField] private bool startCovered = true;

        [Tooltip("다른 UI 위에 확실히 올라가도록 강제할 정렬 순서.")]
        [SerializeField] private int sortingOrder = 32000;

        // 코루틴 핸들을 들고 있지 않다. Cover·Reveal은 부르는 쪽이 yield로 기다리는
        // 형태라 이 컴포넌트가 멈출 권한을 갖지 않는다.
        // 예전에는 쓰지 않는 _routine 필드가 남아 있어서, 읽는 사람이
        // "어딘가에서 멈추겠거니" 하고 오해할 여지가 있었다.
        private CanvasGroup _canvasGroup;

        public bool IsCovered { get; private set; }

        private void Awake()
        {
            // 씬마다 프리팹을 놔뒀다면 두 번째부터는 스스로 사라진다.
            if (Current != null && !ReferenceEquals(Current, this))
            {
                Destroy(gameObject);
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();

            if (transform.parent != null)
            {
                Debug.LogWarning(
                    $"{name}: DontDestroyOnLoad는 루트 오브젝트에만 적용된다. 부모에서 꺼내 최상단에 둘 것.", this);
                transform.SetParent(null, true);
            }

            Current = this;
            DontDestroyOnLoad(gameObject);

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }

            IsCovered = startCovered;
            _canvasGroup.alpha = startCovered ? 1f : 0f;
            _canvasGroup.blocksRaycasts = startCovered;
            _canvasGroup.interactable = false;
        }

        private void Start()
        {
            // 첫 씬이 다 준비된 뒤에 밝아진다.
            if (startCovered)
                StartCoroutine(Reveal());
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Current, this))
                Current = null;
        }

        public IEnumerator Cover()
        {
            if (IsCovered) yield break;

            // 전환 중 클릭이 새어 들어가면 다음 씬에서 엉뚱한 게 눌린다.
            _canvasGroup.blocksRaycasts = true;

            yield return FadeTo(1f, coverDuration);

            IsCovered = true;
        }

        public IEnumerator Reveal()
        {
            if (!IsCovered) yield break;

            yield return FadeTo(0f, revealDuration);

            _canvasGroup.blocksRaycasts = false;
            IsCovered = false;
        }

        /// <summary>
        /// DOTween 대신 직접 보간한다.
        /// 씬 로딩 중에는 트윈 엔진이 다른 씬 오브젝트를 정리하느라 끊길 수 있고,
        /// 여기서는 unscaledDeltaTime만 있으면 충분하다.
        /// </summary>
        private IEnumerator FadeTo(float target, float duration)
        {
            float start = _canvasGroup.alpha;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = target;
        }
    }
}
