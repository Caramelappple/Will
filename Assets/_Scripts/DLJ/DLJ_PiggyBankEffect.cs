using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;

/// <summary>CostRefund의 저장/사망 시점에만 반응하는 황금돼지 연출.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal))]
public sealed class DLJ_PiggyBankEffect : MonoBehaviour
{
    [Header("금화 / 금빛")]
    [SerializeField] private Mesh coinMesh;
    [SerializeField] private Material goldMaterial;
    [SerializeField, Min(0.01f)] private float coinDiameter = 0.28f;
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(1f, 0.56f, 0.06f);
    [SerializeField, Min(0f)] private float baseGlow = 0.15f;
    [SerializeField, Min(0f)] private float glowPerCoin = 0.65f;

    [Header("저금 구멍")]
    [Tooltip("지정하면 이 위치로 삽입. 비우면 모델 로컬 경계의 등 중앙을 사용")]
    [SerializeField] private Transform depositSlot;
    [Tooltip("모델 로컬 좌표 기준 구멍 위치 보정")]
    [SerializeField] private Vector3 slotOffset = new Vector3(-0.018f, 0f, 0f);
    [SerializeField, Range(0f, 1f)] private float slotHeightRatio = 0.76f;
    [SerializeField, Min(0.01f)] private float depositHeight = 1.2f;
    [SerializeField, Min(0.05f)] private float depositDuration = 0.38f;

    [Header("폭발 / 회수")]
    [SerializeField, Min(0.05f)] private float burstDuration = 0.45f;
    [Tooltip("초기 수평 속도를 정하는 흩어짐 거리. 금화는 중력으로 낙하하고 바닥에서 감속")]
    [SerializeField, Min(0.01f)] private float scatterRadius = 0.6f;
    [SerializeField, Min(0.05f)] private float collectionDuration = 0.65f;
    [SerializeField, Min(0f)] private float collectionInterval = 0.12f;

    private LDY_Animal _animal;
    private Transform _model;
    private Bounds _localBounds;
    private bool _prepared;
    private bool _dead;
    private int _displayedCost;
    private float _glow;
    private float _pulse;
    private readonly List<Binding> _bindings = new();
    private readonly List<Renderer> _modelRenderers = new();
    private readonly List<GameObject> _depositCoins = new();
    private readonly Dictionary<Renderer, bool> _previewRendererStates = new();
    private GameObject _deathPreviewRoot;
    private Coroutine _deathPreviewRoutine;

    public bool IsPreviewingDeath => _deathPreviewRoutine != null;

    private sealed class Binding
    {
        public Renderer Renderer;
        public Material[] Originals;
        public Material Gold;
    }

    private void Start() => Prepare();

