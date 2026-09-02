using _Scripts.LDY;
using _Scripts.LSO.Animal.Data;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Will;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 선택한 카드 또는 기물의 SO 데이터를 상세 정보 UI에 표시한다.
/// UI 배치와 애니메이션은 담당하지 않는다.
/// </summary>
public sealed class DLJ_InfoPanel : MonoBehaviour
{
    public static DLJ_InfoPanel Instance { get; private set; }

    [Header("정보 출처")]
    [Tooltip("비워두면 같은 씬의 선택 컨트롤러를 자동으로 찾는다.")]
    [SerializeField] private LDY_SelectionController selection;
    [SerializeField] private DLJ_InfoPanelCatalogSO catalog;
    [SerializeField] private DLJ_WillDatabaseSO willDatabase;

    [Header("표시 루트")]
    [Tooltip("대상이 없을 때 끌 오브젝트. 비워두면 이 컴포넌트의 오브젝트를 사용한다.")]
    [SerializeField] private GameObject content;

    [Header("기물 사진")]
    [SerializeField] private SpriteRenderer portraitRenderer;

    [Header("텍스트")]
    [SerializeField] private TMP_Text pieceName;
    [SerializeField] private TMP_Text attack;
    [SerializeField] private TMP_Text health;
    [SerializeField] private TMP_Text traitName;
    [SerializeField] private TMP_Text traitDescription;
    [SerializeField] private TMP_Text willName;
    [SerializeField] private TMP_Text willDescription;
    [SerializeField] private TMP_Text attackRange;
    [SerializeField] private TMP_Text moveRange;
    [SerializeField] private TMP_Text cost;
    [Tooltip("기물 점수 영역. AnimalSO.playerHealthPoints를 표시한다.")]
    [FormerlySerializedAs("pieceScore")]
    [SerializeField] private TMP_Text playerHealthPoints;

    private LDY_Animal _currentUnit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[DLJ_InfoPanel] 씬에 인포창이 둘 이상 있습니다.", this);
            return;
        }

        Instance = this;

        if (willDatabase == null)
            willDatabase = Resources.Load<DLJ_WillDatabaseSO>("DLJ/DLJ_WillDatabase");

        if (selection == null)
            selection = FindFirstObjectByType<LDY_SelectionController>();

        if (selection != null)
        {
            selection.OnSelectionChanged += HandleSelectionChanged;
            selection.OnEnemyInspectedChanged += HandleEnemyInspected;
        }

        LDY_Animal initial = selection != null
            ? selection.Selected != null ? selection.Selected : selection.InspectedEnemy
            : null;

        if (initial != null)
            Show(initial);
        else
            SetVisible(false);
    }

    private void OnDestroy()
    {
        if (selection != null)
        {
            selection.OnSelectionChanged -= HandleSelectionChanged;
            selection.OnEnemyInspectedChanged -= HandleEnemyInspected;
        }

        BindUnit(null);

        if (Instance == this)
            Instance = null;
    }

    public void Show(LSO_CardSO card)
    {
        if (!DLJ_InfoPanelData.TryFromCard(card, willDatabase, out DLJ_InfoPanelData data))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 표시할 카드 SO가 유효하지 않습니다.", this);
            return;
        }

        BindUnit(null);
        Apply(data);
    }

    public void Show(LSO_AnimalSO animal)
    {
        if (!DLJ_InfoPanelData.TryFromAnimal(animal, catalog, willDatabase, out DLJ_InfoPanelData data))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 표시할 기물 SO가 없습니다.", this);
            return;
        }

        BindUnit(null);
        Apply(data);
    }

    public void Show(LDY_Animal unit)
    {
        if (!DLJ_InfoPanelData.TryFromUnit(unit, catalog, willDatabase, out DLJ_InfoPanelData data))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 선택한 기물에 AnimalSO가 없습니다.", unit);
            return;
        }

        BindUnit(unit);
        Apply(data);
    }

    public void Hide()
    {
        BindUnit(null);
        SetVisible(false);
    }

    private void Apply(DLJ_InfoPanelData data)
    {
        if (portraitRenderer != null) portraitRenderer.sprite = data.Portrait;

        SetText(pieceName, data.Name);
        SetText(attack, data.Attack);
        SetText(health, data.Health);
        SetText(traitName, data.TraitName);
        SetText(traitDescription, data.TraitDescription);
        SetText(willName, data.WillName);
        SetText(willDescription, data.WillDescription);
        SetText(attackRange, data.AttackRange);
        SetText(moveRange, data.MoveRange);
        SetText(cost, data.Cost);
        SetText(playerHealthPoints, data.PlayerHealthPoints);

        SetVisible(true);
    }

    private void HandleSelectionChanged(LDY_Animal unit)
    {
        LDY_Animal target = unit != null ? unit : selection.InspectedEnemy;
        HandleUnitTargetChanged(target);
    }

    private void HandleEnemyInspected(LDY_Animal unit)
    {
        LDY_Animal target = unit != null ? unit : selection.Selected;
        HandleUnitTargetChanged(target);
    }

    private void HandleUnitTargetChanged(LDY_Animal unit)
    {
        if (unit != null)
        {
            Show(unit);
            return;
        }

        if (_currentUnit != null)
            Hide();
    }

    private void BindUnit(LDY_Animal unit)
    {
        if (_currentUnit == unit) return;

        UnsubscribeUnit(_currentUnit);
        _currentUnit = unit;

        if (_currentUnit == null) return;

        if (_currentUnit.health != null)
        {
            _currentUnit.health.OnDamage += HandleUnitDamaged;
            _currentUnit.health.OnRecover += HandleUnitRecovered;
        }

        _currentUnit.AbilitiesChanged += RefreshCurrentUnit;
    }

    private void UnsubscribeUnit(LDY_Animal unit)
    {
        if (unit == null) return;

        if (unit.health != null)
        {
            unit.health.OnDamage -= HandleUnitDamaged;
            unit.health.OnRecover -= HandleUnitRecovered;
        }

        unit.AbilitiesChanged -= RefreshCurrentUnit;
    }

    private void HandleUnitDamaged(DamageResultData _) => RefreshCurrentUnit();

    private void HandleUnitRecovered(RecoverResultData _) => RefreshCurrentUnit();

    private void RefreshCurrentUnit()
    {
        if (DLJ_InfoPanelData.TryFromUnit(_currentUnit, catalog, willDatabase, out DLJ_InfoPanelData data))
            Apply(data);
    }

    private void SetVisible(bool visible)
    {
        GameObject target = content != null ? content : gameObject;
        if (target.activeSelf != visible)
            target.SetActive(visible);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }
}
