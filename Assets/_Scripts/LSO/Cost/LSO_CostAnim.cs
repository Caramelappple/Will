using System.Collections.Generic;
using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Cost
{
    public class LSO_CostAnim : MonoBehaviour
    {
        [Header("배치")]
        [Tooltip("케이스를 매달 부모. 여기 로컬 좌표로 늘어놓는다.")]
        public Transform caseTrm;

        [SerializeField] private LSO_CostCase casePrefab;

        [Tooltip("케이스 하나마다 얼마씩 옆으로(또는 위아래로) 옮겨 놓을지.")]
        [SerializeField] private Vector3 layoutStep = new Vector3(1.2f, 0f, 0f);

        [Header("밀려오는 연출")]
        [Tooltip("어느 쪽에서 밀려 들어올지. 오른쪽에서 오면 (1,0,0).")]
        [SerializeField] private Vector3 enterFrom = Vector3.right;

        [Tooltip("제자리에서 얼마나 떨어진 곳에서 출발할지.")]
        [SerializeField, Min(0f)] private float enterDistance = 6f;

        [Tooltip("한 개가 밀려 들어오는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.01f)] private float enterDuration = 0.45f;

        [Tooltip("다음 케이스가 출발하기까지의 간격(초). 0이면 전부 같이 들어온다.")]
        [SerializeField, Min(0f)] private float stagger = 0.08f;

        [Tooltip("도착할 때 살짝 지나쳤다 돌아오면 밀어 넣은 느낌이 난다.")]
        [SerializeField] private Ease enterEase = Ease.OutBack;

        [Tooltip("켜면 일시정지 중에도 연출이 진행된다.")]
        [SerializeField] private bool ignoreTimeScale;

        private readonly List<LSO_CostCase> _cases = new List<LSO_CostCase>();

        private LDY_ActionPointManager Points => LDY_ActionPointManager.instance;

        /// <summary>케이스 하나가 담는 개수. 화면에 표시되는 기본 코스트다.</summary>
        private int BasicCost => Points != null ? Points.Max : 0;

        /// <summary>추가 획득까지 포함한 상한.</summary>
        private int AddMaxCost => Points != null ? Points.AddMax : 0;

        /// <summary>
        /// 필요한 케이스 개수.
        ///
        /// 나눗셈이라 Max가 0이면 터진다. 인스펙터에서 0으로 둔 채 실행하는 일이 실제로 생기므로
        /// 여기서 막는다. 나누어떨어지지 않을 때는 올림한다 — 남는 몇 개를 담을 데가 없으면
        /// 그만큼이 화면에서 사라진다.
        /// </summary>
        private int CaseCount
        {
            get
            {
                if (BasicCost <= 0) return 0;

                return Mathf.CeilToInt((float)AddMaxCost / BasicCost);
            }
        }

        private void Start()
        {
            // Awake가 아니라 Start다. LDY_ActionPointManager가 자기 Awake에서 instance를 잡으므로
            // 같은 프레임의 Awake에서 읽으면 아직 없을 수 있다.
            Rebuild();

            if (Points != null)
                Points.OnActionPointsChanged += HandlePointsChanged;
        }

        private void OnDestroy()
        {
            if (Points != null)
                Points.OnActionPointsChanged -= HandlePointsChanged;

            Clear();
        }

        /// <summary>
        /// 케이스를 다시 만들고 밀어 넣는다. 상한이 바뀐 뒤에 부르면 된다.
        /// </summary>
        public void Rebuild()
        {
            if (!IsWired()) return;

            Clear();

            int count = CaseCount;

            for (int i = 0; i < count; i++)
                _cases.Add(SpawnCase(i));

            // 처음 그리는 것은 연출 없이 맞춘다.
            // 밀려 들어오는 도중에 코인까지 하나씩 튀어나오면 무엇을 봐야 할지 알 수 없다.
            Apply(Points.Current, animate: false);
        }

        // 이벤트는 (현재, 최대)를 준다. 최대는 표시용이라 여기서 쓰지 않는다.
        private void HandlePointsChanged(int current, int max)
        {
            Apply(current, animate: true);
        }

        /// <summary>
        /// 남은 코스트를 케이스들에 나눠 채운다.
        ///
        /// 케이스 i는 [i*BasicCost, (i+1)*BasicCost) 구간을 맡는다.
        /// 앞 케이스를 다 채워야 다음 케이스로 넘어가므로, 여분을 받으면
        /// 두 번째 케이스부터 차오르는 것이 눈에 보인다.
        /// </summary>
        private void Apply(int current, bool animate)
        {
            int basic = BasicCost;
            if (basic <= 0) return;

            for (int i = 0; i < _cases.Count; i++)
            {
                if (_cases[i] == null) continue;

                int filled = Mathf.Clamp(current - i * basic, 0, basic);

                _cases[i].SetFilled(filled, animate);
            }
        }

        /// <summary>이미 만들어둔 케이스를 그대로 두고 등장 연출만 다시 튼다.</summary>
        public void Replay()
        {
            for (int i = 0; i < _cases.Count; i++)
            {
                if (_cases[i] == null) continue;

                SlideIn(_cases[i].transform, RestPosition(i), i);
            }
        }

        private bool IsWired()
        {
            // 연결이 빠지면 화면에 아무것도 안 나올 뿐이라 원인이 보이지 않는다. 여기서 짚어준다.
            if (caseTrm == null)
            {
                Debug.LogError($"{name}: Case Trm이 비어 있어 케이스를 놓을 곳이 없습니다.", this);
                return false;
            }

            if (casePrefab == null)
            {
                Debug.LogError($"{name}: Case Prefab이 비어 있습니다.", this);
                return false;
            }

            if (Points == null)
            {
                Debug.LogError($"{name}: LDY_ActionPointManager가 없어 케이스 개수를 정할 수 없습니다.", this);
                return false;
            }

            if (BasicCost <= 0)
            {
                Debug.LogError($"{name}: 기본 코스트(Max)가 0이라 케이스를 만들 수 없습니다.", this);
                return false;
            }

            return true;
        }

        private LSO_CostCase SpawnCase(int index)
        {
            // worldPositionStays를 false로 준다. true(기본값)면 유니티가 월드 크기를 지키려고
            // 부모의 스케일을 상쇄하도록 localScale을 다시 계산해서, 프리팹에 맞춰둔 크기와 달라진다.
            // false면 프리팹의 로컬 값이 그대로 들어온다.
            LSO_CostCase item = Instantiate(casePrefab, caseTrm, false);

            item.name = $"{casePrefab.name}_{index}";

            // 케이스 하나가 기본 코스트만큼을 담는다.
            item.Build(BasicCost);

            SlideIn(item.transform, RestPosition(index), index);

            return item;
        }

        private Vector3 RestPosition(int index)
        {
            return layoutStep * index;
        }
        
        private void SlideIn(Transform target, Vector3 rest, int order)
        {
            Vector3 direction = enterFrom.sqrMagnitude > 0f ? enterFrom.normalized : Vector3.right;

            target.localPosition = rest + direction * enterDistance;

            target.DOLocalMove(rest, enterDuration)
                .SetDelay(stagger * order)
                .SetEase(enterEase)
                .SetUpdate(ignoreTimeScale)
                .SetLink(target.gameObject);
        }

        private void Clear()
        {
            foreach (LSO_CostCase item in _cases)
            {
                if (item == null) continue;

                // 트윈이 붙은 채로 지우면 DOTween이 파괴된 대상을 붙들고 경고를 낸다.
                // SetLink가 대신 정리해주지만, 같은 프레임에 다시 만드는 경우까지
                // 맡기면 순서가 애매해지므로 여기서 명시적으로 끊는다.
                item.transform.DOKill();

                Destroy(item.gameObject);
            }

            _cases.Clear();
        }
    }
}
