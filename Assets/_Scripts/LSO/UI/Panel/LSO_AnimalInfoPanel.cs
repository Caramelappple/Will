using System.Collections.Generic;
using System.Text;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Manager;
using TMPro;
using UnityEngine;
using _Scripts.LSO.UI.Text;

namespace _Scripts.LSO.UI.Panel
{
    /// <summary>
    /// 선택한 기물의 상세 정보를 띄우는 창.
    ///
    /// LDY_SelectionController가 알려주는 대상만 따라 그린다.
    /// 어떤 기물을 볼지는 이 창이 정하지 않는다.
    ///
    /// 아군 선택과 적 조회를 한 창이 함께 받는다. 마지막에 알려온 쪽을 그리고,
    /// 한쪽이 비면 다른 쪽으로 넘어간다. 창은 씬에 하나만 두면 된다.
    /// </summary>
    public class LSO_AnimalInfoPanel : MonoBehaviour
    {
        [Header("정보 출처")]
        [SerializeField] private LDY_SelectionController selection;

        [Tooltip("볼 대상이 없을 때 끌 오브젝트. 비우면 자기 자신을 끈다.")]
        [SerializeField] private GameObject content;

        [Header("텍스트")]
        [SerializeField] private TextMeshProUGUI animalName;
        [SerializeField] private TextMeshProUGUI health;
        [SerializeField] private TextMeshProUGUI damage;
        [SerializeField] private TextMeshProUGUI range;
        [SerializeField] private TextMeshProUGUI ability;
        [SerializeField] private TextMeshProUGUI will;
        [SerializeField] private TextMeshProUGUI desc;

        [Tooltip("한 번에 움직일 수 있는 칸 수. 슬롯을 비워두면 표시하지 않는다.")]
        [SerializeField] private TextMeshProUGUI moveRange;

        [Tooltip("소환 코스트. 슬롯을 비워두면 표시하지 않는다.")]
        [SerializeField] private TextMeshProUGUI cost;

        [Header("이름 색")]
        [SerializeField] private Color allyColor = Color.white;
        [SerializeField] private Color enemyColor = Color.red;

        [Header("길이 제한")]
        [Tooltip("설명에 표시할 최대 글자 수. 넘으면 …로 자른다. 0이면 자르지 않는다.\n" +
                 "칸이 정해진 창에 긴 설명이 들어오면 아래 요소를 밀어내거나 겹친다.")]
        [SerializeField, Min(0)] private int maxDescriptionLength = 60;

        [Tooltip("나열할 최대 특성 수. 넘으면 '외 N개'로 접는다. 0이면 접지 않는다.\n" +
                 "되먹임으로 특성이 늘어난 보스는 이름만 나열해도 한 줄을 훌쩍 넘는다.")]
        [SerializeField, Min(0)] private int maxAbilityCount = 3;

        [Tooltip("글자가 칸을 넘으면 자동으로 줄인다. 프리팹 레이아웃이 정리되면 꺼도 된다.")]
        [SerializeField] private bool autoFitText = true;

        [Tooltip("자동 축소로 줄어들 수 있는 최소 글자 크기.")]
        [SerializeField, Min(1f)] private float minFontSize = 12f;

        private LDY_Animal _target;
        private LDY_BoardManager _board;

        // Refresh는 피해를 받을 때마다 불린다. 매번 문자열을 만들면 할당이 쌓이므로
        // 대상이 바뀔 때와 LDY_Animal.AbilitiesChanged가 올 때만 다시 만든다.
        // 되먹임처럼 전투 도중 특성이 늘어나는 경우가 있어 대상 교체만으로는 부족하다.
        private string _abilityText = string.Empty;

        private string _detailText = string.Empty;

        // 구독을 Awake/OnDestroy에 두는 이유:
        // 이 창은 볼 대상이 없을 때 자기 자신을 끈다. 그런데 꺼진 오브젝트는 OnEnable이 다시 돌지 않아서,
        // OnEnable에서 구독했다면 한 번 닫힌 뒤로는 선택 신호를 영영 못 받는다.
        private void Awake()
        {
            AutoFitTexts();
            SubscribeManager();

            if (selection == null)
            {
                Debug.LogError($"{name}: LDY_SelectionController를 연결해야 정보창이 동작합니다.", this);
                return;
            }

            selection.OnSelectionChanged += HandleSelectionChanged;
            selection.OnEnemyInspectedChanged += HandleEnemyInspected;

            // 씬에 저장된 상태와 무관하게 지금 보고 있는 것에서 시작한다.
            Bind(selection.Selected != null ? selection.Selected : selection.InspectedEnemy);
        }

