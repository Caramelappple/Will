using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>날개 표면에서 떨어져 나온 깃털을 월드 공간에서 흩날림.</summary>
public sealed class DLJ_CorvoKingWingFeathers : MonoBehaviour
{
    private sealed class Feather
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 origin, velocity, swayAxis;
        public float age, lifetime, angle, spin, phase, scale, flutter;
    }

    private GameObject root;
    private Feather[] flock;
    private DLJ_CorvoKingWingMotion wing;
    private Sprite sprite;
    private Camera viewCamera;
    private System.Random random;
    private float lifetime, sizeRatio, speedRatio, flutterRatio, burstStrength, worldSpan;

    public void Play(DLJ_CorvoKingWingMotion source, Sprite image, int count,
        float life, float size, float speed, float flutter, float burst)
    {
        Clear();
        if (source == null || image == null)
        {
            Debug.LogWarning("날개 주변 깃털을 재생하려면 Wing Feather Sprite를 연결해 줘.", this);
            return;
        }
        wing = source;
        sprite = image;
        lifetime = Mathf.Max(0.1f, life);
        sizeRatio = Mathf.Max(0.01f, size);
        speedRatio = Mathf.Max(0f, speed);
        flutterRatio = Mathf.Max(0f, flutter);
        burstStrength = Mathf.Max(0f, burst);
        worldSpan = Mathf.Max(0.01f, wing.WorldHalfSpan);
        random = new System.Random(4927);
        viewCamera = Camera.main;
        root = new GameObject("DLJ_Wing_ScatteredFeathers") { hideFlags = HideFlags.DontSave };
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        flock = new Feather[Mathf.Clamp(count, 1, 128)];
        // 호출된 시점에 전량 방출. 좌우를 번갈아 뽑아 양쪽 날개에서 동시에 터짐.
        for (int i = 0; i < flock.Length; i++)
        {
            if (wing.TryGetFeatherOrigin(random.Next(), out Vector3 origin, out Vector3 outward,
                i % 2 == 0 ? -1 : 1))
                Spawn(i, origin, outward);
        }
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    private void LateUpdate()
    {
        if (root == null || flock == null) return;
        float dt = Time.unscaledDeltaTime;
        if (viewCamera == null) viewCamera = Camera.main;

        bool alive = false;
        foreach (Feather feather in flock)
        {
            if (feather == null || feather.age >= feather.lifetime) continue;
            feather.age += dt;
            if (feather.age >= feather.lifetime)
            {
                feather.renderer.enabled = false;
                continue;
            }
            alive = true;
            float age = feather.age;
            float progress = age / feather.lifetime;
            float span = worldSpan;
            // 한 번 떨어져 나온 깃털은 보스의 이동에 끌려가지 않고 그 자리에서 흩날림.
            feather.transform.position = feather.origin
                + feather.velocity * (1f - Mathf.Exp(-age * 1.3f)) / 1.3f
                // 처음 약 0.2초에 강한 힘으로 튀고 빠르게 감속한 뒤 기존 흩날림으로 이어짐.
                + feather.velocity * burstStrength * (1f - Mathf.Exp(-age * 8f)) / 8f
                + feather.swayAxis * ((Mathf.Sin(age * 7f + feather.phase) - Mathf.Sin(feather.phase)) * feather.flutter)
                - Vector3.up * (span * 0.18f * age * age);
            Quaternion facing = viewCamera != null ? viewCamera.transform.rotation : Quaternion.identity;
            feather.transform.rotation = facing * Quaternion.Euler(0f, 0f,
                feather.angle + feather.spin * age + Mathf.Sin(age * 5f + feather.phase) * 25f);
            float flip = Mathf.Lerp(0.2f, 1f, Mathf.Abs(Mathf.Cos(age * 4f + feather.phase)));
            feather.transform.localScale = new Vector3(feather.scale * flip, feather.scale, feather.scale);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, progress));
            feather.renderer.color = new Color(1f, 1f, 1f, alpha);
        }
        if (!alive) Clear();
    }

    private void Spawn(int index, Vector3 origin, Vector3 outward)
    {
        float span = worldSpan;
        var item = new GameObject("Wing Feather " + index) { layer = gameObject.layer };
        item.transform.SetParent(root.transform, false);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        Vector3 swayAxis = Vector3.Cross(Vector3.up, outward).normalized;
        if (swayAxis.sqrMagnitude < 0.1f) swayAxis = Vector3.forward;
        flock[index] = new Feather
        {
            transform = item.transform, renderer = renderer, origin = origin,
            velocity = (outward * Range(0.45f, 1f) + Vector3.up * Range(0.45f, 1.1f)
                + swayAxis * Range(-0.35f, 0.35f)) * (span * speedRatio),
            swayAxis = swayAxis, lifetime = lifetime * Range(0.8f, 1.2f),
            angle = Range(-180f, 180f), spin = Range(-160f, 160f), phase = Range(0f, Mathf.PI * 2f),
            scale = span * sizeRatio * Range(0.7f, 1.3f) / Mathf.Max(0.001f, sprite.bounds.size.y),
            flutter = span * flutterRatio * Range(0.6f, 1.2f)
        };
        Feather feather = flock[index];
        item.transform.position = origin;
        item.transform.rotation = (viewCamera != null ? viewCamera.transform.rotation : Quaternion.identity)
            * Quaternion.Euler(0f, 0f, feather.angle);
        item.transform.localScale = Vector3.one * feather.scale;
    }

    public void Clear()
    {
        if (root != null)
        {
            root.SetActive(false);
            Destroy(root);
        }
        root = null;
        flock = null;
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => Clear();
}
