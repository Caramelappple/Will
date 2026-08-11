using System.Collections.Generic;
using _Scripts.HealthSystem;
using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using TMPro;
using UnityEngine;

/// <summary>
/// 선택한 기물의 상세 정보를 띄우는 창.
///
/// LDY_SelectionController가 알려주는 대상만 따라 그린다.
/// 어떤 기물을 볼지는 이 창이 정하지 않는다.
///
/// 아군(OnSelectionChanged)과 적(OnEnemyInspectedChanged) 중 무엇을 따라갈지는 인스펙터에서 고른다.
/// 창을 두 개 두고 각각 다른 쪽을 구독하면 아군용/적용 창이 따로 뜬다.
/// </summary>
public class LSO_AnimalInfoPanel : MonoBehaviour
{
    [Header("정보 출처")]
    [SerializeField] private LDY_SelectionController selection;

    [Tooltip("켜면 적 기물 조회를, 끄면 내 기물 선택을 따라간다.")]
    [SerializeField] private bool followEnemy;

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

    [Header("이름 색")]
    [SerializeField] private Color allyColor = Color.white;
    [SerializeField] private Color enemyColor = Color.red;

    private LDY_Animal _target;
    private LDY_BoardManager _board;

    // 특성 목록은 전투 중에 변하지 않는데 Refresh는 피해를 받을 때마다 불린다.
    // 매번 string.Join을 돌리면 쓸데없는 할당이 쌓이므로 대상이 바뀔 때만 만든다.
    private string _abilityText = string.Empty;

    // 구독을 Awake/OnDestroy에 두는 이유:
    // 이 창은 볼 대상이 없을 때 자기 자신을 끈다. 그런데 꺼진 오브젝트는 OnEnable이 다시 돌지 않아서,
    // OnEnable에서 구독했다면 한 번 닫힌 뒤로는 선택 신호를 영영 못 받는다.
    private void Awake()
    {
        if (selection == null)
        {
            Debug.LogError($"{name}: LDY_SelectionController를 연결해야 정보창이 동작합니다.", this);
            return;
        }

        if (followEnemy)
            selection.OnEnemyInspectedChanged += Bind;
        else
            selection.OnSelectionChanged += Bind;

        // 씬에 저장된 상태와 무관하게 "볼 대상 없음"에서 시작한다.
        Bind(followEnemy ? selection.InspectedEnemy : selection.Selected);
    }

    private void OnDestroy()
    {
        if (selection != null)
        {
            selection.OnEnemyInspectedChanged -= Bind;
            selection.OnSelectionChanged -= Bind;
        }

        Unsubscribe(_target);
        UnsubscribeBoard();
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

        SubscribeBoard();
    }

    private void Unsubscribe(LDY_Animal animal)
    {
        if (animal == null || animal.health == null) return;

        animal.health.OnDamage -= HandleDamaged;
        animal.health.OnRecover -= HandleRecovered;
    }

    private void SubscribeBoard()
    {
        if (_board != null) return;
        if (!GameManager.HasInstance) return;

        LDY_BoardManager board = GameManager.Instance.Board;
        if (board == null) return;

        _board = board;
        _board.OnBoardChanged += HandleBoardChanged;
    }

    private void UnsubscribeBoard()
    {
        if (_board == null) return;

        _board.OnBoardChanged -= HandleBoardChanged;
        _board = null;
    }

    private void HandleDamaged(DamageResultData data) => Refresh();

    private void HandleRecovered(RecoverResultData data) => Refresh();

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

        SetText(range, _target.RangeType.ToString());
        SetText(ability, _abilityText);
        SetText(will, _target.WillType.ToString());

        SetText(desc, _target.data != null ? _target.data.description : string.Empty);

        if (animalName != null)
            animalName.color = _target.team == LDY_Team.Enemy ? enemyColor : allyColor;
    }

    private static string DescribeAbilities(LDY_Animal animal)
    {
        if (animal == null) return string.Empty;

        IReadOnlyList<LSO_AbilityType> types = animal.AbilityTypes;
        if (types == null || types.Count == 0) return "없음";

        return string.Join(", ", types);
    }

    private int HealthValue => _target.health != null ? _target.health.Value : 0;

    private int MaxHealthValue => _target.health != null ? _target.health.MaxValue : 0;

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
