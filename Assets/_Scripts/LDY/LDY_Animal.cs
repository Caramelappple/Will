
using System;
using System.Collections.Generic;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Manager;
using UnityEngine;
using _Scripts.LSO.Will;

namespace _Scripts.LDY
{
    [RequireComponent(typeof(Health))]
    public class LDY_Animal : MonoBehaviour
    {
        public LSO_AnimalSO data;

        public Health health;

        [Header("Board State")]
        [Tooltip("x/z는 격자 좌표(0~7), y는 모델 표시용 높이값이며 이동/공격 거리 계산에는 쓰이지 않는다.")]
        public Vector3Int pos;
        public LDY_Team team;

        [Header("Test Only")]
        [Tooltip("data(AnimalSO)가 없을 때만 쓰이는 임시값. data가 있으면 data.range가 항상 우선한다.")]
        [SerializeField] private LDY_RangeType fallbackRangeType;

        [Tooltip("data(AnimalSO)가 없을 때만 쓰이는 임시 이동 칸 수. data가 있으면 data.moveRange가 항상 우선한다.")]
        [Min(1)]
        [SerializeField] private int fallbackMoveRange = 1;

        [Tooltip("data(AnimalSO)가 없을 때만 쓰이는 임시값. data가 있으면 data.playerHealthPoints가 항상 우선한다.\n" +
                 "이 기물을 잃었을 때 플레이어 촛불이 깎이는 양이다. 적 기물에는 쓰이지 않는다.")]
        [Min(0)]
        [SerializeField] private int fallbackPlayerHealthPoints;


        [Tooltip("data의 damage로 초기화된 뒤 버프/디버프로 변할 수 있는 값.")]
        public int baseAtk;

        [field: SerializeField] public LSO_WillType WillType { get; private set; }

        /// <summary>
        /// 플레이어가 직접 고른 유언인지. false면 동물 데이터의 기본값이 들어가 있다.
        /// </summary>
        public bool IsWillChosen { get; private set; }

        /// <summary>
        /// 플레이어가 고른 유언으로 확정한다. 소환 직후 선택 UI 결과가 여기로 들어온다.
        ///
        /// 소환 시점에 이미 기본값이 들어가 있으므로 이건 "덮어쓰기"다.
        /// 개체당 한 번만 먹으며, 두 번 들어오는 건 UI가 콜백을 중복 호출했다는 뜻이라 경고를 남긴다.
        /// </summary>
        public void SetWill(LSO_WillType willType)
        {
            if (IsWillChosen)
            {
                Debug.LogWarning($"{name}: 유언이 이미 {WillType}로 정해져 있습니다.", this);
                return;
            }

            WillType = willType;
            IsWillChosen = true;
        }
        
        /// <summary>
        /// UI용 스크립트
        /// </summary>
        public event Action<LSO_WillType> OnWillChange;

        [Header("3D")]
        [Tooltip("이동/공격 연출 시 실제로 움직일 3D 모델 트랜스폼. 비워두면 자기 자신의 transform을 사용한다.")]
        public Transform modelTransform;

        private readonly List<LSO_IAbility> _abilities = new();
        private bool _abilitiesRegistered;

        // AddAbility가 특성 하나만 연결할 때 쓰는 버퍼.
        // LSO_AbilityWiring.Bind가 목록을 받으므로 매번 리스트를 새로 만들지 않으려고 재사용한다.
        private readonly LSO_IAbility[] _bindBuffer = new LSO_IAbility[1];

        /// <summary>사거리는 동물 데이터가 원본이다. 복사본을 따로 들고 있지 않는다.</summary>
        public LDY_RangeType RangeType => data != null ? data.range : fallbackRangeType;

        /// <summary>한 번에 움직일 수 있는 칸 수. 사거리와 마찬가지로 동물 데이터가 원본이다.</summary>
        public int MoveRange => data != null ? data.MoveRange : Mathf.Max(1, fallbackMoveRange);

        /// <summary>
        /// 이 기물을 잃었을 때 플레이어 촛불이 깎이는 양. 동물 데이터가 원본이다.
        ///
        /// 죽는 시점에 data를 직접 읽으면 비어 있는 기물에서 그대로 터진다.
        /// 사거리·이동력과 같은 규칙으로 맞춰 두어, 부르는 쪽이 data가 있는지 몰라도 되게 한다.
        /// </summary>
        public int PlayerHealthPoints =>
            data != null ? data.playerHealthPoints : Mathf.Max(0, fallbackPlayerHealthPoints);

        /// <summary>
        /// 특성 종류 목록. 동물 데이터가 원본이다.
        ///
        /// "첫 번째 것"을 돌려주는 단수 접근자를 남겨두지 않은 이유는,
        /// 특성이 여럿인 기물에서 하나만 돌려주는 프로퍼티가 반드시 누군가를 속이기 때문이다.
        /// </summary>
        public IReadOnlyList<LSO_AbilityType> AbilityTypes =>
            data != null ? data.AbilityTypes : Array.Empty<LSO_AbilityType>();

