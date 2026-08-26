using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Cost
{
    /// <summary>
    /// 코인을 담는 케이스 하나. 몇 개를 담고 몇 개가 차 있는지만 안다.
    ///
    /// 전체 코스트가 얼마인지는 모른다. LSO_CostAnim이 케이스마다 몫을 나눠 알려준다.
    /// 그래서 케이스를 몇 개 늘리든 이 클래스는 고칠 것이 없다.
    /// </summary>
    public class LSO_CostCase : MonoBehaviour
    {
        [Header("배치")]
        [Tooltip("코인을 매달 부모. 비워두면 자신에게 붙인다.")]
        [SerializeField] private Transform coinRoot;

        [Tooltip("모자란 코인을 채울 프리팹. 코인을 손으로 다 넣어뒀다면 비워둬도 된다.")]
        [SerializeField] private LSO_CostCoin coinPrefab;

        [Tooltip("코인이 늘어설 방향. 케이스의 로컬 축 기준이다.")]
        [SerializeField] private LSO_CostAxis axis = LSO_CostAxis.X;

        [Tooltip("첫 코인이 놓일 자리. 케이스 원점에서 X·Y·Z로 얼마나 밀지 각각 넣는다.\n" +
                 "케이스 모델의 테두리 안쪽으로 넣거나 살짝 띄울 때 쓴다.")]
        [SerializeField] private Vector3 startOffset;

        [Tooltip("코인 사이 간격. 음수로 주면 반대 방향으로 뻗는다.\n" +
                 "UI(RectTransform)는 픽셀 단위라 40 정도가 필요하다.")]
        [SerializeField] private float spacing = 0.35f;

        [Tooltip("Axis가 Custom일 때만 쓴다. 축을 섞어 비스듬히 놓을 때.")]
        [SerializeField] private Vector3 customStep = new Vector3(0.35f, 0f, 0f);

        [Header("연출")]
        [Tooltip("코인끼리 시작을 얼마나 밀지(초). 0이면 한꺼번에 바뀐다.")]
        [SerializeField, Min(0f)] private float coinStagger = 0.03f;

        private readonly List<LSO_CostCoin> _coins = new List<LSO_CostCoin>();

        // GetComponentsInChildren이 매번 배열을 새로 만들지 않도록 재사용한다.
        private readonly List<LSO_CostCoin> _found = new List<LSO_CostCoin>();

        private Transform CoinRoot => coinRoot != null ? coinRoot : transform;

        /// <summary>
        /// 코인 하나마다 옮길 양. 방향과 간격을 하나로 합친 값이다.
        ///
        /// 축을 골라 쓰게 한 이유는 Vector3를 직접 채우면 (0.35, 0, 0)처럼 적어야 하는데,
        /// 방향을 바꾸려다 두 칸을 동시에 채워 비스듬히 놓이는 실수가 잦기 때문이다.
        /// 비스듬히가 정말 필요하면 Custom을 고른다.
        /// </summary>
        private Vector3 Step
        {
            get
            {
                switch (axis)
                {
                    case LSO_CostAxis.X: return new Vector3(spacing, 0f, 0f);
                    case LSO_CostAxis.Y: return new Vector3(0f, spacing, 0f);
                    case LSO_CostAxis.Z: return new Vector3(0f, 0f, spacing);

                    case LSO_CostAxis.MinusX: return new Vector3(-spacing, 0f, 0f);
                    case LSO_CostAxis.MinusY: return new Vector3(0f, -spacing, 0f);
                    case LSO_CostAxis.MinusZ: return new Vector3(0f, 0f, -spacing);

                    default: return customStep;
                }
            }
        }


        /// <summary>이 케이스가 담을 수 있는 개수.</summary>
        public int Capacity => _coins.Count;

        /// <summary>지금 차 있는 개수.</summary>
        public int FilledCount { get; private set; }

        /// <summary>
        /// 담을 개수를 맞춘다.
        ///
        /// 이미 자식으로 놓여 있는 코인을 먼저 거둬 쓴다. 프리팹에 코인을 손으로 배치해두는 경우가
        /// 흔한데, 그것을 무시하고 새로 만들면 화면의 코인과 이 목록이 서로 다른 것을 가리키게 된다.
        /// 그러면 코인은 보이는데 아무리 코스트를 써도 줄어들지 않는다 —
        /// 줄어드는 것은 목록에 든 쪽이고, 보이는 것은 손으로 넣은 쪽이기 때문이다.
        ///
        /// 모자라면 프리팹으로 채우고, 남으면 지운다.
        /// </summary>
        public void Build(int capacity)
        {
            CollectExisting();

            // 모자란 만큼만 새로 만든다.
            while (_coins.Count < capacity)
            {
                if (coinPrefab == null)
                {
                    Debug.LogError(
                        $"{name}: 코인이 {_coins.Count}개뿐인데 {capacity}개가 필요합니다. " +
                        "Coin Prefab을 연결하거나 코인을 그만큼 넣어두세요.", this);
                    break;
                }

                // worldPositionStays를 false로 준다. true(기본값)면 유니티가 월드 크기를 지키려고
                // 부모의 스케일을 상쇄하도록 localScale을 다시 계산해서, 프리팹에 맞춰둔 크기와 달라진다.
                LSO_CostCoin coin = Instantiate(coinPrefab, CoinRoot, false);

                coin.name = $"{coinPrefab.name}_{_coins.Count}";

                _coins.Add(coin);
            }

            // 남는 것은 지운다. 켜둔 채 두면 못 쓰는 코인이 화면에 남는다.
            while (_coins.Count > capacity)
            {
                int last = _coins.Count - 1;

                if (_coins[last] != null)
                    Destroy(_coins[last].gameObject);

                _coins.RemoveAt(last);
            }

            Layout();

            // 만든 직후에는 전부 차 있는 것으로 본다. 실제 값은 LSO_CostAnim이 곧바로 맞춘다.
            FilledCount = _coins.Count;
        }

        /// <summary>
        /// 지금 자식으로 있는 코인을 고른 축 방향으로 다시 늘어놓는다.
        ///
        /// 플레이하지 않아도 부를 수 있다. 인스펙터의 톱니 메뉴 &gt; "코인 정렬"로 눌러
        /// Axis와 Spacing을 만지면서 눈으로 맞추면 된다.
        /// 실행해야만 자리가 잡히면 값이 맞는지 확인할 방법이 없다.
        /// </summary>
        [ContextMenu("코인 정렬")]
        public void LayoutNow()
        {
            CollectExisting();
            Layout();
        }

        /// <summary>
        /// 코인을 순서대로 고른 축 방향으로 늘어놓는다.
        ///
        /// 손으로 넣어둔 코인까지 전부 다시 잡는다. 새로 만든 것만 정렬하면
        /// 케이스 하나 안에서 손으로 놓은 자리와 계산한 자리가 섞여, 코인 개수가
        /// 바뀔 때마다 간격이 들쭉날쭉해진다. 기준을 하나로 둔다.
        /// </summary>
        private void Layout()
        {
            Vector3 step = Step;

            for (int i = 0; i < _coins.Count; i++)
            {
                if (_coins[i] == null) continue;

                Transform coin = _coins[i].transform;

                // 첫 자리에서 시작해 간격만큼씩 밀어 나간다.
                Vector3 target = startOffset + step * i;

                if (coin.localPosition == target) continue;

                coin.localPosition = target;

#if UNITY_EDITOR
                // 에디트 모드에서는 이렇게 표시해야 씬/프리팹에 저장되고 Undo도 먹는다.
                // 안 하면 자리를 옮겨놓고도 저장이 안 돼 다시 열면 원래대로 돌아간다.
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(coin);
#endif
            }
        }

        /// <summary>
        /// 앞에서부터 count개를 채우고 나머지를 비운다.
        ///
        /// 앞에서부터인 이유는 쓸 때 뒤에서 빠지는 것이 자연스럽기 때문이다.
        /// 가운데가 뚫리면 몇 개가 남았는지 세어야 알 수 있다.
        /// </summary>
        public void SetFilled(int count, bool animate = true)
        {
            count = Mathf.Clamp(count, 0, _coins.Count);

            // 물결 방향을 바꾼다. 채울 때는 앞에서부터, 쓸 때는 뒤에서부터 밀어야
            // 실제로 그 순서로 움직이는 것처럼 보인다.
            bool filling = count > FilledCount;

            for (int i = 0; i < _coins.Count; i++)
            {
                if (_coins[i] == null) continue;

                int order = filling ? i : _coins.Count - 1 - i;

                _coins[i].SetFilled(i < count, animate, coinStagger * order);
            }

            FilledCount = count;
        }

        /// <summary>
        /// 자식으로 이미 놓여 있는 코인을 계층 순서대로 거둔다.
        ///
        /// 계층 순서를 그대로 쓰는 이유는 그것이 인스펙터에서 보이는 순서이기 때문이다.
        /// 좌표로 정렬하면 케이스를 뒤집거나 세로로 놓았을 때 순서가 뒤바뀐다.
        /// </summary>
        private void CollectExisting()
        {
            _coins.Clear();

            CoinRoot.GetComponentsInChildren(true, _found);

            foreach (LSO_CostCoin coin in _found)
            {
                if (coin == null) continue;

                _coins.Add(coin);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 인스펙터에서 Axis나 Spacing을 만지는 즉시 씬 뷰에 반영한다.
            // OnValidate 안에서 곧바로 Transform을 건드리면 경고가 나므로 한 박자 미룬다.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (Application.isPlaying) return;

                LayoutNow();
            };
        }
#endif
    }
}
