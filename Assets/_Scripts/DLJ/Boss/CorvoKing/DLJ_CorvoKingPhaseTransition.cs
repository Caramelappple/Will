using _Scripts.LSO.Boss;
using UnityEngine;
using UnityEngine.UI;

/// <summary>2페이즈 진입 시 깃털 뒤에 접힌 날개를 준비하고, 화면이 걷히면 위로 펼침.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LSO_BossPhase))]
public sealed class DLJ_CorvoKingPhaseTransition : MonoBehaviour
{
    [Tooltip("비워두면 비활성 자식까지 포함해 Wing Child Name으로 검색")]
    [SerializeField] private GameObject wings;
    [SerializeField] private string wingChildName = "Will_CrowKingWing";
    [Header("Phase Camera Zoom")]
    [SerializeField] private bool cameraZoomEnabled = true;
    [Tooltip("기본 렌즈 대비 배율. 0.88이면 살짝 줌인, 1이면 변화 없음")]
    [SerializeField, Range(0.5f, 1f)] private float cameraZoomRatio = 0.88f;
    [SerializeField, Min(0.01f)] private float cameraZoomInDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float cameraZoomOutDuration = 0.65f;
    [Tooltip("날개 펼침과 주변 깃털이 끝난 뒤 줌아웃까지 추가 대기 시간(초)")]
    [SerializeField, Min(0f)] private float cameraZoomReturnDelay = 0.15f;
    [SerializeField] private AnimationCurve cameraZoomInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve cameraZoomOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Wing Unfold")]
    [Tooltip("깃털이 걷히기 시작한 뒤 날개를 펼치는 시간(초)")]
    [SerializeField, Min(0.1f)] private float wingUnfoldDuration = 1.2f;
    [Tooltip("처음 아래로 접어둘 각도. 0이면 회전 없이 아래에서 위로만 이동")]
    [SerializeField, Range(0f, 85f)] private float wingFoldAngle = 65f;
    [Tooltip("접힌 날개를 추가로 내리는 거리. 날개 반폭에 대한 비율")]
    [SerializeField, Range(0f, 0.4f)] private float wingLowerDistance = 0.08f;
    [Tooltip("X: 펼침 시간 비율(0~1), Y: 각도 펼침 정도(0=접힘, 1=펼침). 끝점은 (1,1) 권장")]
    [SerializeField] private AnimationCurve wingUnfoldCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("X: 펼침 시간 비율(0~1), Y: 상승 정도(0=아래, 1=원래 높이). 끝점은 (1,1) 권장")]
    [SerializeField] private AnimationCurve wingRiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Feather Transition")]
    [Tooltip("화면을 덮는 기존 깃털 연출. 끄면 날개 펼침과 주변 깃털만 바로 재생")]
    [SerializeField] private bool screenFeathersEnabled = true;
    [Tooltip("연출 전체 지속 시간(초). 깃털 이동 속도와 별개")]
    [SerializeField, Min(0.5f)] private float duration = 2f;
    [Tooltip("1080 높이 화면 기준 깃털 이동 거리(픽셀/초). 지속 시간과 날개 등장 시점은 그대로 유지. 재생 중 조절 가능")]
    [SerializeField, Min(0f)] private float featherMoveSpeed = 600f;
    [SerializeField, Range(80, 280)] private int featherCount = 220;
    [Header("Feather Wind")]
    [Tooltip("바람에 옆으로 휘어지는 거리. 1080 높이 화면 기준 픽셀")]
    [SerializeField, Min(0f)] private float featherWindStrength = 120f;
    [Tooltip("초당 흔들림 횟수. 직선 이동 속도 및 연출 시간과 별개")]
    [SerializeField, Min(0f)] private float featherFlutterSpeed = 1.5f;
    [Tooltip("바람을 타고 기울어지는 최대 각도")]
    [SerializeField, Range(0f, 180f)] private float featherTumbleAngle = 75f;
    [Tooltip("깃털이 옆면을 보이듯 폭이 좁아지는 정도. 0이면 사용하지 않음")]
    [SerializeField, Range(0f, 1f)] private float featherFlipAmount = 0.8f;
    [Tooltip("X: 화면 연출 시간 비율, Y: 휘날림 강도 배율. 0이면 직선 이동만 남음")]
    [SerializeField] private AnimationCurve featherWindCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    [Header("Feather Image")]
    [Tooltip("커스텀 깃털 Sprite. 비워두면 기존 절차적 깃털 모양 사용")]
    [SerializeField] private Sprite featherSprite;
    [Tooltip("커스텀 이미지에 곱할 색상. 흰색이면 원본 색상 유지")]
    [SerializeField] private Color featherImageTint = Color.white;
    [Tooltip("이미지 방향 보정(도). 깃털 끝이 위를 향하는 이미지 기준 0")]
    [SerializeField, Range(-180f, 180f)] private float featherImageRotation;
    [SerializeField] private int sortingOrder = 32000;
    [Header("Wing Surrounding Feathers")]
    [SerializeField] private bool wingFeathersEnabled = true;
    [SerializeField] private Sprite wingFeatherSprite;
    [Tooltip("날개 펼침 시작 후 깃털을 터뜨릴 시간(초). 0=즉시, 0.5=0.5초 뒤. 펼침이 끝난 뒤도 가능")]
    [SerializeField, Min(0f)] private float wingFeatherBurstDelay = 0.4f;
    [SerializeField, Range(1, 128)] private int wingFeatherCount = 48;
    [SerializeField, Min(0.1f)] private float wingFeatherLifetime = 1.5f;
    [Tooltip("날개 반폭 기준 깃털 크기")]
    [SerializeField, Min(0.01f)] private float wingFeatherSize = 0.22f;
    [Tooltip("날개 반폭 기준 초당 퍼지는 속도")]
    [SerializeField, Min(0f)] private float wingFeatherSpeed = 1.2f;
    [Tooltip("펼침 순간 깃털이 터져 나오는 추가 힘. 빠르게 감속한 뒤 흩날림. 0이면 추가 힘 없음")]
    [SerializeField, Min(0f)] private float wingFeatherBurstStrength = 4f;
    [Tooltip("날개 반폭 기준 휘날리는 거리")]
    [SerializeField, Min(0f)] private float wingFeatherFlutter = 0.12f;

    private LSO_BossPhase bossPhase;
    private GameObject overlay;
    private DLJ_CorvoKingFeatherGraphic feathers;
    private float elapsed;
    private bool enteredPhaseTwo;
    private bool revealed;
    private bool unfoldingStarted;
    private DLJ_CorvoKingWingMotion wingMotion;
    private DLJ_CorvoKingWingFeathers wingFeathers;
    private DLJ_CorvoKingCameraZoom cameraZoom;
    private bool wingBurstPending;
    private double wingUnfoldStartedAt;

    private void OnEnable()
    {
        bossPhase = GetComponent<LSO_BossPhase>();
        FindWings();
        bossPhase.OnPhaseChange += HandlePhaseChange;
        // 재활성화 시 이미 진입한 페이즈의 날개 상태를 복구.
        if (bossPhase.CurrentPhase >= 2)
        {
            enteredPhaseTwo = true;
            SetWings(true);
        }
        else SetWings(false);
    }

    private void HandlePhaseChange(int phase)
    {
        if (phase < 2 || enteredPhaseTwo) return;
        enteredPhaseTwo = true;
        PlayTransition();
    }

    [ContextMenu("2페이즈 깃털 연출 미리보기 (Play Mode)")]
    public void PlayTransition()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        FindWings();
        if (wings == null)
            Debug.LogWarning($"{name}: '{wingChildName}' 날개 자식을 찾지 못했어. Wings에 연결해 줘.", this);
        ClearOverlay();
        wingBurstPending = false;
        if (wingFeathers != null) wingFeathers.Clear();
        if (wingMotion != null) wingMotion.ShowUnfolded();
        SetWings(false);
        revealed = false;
        unfoldingStarted = false;
        elapsed = 0f;
        BeginCameraZoom();
        if (!screenFeathersEnabled)
        {
            PrepareFoldedWings();
            SetWings(true);
            revealed = true;
            BeginWingUnfold();
            return;
        }

        // 카메라/월드 깊이에 가려지지 않는 화면 연출. 게임 시간과 입력은 변경하지 않음.
        overlay = new GameObject("DLJ_CorvoKing_FeatherTransition", typeof(RectTransform), typeof(Canvas));
        overlay.hideFlags = HideFlags.DontSave;
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        // 암막은 별도 흰 텍스처 UI로 유지해 깃털 이미지의 투명 영역에 영향을 받지 않음.
        var coverObject = new GameObject("Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coverObject.transform.SetParent(overlay.transform, false);
        var coverRect = (RectTransform)coverObject.transform;
        coverRect.anchorMin = Vector2.zero;
        coverRect.anchorMax = Vector2.one;
        coverRect.offsetMin = coverRect.offsetMax = Vector2.zero;
        var cover = coverObject.GetComponent<Image>();
        cover.raycastTarget = false;
        cover.color = Color.clear;
        var surface = new GameObject("Feathers", typeof(RectTransform), typeof(CanvasRenderer), typeof(DLJ_CorvoKingFeatherGraphic));
        surface.transform.SetParent(overlay.transform, false);
        var rect = (RectTransform)surface.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        feathers = surface.GetComponent<DLJ_CorvoKingFeatherGraphic>();
        feathers.raycastTarget = false;
        feathers.Initialize(Mathf.Clamp(featherCount, 80, 280), featherSprite,
            featherImageTint, featherImageRotation, cover);
    }

    private void Update()
    {
        if (feathers == null) return;
        float deltaTime = Time.unscaledDeltaTime;
        elapsed += deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.5f, duration));
        float wind = featherWindCurve != null && featherWindCurve.length > 0
            ? Mathf.Max(0f, featherWindCurve.Evaluate(progress)) : 1f;
        feathers.SetWind(featherWindStrength * wind, featherFlutterSpeed,
            featherTumbleAngle * wind, featherFlipAmount * wind);
        // 불투명 구간을 최소 한 렌더 프레임 유지. 큰 프레임 지연에도 교체가 노출되지 않음.
        if (!revealed && progress >= DLJ_CorvoKingFeatherGraphic.CoveredAt)
        {
            progress = DLJ_CorvoKingFeatherGraphic.CoveredAt;
            feathers.SetFrame(progress, deltaTime, featherMoveSpeed);
            PrepareFoldedWings();
            SetWings(true);
            revealed = true;
            return;
        }
        feathers.SetFrame(progress, deltaTime, featherMoveSpeed);
        // 암막이 완전히 걷힌 뒤 펼침/깃털 폭발을 시작해 첫 순간이 가려지지 않게 함.
        if (revealed && !unfoldingStarted && progress >= 0.85f)
        {
            BeginWingUnfold();
        }
        if (progress >= 1f) ClearOverlay();
    }

    private void PrepareFoldedWings()
    {
        if (wings == null || wings == gameObject) return;
        if (wingMotion == null)
            wingMotion = wings.GetComponent<DLJ_CorvoKingWingMotion>();
        if (wingMotion == null)
            wingMotion = wings.AddComponent<DLJ_CorvoKingWingMotion>();
        wingMotion.SetFolded(wingFoldAngle, wingLowerDistance, wingUnfoldCurve, wingRiseCurve);
    }

    private void BeginWingUnfold()
    {
        unfoldingStarted = true;
        if (wingMotion == null) return;
        wingMotion.PlayUnfold(wingUnfoldDuration);
        wingUnfoldStartedAt = Time.unscaledTimeAsDouble;
        wingBurstPending = wingFeathersEnabled;
    }

    private void BeginCameraZoom()
    {
        if (cameraZoom != null) cameraZoom.Restore();
        if (!cameraZoomEnabled) return;
        if (cameraZoom == null) cameraZoom = GetComponent<DLJ_CorvoKingCameraZoom>();
        if (cameraZoom == null) cameraZoom = gameObject.AddComponent<DLJ_CorvoKingCameraZoom>();
        float unfoldStartsAt = screenFeathersEnabled ? Mathf.Max(0.5f, duration) * 0.85f : 0f;
        float afterUnfold = Mathf.Max(0.1f, wingUnfoldDuration);
        if (wingFeathersEnabled)
            afterUnfold = Mathf.Max(afterUnfold, Mathf.Max(0f, wingFeatherBurstDelay)
                + Mathf.Max(0.1f, wingFeatherLifetime) * 1.2f);
        cameraZoom.Play(cameraZoomRatio, cameraZoomInDuration,
            unfoldStartsAt + afterUnfold + Mathf.Max(0f, cameraZoomReturnDelay), cameraZoomOutDuration,
            cameraZoomInCurve, cameraZoomOutCurve);
    }

    private void LateUpdate()
    {
        // 화면 전환이 끝나도 기다리며, 해당 프레임의 날개 변형이 끝난 위치에서 방출.
        if (!wingBurstPending) return;
        if (!wingFeathersEnabled || wingMotion == null || wings == null || !wings.activeInHierarchy)
        {
            wingBurstPending = false;
            return;
        }
        if (Time.unscaledTimeAsDouble - wingUnfoldStartedAt < Mathf.Max(0f, wingFeatherBurstDelay)) return;
        wingBurstPending = false;
        if (wingFeathers == null) wingFeathers = GetComponent<DLJ_CorvoKingWingFeathers>();
        if (wingFeathers == null) wingFeathers = gameObject.AddComponent<DLJ_CorvoKingWingFeathers>();
        wingFeathers.Play(wingMotion, wingFeatherSprite != null ? wingFeatherSprite : featherSprite,
            wingFeatherCount, wingFeatherLifetime, wingFeatherSize,
            wingFeatherSpeed, wingFeatherFlutter, wingFeatherBurstStrength);
    }

    private void FindWings()
    {
        if (wings != null) return;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform || child.name != wingChildName) continue;
            wings = child.gameObject;
            break;
        }
    }

    private void SetWings(bool active)
    {
        if (wings != null && wings != gameObject) wings.SetActive(active);
    }

    private void ClearOverlay()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
            Destroy(overlay);
        }
        overlay = null;
        feathers = null;
    }

    private void OnDisable()
    {
        if (bossPhase != null) bossPhase.OnPhaseChange -= HandlePhaseChange;
        if (cameraZoom != null) cameraZoom.Restore();
        wingBurstPending = false;
        if (wingFeathers != null) wingFeathers.Clear();
        ClearOverlay();
        if (wingMotion != null) wingMotion.ShowUnfolded();
        // 연출 도중 컴포넌트가 꺼져도 2페이즈 모델이 숨겨진 채 남지 않게 처리.
        if (enteredPhaseTwo) SetWings(true);
    }
}