        /// <summary>이 개체가 실제로 들고 있는 특성 인스턴스. 외부에서 목록을 바꿀 수 없다.</summary>
        public IReadOnlyList<LSO_IAbility> Abilities => _abilities;

        /// <summary>
        /// 특성 목록이 바뀌었을 때. 되먹임처럼 전투 도중 특성이 늘어나는 경우에 발행된다.
        /// 표시하는 쪽은 이 신호로 다시 그린다. 기물은 누가 보고 있는지 알 필요가 없다.
        /// </summary>
        public event Action AbilitiesChanged;

        private void Awake()
        {
CacheComponents();
Init();
        }

        private void OnEnable()
        {
            RegisterAbilities();
        }

        private void OnDisable()
        {
            UnregisterAbilities();
        }

        #if UNITY_EDITOR
        /// <summary>
        /// 에디터에서는 참조 캐싱까지만 한다.
        /// Init()은 baseAtk와 Health의 직렬화 값을 덮어쓰는데, 이걸 OnValidate에서 하면
        /// AnimalSO ↔ 프리팹이 서로를 참조할 때 "값 변경 → 재임포트 → 값 변경"이 끝없이 반복된다.
        /// 실제 스탯 반영은 Awake와 Setup에서만 이뤄지므로 게임 동작에는 영향이 없다.
        /// </summary>
        private void OnValidate()
        {
            CacheComponents();
        }
        #endif

        /// <summary>
        /// 카드로부터 이 기물을 구성한다. 스탯은 카드가 가리키는 동물SO에서, 유언은 카드에서 가져온다.
        /// 카드가 기물을 만드는 유일한 경로이므로 LSO_AnimalFactory를 통해 호출할 것.
        /// </summary>
        
        public void Setup(LSO_CardSO card, LDY_Team ownerTeam)
        {
            if (card == null || !card.IsValid)
            {
                Debug.LogWarning($"{name}: 유효하지 않은 카드로 Setup이 호출되었습니다.", this);
                return;
            }

            data = card.Animal;

            // 태어날 때부터 유효한 유언을 갖게 한다.
            // 플레이어가 소환한 기물은 곧바로 선택 UI 결과가 이 값을 덮어쓴다.
            // 적 기물이나 스테이지 초기 배치는 이 값을 그대로 쓴다.
            WillType = card.DefaultWill;

            team = ownerTeam;

            CacheComponents();
            Init();
}

        private void CacheComponents()
        {
         if (health == null)
                health = GetComponent<Health>();

         if (modelTransform == null)
                modelTransform = transform;
        }

private void Init()
{
            if (data == null)
            {
                Debug.LogWarning($"{name}: LDY_Animal data is null", this);
                return;
            }

// 특성 목록이 교체되므로 이전 인스턴스가 Health에 남지 않도록 먼저 떼어낸다.
UnregisterAbilities();
_abilities.Clear();

CreateAbilities();

if (Application.isPlaying && isActiveAndEnabled)
    RegisterAbilities();

AbilitiesChanged?.Invoke();

if (modelTransform == null)
    modelTransform = transform;
this.baseAtk = data.damage;

if (health != null)
    health.Init(data.maxHealth);
else
    Debug.LogError($"{name}: Health 컴포넌트를 찾을 수 없습니다.", this);
        }

        /// <summary>
        /// 동물 데이터에 적힌 특성들을 실제 인스턴스로 만든다.
        ///
        /// 팩토리가 매번 새 인스턴스를 만드는 건 의도된 것이다.
        /// 특성은 개체마다 상태(발동 여부, 누적 수치)를 가지므로 인스턴스를 공유하면 상태가 섞인다.
        /// </summary>
        private void CreateAbilities()
        {
            IReadOnlyList<LSO_AbilityType> types = data.AbilityTypes;

            for (int i = 0; i < types.Count; i++)
            {
                LSO_AbilityType type = types[i];
                if (type == LSO_AbilityType.None) continue;

                // 같은 특성이 두 번 들어오면 Health와 Dispatcher에 두 번 등록되어 효과가 두 배가 된다.
                // 팩토리가 서로 다른 인스턴스를 만들기 때문에 등록 쪽의 중복 검사에도 걸리지 않는다.
                if (ContainsBefore(types, i, type))
                {
                    Debug.LogWarning($"{name}: 특성 {type}이 중복되어 뒤쪽을 무시합니다.", this);
                    continue;
                }

                LSO_IAbility created = LSO_AbilityFactory.Create(type);
                if (created == null)
                {
                    // 특성이 하나뿐일 때는 "안 먹네" 하고 금방 알아채지만,
                    // 여러 개 중 하나가 빠지면 조용히 묻힌다.
                    Debug.LogWarning($"{name}: 특성 {type}의 생성기가 LSO_AbilityFactory에 없습니다.", this);
                    continue;
                }

                // 보드나 소유자 정보가 필요한 특성에만 컨텍스트를 넘긴다.
                if (created is LSO_IAbilityInitializable initializable)
                    initializable.Initialize(new LSO_AbilityContext(this));

                _abilities.Add(created);
            }
        }

