using System.Text;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Boss.CrowKing
{
    /// <summary>
    /// 까마귀왕이 기획대로 도는지 확인하는 테스트용 컴포넌트.
    /// 보스 프리팹에 붙였다가 확인이 끝나면 떼면 된다. 다른 코드는 전혀 건드리지 않는다.
    ///
    /// 값이 바뀔 때만 찍으므로 콘솔이 도배되지 않는다.
    /// dumpKey를 누르면 현재 상태 전체를 한 번에 볼 수 있다.
    /// </summary>
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_CrowKingDebug : MonoBehaviour
    {
        [Header("전체 상태 출력")]
        [SerializeField] private Key dumpKey = Key.F1;

        [Header("무엇을 찍을지")]
        [SerializeField] private bool logPrey = true;
        [SerializeField] private bool logPhase = true;
        [SerializeField] private bool logInherit = true;
        [SerializeField] private bool logFrenzy = true;
        [SerializeField] private bool logAbilities = true;
        [SerializeField] private bool logDamage;

        private const int FrenzyStep = 6;

        private LDY_Animal _animal;
        private LSO_PreyTracker _tracker;
        private LSO_BossPhase _phase;
        private LSO_CrowKingMemory _memory;

        private int _lastInheritedAtk;
        private int _lastInheritedHp;
        private int _lastAbilityCount;
        private int _lastStoredCount;
        private int _lastFrenzyStep;

        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();
            _tracker = GetComponent<LSO_PreyTracker>();
            _phase = GetComponent<LSO_BossPhase>();
            _memory = GetComponent<LSO_CrowKingMemory>();

            Report("부착됨", $"Tracker={Yes(_tracker)}  Phase={Yes(_phase)}  Memory={Yes(_memory)}");
        }

        private void Start()
        {
            Snapshot();
            Dump("시작 상태");
        }

        private void OnEnable()
        {
            if (_tracker != null) _tracker.PreyChanged += HandlePreyChanged;
            if (_phase != null) _phase.OnPhaseChange += HandlePhaseChanged;
            if (_animal.health != null) _animal.health.OnDamage += HandleDamaged;
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.PreyChanged -= HandlePreyChanged;
            if (_phase != null) _phase.OnPhaseChange -= HandlePhaseChanged;
            if (_animal != null && _animal.health != null) _animal.health.OnDamage -= HandleDamaged;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[dumpKey].wasPressedThisFrame)
                Dump("수동 출력");

            CheckInherit();
            CheckAbilities();
            CheckFrenzy();
        }

        private void HandlePreyChanged(LDY_Animal prey)
        {
            if (!logPrey) return;

            Report("사냥감", prey != null
                ? $"{Name(prey)} @({prey.pos.x},{prey.pos.z})  이미 먹어본 종류={Devoured(prey)}"
                : "없음");
        }

        private void HandlePhaseChanged(int phase)
        {
            if (!logPhase) return;

            Report("페이즈", $"{phase}  (HP {Hp()})");
        }

        private void HandleDamaged(DamageResultData data)
        {
            if (!logDamage) return;

            Report("피격", $"-{data.damage}  HP {Hp()}  페이즈={Phase()}");
        }

        private void CheckInherit()
        {
            if (_memory == null) return;

            int atk = _memory.InheritedAtk;
            int hp = _memory.InheritedHp;
            if (atk == _lastInheritedAtk && hp == _lastInheritedHp) return;

            int dAtk = atk - _lastInheritedAtk;
            int dHp = hp - _lastInheritedHp;
            _lastInheritedAtk = atk;
            _lastInheritedHp = hp;

            if (logInherit)
                Report("포식", $"+{dAtk} ATK / +{dHp} HP   누적 {atk}/{hp}   실제 ATK={_animal.GetAtk()}  HP {Hp()}");
        }

        private void CheckAbilities()
        {
            if (_animal.Abilities == null) return;

            int count = _animal.Abilities.Count;
            int stored = _memory != null ? _memory.StoredAbilities.Count : 0;
            if (count == _lastAbilityCount && stored == _lastStoredCount) return;

            _lastAbilityCount = count;
            _lastStoredCount = stored;

            if (logAbilities)
                Report("되먹임", $"특성 {count}개 → {AbilityNames()}   보관 {stored}개");
        }

        private void CheckFrenzy()
        {
            int step = _animal.GetAtk() / FrenzyStep;
            if (step == _lastFrenzyStep) return;

            int previous = _lastFrenzyStep;
            _lastFrenzyStep = step;

            if (logFrenzy && step > previous)
                Report("폭주 구간", $"{previous} → {step}  (ATK {_animal.GetAtk()})  페이즈={Phase()}  2페이즈여야 충전됨");
        }

        private void Snapshot()
        {
            if (_memory != null)
            {
                _lastInheritedAtk = _memory.InheritedAtk;
                _lastInheritedHp = _memory.InheritedHp;
                _lastStoredCount = _memory.StoredAbilities.Count;
            }

            _lastAbilityCount = _animal.Abilities?.Count ?? 0;
            _lastFrenzyStep = _animal.GetAtk() / FrenzyStep;
        }

        private void Dump(string title)
        {
            var sb = new StringBuilder();
            sb.Append($"<color=#ffcc00>[까마귀왕] {title}</color>\n");
            sb.Append($"  ATK {_animal.GetAtk()}   HP {Hp()}\n");
            sb.Append($"  페이즈 {Phase()}\n");
            sb.Append($"  사냥감 {(_tracker != null && _tracker.Prey != null ? Name(_tracker.Prey) : "없음")}\n");

            if (_memory != null)
            {
                sb.Append($"  계승 누적 +{_memory.InheritedAtk} ATK / +{_memory.InheritedHp} HP\n");
                sb.Append($"  보관 특성 {_memory.StoredAbilities.Count}개\n");
            }

            sb.Append($"  폭주 구간 {_animal.GetAtk() / FrenzyStep}\n");
            sb.Append($"  특성 {AbilityNames()}");

            Debug.Log(sb.ToString(), this);
        }

        private string AbilityNames()
        {
            if (_animal.Abilities == null || _animal.Abilities.Count == 0) return "없음";

            var sb = new StringBuilder();
            for (int i = 0; i < _animal.Abilities.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(_animal.Abilities[i].GetType().Name);
            }

            return sb.ToString();
        }

        private string Devoured(LDY_Animal prey)
        {
            if (_memory == null || prey.data == null) return "?";

            return _memory.HasDevoured(prey.data) ? "예 (되먹임 대상)" : "아니오";
        }

        private string Hp() =>
            _animal.health != null ? $"{_animal.health.Value}/{_animal.health.MaxValue}" : "?";

        private string Phase() => _phase != null ? _phase.CurrentPhase.ToString() : "?";

        private static string Name(LDY_Animal animal) =>
            animal.data != null && !string.IsNullOrWhiteSpace(animal.data.animalName)
                ? animal.data.animalName
                : animal.name;

        private static string Yes(Object component) => component != null ? "O" : "X";

        private void Report(string tag, string body) =>
            Debug.Log($"<color=#ffcc00>[까마귀왕/{tag}]</color> {body}", this);
    }
}