        private void OnDestroy()
        {
            if (selection != null)
            {
                selection.OnSelectionChanged -= HandleSelectionChanged;
                selection.OnEnemyInspectedChanged -= HandleEnemyInspected;
            }

            Unsubscribe(_target);

            if (GameManager.HasInstance)
                GameManager.Instance.BoardChanged -= BindBoard;

            BindBoard(null);
        }

        // 한쪽이 비었다고 바로 닫지 않고 다른 쪽으로 넘어간다.
        // 아군을 고른 채 적을 들여다보다가 아군 선택이 풀리면, 보던 적이 그대로 남아야 자연스럽다.
        private void HandleSelectionChanged(LDY_Animal animal)
        {
            Bind(animal != null ? animal : selection.InspectedEnemy);
        }

        private void HandleEnemyInspected(LDY_Animal animal)
        {
            Bind(animal != null ? animal : selection.Selected);
        }

        /// <summary>볼 대상을 바꾼다. null이면 창을 닫는다.</summary>
        public void Bind(LDY_Animal animal)
        {
            if (_target == animal)
            {
                Refresh();
                return;
            }

            Unsubscribe(_target);
            _target = animal;
            _abilityText = DescribeAbilities(_target);
            _detailText = DescribeDetail(_target);
            Subscribe(_target);

            SetVisible(_target != null);
            Refresh();
        }

        // 창이 떠 있는 동안 값이 변할 수 있다. 맞아서 체력이 줄거나, 특성 때문에 공격력이 달라지거나.
        //
        // 체력은 Health가 바뀌는 순간을 정확히 알려준다.
        // 공격력은 GetAtk()가 매번 계산하는 값이라 바뀌는 순간이 없어서, 그 계산이 의존하는
        // 보드 상태가 바뀔 때(LDY_BoardManager.OnBoardChanged) 다시 그린다.
        private void Subscribe(LDY_Animal animal)
        {
            if (animal == null) return;

            if (animal.health != null)
            {
                animal.health.OnDamage += HandleDamaged;
                animal.health.OnRecover += HandleRecovered;
            }

            animal.AbilitiesChanged += HandleAbilitiesChanged;
        }

        private void Unsubscribe(LDY_Animal animal)
        {
            if (animal == null) return;

            if (animal.health != null)
            {
                animal.health.OnDamage -= HandleDamaged;
                animal.health.OnRecover -= HandleRecovered;
            }

            animal.AbilitiesChanged -= HandleAbilitiesChanged;
        }

        // 보드는 씬마다 새로 생기고, 이 창이 켜지는 시점에 아직 없을 수도 있다.
        // 그래서 "지금 있는 보드"와 "앞으로 바뀔 보드"를 한 경로로 받는다.
        private void SubscribeManager()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.BoardChanged += BindBoard;
            BindBoard(manager.Board);
        }

        private void BindBoard(LDY_BoardManager board)
        {
            if (_board == board) return;

            if (_board != null)
                _board.OnBoardChanged -= HandleBoardChanged;

            _board = board;

            if (_board != null)
                _board.OnBoardChanged += HandleBoardChanged;
        }

        private void HandleDamaged(DamageResultData data) => Refresh();

        private void HandleRecovered(RecoverResultData data) => Refresh();

        private void HandleAbilitiesChanged()
        {
            _abilityText = DescribeAbilities(_target);
            Refresh();
        }

        // 보고 있던 기물이 죽으면 파괴된 오브젝트를 그리게 되므로 창을 닫는다.
        // 선택 자체는 LDY_SelectionController가 관리하므로 여기서는 표시만 정리한다.
        private void HandleBoardChanged()
        {
            if (_target == null && !ReferenceEquals(_target, null))
            {
                Bind(null);
                return;
            }

            Refresh();
        }

