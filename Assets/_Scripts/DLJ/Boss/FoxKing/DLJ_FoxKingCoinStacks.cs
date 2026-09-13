using System.Collections.Generic;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

/// <summary>수탈 자원 1당 동전 1개. 씬의 세 동전은 위치/외형 견본으로만 사용.</summary>
[DisallowMultipleComponent]
public sealed class DLJ_FoxKingCoinStacks : MonoBehaviour
{
    [Header("수탈 자원 연결")]
    [SerializeField] private DLJ_FoxKingBoss foxKing;
    [Tooltip("지정한 여우왕이 비활성/제거되면 같은 씬의 활성 여우왕에 연결")]
    [SerializeField] private bool findSpawnedBoss = true;

    [Header("위치 인디케이터 (플레이 시작 시 숨김)")]
    [SerializeField] private Transform topIndicator;
    [SerializeField] private Transform leftIndicator;
    [SerializeField] private Transform rightIndicator;

    [Header("쌓이는 순서 / 간격")]
    [Tooltip("위 → 왼쪽 → 오른쪽 다음에 위쪽에 추가할 개수. 1이면 2:1:1, 0이면 1:1:1")]
    [SerializeField, Range(0, 4)] private int extraTopCoinsPerCycle = 1;
    [Tooltip("0이면 견본 메시의 월드 높이로 자동 계산. 양수면 월드 단위 간격")]
    [SerializeField, Min(0f)] private float stackSpacing;
    [SerializeField, Min(1f)] private float spacingMultiplier = 1.02f;
    [SerializeField, Range(0f, 30f)] private float yawVariation = 9f;

    [Header("낙하 연출")]
    [SerializeField, Min(0f)] private float dropHeight = 0.65f;
    [SerializeField, Min(0.01f)] private float dropDuration = 0.24f;
    [SerializeField, Min(0.01f)] private float coinInterval = 0.1f;

    private sealed class Coin
    {
        public Transform visual;
        public int pile;
        public Vector3 destination;
        public float elapsed;
    }

    private readonly List<Coin> coins = new();
    private readonly GameObject[] templates = new GameObject[3];
    private readonly float[] heights = new float[3];
    private readonly int[] pileCounts = new int[3];
    private Transform[] indicators;
    private GameObject visualRoot;
    private DLJ_FoxKingBoss boundBoss;
    private Health boundHealth;
    private int targetCount;
    private float nextCoinTime;
    private float nextSearchTime;
    private bool initialized;

    public int DisplayedCoinCount => coins.Count;
    public int TargetCoinCount => targetCount;
    public DLJ_FoxKingBoss BoundBoss => boundBoss;

    public static int GetPileIndex(int coinIndex, int extraTopCoins)
    {
        int step = Mathf.Max(0, coinIndex) % (3 + Mathf.Clamp(extraTopCoins, 0, 4));
        return step < 3 ? step : 0;
    }

