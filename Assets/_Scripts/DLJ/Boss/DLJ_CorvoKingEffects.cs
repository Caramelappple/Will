using _Scripts.LDY;
using _Scripts.LSO.Boss.CrowKing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

/// <summary>까마귀왕의 시각 효과만 담당. 스탯, 공격 횟수, 사냥감은 변경하지 않음.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_CorvoKingEffects : MonoBehaviour
{
    [Header("Model / Material")]
    [SerializeField] private MeshFilter bodyMesh;
    [SerializeField] private Material effectMaterial;
    [Tooltip("실제 포식/되먹임 완료와 기억 폭주 준비/소비에 맞춰 이펙트 자동 재생")]
    [FormerlySerializedAs("observeInheritance")]
    [SerializeField] private bool autoPlaySkills = true;

    [Header("Predation")]
    [SerializeField, Min(0.05f)] private float predationDuration = 0.48f;
    [SerializeField, ColorUsage(true, true)] private Color predationColor = new Color(1.8f, 0.015f, 0.055f, 0.65f);
    [SerializeField, Range(0f, 0.2f)] private float predationExpansion = 0.035f;

    [Header("Feedback")]
    [SerializeField, Min(0.05f)] private float feedbackDuration = 0.8f;
    [SerializeField, ColorUsage(true, true)] private Color feedbackColor = new Color(3.2f, 0.008f, 0.025f, 0.85f);
    [SerializeField, Range(0f, 0.2f)] private float feedbackExpansion = 0.09f;

    [Header("Memory Frenzy / Eyes")]
    [Tooltip("메시 로컬 Bounds 안의 정규화 좌표 (0~1). Will_Corvo는 Z가 높이 축")]
    [SerializeField] private Vector3 eyeCenter = new Vector3(0.59f, 0.5f, 0.83f);
    [Tooltip("양쪽 눈의 로컬 Y 간격. 중앙에서 각각 이만큼 이동")]
    [SerializeField, Range(0f, 0.5f)] private float eyeSeparation = 0.29f;
    [SerializeField] private Vector3 eyeRadius = new Vector3(0.055f, 0.12f, 0.028f);
    [SerializeField, ColorUsage(true, true)] private Color frenzyColor = new Color(4f, 0.015f, 0.025f, 1f);
    [SerializeField, Min(0.05f)] private float eyeFadeDuration = 0.12f;

    [Header("Eye Light Spill")]
    [Tooltip("몸 크기 기준으로 눈 주위에 둥글게 번지는 빛의 지름")]
    [SerializeField, Range(0.02f, 0.4f)] private float eyeGlowSize = 0.16f;
    [SerializeField, Range(0f, 2f)] private float eyeGlowIntensity = 0.7f;
    [Tooltip("눈의 빛을 모델 표면 밖으로 띄우는 거리. 몸 크기에 대한 비율")]
    [SerializeField, Range(0f, 0.15f)] private float eyeGlowLift = 0.04f;

    private readonly MeshRenderer[] shells = new MeshRenderer[3];
    private readonly MeshRenderer[] eyeGlows = new MeshRenderer[2];
    private Material eyeGlowMaterial;
    private Mesh eyeGlowMesh;
    private MeshRenderer eyes;
    private GameObject visualRoot;
    private MaterialPropertyBlock block;
    private LDY_Animal animal;
    private LSO_Predation predation;
    private LSO_MemoryFrenzy frenzy;
    private LSO_PreyTracker preyTracker;
    private LSO_CrowKingMemory memory;
    private int devourEventCount;
    private float flashTime, flashDuration, flashExpansion, eyeAmount, clock;
    private Color flashColor;
    private bool frenzyActive;

    public bool IsPlaying => flashTime > 0f || frenzyActive || eyeAmount > 0f;
    public bool AutoPlaySkills => autoPlaySkills;
    public bool IsPredationBound => predation != null;
    public bool IsFrenzyBound => frenzy != null;
    public bool IsFrenzyActive => frenzyActive;
    public bool HasVisualSource => bodyMesh != null && bodyMesh.sharedMesh != null && effectMaterial != null;
    public bool AreTurnEventsBound => animal != null && animal.AreAbilityEventsRegistered;
    public LDY_Animal CurrentPrey => preyTracker != null ? preyTracker.Prey : null;
    public int InheritedAttack => memory != null ? memory.InheritedAtk : 0;
    public int DevourEventCount => devourEventCount;
    public int KillAttempts => predation != null ? predation.KillAttempts : 0;
    public string LastDevourResult => predation != null ? predation.LastDevourResult : "포식 특성 연결 없음";

    private void OnEnable()
    {
        animal = GetComponent<LDY_Animal>();
        preyTracker = GetComponent<LSO_PreyTracker>();
        memory = GetComponent<LSO_CrowKingMemory>();
        if (animal == null) return;
        animal.AbilitiesChanged += BindSkills;
        BindSkills();
    }

    // 다른 컴포넌트의 Awake에서 생성되는 특성도 시작 시 한 번 더 연결.
    private void Start() => BindSkills();

    private void BindSkills()
    {
        LSO_Predation nextPredation = null;
        LSO_MemoryFrenzy nextFrenzy = null;
        if (animal != null)
        {
            foreach (var ability in animal.Abilities)
            {
                if (ability is LSO_Predation p) nextPredation = p;
                if (ability is LSO_MemoryFrenzy f) nextFrenzy = f;
            }
        }
        // 되먹임 중 AddAbility 알림이 와도 같은 인스턴스는 재구독하지 않음.
        if (predation != nextPredation)
        {
            if (predation != null) predation.Devoured -= HandleDevoured;
            predation = nextPredation;
            if (predation != null) predation.Devoured += HandleDevoured;
        }
        if (frenzy != nextFrenzy)
        {
            if (frenzy != null) frenzy.ChargeChanged -= HandleFrenzyChanged;
            frenzy = nextFrenzy;
            if (frenzy != null) frenzy.ChargeChanged += HandleFrenzyChanged;
            if (autoPlaySkills) SetMemoryFrenzy(frenzy != null && frenzy.IsReady(animal));
        }
    }

    private void UnbindSkills()
    {
        if (animal != null) animal.AbilitiesChanged -= BindSkills;
        if (predation != null) predation.Devoured -= HandleDevoured;
        if (frenzy != null) frenzy.ChargeChanged -= HandleFrenzyChanged;
        predation = null;
        frenzy = null;
    }

    private void HandleDevoured(bool repeatedKind)
    {
        devourEventCount++;
        if (!autoPlaySkills || !isActiveAndEnabled) return;
        if (repeatedKind) PlayFeedback();
        else PlayPredation();
    }

    private void HandleFrenzyChanged(bool charged)
    {
        if (autoPlaySkills && isActiveAndEnabled) SetMemoryFrenzy(charged);
    }

    private void LateUpdate()
    {
        // 공격 횟수 질의 전에도 실제 특성이 판단한 준비 상태를 표시.
        if (autoPlaySkills && frenzy != null) SetMemoryFrenzy(frenzy.IsReady(animal));
        Tick(Time.deltaTime);
    }

    public void PlayPredation() => BeginFlash(predationDuration, predationColor, predationExpansion);
    public void PlayFeedback() => BeginFlash(feedbackDuration, feedbackColor, feedbackExpansion);

    /// <summary>폭주 충전 시 true, 공격으로 소비 시 false를 전달. 충전 규칙은 호출자 담당.</summary>
    public void SetMemoryFrenzy(bool active)
    {
        if (frenzyActive == active) return;
        if (!EnsureVisuals()) return;
        frenzyActive = active;
    }

    public void PlayMemoryFrenzy() => SetMemoryFrenzy(true);
    public void ClearMemoryFrenzy() => SetMemoryFrenzy(false);

    private void BeginFlash(float duration, Color color, float expansion)
    {
        if (!EnsureVisuals()) return;
        flashDuration = Mathf.Max(0.05f, duration);
        flashTime = flashDuration;
        flashColor = color;
        flashExpansion = expansion;
        Tick(0f);
    }

    private bool EnsureVisuals()
    {
        if (visualRoot != null) return true;
        if (bodyMesh == null) bodyMesh = GetComponent<MeshFilter>();
        if (bodyMesh == null || bodyMesh.sharedMesh == null || effectMaterial == null) return false;
        block = new MaterialPropertyBlock();
        visualRoot = new GameObject("DLJ_CorvoKing_Visuals") { hideFlags = HideFlags.HideAndDontSave };
        visualRoot.transform.SetParent(bodyMesh.transform, false);
        for (int i = 0; i < shells.Length; i++) shells[i] = CreateLayer("Crimson Shell " + i);
        eyes = CreateLayer("Memory Frenzy Eyes");
        CreateEyeGlows();
        return true;
    }

    private void CreateEyeGlows()
    {
        // 원본 머티리얼의 블렌딩은 유지하고 눈의 빛만 가산 합성.
        eyeGlowMaterial = new Material(effectMaterial)
        {
            name = "DLJ_CorvoKing_EyeSpill",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = effectMaterial.renderQueue + 1
        };
        eyeGlowMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
        eyeGlowMaterial.SetFloat("_Cull", (float)CullMode.Off);
        eyeGlowMaterial.SetFloat("_EyeMode", 2f);
        eyeGlowMesh = new Mesh { name = "DLJ_EyeSpillQuad", hideFlags = HideFlags.HideAndDontSave };
        eyeGlowMesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
        eyeGlowMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        eyeGlowMesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
        eyeGlowMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        for (int i = 0; i < eyeGlows.Length; i++)
        {
            eyeGlows[i] = CreateLayer("Memory Frenzy Light Spill " + i);
            eyeGlows[i].GetComponent<MeshFilter>().sharedMesh = eyeGlowMesh;
            eyeGlows[i].sharedMaterial = eyeGlowMaterial;
        }
    }

    private MeshRenderer CreateLayer(string layerName)
    {
        var layer = new GameObject(layerName) { hideFlags = HideFlags.HideAndDontSave, layer = bodyMesh.gameObject.layer };
        layer.transform.SetParent(visualRoot.transform, false);
        layer.AddComponent<MeshFilter>().sharedMesh = bodyMesh.sharedMesh;
        var renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = effectMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;
        return renderer;
    }

    /// <summary>에디터 미리보기에서도 같은 연출 사용. 전투 로직은 실행하지 않음.</summary>
    public void Tick(float deltaTime)
    {
        if (visualRoot == null || bodyMesh == null || bodyMesh.sharedMesh == null) return;
        deltaTime = Mathf.Max(0f, deltaTime);
        clock += deltaTime;
        flashTime = Mathf.Max(0f, flashTime - deltaTime);
        eyeAmount = Mathf.MoveTowards(eyeAmount, frenzyActive ? 1f : 0f, deltaTime / Mathf.Max(0.05f, eyeFadeDuration));
        float progress = flashDuration > 0f ? 1f - flashTime / flashDuration : 1f;
        float envelope = Mathf.SmoothStep(0f, 1f, progress / 0.12f) * Mathf.Pow(1f - progress, 1.35f);
        var sourceRenderer = bodyMesh.GetComponent<MeshRenderer>();
        bool visible = sourceRenderer != null && sourceRenderer.enabled;
        Bounds bounds = bodyMesh.sharedMesh.bounds;
        float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        for (int i = 0; i < shells.Length; i++)
        {
            shells[i].enabled = visible && flashTime > 0f;
            if (!shells[i].enabled) continue;
            block.Clear();
            block.SetColor("_Tint", flashColor);
            block.SetFloat("_Strength", envelope * (i == 0 ? 0.55f : 0.24f / i));
            block.SetFloat("_Expansion", size * (0.0005f + flashExpansion * i * (0.35f + progress * 0.65f)));
            block.SetFloat("_EyeMode", 0f);
            block.SetFloat("_Clock", clock);
            shells[i].SetPropertyBlock(block);
        }
        eyes.enabled = visible && eyeAmount > 0f;
        UpdateEyeGlows(bounds, size, eyes.enabled);
        if (!eyes.enabled) return;
        block.Clear();
        block.SetColor("_Tint", frenzyColor);
        block.SetFloat("_Strength", eyeAmount * (0.88f + 0.12f * Mathf.Sin(clock * 7f)));
        block.SetFloat("_Expansion", size * 0.0007f);
        block.SetFloat("_EyeMode", 1f);
        block.SetVector("_BoundsMin", bounds.min);
        block.SetVector("_BoundsSize", bounds.size);
        block.SetVector("_EyeCenter", eyeCenter);
        block.SetVector("_EyeRadius", eyeRadius);
        block.SetFloat("_EyeSeparation", eyeSeparation);
        eyes.SetPropertyBlock(block);
    }

    private void UpdateEyeGlows(Bounds bounds, float size, bool visible)
    {
        float length = Mathf.Max(size * eyeGlowSize, 0.00001f);
        // 셰이더에서 회전하는 빌보드가 CPU 프러스텀 컬링에서 잘리지 않게 확보.
        eyeGlowMesh.bounds = new Bounds(Vector3.zero, Vector3.one * length * 4f);
        float worldLength = Mathf.Max(bodyMesh.transform.TransformVector(Vector3.right * length).magnitude,
            Mathf.Max(bodyMesh.transform.TransformVector(Vector3.up * length).magnitude,
                bodyMesh.transform.TransformVector(Vector3.forward * length).magnitude));
        for (int i = 0; i < eyeGlows.Length; i++)
        {
            var glow = eyeGlows[i];
            glow.enabled = visible && eyeGlowIntensity > 0f;
            if (!glow.enabled) continue;
            float side = i == 0 ? -1f : 1f;
            Vector3 center = eyeCenter + Vector3.up * (side * eyeSeparation);
            glow.transform.localPosition = bounds.min + Vector3.Scale(bounds.size, center)
                + Vector3.up * (side * size * eyeGlowLift);
            block.Clear();
            block.SetFloat("_EyeMode", 2f);
            block.SetColor("_Tint", frenzyColor);
            block.SetFloat("_Strength", eyeAmount * eyeGlowIntensity * (0.92f + 0.08f * Mathf.Sin(clock * 7f)));
            block.SetFloat("_GlowSize", worldLength);
            glow.SetPropertyBlock(block);
        }
    }

    public void StopAndReset()
    {
        flashTime = 0f;
        eyeAmount = 0f;
        frenzyActive = false;
        foreach (var shell in shells) if (shell != null) shell.enabled = false;
        foreach (var glow in eyeGlows) if (glow != null) glow.enabled = false;
        if (eyes != null) eyes.enabled = false;
    }

    public void ReleaseVisuals()
    {
        StopAndReset();
        if (visualRoot != null)
        {
            if (Application.isPlaying) Destroy(visualRoot);
            else DestroyImmediate(visualRoot);
        }
        visualRoot = null;
        ReleaseGeneratedAsset(eyeGlowMesh);
        ReleaseGeneratedAsset(eyeGlowMaterial);
        eyeGlowMesh = null;
        eyeGlowMaterial = null;
    }

    private static void ReleaseGeneratedAsset(Object asset)
    {
        if (asset == null) return;
        if (Application.isPlaying) Destroy(asset);
        else DestroyImmediate(asset);
    }

    private void OnDisable()
    {
        UnbindSkills();
        ReleaseVisuals();
    }

    private void OnDestroy()
    {
        UnbindSkills();
        ReleaseVisuals();
    }
}
