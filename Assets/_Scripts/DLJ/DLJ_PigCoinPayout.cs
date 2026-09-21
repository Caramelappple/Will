using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using UnityEngine;

/// <summary>사망한 기물과 독립적으로 금화를 보관하고 다음 플레이어 턴에 케이스로 회수한다.</summary>
public sealed class DLJ_PigCoinPayout : MonoBehaviour
{
    // 보드의 월드 스케일에 맞춘 연출 중력. 짧고 빠른 낙하와 낮은 반발을 사용.
    private const float CoinGravity = 32f;
    private const float SettleDuration = 0.1f;
    private LDY_TurnManager _turns;
    private LDY_ActionPointManager _points;
    private LDY_StageDirector _stage;
    private bool _eligible;
    private bool _settled;
    private bool _paid;
    private bool _cancelled;
    private float _flightDuration;
    private float _interval;
    private AnimationCurve _collectionEase;
    private float _coinRadius;
    private float _coinHalfThickness;
    private float _fallbackGround;
    private bool _burstFinished;
    private bool _previewOnly;
    private bool _externalPayout;
    private bool _collectionPending;
    private int _collectionStart;
    private int _collectionCount;
    private float _previewRestTime;
    private readonly List<Coin> _coins = new();

    private sealed class Coin
    {
        public Transform Visual;
        public Vector3 Scale;
        public DLJ_CostCase Case;
        public DLJ_CostCoinSlot Slot;
        public DLJ_CostAnimation Entrance;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public float Ground;
        public float Restitution;
        public float RestYaw;
        public float SettleTime;
        public Quaternion SettleRotation;
        public int Bounces;
        public bool Settling;
        public bool Resting;
    }

    public void Initialize(int amount, LDY_TurnManager turns, LDY_ActionPointManager points,
        Mesh mesh, Material material, float diameter, float radius, float burstDuration,
        float flightDuration, float interval, float fallbackGround = float.NaN, bool previewOnly = false,
        AnimationCurve collectionEase = null, bool externalPayout = false)
    {
        Initialize(amount, turns, points, mesh, material, material, diameter, radius, burstDuration,
            flightDuration, interval, fallbackGround, previewOnly, collectionEase, externalPayout);
    }

