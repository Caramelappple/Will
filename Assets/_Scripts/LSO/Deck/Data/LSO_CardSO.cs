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

        //[Tooltip("유언은 동물이 아니라 카드가 정한다. 같은 동물이라도 카드마다 다를 수 있다.")]
        //[SerializeField] private LSO_WillType willType;
        //이제 유언 선택은 소환 할때 진행

        [Tooltip("카드에 표시할 일러스트.")]
        [SerializeField] private Sprite image;

        /// <summary>
        /// 세이브/로드에서 이 카드를 찾는 열쇠.
        /// 비워두면 에셋 이름으로 대체한다. 다만 에셋 이름은 언제든 바뀔 수 있으니 id는 직접 채워둘 것.
        /// </summary>

        public LSO_AnimalSO Animal => animal;
        public Sprite Image => image;
        
        //public LSO_WillType will => willType;

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

        /// <summary>
        /// 플레이어가 고르지 않고 소환될 때 쓸 유언. 적 기물과 스테이지 초기 배치가 이 값으로 세워진다.
        /// 플레이어가 직접 소환하면 선택 UI 결과가 이 값을 덮어쓴다.
        /// </summary>
        public LSO_WillType DefaultWill => IsValid ? animal.defaultWill : default;

        /// <summary>보드에 소환될 기물 프리팹. 동물 데이터가 원본이다.</summary>
        [Header("기물의 모델, 모델안에는 빈 LDY_Animal 넣기, Health는 자동으로 들어가게 됨")]
        public GameObject UnitPrefab => IsValid ? animal.unitPrefab : null;
    }
}
