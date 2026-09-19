using System;
using System.Collections;
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
/// 정보가 바뀌면 기존 내용을 지운 뒤 새 내용을 잉크처럼 드러낸다.
/// </summary>
public sealed class DLJ_InfoPanel : MonoBehaviour
{
    public static DLJ_InfoPanel Instance { get; private set; }

    /// <summary>
    /// 열려 있던 인포창에 닫기 요청이 들어왔을 때 한 번 발생한다.
    /// 이미 닫힌 상태에서 Hide를 다시 호출해도 발생하지 않는다.
    /// </summary>
    public event Action Closed;

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
    [Tooltip("같은 기물을 두 번 눌렀다고 인정할 최대 시간 간격.\n" +
             "\n" +
             "여는 데만 쓴다. 닫는 것은 한 번 클릭이라 간격을 재지 않는다.")]
    [SerializeField, Min(0.05f)] private float pieceDoubleClickInterval = 0.35f;

    [Header("표시 루트")]
    [Tooltip("대상이 없을 때 끌 오브젝트. 비워두면 이 컴포넌트의 오브젝트를 사용한다.")]
    [SerializeField] private GameObject content;
    [Tooltip("인포창의 열기/닫기 이동 애니메이션. 비워두면 자식에서도 자동으로 찾는다.")]
    [SerializeField] private DLJ_InfoPanelAnimation panelAnimation;

    [Header("정보 교체 연출")]
    [Tooltip("글자에 적용할 DLJ/Ledger Ink 머티리얼. 각 글자용 복사본을 만들어 사용한다.")]
    [SerializeField] private Material inkMaterial;
    [SerializeField, Min(0f)] private float inkDisappearDuration = 0.18f;
    [SerializeField, Min(0f)] private float inkAppearDuration = 0.35f;

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
    private FontStyles _costFontStyle;
    private FontStyles _playerHealthPointsFontStyle;
    private TMP_Text[] _inkTexts;
    private Material[] _originalTextMaterials;
    private Material[] _inkTextMaterials;
    private Color[] _originalTextColors;
    private SpriteRenderer[] _fadeSprites;
    private Color[] _originalSpriteColors;
    private Coroutine _inkRoutine;
    private float _inkProgress = 1f;
    private bool _wantsVisible;
    private bool _hasDisplayedData;
    private bool _hasPendingData;
    private DLJ_InfoPanelData _displayedData;
    private DLJ_InfoPanelData _pendingData;
    private static readonly int InkProgressId = Shader.PropertyToID("_InkProgress");

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

        if (cost != null)
            _costFontStyle = cost.fontStyle;
        if (playerHealthPoints != null)
            _playerHealthPointsFontStyle = playerHealthPoints.fontStyle;

        InitializeInk();

        if (selection != null)
            selection.OnAnimalClicked += HandleAnimalClicked;

        KTH_HandCard.OnCardDoubleClicked += HandleHandCardDoubleClicked;

