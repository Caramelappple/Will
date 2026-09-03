using System;
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
/// 인포창을 여는 쪽에서 구독할 카드/기물 더블클릭 이벤트.
/// 이 클래스는 입력 대상만 전달하며 인포창을 직접 열지 않는다.
/// </summary>
public static class DLJ_InfoPanelEvents
{
    public static event Action<LDY_Animal> PieceDoubleClicked;
    public static event Action<LSO_CardSO> CardDoubleClicked;

    internal static void RaisePieceDoubleClicked(LDY_Animal unit) =>
        PieceDoubleClicked?.Invoke(unit);

    internal static void RaiseCardDoubleClicked(LSO_CardSO card) =>
        CardDoubleClicked?.Invoke(card);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSubscribers()
    {
        PieceDoubleClicked = null;
        CardDoubleClicked = null;
    }
}

/// <summary>
/// 선택한 카드 또는 기물의 SO 데이터를 상세 정보 UI에 표시한다.
/// UI 배치와 애니메이션은 담당하지 않는다.
/// </summary>
public sealed class DLJ_InfoPanel : MonoBehaviour
{
    public static DLJ_InfoPanel Instance { get; private set; }

    public bool IsHidden
    {
        get
        {
            if (panelAnimation != null)
                return panelAnimation.IsHidden;

            GameObject target = content != null ? content : gameObject;
            return !target.activeSelf;
        }
    }

    [Header("정보 출처")]
    [Tooltip("비워두면 같은 씬의 선택 컨트롤러를 자동으로 찾는다.")]
    [SerializeField] private LDY_SelectionController selection;
    [SerializeField] private DLJ_InfoPanelCatalogSO catalog;
    [SerializeField] private DLJ_WillDatabaseSO willDatabase;

    [Header("열기 입력")]
    [Tooltip("같은 기물을 두 번 눌렀다고 인정할 최대 시간 간격.")]
    [SerializeField, Min(0.05f)] private float pieceDoubleClickInterval = 0.35f;

    [Header("표시 루트")]
    [Tooltip("대상이 없을 때 끌 오브젝트. 비워두면 이 컴포넌트의 오브젝트를 사용한다.")]
    [SerializeField] private GameObject content;
    [Tooltip("인포창의 열기/닫기 이동 애니메이션. 비워두면 자식에서도 자동으로 찾는다.")]
    [SerializeField] private DLJ_InfoPanelAnimation panelAnimation;

    [Header("기물 사진")]
    [SerializeField] private SpriteRenderer portraitRenderer;
    [SerializeField] private SpriteRenderer attackPortraitRenderer;
    [SerializeField] private SpriteRenderer healthPortraitRenderer;
    [SerializeField] private SpriteRenderer aRPortraitRenderer;
    [SerializeField] private SpriteRenderer mRPortraitRenderer;
    [SerializeField] private SpriteRenderer willPortraitRenderer;

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
    private LDY_Animal _lastClickedUnit;
    private float _lastPieceClickTime = float.NegativeInfinity;

    private DLJ_InfoPanelPortraits CommonPortraits =>
        new DLJ_InfoPanelPortraits(
            GetSprite(attackPortraitRenderer),
            GetSprite(healthPortraitRenderer),
            GetSprite(aRPortraitRenderer),
            GetSprite(mRPortraitRenderer));

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

        if (panelAnimation == null)
            panelAnimation = GetComponentInChildren<DLJ_InfoPanelAnimation>(true);

        if (selection != null)
            selection.OnAnimalClicked += HandleAnimalClicked;

        SetVisible(false, true);
    }

    private void OnDestroy()
    {
        if (selection != null)
            selection.OnAnimalClicked -= HandleAnimalClicked;

        BindUnit(null);

        if (Instance == this)
            Instance = null;
    }

    public void Show(LSO_CardSO card)
    {
        if (!DLJ_InfoPanelData.TryFromCard(
                card,
                CommonPortraits,
                willDatabase,
                out DLJ_InfoPanelData data))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 표시할 카드 SO가 유효하지 않습니다.", this);
            return;
        }

        BindUnit(null);
        Apply(data);
    }

    public void Show(LSO_AnimalSO animal)
    {
        if (!DLJ_InfoPanelData.TryFromAnimal(
                animal,
                catalog,
                CommonPortraits,
                willDatabase,
                out DLJ_InfoPanelData data))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 표시할 기물 SO가 없습니다.", this);
            return;
        }

        BindUnit(null);
        Apply(data);
    }

    public void Show(LDY_Animal unit)
    {
        if (!DLJ_InfoPanelData.TryFromUnit(
                unit,
                catalog,
                CommonPortraits,
                willDatabase,
                out DLJ_InfoPanelData data))
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
        SetSprite(portraitRenderer, data.portrait);
        SetSprite(attackPortraitRenderer, data.attackPortrait);
        SetSprite(healthPortraitRenderer, data.healthPortrait);
        SetSprite(aRPortraitRenderer, data.aRPortrait);
        SetSprite(mRPortraitRenderer, data.mRPortrait);
        SetSprite(willPortraitRenderer, data.willPortrait);

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

    private void HandleAnimalClicked(LDY_Animal unit)
    {
        if (unit == null)
        {
            ResetPieceClick();
            return;
        }

        float now = Time.unscaledTime;
        bool isDoubleClick =
            _lastClickedUnit == unit &&
            now - _lastPieceClickTime <= pieceDoubleClickInterval;

        if (isDoubleClick)
        {
            ResetPieceClick();
            DLJ_InfoPanelEvents.RaisePieceDoubleClicked(unit);
            return;
        }

        _lastClickedUnit = unit;
        _lastPieceClickTime = now;
    }

    private void ResetPieceClick()
    {
        _lastClickedUnit = null;
        _lastPieceClickTime = float.NegativeInfinity;
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
        if (DLJ_InfoPanelData.TryFromUnit(
                _currentUnit,
                catalog,
                CommonPortraits,
                willDatabase,
                out DLJ_InfoPanelData data))
            Apply(data);
    }

    private void SetVisible(bool visible, bool immediate = false)
    {
        if (panelAnimation != null)
        {
            if (visible)
                panelAnimation.Show();
            else if (immediate)
                panelAnimation.HideImmediate();
            else
                panelAnimation.Hide();

            return;
        }

        GameObject target = content != null ? content : gameObject;
        if (target.activeSelf != visible)
            target.SetActive(visible);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }

    private static Sprite GetSprite(SpriteRenderer target) =>
        target != null ? target.sprite : null;

    private static void SetSprite(SpriteRenderer target, Sprite value)
    {
        if (target != null)
            target.sprite = value;
    }
}
