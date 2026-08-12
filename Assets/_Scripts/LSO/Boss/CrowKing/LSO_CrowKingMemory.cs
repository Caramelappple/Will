using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    /// <summary>
    /// 까마귀왕이 먹어치운 것들의 기록.
    ///
    /// 보관과 한도 검사만 한다. "얼마를 계승할지"나 "2페이즈면 더 준다" 같은 규칙은
    /// LSO_Predation이 정하고, 계산이 끝난 수치만 여기로 넘어온다.
    /// 그래야 페이즈가 늘어나도 이 클래스는 그대로 둘 수 있다.
    ///
    /// 기물을 실제로 바꾸는 일(AddAbility 등)도 하지 않는다. 그건 특성의 몫이다.
    /// </summary>
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_CrowKingMemory : MonoBehaviour
    {
        [Tooltip("되먹임으로 보관할 수 있는 특성 개수.")]
        [SerializeField, Min(1)] private int maxStoredAbilities = 3;

        // 먹어본 "종류". 되먹임의 재처치 판정에 쓴다.
        // 개체(LDY_Animal)가 아니라 동물 데이터로 담는 이유는 두 가지다.
        //   1. 처치한 기물은 곧바로 파괴되어 참조가 죽는다.
        //   2. 곰 두 마리는 서로 다른 개체지만 같은 종류다.
        private readonly List<LSO_AnimalSO> _devoured = new();

        private readonly List<LSO_AbilityType> _stored = new();

        public int InheritedAtk { get; private set; }
        public int InheritedHp { get; private set; }

        /// <summary>흡수해서 보관 중인 특성. 밖에서 목록을 바꿀 수 없다.</summary>
        public IReadOnlyList<LSO_AbilityType> StoredAbilities => _stored;

        public bool HasDevoured(LSO_AnimalSO animal) => animal != null && _devoured.Contains(animal);

        /// <summary>먹은 종류로 등록한다.</summary>
        /// <returns>처음 먹는 종류면 true. 이미 먹어봤으면 false(= 되먹임 조건 성립).</returns>
        public bool TryAddDevour(LSO_AnimalSO animal)
        {
            if (animal == null) return false;
            if (_devoured.Contains(animal)) return false;

            _devoured.Add(animal);
            return true;
        }

        /// <summary>
        /// 계승 수치를 누적한다. 계산이 이미 끝난 값을 넘길 것.
        ///
        /// 능력치 계승에는 횟수 제한이 없다. 한도가 걸린 건 되먹임으로 흡수하는 특성뿐이다.
        /// </summary>
        public void Inherit(int atk, int hp)
        {
            InheritedAtk += Mathf.Max(0, atk);
            InheritedHp += Mathf.Max(0, hp);
        }

        /// <summary>
        /// 특성을 보관 목록에 올린다. 실제로 기물에 붙이는 건 호출한 쪽이 한다.
        /// </summary>
        /// <returns>보관에 성공하면 true. 한도 초과이거나 이미 갖고 있으면 false.</returns>
        public bool TryStoreAbility(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return false;
            if (_stored.Count >= maxStoredAbilities) return false;
            if (_stored.Contains(type)) return false;

            _stored.Add(type);
            return true;
        }
    }
}
