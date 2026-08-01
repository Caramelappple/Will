using System.Collections.Generic;
using System.Linq;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

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

        [Header("Stats")]
        [Tooltip("data의 damage로 초기화된 뒤 버프/디버프로 변할 수 있는 값.")]
        public int baseAtk;

        [Header("Test Only")]
        [Tooltip("data(AnimalSO)가 없을 때만 쓰이는 임시값. data가 있으면 data.range가 항상 우선한다.")]
        [SerializeField] private LDY_RangeType fallbackRangeType;

        [field: SerializeField] public LSO_WillType WillType { get; private set; }

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
        private void OnValidate()
        {
            CacheComponents();
            Init();
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
WillType = card.WillType;
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
    _abilities.Add(ability);

if (Application.isPlaying && isActiveAndEnabled)
    RegisterAbilities();

if (modelTransform == null)
    modelTransform = transform;
this.pos = data.pos;
this.baseAtk = data.damage;
this.rangeType = data.range;
this.abilityType = data.ability;

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

            foreach (LSO_IDamageModifier modifier in _abilities.OfType<LSO_IDamageModifier>())
                health.AddDamageModifier(modifier);

            GameEventDispatcher dispatcher = GameManager.Instance != null
                ? GameManager.Instance.EventDispatcher
                : null;

            if (dispatcher != null)
            {
                foreach (LSO_IAbility ability in _abilities)
                    dispatcher.Register(ability);
            }

            _abilitiesRegistered = true;
        }

        private void UnregisterAbilities()
        {
            if (!_abilitiesRegistered || health == null) return;

            foreach (LSO_IDamageModifier modifier in _abilities.OfType<LSO_IDamageModifier>())
                health.RemoveDamageModifier(modifier);

            // 종료 시점에는 매니저가 이미 사라졌을 수 있으므로 새로 만들지 않는다.
            GameEventDispatcher dispatcher = GameManager.HasInstance
                ? GameManager.Instance.EventDispatcher
                : null;

            if (dispatcher != null)
            {
                foreach (LSO_IAbility ability in _abilities)
                    dispatcher.Unregister(ability);
            }

            _abilitiesRegistered = false;
        }

        #if UNITY_EDITOR
        /// <summary>동물SO 없이 테스트 기물을 배치하는 에디터 도구 전용 진입점.</summary>
        public void EditorSetupWithoutData(LDY_RangeType range, int atk)
        {
            fallbackRangeType = range;
            baseAtk = atk;
        }
        #endif

        // ATK는 항상 이 메서드를 통해서만 조회한다.
        // 늑대/하이에나처럼 상황에 따라 공격력이 변하는 특성은 하위 클래스에서 이 메서드를 override해서 구현한다.
        public virtual int GetAtk()
        {
            int atk = baseAtk;

            foreach(var mod in _abilities.OfType<IStatModifier>())
                atk = mod.ModifyAttack(this, atk);

            return atk;
        }
    }
}