        SetVisible(false, true);
    }

    private void OnDestroy()
    {
        if (selection != null)
            selection.OnAnimalClicked -= HandleAnimalClicked;

        KTH_HandCard.OnCardDoubleClicked -= HandleHandCardDoubleClicked;

        BindUnit(null);

        StopInkRoutine();
        if (_inkTextMaterials != null)
        {
            for (int i = 0; i < _inkTextMaterials.Length; i++)
            {
                if (_inkTextMaterials[i] == null) continue;
                if (_inkTexts[i] != null)
                    _inkTexts[i].fontSharedMaterial = _originalTextMaterials[i];
                Destroy(_inkTextMaterials[i]);
            }
        }

        if (Instance == this)
            Instance = null;
    }

    public void Show(LSO_CardSO card)
    {
        Show(card, null);
    }

    /// <param name="willOverride">
    /// 손패에서 이 카드에 붙여둔 유언. 넘기면 기본값 대신 그것을 보여준다.
    /// </param>
    public void Show(LSO_CardSO card, LSO_WillType? willOverride)
    {
        if (!DLJ_InfoPanelData.TryFromCard(
                card,
                CommonPortraits,
                willDatabase,
                out DLJ_InfoPanelData data,
                willOverride))
        {
            Debug.LogWarning("[DLJ_InfoPanel] 표시할 카드 SO가 유효하지 않습니다.", this);
            return;
        }

        BindUnit(null);
        ShowData(data);
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
        ShowData(data);
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

        // 보던 기물과 다른 기물이면 창을 한 번 내렸다 올린다.
        // BindUnit 이 _currentUnit 을 덮기 전에 봐야 한다.
        bool changedUnit = _currentUnit != unit;

        BindUnit(unit);
        ShowData(data, replay: changedUnit);
    }

    public void Hide()
    {
        bool wasVisible = _wantsVisible;

        BindUnit(null);
        StopInkRoutine();
        _hasPendingData = false;
        _wantsVisible = false;
        SetVisible(false);

        if (wasVisible)
            Closed?.Invoke();
    }

    /// <param name="replay">
    /// 이미 떠 있을 때 창을 내렸다 올릴지.
    ///
    /// 보던 대상이 바뀌었을 때 켠다. 글자만 갈리면 창이 제자리에 있어서
    /// 지금 누구를 보고 있는지, 클릭이 먹기는 했는지 알기 어렵다.
    ///
    /// **내용이 같아도 내렸다 올린다.** 같은 종류의 기물을 번갈아 볼 때가
    /// 그런 경우인데, 그때야말로 바뀐 것을 알려줄 다른 단서가 없다.
    /// </param>
    private void ShowData(DLJ_InfoPanelData data, bool replay = false)
    {
        if (!_wantsVisible || !_hasDisplayedData)
        {
            StopInkRoutine();
            _hasPendingData = false;
            ApplyData(data);
            SetInkProgress(0f);
            _wantsVisible = true;
            SetVisible(true);
            _inkRoutine = StartCoroutine(RevealInk());
            return;
        }

        bool sameData = _hasPendingData
            ? SameVisualData(_pendingData, data)
            : SameVisualData(_displayedData, data);

        if (sameData && !replay)
            return;

        _pendingData = data;
        _hasPendingData = true;

        if (replay && panelAnimation != null)
        {
            // 글자 갈아 끼우기는 멈춘다. 갈아 끼우는 자리가 바닥으로 옮겨간다.
            StopInkRoutine();

            panelAnimation.Replay(SwapAtBottom);
            return;
        }

        if (_inkRoutine == null)
            _inkRoutine = StartCoroutine(ReplaceInk());
    }

    /// <summary>
    /// 창이 바닥까지 내려간 순간 내용을 갈아 끼운다.
    ///
    /// 올라오는 도중에 바꾸면 바뀌는 장면이 그대로 보인다.
    /// 글자는 0부터 다시 번지게 해서 처음 띄울 때와 같은 모양으로 만든다.
    /// </summary>
    private void SwapAtBottom()
    {
        if (!_hasPendingData) return;

        ApplyData(_pendingData);
        _hasPendingData = false;

        SetInkProgress(0f);

        _inkRoutine = StartCoroutine(RevealInk());
    }

    private IEnumerator RevealInk()
    {
        yield return AnimateInk(1f, inkAppearDuration);
        _inkRoutine = null;
        if (_hasPendingData)
            _inkRoutine = StartCoroutine(ReplaceInk());
    }

    private IEnumerator ReplaceInk()
    {
        do
        {
            yield return AnimateInk(0f, inkDisappearDuration);
            ApplyData(_pendingData);
            _hasPendingData = false;
            yield return AnimateInk(1f, inkAppearDuration);
        } while (_hasPendingData);

        _inkRoutine = null;
    }

    private IEnumerator AnimateInk(float target, float duration)
    {
        float start = _inkProgress;
        if (duration <= 0f)
        {
            SetInkProgress(target);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetInkProgress(Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetInkProgress(target);
    }

    private void StopInkRoutine()
    {
        if (_inkRoutine == null) return;
        StopCoroutine(_inkRoutine);
        _inkRoutine = null;
    }

    private void InitializeInk()
    {
        _inkTexts = new[]
        {
            pieceName, attack, health, traitName, traitDescription, willName,
            willDescription, attackRange, moveRange, cost, playerHealthPoints
        };
        _originalTextMaterials = new Material[_inkTexts.Length];
        _inkTextMaterials = new Material[_inkTexts.Length];
        _originalTextColors = new Color[_inkTexts.Length];

        for (int i = 0; i < _inkTexts.Length; i++)
        {
            TMP_Text label = _inkTexts[i];
            if (label == null) continue;
            _originalTextColors[i] = label.color;
            if (inkMaterial == null || label.font == null) continue;

            Material original = label.fontSharedMaterial;
            _originalTextMaterials[i] = original;
            Material material = new Material(inkMaterial) { name = $"{label.name}_InfoInk" };
            material.mainTexture = label.font.atlasTexture;
            Material fontMaterial = original != null ? original : label.font.material;
            if (fontMaterial != null && fontMaterial.HasProperty("_GradientScale"))
                material.SetFloat("_GradientScale", fontMaterial.GetFloat("_GradientScale"));
            material.SetFloat("_TextureWidth", label.font.atlasWidth);
            material.SetFloat("_TextureHeight", label.font.atlasHeight);
            material.SetFloat(InkProgressId, 1f);
            label.fontSharedMaterial = material;
            _inkTextMaterials[i] = material;
        }

        _fadeSprites = new[]
        {
            portraitRenderer, attackPortraitRenderer, healthPortraitRenderer,
            aRPortraitRenderer, mRPortraitRenderer, willPortraitRenderer
        };
        _originalSpriteColors = new Color[_fadeSprites.Length];
        for (int i = 0; i < _fadeSprites.Length; i++)
            if (_fadeSprites[i] != null)
                _originalSpriteColors[i] = _fadeSprites[i].color;
    }

    private void SetInkProgress(float progress)
    {
        _inkProgress = progress;
        for (int i = 0; i < _inkTexts.Length; i++)
        {
            if (_inkTextMaterials[i] != null)
                _inkTextMaterials[i].SetFloat(InkProgressId, progress);
            else if (_inkTexts[i] != null)
            {
                Color color = _originalTextColors[i];
                color.a *= progress;
                _inkTexts[i].color = color;
            }
        }

        for (int i = 0; i < _fadeSprites.Length; i++)
        {
            if (_fadeSprites[i] == null) continue;
            Color color = _originalSpriteColors[i];
            color.a *= progress;
            _fadeSprites[i].color = color;
        }
    }

    private static bool SameVisualData(DLJ_InfoPanelData a, DLJ_InfoPanelData b) =>
        a.portrait == b.portrait && a.attackPortrait == b.attackPortrait &&
        a.healthPortrait == b.healthPortrait && a.aRPortrait == b.aRPortrait &&
        a.mRPortrait == b.mRPortrait && a.willPortrait == b.willPortrait &&
        a.Name == b.Name && a.Attack == b.Attack && a.Health == b.Health &&
        a.TraitName == b.TraitName && a.TraitDescription == b.TraitDescription &&
        a.WillName == b.WillName && a.WillDescription == b.WillDescription &&
        a.AttackRange == b.AttackRange && a.MoveRange == b.MoveRange &&
        a.Cost == b.Cost && a.PlayerHealthPoints == b.PlayerHealthPoints &&
        a.HasCost == b.HasCost && a.HasPlayerHealthPoints == b.HasPlayerHealthPoints;

    private void ApplyData(DLJ_InfoPanelData data)
    {
        _displayedData = data;
        _hasDisplayedData = true;
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
        SetFontStyle(cost, data.HasCost, _costFontStyle);
        SetFontStyle(playerHealthPoints, data.HasPlayerHealthPoints, _playerHealthPointsFontStyle);
    }

    /// <summary>
    /// 기물을 눌렀다.
    ///
    ///     띄우는 중인 기물을 누름  → 한 번으로 닫는다
    ///     그 밖                    → 두 번 눌러야 뜬다
    ///
    /// ── 여는 것과 닫는 것이 왜 다른가 ─────────────────────────
    /// 기물 클릭은 원래 "고른다"는 뜻이다(LDY_SelectionController). 한 번 누를
    /// 때마다 창이 뜨면 기물을 고를 때마다 화면이 가려진다. 그래서 여는 데는
    /// 두 번을 요구한다.
    ///
    /// 닫는 쪽은 반대다. 이미 창이 떠서 화면을 가리고 있으므로, 치우는 데 두 번을
    /// 요구할 이유가 없다. 한 번에 치운다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 카드로 띄운 경우에는 BindUnit(null) 이라 _currentUnit 이 비어 있어,
    /// 기물을 누르면 닫히지 않고 그 기물로 갈아탄다.
    /// </summary>
    private void HandleAnimalClicked(LDY_Animal unit)
    {
        if (unit == null)
        {
            ResetPieceClick();
            return;
        }

        // 지금 이 기물을 띄우고 있으면 한 번으로 닫는다. 간격을 재기 전에 본다 —
        // 여기까지 오면 더블클릭 여부는 답에 아무 영향이 없다.
        if (_wantsVisible && _currentUnit == unit)
        {
            ResetPieceClick();
            Hide();
            return;
        }

        float now = Time.unscaledTime;
        bool isDoubleClick =
            _lastClickedUnit == unit &&
            now - _lastPieceClickTime <= pieceDoubleClickInterval;

        if (isDoubleClick)
        {
            ResetPieceClick();
            Show(unit);
            DLJ_InfoPanelEvents.RaisePieceDoubleClicked(unit);
            return;
        }

        _lastClickedUnit = unit;
        _lastPieceClickTime = now;
    }

    private void HandleHandCardDoubleClicked(KTH_HandCard handCard)
    {
        if (handCard == null || handCard.CardData == null)
        {
            Debug.LogWarning("[DLJ_InfoPanel] 더블클릭한 카드의 CardSO를 가져올 수 없습니다.", this);
            return;
        }

        DLJ_InfoPanelEvents.RaiseCardDoubleClicked(handCard.CardData);
    }

    /// <summary>
    /// 재던 간격을 버린다. 창이 뜨거나 닫힌 뒤에 부른다.
    ///
    /// 안 버리면 방금 연 기물을 한 번만 더 눌러도 그것이 "두 번째 클릭"으로 읽힌다.
    /// </summary>
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
            ShowData(data);
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

    private static void SetFontStyle(TMP_Text target, bool hasValue, FontStyles originalStyle)
    {
        if (target != null)
            target.fontStyle = hasValue ? originalStyle : originalStyle & ~FontStyles.Bold;
    }

    private static Sprite GetSprite(SpriteRenderer target) =>
        target != null ? target.sprite : null;

    private static void SetSprite(SpriteRenderer target, Sprite value)
    {
        if (target != null)
            target.sprite = value;
    }
}
