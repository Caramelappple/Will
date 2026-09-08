using System;
using System.Collections.Generic;
using _Scripts.LSO.Reward;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 유언 양초. **하나뿐이고, 항상 어느 유언 하나를 들고 있다.**
    ///
    /// 숫자키를 누르면 불꽃 색이 그 유언의 색으로 바뀐다.
    /// 카드를 누르면 그때 들고 있던 색이 그 카드에 붙는다(LSO_WillPainter).
    ///
    /// ── 초를 여러 개 세우지 않는 이유 ─────────────────────────
    /// 유언마다 초를 하나씩 세우면 초 모델과 자리를 그만큼 챙겨야 하고,
    /// 화면에 촛불이 여섯 개 늘어서면 목숨 양초와도 헷갈린다.
    /// 초 하나가 색만 바꾸면 만들 것도 없고 읽기도 쉽다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **"안 든 상태"가 없다.** 늘 무언가를 들고 있으므로 켜고 끄는 상태를 따로 두지 않는다.
    /// "유언 없음"은 목록의 한 자리이고, 그것을 고르면 불이 꺼진다.
    ///
    /// 씬 배선: 양초 오브젝트에 붙이고 View 를 연결할 것.
    /// 같은 오브젝트에 LSO_WillPainter 를 붙이면 눌러서 카드에 붙일 수 있다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_WillCandle : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("불꽃 색을 바꿀 겉모습. 비워두면 자신과 자식에서 찾는다.")]
        [SerializeField] private LSO_WillCandleView view;

        [Header("목록")]
        [Tooltip("켜면 맨 끝에 '유언 없음'을 넣는다. 고르면 불이 꺼진다.\n" +
                 "\n" +
                 "끄면 유언을 반드시 붙이게 된다.")]
        [SerializeField] private bool includeNone = true;

        [Tooltip("처음에 들고 있을 자리. 0부터 센다.\n" +
                 "목록보다 크면 맨 앞으로 돌아간다.")]
        [SerializeField, Min(0)] private int startIndex;

        [Header("조작")]
        [Tooltip("켜면 1~9 로 고를 수 있다. 얻은 순서와 같은 번호이고,\n" +
                 "'유언 없음'이 마지막 번호다.")]
        [SerializeField] private bool useNumberKeys = true;

        // 양초를 누르는 것은 "색 넘기기"가 아니라 "고른 카드에 불을 대기"다.
        // 그쪽은 LSO_WillPainter 가 맡는다 — 같은 오브젝트에 붙여두면 된다.
        // 색은 숫자키 전담이다.

        [Header("반응")]
        [Tooltip("든 유언이 바뀔 때마다. 이름을 화면에 띄우는 쪽이 듣는다.")]
        [SerializeField] private LSO_WillTypeEvent onChanged;

        private readonly List<LSO_WillType> _wills = new();
        private int _index;

        /// <summary>
        /// 지금 들고 있는 유언. **늘 유효하다.**
        ///
        /// None이면 "유언 없음"을 든 것이지 안 든 것이 아니다.
        /// 이 양초에는 안 든 상태가 없다.
        /// </summary>
        public LSO_WillType Current =>
            _index >= 0 && _index < _wills.Count ? _wills[_index] : LSO_WillType.None;

        /// <summary>고를 수 있는 유언 수. 숫자키 개수와 같다.</summary>
        public int Count => _wills.Count;

        /// <summary>든 유언이 바뀌었을 때. 코드로 구독하는 쪽이 쓴다.</summary>
        public event Action<LSO_WillType> Changed;

        private void Awake()
        {
            if (view == null) view = GetComponentInChildren<LSO_WillCandleView>(true);

            if (view == null)
                Debug.LogError($"{name}: LSO_WillCandleView가 없어 불꽃을 바꿀 수 없습니다.", this);
        }

        private void OnEnable()
        {
            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null) library.OnItemLibraryUpdated += Rebuild;

            Rebuild();
        }

        private void OnDisable()
        {
            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null) library.OnItemLibraryUpdated -= Rebuild;
        }

        /// <summary>
        /// 고를 수 있는 목록을 다시 만든다.
        ///
        /// 보상으로 유언을 받으면 라이브러리가 알려주므로 그때 다시 부른다.
        /// 들고 있던 유언이 목록에 그대로 있으면 그 자리를 지킨다 —
        /// 유언 하나 받았다고 손에 든 색이 바뀌면 놀란다.
        /// </summary>
        public void Rebuild()
        {
            LSO_WillType held = Current;

            _wills.Clear();
            CollectWills(_wills);

            if (includeNone) _wills.Add(LSO_WillType.None);

            if (_wills.Count == 0)
            {
                // 해금한 유언도 없고 '없음'도 끈 경우. 들 것이 없다.
                _index = 0;
                Apply();
                return;
            }

            int keep = _wills.IndexOf(held);

            _index = keep >= 0 ? keep : Mathf.Clamp(startIndex, 0, _wills.Count - 1);

            Apply();
        }

        /// <summary>
        /// 보유한 유언을 얻은 순서대로 모은다.
        ///
        /// 재고는 중복을 허용하므로 같은 유언이 여러 번 들어 있을 수 있다.
        /// 색은 종류마다 하나면 되니 처음 나온 것만 남긴다.
        /// </summary>
        private static void CollectWills(List<LSO_WillType> into)
        {
            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library == null) return;

            foreach (DLJ_WillDataSO will in library.UnlockedWills)
            {
                if (will == null) continue;

                LSO_WillType type = will.WillType;

                if (type == LSO_WillType.None) continue;
                if (into.Contains(type)) continue;

                into.Add(type);
            }
        }

        /// <summary>그 유언으로 바꾼다. 목록에 없으면 아무 일도 하지 않는다.</summary>
        public void Select(LSO_WillType will)
        {
            int index = _wills.IndexOf(will);

            if (index < 0)
            {
                Debug.LogWarning($"{name}: {will} 은 지금 고를 수 없습니다.", this);
                return;
            }

            SelectAt(index);
        }

        /// <summary>몇 번째 유언으로 바꾼다. 숫자키가 이걸 부른다.</summary>
        public void SelectAt(int index)
        {
            if (_wills.Count == 0) return;
            if (index < 0 || index >= _wills.Count) return;
            if (index == _index) return;

            _index = index;

            Apply();
        }

        /// <summary>다음 색으로 넘긴다. 끝에서는 처음으로 돌아온다.</summary>
        public void Next()
        {
            if (_wills.Count == 0) return;

            SelectAt((_index + 1) % _wills.Count);
        }

        private void Update()
        {
            if (!useNumberKeys || Keyboard.current == null) return;

            int max = Mathf.Min(_wills.Count, 9);

            for (int i = 0; i < max; i++)
            {
                // digit1Key 부터 차례로. 숫자는 얻은 순서와 같고, '없음'이 마지막 번호다.
                Key key = Key.Digit1 + i;

                if (!Keyboard.current[key].wasPressedThisFrame) continue;

                SelectAt(i);
                return;
            }
        }

        /// <summary>지금 자리를 화면과 바깥에 반영한다.</summary>
        private void Apply()
        {
            LSO_WillType will = Current;

            if (view != null) view.Show(will);

            Changed?.Invoke(will);
            onChanged?.Invoke(will);
        }
    }
}