    private void Awake()
    {
        indicators = new[] { topIndicator, leftIndicator, rightIndicator };
        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] == null || transform.IsChildOf(indicators[i]) ||
                (i > 0 && indicators[i] == indicators[0]) ||
                (i > 1 && indicators[i] == indicators[1]))
            {
                Debug.LogWarning("[여우왕 동전] 서로 다른 위/왼쪽/오른쪽 인디케이터를 연결해 줘.", this);
                enabled = false;
                return;
            }
        }

        visualRoot = new GameObject("DLJ_FoxKingCoinVisuals");
        visualRoot.transform.SetParent(transform, false);
        for (int i = 0; i < indicators.Length; i++)
        {
            templates[i] = CreateVisualTemplate(indicators[i], out heights[i]);
            indicators[i].gameObject.SetActive(false);
        }
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized) return;
        visualRoot.SetActive(true);
        RefreshBoss();
    }

    private void OnDisable()
    {
        Unbind();
        if (visualRoot != null) visualRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        Unbind();
        if (visualRoot != null) Destroy(visualRoot);
    }

    private bool IsAvailable(DLJ_FoxKingBoss boss)
    {
        if (boss == null || !boss.isActiveAndEnabled) return false;
        Health health = boss.GetComponent<Health>();
        return health == null || !health.IsDestroyed;
    }

    private void RefreshBoss()
    {
        DLJ_FoxKingBoss next = IsAvailable(foxKing) ? foxKing : null;
        if (next == null && findSpawnedBoss)
        {
            foreach (var candidate in FindObjectsByType<DLJ_FoxKingBoss>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != gameObject.scene || !IsAvailable(candidate)) continue;
                next = candidate;
                break;
            }
        }
        if (next == boundBoss) return;
        Unbind();
        SetResourceCount(0);
        boundBoss = next;
        if (boundBoss == null) return;
        boundHealth = boundBoss.GetComponent<Health>();
        boundBoss.OnStolenResourcesChanged += SetResourceCount;
        SetResourceCount(boundBoss.StolenResources);
    }

    private void Unbind()
    {
        if (boundBoss != null) boundBoss.OnStolenResourcesChanged -= SetResourceCount;
        boundBoss = null;
        boundHealth = null;
    }

    private void Update()
    {
        if (!initialized) return;
        if (boundBoss == null || !boundBoss.isActiveAndEnabled ||
            (boundHealth != null && boundHealth.IsDestroyed))
        {
            Unbind();
            SetResourceCount(0);
            if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + 0.25f;
                RefreshBoss();
            }
        }

        if (coins.Count < targetCount && Time.time >= nextCoinTime)
        {
            AddCoin();
            nextCoinTime = Time.time + Mathf.Max(0.01f, coinInterval);
        }
        foreach (Coin coin in coins)
        {
            coin.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(coin.elapsed / Mathf.Max(0.01f, dropDuration));
            // 가속 낙하 후 작은 착지 반동. 물리 충돌 없이 층 높이를 유지.
            float height = t >= 1f ? 0f : t < 0.8f
                ? dropHeight * (1f - (t / 0.8f) * (t / 0.8f))
                : Mathf.Sin((t - 0.8f) / 0.2f * Mathf.PI) * dropHeight * 0.055f;
            coin.visual.position = coin.destination + Vector3.up * height;
        }
    }

    private void SetResourceCount(int count)
    {
        targetCount = Mathf.Max(0, count);
        while (coins.Count > targetCount)
        {
            int last = coins.Count - 1;
            Coin coin = coins[last];
            pileCounts[coin.pile]--;
            coin.visual.gameObject.SetActive(false);
            Destroy(coin.visual.gameObject);
            coins.RemoveAt(last);
        }
        if (coins.Count == 0) nextCoinTime = Time.time;
    }

    private void AddCoin()
    {
        int pile = GetPileIndex(coins.Count, extraTopCoinsPerCycle);
        if (indicators[pile] == null) return;
        int level = pileCounts[pile]++;
        float spacing = stackSpacing > 0f ? stackSpacing : heights[pile] * Mathf.Max(1f, spacingMultiplier);
        Vector3 destination = indicators[pile].position + Vector3.up * (level * spacing);
        GameObject visual = Instantiate(templates[pile], visualRoot.transform);
        visual.name = $"Coin_{coins.Count + 1}_{pile}_{level + 1}";
        visual.transform.position = destination + Vector3.up * dropHeight;
        visual.transform.rotation = Quaternion.AngleAxis(Mathf.Sin((coins.Count + 1) * 2.4f) * yawVariation, Vector3.up);
        visual.SetActive(true);
        coins.Add(new Coin { visual = visual.transform, pile = pile, destination = destination });
    }

    private GameObject CreateVisualTemplate(Transform indicator, out float height)
    {
        GameObject template = new GameObject($"{indicator.name}_VisualTemplate");
        template.SetActive(false);
        template.transform.SetParent(visualRoot.transform, false);
        template.transform.position = indicator.position;
        template.transform.rotation = Quaternion.identity;
        float bottom = float.PositiveInfinity;
        float top = float.NegativeInfinity;
        // 견본의 게임용 스크립트/콜라이더 없이 메시와 공유 머티리얼만 복제.
        foreach (MeshFilter source in indicator.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer renderer = source.GetComponent<MeshRenderer>();
            if (source.sharedMesh == null || renderer == null || !renderer.enabled) continue;
            GameObject meshObject = new GameObject(source.name);
            meshObject.layer = source.gameObject.layer;
            meshObject.transform.SetParent(template.transform, false);
            meshObject.transform.position = source.transform.position;
            meshObject.transform.rotation = source.transform.rotation;
            meshObject.transform.localScale = source.transform.lossyScale;
            meshObject.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            MeshRenderer copy = meshObject.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.shadowCastingMode = renderer.shadowCastingMode;
            copy.receiveShadows = renderer.receiveShadows;
            copy.renderingLayerMask = renderer.renderingLayerMask;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder;
            // 비활성 견본에서도 정확하도록 Renderer.bounds 대신 메시의 8개 꼭짓점을 변환.
            Bounds bounds = source.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                float y = source.transform.TransformPoint(point).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
        }
        height = float.IsInfinity(bottom) ? 0.05f : Mathf.Max(0.001f, top - bottom);
        return template;
    }
}