        /// <summary>
        /// 런타임에 특성을 하나 더 붙인다. 되먹임처럼 게임 도중 특성을 얻는 경우에 쓴다.
        ///
        /// RegisterAbilities는 _abilitiesRegistered 플래그 때문에 개체당 한 번만 돈다.
        /// 그래서 나중에 추가된 특성은 여기서 개별로 연결해줘야 한다.
        /// 그러지 않으면 목록에는 들어가 있는데 피해 계산과 전역 이벤트에는 걸리지 않아,
        /// "붙었는데 아무 일도 안 일어나는" 상태가 된다.
        /// </summary>
        /// <returns>실제로 붙은 특성. 중복이거나 생성기가 없으면 null.</returns>
        public LSO_IAbility AddAbility(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return null;

            LSO_IAbility created = LSO_AbilityFactory.Create(type);
            if (created == null)
            {
                Debug.LogWarning($"{name}: 특성 {type}의 생성기가 LSO_AbilityFactory에 없습니다.", this);
                return null;
            }

            // Init의 중복 규칙과 같은 기준을 쓴다. 같은 특성이 두 번 등록되면 효과가 두 배가 된다.
            if (HasAbilityOfSameType(created))
            {
                Debug.LogWarning($"{name}: 특성 {type}을 이미 갖고 있어 추가하지 않습니다.", this);
                return null;
            }

            if (created is LSO_IAbilityInitializable initializable)
                initializable.Initialize(new LSO_AbilityContext(this));

            _abilities.Add(created);

            // 아직 등록 절차를 안 거쳤다면 나중에 RegisterAbilities가 목록째로 처리한다.
            if (_abilitiesRegistered)
            {
                _bindBuffer[0] = created;
                LSO_AbilityWiring.Bind(_bindBuffer, health, Dispatcher);
                _bindBuffer[0] = null; // 파괴된 특성을 계속 붙들고 있지 않도록 비운다.
            }

            AbilitiesChanged?.Invoke();

            return created;
        }

        private bool HasAbilityOfSameType(LSO_IAbility candidate)
        {
            for (int i = 0; i < _abilities.Count; i++)
            {
                if (_abilities[i] != null && _abilities[i].GetType() == candidate.GetType())
                    return true;
            }

            return false;
        }

        private static bool ContainsBefore(IReadOnlyList<LSO_AbilityType> types, int index, LSO_AbilityType type)
        {
            for (int i = 0; i < index; i++)
            {
                if (types[i] == type) return true;
            }

            return false;
        }

        // 특성을 두 곳에 연결한다.
        // - 데미지 계열(LSO_IDamageModifier)은 자신의 Health에
        // - 턴/사망 등 게임 이벤트 계열은 GameEventDispatcher에
        private void RegisterAbilities()
        {
            if (_abilitiesRegistered || health == null) return;

            LSO_AbilityWiring.Bind(_abilities, health, Dispatcher);

            // 특성 유무와 무관하게 항상 연결해둔다.
            // 조건부로 걸면 나중에 특성이 바뀌었을 때 해제 조건과 어긋나 구독이 남는다.
            health.OnHit += HandleHit;

            _abilitiesRegistered = true;
        }

        private void UnregisterAbilities()
        {
            if (!_abilitiesRegistered || health == null) return;

            LSO_AbilityWiring.Unbind(_abilities, health, Dispatcher);

            health.OnHit -= HandleHit;

            _abilitiesRegistered = false;
        }

        /// <summary>
        /// 전역 이벤트 통로. 종료 시점에는 매니저가 이미 사라졌을 수 있으므로 새로 만들지 않는다.
        /// </summary>
        private static GameEventDispatcher Dispatcher =>
            GameManager.HasInstance ? GameManager.Instance.EventDispatcher : null;

        #if UNITY_EDITOR
        /// <summary>동물SO 없이 테스트 기물을 배치하는 에디터 도구 전용 진입점.</summary>
        public void EditorSetupWithoutData(LDY_RangeType range, int atk)
        {
            fallbackRangeType = range;
            baseAtk = atk;
        }
        #endif

        /// <summary>Health가 보낸 피격 신호를 피격 반응 특성들에게 전달한다.</summary>
        private void HandleHit(DamageData data)
        {
            LSO_AbilityNotify.Notify<LSO_IOnHit>(_abilities, a => a.OnHit(this, data));
        }

        // ATK는 항상 이 메서드를 통해서만 조회한다.
        // 늑대/하이에나처럼 상황에 따라 공격력이 변하는 특성은 하위 클래스에서 이 메서드를 override해서 구현한다.
        public virtual int GetAtk()
        {
            return LSO_AbilityNotify.Accumulate<IStatModifier>(
                _abilities, baseAtk, (mod, value) => mod.ModifyAttack(this, value));
        }
    }
}
 

