using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LSO.Deck.Data
{
    [CreateAssetMenu(fileName = "LSO_CardSO", menuName = "LSO/Deck/CardSO")]
    public class LSO_CardSO : ScriptableObject
    {
        [Tooltip("이 카드가 소환하는 동물 데이터.")]
        [SerializeField] private LSO_AnimalSO animal;

        [Tooltip("유언은 동물이 아니라 카드가 정한다. 같은 동물이라도 카드마다 다를 수 있다.")]
        [SerializeField] private LSO_WillType willType;

        [Tooltip("카드에 표시할 일러스트.")]
        [SerializeField] private Sprite image;

        public LSO_AnimalSO Animal => animal;
        public LSO_WillType WillType => willType;
        public Sprite Image => image;

        /// <summary>동물 데이터가 연결되어 있는지. 사용하는 쪽은 항상 이걸 먼저 확인할 것.</summary>
        public bool IsValid => animal != null;

        // 아래 접근자들은 사용하는 쪽이 animal을 두 단계 뚫고 들어가지 않도록 감싼 것이다.
        // "이 카드는 공격력 +1" 같은 카드 단위 보정이 생기면 여기서만 고치면 된다.
        public string AnimalName => IsValid ? animal.animalName : string.Empty;
        public string Description => IsValid ? animal.description : string.Empty;
        public int Cost => IsValid ? animal.cost : 0;
        public int Damage => IsValid ? animal.damage : 0;
        public int MaxHealth => IsValid ? animal.maxHealth : 0;
        public LSO_AbilityType Ability => IsValid ? animal.ability : LSO_AbilityType.None;
        public LDY_RangeType Range => IsValid ? animal.range : LDY_RangeType.Melee;

        /// <summary>보드에 소환될 기물 프리팹. 동물 데이터가 원본이다.</summary>
        public GameObject UnitPrefab => IsValid ? animal.unitPrefab : null;
    }
}