        /// <summary>지금 대상의 값을 다시 그린다.</summary>
        public void Refresh()
        {
            if (_target == null) return;

            SetText(animalName, ResolveName());
            SetText(health, $"Hp: {HealthValue}/{MaxHealthValue}");
            SetText(damage, $"Atk: {_target.GetAtk()}");

            SetText(range, LSO_DisplayNames.Of(_target.RangeType));
            SetText(ability, _abilityText);
            SetText(will, LSO_DisplayNames.Of(_target.WillType));

            SetText(moveRange, $"이동: {MoveRangeValue}");
            SetText(cost, $"코스트: {CostValue}");

            SetText(desc, _detailText);

            if (animalName != null)
                animalName.color = _target.team == LDY_Team.Enemy ? enemyColor : allyColor;
        }

        private string DescribeAbilities(LDY_Animal animal)
        {
            if (animal == null) return string.Empty;

            IReadOnlyList<LSO_AbilityType> types = animal.AbilityTypes;
            if (types == null || types.Count == 0) return "없음";

            int shown = maxAbilityCount > 0
                ? Mathf.Min(maxAbilityCount, types.Count)
                : types.Count;

            var sb = new StringBuilder();

            for (int i = 0; i < shown; i++)
            {
                if (i > 0) sb.Append(", ");

                sb.Append(LSO_DisplayNames.Of(types[i]));
            }

            int hidden = types.Count - shown;
            if (hidden > 0)
                sb.Append($" 외 {hidden}개");

            return sb.ToString();
        }

        private string DescribeDetail(LDY_Animal animal)
        {
            if (animal == null || animal.data == null) return string.Empty;

            return Shorten(animal.data.description, maxDescriptionLength);
        }

        /// <summary>
        /// 글자 수로 자른다. 줄 수가 아니라 글자 수인 것은 폰트와 창 폭에 따라
        /// 줄 수가 달라져서 코드 쪽에서는 알 수 없기 때문이다.
        /// </summary>
        private static string Shorten(string text, int limit)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (limit <= 0 || text.Length <= limit) return text;

            return text.Substring(0, limit).TrimEnd() + "…";
        }

        private int HealthValue => _target.health != null ? _target.health.Value : 0;

        private int MaxHealthValue => _target.health != null ? _target.health.MaxValue : 0;

        private int MoveRangeValue => _target.data != null ? _target.data.MoveRange : 1;

        private int CostValue => _target.data != null ? _target.data.cost : 0;

        /// <summary>
        /// 글자가 칸을 넘지 않게 자동 축소를 켠다.
        ///
        /// 프리팹의 칸은 "근거리" 같은 짧은 한글 더미로 폭이 잡혀 있는데,
        /// 실제로는 그보다 긴 값이 들어와 두 번째 줄이 생기고 아래 요소 위로 흘러내린다.
        /// ContentSizeFitter와 LayoutGroup을 제대로 넣는 것이 정석이지만,
        /// 그 전까지 글자가 칸 밖으로 나가는 것만이라도 여기서 막는다.
        /// </summary>
        private void AutoFitTexts()
        {
            if (!autoFitText) return;

            AutoFit(animalName);
            AutoFit(health);
            AutoFit(damage);
            AutoFit(range);
            AutoFit(ability);
            AutoFit(will);
            AutoFit(moveRange);
            AutoFit(cost);
            AutoFit(desc);
        }

        private void AutoFit(TMP_Text target)
        {
            if (target == null) return;

            target.enableAutoSizing = true;
            target.fontSizeMin = Mathf.Max(1f, minFontSize);
            target.fontSizeMax = target.fontSize;
            target.overflowMode = TextOverflowModes.Ellipsis;
        }

        // 오브젝트 이름은 소환하면 "(Clone)"이 붙는다. 동물 데이터의 이름이 있으면 그쪽이 정확하다.
        private string ResolveName()
        {
            if (_target.data != null && !string.IsNullOrWhiteSpace(_target.data.animalName))
                return _target.data.animalName;

            return _target.name;
        }

        private void SetVisible(bool visible)
        {
            GameObject go = content != null ? content : gameObject;
            if (go.activeSelf == visible) return;

            go.SetActive(visible);
        }

        // 슬롯을 비워둬도 나머지가 정상 동작하게 한다. 창마다 보여줄 항목이 다를 수 있다.
        private static void SetText(TMP_Text target, string value)
        {
            if (target == null) return;
            if (target.text == value) return;

            target.text = value;
        }
    }
}
