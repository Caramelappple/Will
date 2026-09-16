using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// 여러 조건을 하나로 묶는다. **전부 맞아야** 맞는 것으로 본다.
    ///
    /// ── 왜 따로 두나 ──────────────────────────────────────────
    /// 표식을 띄울 조건이 둘이다 — 턴을 넘길 수 있고(LSO_EndTurnReady),
    /// 코스트를 다 썼고(LSO_OutOfCost). 유니티 이벤트는 여럿을 "그리고"로
    /// 엮을 수 없어서, 각자 SetShown 을 부르면 나중에 부른 쪽이 이긴다.
    ///
    /// 조건이 셋이 되어도 표식 쪽은 그대로다. 여기 목록에 한 줄 더하면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 조건들과 같은 오브젝트에 붙이고, 아래 목록에 끌어다 놓는다.
    /// On Changed 에 LSO_ReadyMarker.SetShown 을 건다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_ReadyConditions : MonoBehaviour, LSO_IReadyCondition
    {
        [Tooltip("전부 맞아야 하는 조건들. LSO_IReadyCondition 을 구현한 것만 쓰인다.\n" +
                 "\n" +
                 "비워두면 언제나 맞는 것으로 본다 — 조건 없이 늘 켜진다.")]
        [SerializeField] private MonoBehaviour[] conditions;

        [Header("반응")]
        [Tooltip("바뀔 때마다. 인자는 전부 맞는지다.\n" +
                 "LSO_ReadyMarker.SetShown 을 동적 인자로 걸면 된다.")]
        [SerializeField] private UnityEvent<bool> onChanged;

        /// <summary>전부 맞는지.</summary>
        public bool IsMet { get; private set; }

        public event Action<bool> Changed;

        private readonly List<LSO_IReadyCondition> _valid = new();

        private void OnEnable()
        {
            Collect();

            foreach (LSO_IReadyCondition c in _valid)
                c.Changed += HandleAnyChanged;

            // 켜질 때 한 번 맞춰 알린다. 안 그러면 조건이 처음 바뀔 때까지
            // 듣는 쪽이 지난 상태로 남는다.
            IsMet = Evaluate();

            onChanged?.Invoke(IsMet);
            Changed?.Invoke(IsMet);
        }

        private void OnDisable()
        {
            foreach (LSO_IReadyCondition c in _valid)
            {
                if (c != null) c.Changed -= HandleAnyChanged;
            }

            _valid.Clear();
        }

        /// <summary>
        /// 쓸 수 있는 조건만 골라둔다.
        ///
        /// 인스펙터는 인터페이스를 직접 못 받아서 MonoBehaviour 로 받는다.
        /// 엉뚱한 것이 꽂히면 조용히 빼지 않고 이름을 짚어 알린다 — 조건 하나가
        /// 빠진 채로 도는 것은 "표식이 아무 때나 뜬다"로 나타난다.
        /// </summary>
        private void Collect()
        {
            _valid.Clear();

            if (conditions == null) return;

            foreach (MonoBehaviour behaviour in conditions)
            {
                if (behaviour == null) continue;

                if (behaviour is LSO_IReadyCondition condition)
                {
                    _valid.Add(condition);
                    continue;
                }

                Debug.LogWarning(
                    $"{name}: '{behaviour.GetType().Name}' 은 조건이 아니라 무시합니다. " +
                    "LSO_IReadyCondition 을 구현한 것만 꽂을 수 있습니다.", this);
            }
        }

        private void HandleAnyChanged(bool _)
        {
            bool met = Evaluate();

            if (met == IsMet) return;

            IsMet = met;

            onChanged?.Invoke(met);
            Changed?.Invoke(met);
        }

        private bool Evaluate()
        {
            for (int i = 0; i < _valid.Count; i++)
            {
                if (!_valid[i].IsMet) return false;
            }

            return true;
        }
    }
}
