using System;
using System.Collections.Generic;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Attributes;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 기물별 전용 scorer 매핑.
    /// 기물 식별용 문자열 id가 아직 없으므로 LDY_Animal.data(LSO_AnimalSO 에셋 참조)를 키로 쓴다.
    /// 문자열 id가 생기면 Key만 바꾸면 되고 Brain은 손대지 않아도 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "LDY_ScorerRegistry", menuName = "SO/LDY/Enemy Scorer Registry")]
    public class LDY_ScorerRegistry : ScriptableObject
    {
        [Serializable]
        private class Binding
        {
            [SerializeField] private LSO_AnimalSO animal;

            // 인터페이스 목록은 SerializeReference로만 직렬화된다.
            //
            // 다만 SerializeReference는 저장만 해결한다. 유니티 기본 인스펙터에는
            // 구현 타입을 고를 UI가 없어서, 요소를 추가해도 계속 null로 남는다.
            // LSO_SubclassPicker가 그 드롭다운을 그려준다.
            [SerializeReference, LSO_SubclassPicker]
            private List<LDY_IActionScorer> scorers = new();

            public LSO_AnimalSO Animal => animal;
            public IReadOnlyList<LDY_IActionScorer> Scorers => scorers;
        }

        [SerializeField] private List<Binding> bindings = new();

        private static readonly LDY_IActionScorer[] Empty = Array.Empty<LDY_IActionScorer>();

        private Dictionary<LSO_AnimalSO, IReadOnlyList<LDY_IActionScorer>> _lookup;

        public IReadOnlyList<LDY_IActionScorer> GetScorers(LDY_Animal animal)
        {
            if (animal == null || animal.data == null) return Empty;

            if (_lookup == null)
                BuildLookup();

            return _lookup.TryGetValue(animal.data, out IReadOnlyList<LDY_IActionScorer> scorers) ? scorers : Empty;
        }

        /// <summary>에디터에서 바인딩을 고친 뒤 재생 중에 즉시 반영하고 싶을 때 호출한다.</summary>
        public void Invalidate()
        {
            _lookup = null;
        }

        private void OnDisable()
        {
            Invalidate();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<LSO_AnimalSO, IReadOnlyList<LDY_IActionScorer>>();

            foreach (Binding binding in bindings)
            {
                if (binding == null || binding.Animal == null || binding.Scorers == null) continue;

                if (_lookup.ContainsKey(binding.Animal))
                {
                    Debug.LogWarning($"{name}: {binding.Animal.name}에 대한 바인딩이 중복되어 뒤쪽 항목을 무시합니다.", this);
                    continue;
                }

                _lookup.Add(binding.Animal, binding.Scorers);
            }
        }
    }
}