    public void Initialize(int amount, LDY_TurnManager turns, LDY_ActionPointManager points,
        Mesh mesh, Material coinMaterial, Material burstMaterial, float diameter, float radius,
        float burstDuration, float flightDuration, float interval, float fallbackGround = float.NaN,
        bool previewOnly = false, AnimationCurve collectionEase = null, bool externalPayout = false)
    {
        _previewOnly = previewOnly;
        _externalPayout = externalPayout;
        _turns = turns;
        _points = points;
        _flightDuration = Mathf.Max(0.05f, flightDuration);
        _interval = Mathf.Max(0f, interval);
        _collectionEase = collectionEase;
        _coinRadius = Mathf.Max(0.005f, diameter * 0.5f);
        _coinHalfThickness = diameter * 0.08f;
        if (mesh != null)
        {
            Vector3 size = mesh.bounds.size;
            _coinHalfThickness = diameter * size.y / Mathf.Max(0.001f, size.x, size.y, size.z) * 0.5f;
        }
        _fallbackGround = float.IsNaN(fallbackGround)
            ? transform.position.y - Mathf.Max(0.2f, diameter) : fallbackGround;
        if (!_previewOnly)
        {
            _stage = FindFirstObjectByType<LDY_StageDirector>();
            if (_stage != null) _stage.OnStageLoaded += CancelStage;
            if (_turns != null && !_externalPayout) _turns.OnTurnChanged += OnTurnChanged;
        }
        // 게임의 UnityEngine.Random 상태를 바꾸지 않는 연출 전용 난수.
        var random = new System.Random(GetInstanceID());
        float phase = (float)random.NextDouble() * Mathf.PI * 2f;
        for (int i = 0; i < amount; i++)
        {
            Transform visual = DLJ_PiggyBankEffect.CreateCoin(transform, mesh, coinMaterial, diameter);
            visual.position = transform.position;
            float angle = phase + i * 2.399963f + (float)random.NextDouble() * 0.5f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float fallTime = Mathf.Sqrt(2f * Mathf.Max(0.1f, transform.position.y - _fallbackGround) / CoinGravity);
            float speed = Mathf.Max(0.1f, radius) / fallTime * Mathf.Lerp(0.7f, 1.2f, (float)random.NextDouble());
            visual.rotation = Quaternion.Euler((float)random.NextDouble() * 180f,
                (float)random.NextDouble() * 360f, (float)random.NextDouble() * 180f);
            Vector3 spinAxis = new Vector3((float)random.NextDouble() - 0.5f,
                (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f).normalized;
            _coins.Add(new Coin
            {
                Visual = visual, Scale = visual.localScale,
                Velocity = direction * speed + Vector3.down * Mathf.Lerp(0.3f, 0.7f, (float)random.NextDouble()),
                AngularVelocity = spinAxis * Mathf.Lerp(550f, 1000f, (float)random.NextDouble()),
                Ground = FindGround(visual.position),
                Restitution = Mathf.Lerp(0.12f, 0.2f, (float)random.NextDouble()),
                RestYaw = (float)random.NextDouble() * 360f
            });
        }
        if (_externalPayout)
            _burstFinished = true;
        else
            StartCoroutine(Burst(burstMaterial, radius, Mathf.Max(0.05f, burstDuration)));
    }

    /// <summary>계약 환급이 실제로 지급한 코인만 케이스로 회수한다.</summary>
    public void OnRefundPaid(int firstSlot, int gained)
    {
        if (_cancelled || _paid || !_externalPayout) return;
        _collectionStart = firstSlot;
        _collectionCount = Mathf.Clamp(gained, 0, _coins.Count);
        _collectionPending = true;
    }

    private IEnumerator Burst(Material material, float radius, float duration)
    {
        var shards = new List<Transform>();
        for (int i = 0; i < 18; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "DLJ_PigGoldShard";
            shard.layer = 2;
            Collider collider = shard.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            shard.transform.SetParent(transform, false);
            if (material != null) shard.GetComponent<Renderer>().sharedMaterial = material;
            shards.Add(shard.transform);
        }
        GameObject flashObject = new GameObject("DLJ_PigBurstLight", typeof(Light));
        flashObject.transform.SetParent(transform, false);
        Light flash = flashObject.GetComponent<Light>();
        flash.color = new Color(1f, 0.65f, 0.1f);
        flash.range = Mathf.Max(2f, radius * 6f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += _previewOnly ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flash.intensity = 6f * (1f - t);
            for (int i = 0; i < shards.Count; i++)
            {
                float angle = i * 2.399963f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0.3f + (i % 4) * 0.2f, Mathf.Sin(angle));
                shards[i].localPosition = direction * (radius * 2.2f * t) + Vector3.down * (t * t * radius);
                shards[i].localRotation = Quaternion.Euler(i * 27f + t * 460f, t * 300f, i * 19f);
                shards[i].localScale = new Vector3(0.08f, 0.12f, 0.035f) * (1f - t);
            }
            yield return null;
        }
        foreach (Transform shard in shards) Destroy(shard.gameObject);
        Destroy(flashObject);
        _burstFinished = true;
        if (_coins.Count == 0) Destroy(gameObject);
    }

    private void OnTurnChanged(LDY_Team team)
    {
        if (team == LDY_Team.Player) _eligible = true;
        else if (_paid) Cancel();
    }

    private void Update()
    {
        if (_cancelled) return;
        if (_previewOnly)
        {
            if (!_settled) SimulateCoins(Time.unscaledDeltaTime);
            else
            {
                _previewRestTime += Time.unscaledDeltaTime;
                if (_previewRestTime >= 1.1f) Destroy(gameObject);
            }
            return;
        }
        if (_points == null || _turns == null) { Cancel(); return; }
        if (!_paid && !_settled) SimulateCoins(Time.deltaTime);
        if (_externalPayout)
        {
            if (_collectionPending && _settled)
                BeginCollection(_collectionStart, _collectionCount);
            return;
        }
        // Update에서 처리해 같은 턴 이벤트의 AP 리셋/계약 환급 및 스테이지 교체가 끝난 뒤 계산.
        if (!_paid && _settled && _eligible && _turns.CurrentTurn == LDY_Team.Player)
            Pay();
    }