    private void Prepare()
    {
        if (_prepared) return;
        _prepared = true;
        _animal = GetComponent<LDY_Animal>();
        _model = _animal.modelTransform != null ? _animal.modelTransform : transform;
        _localBounds = CalculateLocalBounds(_model);
        foreach (Renderer renderer in _model.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
            _modelRenderers.Add(renderer);
            // Will_Pig의 눈 색은 보존하고 몸체만 금빛으로 바꾼다.
            if (renderer.name.IndexOf("Eyes", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            Material[] originals = renderer.sharedMaterials;
            if (originals.Length == 0 || (goldMaterial == null && originals[0] == null)) continue;
            Material material = new Material(goldMaterial != null ? goldMaterial : originals[0]);
            material.name = "DLJ_PigGold_Runtime";
            material.EnableKeyword("_EMISSION");
            Material[] replacement = (Material[])originals.Clone();
            replacement[0] = material;
            renderer.sharedMaterials = replacement;
            _bindings.Add(new Binding { Renderer = renderer, Originals = originals, Gold = material });
        }
        _glow = baseGlow;
        ApplyGlow();
    }

    private void LateUpdate()
    {
        if (!_prepared || _dead) return;
        _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 3f);
        _glow = Mathf.MoveTowards(_glow, baseGlow + glowPerCoin * _displayedCost,
            Time.deltaTime * Mathf.Max(1f, glowPerCoin * 4f));
        ApplyGlow();
    }

    private void ApplyGlow()
    {
        foreach (Binding binding in _bindings)
            if (binding.Gold != null && binding.Gold.HasProperty("_EmissionColor"))
                binding.Gold.SetColor("_EmissionColor", glowColor * (_glow + _pulse));
    }

    public void PlayDeposit(int storedCost)
    {
        if (_dead || !isActiveAndEnabled || !Application.isPlaying) return;
        Prepare();
        StartCoroutine(Deposit(Mathf.Clamp(storedCost, 0, 3)));
    }

    private IEnumerator Deposit(int storedCost)
    {
        Transform coin = CreateCoin(transform, coinMesh, goldMaterial, coinDiameter);
        _depositCoins.Add(coin.gameObject);
        Vector3 fullScale = coin.localScale;
        Quaternion upright = Quaternion.Euler(90f, 0f, 0f);
        float elapsed = 0f;
        while (elapsed < depositDuration && !_dead)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, depositDuration));
            Vector3 slot = SlotPosition;
            // 출발 대기와 도착 감속 없이 가속 낙하. 구멍 앞에서 떠 있는 인상을 없앰.
            float descend = t * t;
            coin.position = slot + _model.up * (depositHeight * (1f - descend));
            coin.rotation = _model.rotation * upright * Quaternion.Euler(12f * (1f - t), 18f * (1f - t), 0f);
            float appear = Mathf.Clamp01(t / 0.08f);
            float insert = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1f, t));
            coin.localScale = fullScale * appear * insert;
            yield return null;
        }
        if (!_dead)
        {
            _displayedCost = Mathf.Max(_displayedCost, storedCost);
            _pulse = 1.1f;
        }
        _depositCoins.Remove(coin.gameObject);
        Destroy(coin.gameObject);
    }

    private Vector3 SlotPosition => depositSlot != null ? depositSlot.position :
        _model.TransformPoint(new Vector3(_localBounds.center.x,
            Mathf.Lerp(_localBounds.min.y, _localBounds.max.y, slotHeightRatio),
            _localBounds.center.z) + slotOffset);

    /// <summary>성공 시 떨어진 금화가 환급을 소유하므로 기존 큐에 다시 넣지 않는다.</summary>
    public bool TryPlayDeath(int storedCost)
    {
        if (_dead) return true;
        if (!isActiveAndEnabled || !Application.isPlaying) return false;
        Prepare();
        LDY_TurnManager turns = FindFirstObjectByType<LDY_TurnManager>();
        LDY_ActionPointManager points = turns != null ? turns.ActionPoints : LDY_ActionPointManager.instance;
        bool canRefund = _animal.team == LDY_Team.Player;
        // 연결이 없으면 기존 환급 서비스의 오류 처리/폴백을 유지.
        if (canRefund && storedCost > 0 && (turns == null || points == null)) return false;

        StopDeathPreview();
        _dead = true;
        StopAllCoroutines();
        foreach (GameObject coin in _depositCoins) if (coin != null) Destroy(coin);
        _depositCoins.Clear();
        CreateDeathVisual(canRefund ? Mathf.Clamp(storedCost, 0, 3) : 0, turns, points, false);
        foreach (Renderer renderer in _modelRenderers)
            if (renderer != null) renderer.enabled = false;
        return true;
    }

    private GameObject CreateDeathVisual(int count, LDY_TurnManager turns,
        LDY_ActionPointManager points, bool previewOnly)
    {
        Vector3 center = _model.TransformPoint(_localBounds.center);
        float ground = float.PositiveInfinity;
        foreach (Renderer renderer in _modelRenderers)
        {
            if (renderer == null) continue;
            ground = Mathf.Min(ground, renderer.bounds.min.y);
        }
        if (float.IsPositiveInfinity(ground)) ground = center.y - coinDiameter;

        GameObject root = new GameObject(previewOnly ? "DLJ_PigDeathPreview" : "DLJ_PigDroppedCoins");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.transform.position = center;
        root.AddComponent<DLJ_PigCoinPayout>().Initialize(
            count, turns, points,
            coinMesh, goldMaterial, coinDiameter, scatterRadius, burstDuration,
            collectionDuration, collectionInterval, ground, previewOnly);
        return root;
    }

    /// <summary>HP/저장량/보드/코스트를 바꾸지 않고 금화 3개의 사망 연출만 반복 재생.</summary>
    [ContextMenu("Preview/Death (Play Mode)")]
    public void PreviewDeath()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _dead) return;
        Prepare();
        StopDeathPreview();
        _deathPreviewRoot = CreateDeathVisual(3, null, null, true);
        foreach (Renderer renderer in _modelRenderers)
        {
            if (renderer == null) continue;
            _previewRendererStates.Add(renderer, renderer.enabled);
            renderer.enabled = false;
        }
        _deathPreviewRoutine = StartCoroutine(RestoreAfterDeathPreview());
    }

    private IEnumerator RestoreAfterDeathPreview()
    {
        while (_deathPreviewRoot != null) yield return null;
        _deathPreviewRoutine = null;
        StopDeathPreview();
    }

    public void StopDeathPreview()
    {
        if (_deathPreviewRoutine != null) StopCoroutine(_deathPreviewRoutine);
        _deathPreviewRoutine = null;
        if (_deathPreviewRoot != null)
        {
            _deathPreviewRoot.SetActive(false);
            Destroy(_deathPreviewRoot);
        }
        _deathPreviewRoot = null;
        foreach (KeyValuePair<Renderer, bool> state in _previewRendererStates)
            if (state.Key != null && !_dead) state.Key.enabled = state.Value;
        _previewRendererStates.Clear();
    }

    internal static Transform CreateCoin(Transform parent, Mesh mesh, Material material, float diameter)
    {
        GameObject coin;
        if (mesh != null)
        {
            coin = new GameObject("DLJ_PigCoin", typeof(MeshFilter), typeof(MeshRenderer));
            coin.GetComponent<MeshFilter>().sharedMesh = mesh;
            Vector3 size = mesh.bounds.size;
            coin.transform.localScale = Vector3.one * (diameter / Mathf.Max(0.001f, size.x, size.y, size.z));
        }
        else
        {
            coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "DLJ_PigCoin";
            Collider collider = coin.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            coin.transform.localScale = new Vector3(diameter, diameter * 0.08f, diameter);
        }
        coin.layer = 2;
        coin.transform.SetParent(parent, true);
        if (material != null) coin.GetComponent<Renderer>().sharedMaterial = material;
        return coin.transform;
    }

    private static Bounds CalculateLocalBounds(Transform model)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0.5f);
        bool found = false;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<TMPro.TMP_Text>() != null) continue;
            Bounds meshBounds = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                corner = model.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!found) { bounds = new Bounds(corner, Vector3.zero); found = true; }
                else bounds.Encapsulate(corner);
            }
        }
        return bounds;
    }

    private void OnDisable()
    {
        StopDeathPreview();
        StopAllCoroutines();
        foreach (GameObject coin in _depositCoins) if (coin != null) Destroy(coin);
        _depositCoins.Clear();
    }

    private void OnDestroy()
    {
        foreach (Binding binding in _bindings)
        {
            if (binding.Renderer != null)
            {
                Material[] current = binding.Renderer.sharedMaterials;
                if (current.Length > 0 && current[0] == binding.Gold)
                    binding.Renderer.sharedMaterials = binding.Originals;
            }
            if (binding.Gold != null) Destroy(binding.Gold);
        }
    }

    [ContextMenu("Preview/Deposit (Play Mode)")]
    public void PreviewDeposit() => PlayDeposit(Mathf.Min(3, _displayedCost + 1));

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        LDY_Animal animal = GetComponent<LDY_Animal>();
        Transform model = animal != null && animal.modelTransform != null ? animal.modelTransform : transform;
        Bounds bounds = CalculateLocalBounds(model);
        Vector3 position = depositSlot != null ? depositSlot.position : model.TransformPoint(
            new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, slotHeightRatio), bounds.center.z) + slotOffset);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, coinDiameter * 0.2f);
        Gizmos.DrawLine(position, position + model.up * depositHeight);
    }
#endif
}
