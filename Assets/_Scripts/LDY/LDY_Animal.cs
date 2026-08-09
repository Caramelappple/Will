
using System;
using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.HealthSystem;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        [Tooltip("data의 damage로 초기화된 뒤 버프/디버프로 변할 수 있는 값.")]
        [SerializeField, FormerlySerializedAs("baseAtk")] private int _baseAtk;

        /// <summary>
        /// 버프/디버프가 더하고 빼는 원본 공격력.
        ///
        /// 필드가 아니라 프로퍼티인 이유는 값이 바뀔 때 OnStatsChanged를 발행하기 위해서다.
        /// 이름을 소문자로 둔 건 기존 호출부(DLJ_SacrificeSystem 등)의 baseAtk += 를 그대로 두기 위해서다.
        /// </summary>
        public int baseAtk
        {
            get => _baseAtk;
            set
            {
                if (_baseAtk == value) return;

                _baseAtk = value;
                RefreshStats();
            }
        }

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

        /// <summary>사거리는 동물 데이터가 원본이다. 복사본을 따로 들고 있지 않는다.</summary>
        public LDY_RangeType RangeType => data != null ? data.range : fallbackRangeType;

        /// <summary>특성 종류도 동물 데이터가 원본이다.</summary>
        public LSO_AbilityType AbilityType => data != null ? data.ability : LSO_AbilityType.None;

        /// <summary>이 개체가 실제로 들고 있는 특성 인스턴스. 외부에서 목록을 바꿀 수 없다.</summary>
        public IReadOnlyList<LSO_IAbility> Abilities => _abilities;

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

LSO_IAbility ability = LSO_AbilityFactory.Create(data.ability);
if (ability != null)
{
    // 보드나 소유자 정보가 필요한 특성에만 컨텍스트를 넘긴다.
    if (ability is LSO_IAbilityInitializable initializable)
        initializable.Initialize(new LSO_AbilityContext(this));

    _abilities.Add(ability);
}

if (Application.isPlaying && isActiveAndEnabled)
    RegisterAbilities();

if (modelTransform == null)
    modelTransform = transform;
this.baseAtk = data.damage;
//this.RangeType = data.range;
        //this.AbilityType = data.ability;

if (health != null)
    health.Init(data.maxHealth);
else
    Debug.LogError($"{name}: Health 컴포넌트를 찾을 수 없습니다.", this);
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

        /// <summary>
        /// 한 시점의 전투 스탯 묶음. 값 비교로 "정말 바뀌었는지"를 판단하는 데 쓴다.
        /// </summary>
        public readonly struct Stats : IEquatable<Stats>
        {
            public readonly int Atk;
            public readonly int Health;
            public readonly int MaxHealth;

            public Stats(int atk, int health, int maxHealth)
            {
                Atk = atk;
                Health = health;
                MaxHealth = maxHealth;
            }

            public bool Equals(Stats other) =>
                Atk == other.Atk && Health == other.Health && MaxHealth == other.MaxHealth;

            public override bool Equals(object obj) => obj is Stats other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Atk, Health, MaxHealth);

            public override string ToString() => $"ATK {Atk}, HP {Health}/{MaxHealth}";
        }

        /// <summary>
        /// 공격력이나 체력이 바뀔 때 발행된다. UI가 값을 다시 그릴 때 쓴다.
        ///
        /// 피해/회복/baseAtk 변경은 자동으로 잡힌다.
        /// 다만 GetAtk()는 특성이 매번 계산하는 값이라(늑대처럼 주변 상황에 반응하는 특성)
        /// 아무도 쓰기를 하지 않아도 결과가 달라질 수 있다. 그런 특성을 만들었다면
        /// 상황이 바뀌는 지점에서 RefreshStats()를 직접 불러야 한다.
        /// </summary>
        public event Action<Stats> OnStatsChanged;

        private Stats _lastStats;

        /// <summary>지금 이 순간의 스탯. 호출할 때마다 새로 계산한다.</summary>
        public Stats CurrentStats =>
            health != null
                ? new Stats(GetAtk(), health.Value, health.MaxValue)
                : new Stats(GetAtk(), 0, 0);

        /// <summary>
        /// 스탯을 다시 계산해 이전과 다르면 OnStatsChanged를 발행한다.
        ///
        /// 값이 같으면 아무 일도 일어나지 않으므로 의심스러우면 그냥 불러도 된다.
        /// Health.Value처럼 이벤트 없이 직접 대입할 수 있는 경로를 메우는 탈출구이기도 하다.
        /// </summary>
        public void RefreshStats()
        {
            Stats next = CurrentStats;
            if (next.Equals(_lastStats)) return;

            _lastStats = next;
            OnStatsChanged?.Invoke(next);
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
 