    private void SimulateCoins(float deltaTime)
    {
        // 짧은 서브스텝으로 얇은 동전이 바닥을 관통하거나 프레임마다 튀는 양이 달라지는 것을 줄임.
        float remaining = Mathf.Min(deltaTime, 0.1f);
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            remaining -= dt;
            foreach (Coin coin in _coins)
            {
                if (coin.Resting) continue;
                Vector3 position = coin.Visual.position;
                if (coin.Settling)
                {
                    coin.SettleTime += dt;
                    float t = Mathf.Clamp01(coin.SettleTime / SettleDuration);
                    Quaternion flat = Quaternion.Euler(0f, coin.RestYaw, 0f);
                    // 마지막 접촉 후 짧게 기울어지며 눕고 정지. 공중 부유/상시 회전 없음.
                    float wobble = Mathf.Sin(t * Mathf.PI * 3f) * 3f * (1f - t) * (1f - t);
                    coin.Visual.rotation = Quaternion.Slerp(coin.SettleRotation, flat, 1f - Mathf.Pow(1f - t, 3f)) *
                        Quaternion.AngleAxis(wobble, Vector3.right);
                    coin.Velocity = Vector3.MoveTowards(coin.Velocity, Vector3.zero, 12f * dt);
                    position += coin.Velocity * dt;
                    position.y = coin.Ground + SupportHeight(coin.Visual) + 0.002f;
                    coin.Resting = t >= 1f;
                }
                else
                {
                    coin.Velocity += Vector3.down * (CoinGravity * dt);
                    position += coin.Velocity * dt;
                    coin.Visual.Rotate(coin.AngularVelocity * dt, Space.World);
                    float contactY = coin.Ground + SupportHeight(coin.Visual) + 0.002f;
                    if (position.y <= contactY)
                    {
                        position.y = contactY;
                        float impact = Mathf.Max(0f, -coin.Velocity.y);
                        coin.Velocity = new Vector3(coin.Velocity.x * 0.45f,
                            impact * coin.Restitution, coin.Velocity.z * 0.45f);
                        coin.AngularVelocity *= 0.4f;
                        coin.Bounces++;
                        if (impact < 1f || coin.Bounces >= 2)
                        {
                            coin.Settling = true;
                            coin.SettleRotation = coin.Visual.rotation;
                            coin.Velocity.y = 0f;
                        }
                    }
                }
                coin.Visual.position = position;
            }
        }
        _settled = _burstFinished && _coins.TrueForAll(coin => coin.Resting);
    }

    private float SupportHeight(Transform coin)
    {
        float normalY = Mathf.Abs(Vector3.Dot(coin.up, Vector3.up));
        return _coinHalfThickness * normalY + _coinRadius * Mathf.Sqrt(Mathf.Max(0f, 1f - normalY * normalY));
    }

    private float FindGround(Vector3 position)
    {
        float ground = _fallbackGround;
        float highest = float.NegativeInfinity;
        // 기물의 몸체/트리거는 제외하고 돼지 아래 실제 보드 표면을 사용.
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 0.1f, Vector3.down,
            Mathf.Max(0.5f, position.y - _fallbackGround + 0.5f), Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.scene != gameObject.scene || hit.normal.y < 0.7f ||
                hit.point.y > position.y || hit.collider.GetComponentInParent<LDY_Animal>() != null) continue;
            if (hit.point.y <= highest) continue;
            highest = hit.point.y;
            ground = highest;
        }
        return ground;
    }

    private void Pay()
    {
        int firstSlot = _points.Current;
        int requested = Mathf.Min(_coins.Count, Mathf.Max(0, 10 - firstSlot));
        int gained = _points.AddActionPoints(requested);
        BeginCollection(firstSlot, gained);
    }

    private void BeginCollection(int firstSlot, int gained)
    {
        _paid = true;
        _collectionPending = false;
        DLJ_CostSystem costSystem = FindFirstObjectByType<DLJ_CostSystem>();
        for (int i = 0; i < gained; i++)
        {
            Coin coin = _coins[i];
            if (costSystem != null)
                costSystem.TryReserveWorldArrival(firstSlot + i, out coin.Case, out coin.Slot, out coin.Entrance);
        }
        StartCoroutine(Collect(gained));
    }

    private IEnumerator Collect(int gained)
    {
        // 초과분은 케이스를 만들지 않고 폭발 자리에 남은 채 축소 소멸.
        for (int i = gained; i < _coins.Count; i++) StartCoroutine(Vanish(_coins[i]));
        for (int i = 0; i < gained; i++)
        {
            Coin coin = _coins[i];
            if (coin.Case == null || !coin.Case.HasWorldArrival(coin.Slot))
            {
                yield return Vanish(coin);
                continue;
            }
            // Start 이전에 생성된 케이스도 시작할 기회를 준다.
            yield return null;
            float deadline = Time.time + 5f;
            while (coin.Entrance != null && coin.Entrance.IsPlaying && Time.time < deadline)
                yield return null;
            Vector3 start = coin.Visual.position;
            Quaternion rotation = coin.Visual.rotation;
            float elapsed = 0f;
            while (elapsed < _flightDuration && coin.Case != null && coin.Case.HasWorldArrival(coin.Slot))
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _flightDuration);
                float eased = t >= 1f ? 1f : _collectionEase != null && _collectionEase.length > 0
                    ? Mathf.Clamp01(_collectionEase.Evaluate(t)) : Mathf.SmoothStep(0f, 1f, t);
                Vector3 end = coin.Slot.GetRestWorldPosition();
                coin.Visual.position = Vector3.Lerp(start, end, eased) + Vector3.up * (Mathf.Sin(Mathf.PI * eased) * _coinRadius);
                Quaternion targetRotation = coin.Slot.HomeParent != null
                    ? coin.Slot.HomeParent.rotation * coin.Slot.RestLocalRotation : coin.Slot.RestLocalRotation;
                coin.Visual.rotation = Quaternion.Slerp(rotation, targetRotation, eased);
                Vector3 targetScale = coin.Slot.HomeParent != null
                    ? Vector3.Scale(coin.Slot.HomeParent.lossyScale, coin.Slot.RestLocalScale) : coin.Slot.RestLocalScale;
                coin.Visual.localScale = Vector3.Lerp(coin.Scale, targetScale, eased);
                yield return null;
            }
            if (coin.Case != null) coin.Case.CompleteWorldArrival(coin.Slot);
            coin.Visual.gameObject.SetActive(false);
            if (_interval > 0f) yield return new WaitForSeconds(_interval);
        }
        yield return new WaitForSeconds(0.3f);
        Destroy(gameObject);
    }

    private static IEnumerator Vanish(Coin coin)
    {
        float elapsed = 0f;
        while (elapsed < 0.25f && coin.Visual != null)
        {
            elapsed += Time.deltaTime;
            coin.Visual.localScale = coin.Scale * (1f - Mathf.Clamp01(elapsed / 0.25f));
            yield return null;
        }
        if (coin.Visual != null) coin.Visual.gameObject.SetActive(false);
    }

    private void CancelStage(LDY_StageSO stage) => Cancel();

    private void Cancel()
    {
        _cancelled = true;
        StopAllCoroutines();
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        _cancelled = true;
        StopAllCoroutines();
        if (_turns != null) _turns.OnTurnChanged -= OnTurnChanged;
        if (_stage != null) _stage.OnStageLoaded -= CancelStage;
        foreach (Coin coin in _coins)
            if (coin.Case != null) coin.Case.CompleteWorldArrival(coin.Slot);
    }
}
