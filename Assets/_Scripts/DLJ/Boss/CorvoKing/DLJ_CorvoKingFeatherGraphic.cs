using UnityEngine;
using UnityEngine.UI;

/// <summary>커스텀 Sprite 또는 절차적 깃털을 그리는 단일 화면 UI 메시.</summary>
public sealed class DLJ_CorvoKingFeatherGraphic : MaskableGraphic
{
    public const float CoveredAt = 0.48f;
    private struct Feather
    {
        public Vector2 position;
        public float length, angle, spin, phase;
        public Color tint;
    }

    private Feather[] flock;
    private float progress;
    private float travelDistance;
    private float flutterTime;
    private float windStrength, flutterSpeed, tumbleAngle, flipAmount;
    private Sprite featherSprite;
    private Vector2[] spriteVertices, spriteUV;
    private ushort[] spriteTriangles;
    private Color imageTint;
    private float imageRotation;
    private Image coverImage;

    public override Texture mainTexture => featherSprite != null ? featherSprite.texture : base.mainTexture;

    public void Initialize(int count, Sprite sprite, Color tint, float rotation, Image cover)
    {
        featherSprite = sprite;
        imageTint = tint;
        imageRotation = rotation;
        coverImage = cover;
        if (sprite != null)
        {
            // 실제 Sprite 메시와 UV를 사용해 잘린 이미지와 아틀라스도 그대로 표시.
            spriteVertices = sprite.vertices;
            spriteUV = sprite.uv;
            spriteTriangles = sprite.triangles;
            Vector2 center = (sprite.rect.size * 0.5f - sprite.pivot) / sprite.pixelsPerUnit;
            float longestSide = Mathf.Max(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
            for (int i = 0; i < spriteVertices.Length; i++)
                spriteVertices[i] = (spriteVertices[i] - center) / Mathf.Max(0.0001f, longestSide);
            // 복잡한 커스텀 메시도 Unity UI의 정점 한도를 넘지 않도록 제한.
            count = Mathf.Min(count, 64000 / Mathf.Max(1, spriteVertices.Length));
        }
        SetMaterialDirty();
        // 전투에서 사용하는 UnityEngine.Random의 상태를 건드리지 않음.
        var random = new System.Random(7319);
        travelDistance = flutterTime = progress = 0f;
        flock = new Feather[count];
        for (int i = 0; i < count; i++)
        {
            float shade = (float)random.NextDouble();
            flock[i] = new Feather
            {
                position = new Vector2((float)random.NextDouble() * 1.8f - 0.9f,
                    (float)random.NextDouble() * 1.8f - 0.9f),
                length = Mathf.Lerp(0.12f, 0.36f, (float)random.NextDouble()),
                angle = Mathf.Lerp(195f, 255f, (float)random.NextDouble()),
                spin = Mathf.Lerp(-45f, 45f, (float)random.NextDouble()),
                phase = (float)random.NextDouble() * Mathf.PI * 2f,
                tint = Color.Lerp(new Color(0.012f, 0.014f, 0.022f), new Color(0.075f, 0.065f, 0.09f), shade)
            };
        }
        SetVerticesDirty();
    }

    public void SetWind(float strength, float speed, float angle, float flip)
    {
        windStrength = Mathf.Max(0f, strength);
        flutterSpeed = Mathf.Max(0f, speed);
        tumbleAngle = Mathf.Max(0f, angle);
        flipAmount = Mathf.Clamp01(flip);
    }

    public void SetFrame(float value, float deltaTime, float moveSpeed)
    {
        progress = Mathf.Clamp01(value);
        flutterTime += Mathf.Max(0f, deltaTime) * flutterSpeed;
        // 화면 높이 단위로 거리를 누적해 속도를 바꿔도 위치가 튀지 않게 처리.
        travelDistance += Mathf.Max(0f, moveSpeed) / 1080f * Mathf.Max(0f, deltaTime);
        if (coverImage != null)
        {
            float opacity = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.20f, CoveredAt, progress))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 0.85f, progress)));
            coverImage.color = new Color(0.009f, 0.008f, 0.015f, opacity);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (flock == null || progress <= 0f || progress >= 1f) return;
        Rect rect = rectTransform.rect;
        float size = Mathf.Max(rect.width, rect.height);
        // 화면 밖에서 순환시켜 빠르게 흘러도 연출 시간 내내 깃털 수를 유지.
        // 충분한 여백을 두므로 순환 순간은 화면에 보이지 않음.
        float margin = size * 0.45f;
        float fieldWidth = rect.width + margin * 2f;
        float fieldHeight = rect.height + margin * 2f;
        float travel = travelDistance * rect.height * 0.70710678f;
        foreach (Feather feather in flock)
        {
            // 크기/위상마다 다른 주기로 움직여 깃털 무리가 한 덩어리처럼 흔들리지 않음.
            float frequency = Mathf.Lerp(1.3f, 0.7f, Mathf.InverseLerp(0.12f, 0.36f, feather.length));
            float phase = flutterTime * Mathf.PI * 2f * frequency + feather.phase;
            float sway = Mathf.Sin(phase);
            float gust = Mathf.Sin(phase * 0.47f + feather.phase * 1.7f);
            float amplitude = windStrength * rect.height / 1080f;
            Vector2 windOffset = new Vector2(-0.70710678f, 0.70710678f)
                * (amplitude * (sway * 0.65f + gust * 0.35f));
            windOffset += new Vector2(0.70710678f, 0.70710678f)
                * (amplitude * 0.3f * Mathf.Cos(phase * 0.71f + feather.phase));
            Vector2 seed = feather.position / 1.8f + Vector2.one * 0.5f;
            Vector2 center = new Vector2(
                rect.xMin - margin + Mathf.Repeat(seed.x * fieldWidth - travel + windOffset.x, fieldWidth),
                rect.yMin - margin + Mathf.Repeat(seed.y * fieldHeight - travel + windOffset.y, fieldHeight));
            float length = size * feather.length;
            if (center.x < rect.xMin - length || center.x > rect.xMax + length
                || center.y < rect.yMin - length || center.y > rect.yMax + length) continue;
            float turnDirection = feather.spin < 0f ? -1f : 1f;
            float angle = (feather.angle + tumbleAngle * turnDirection
                * (0.65f * Mathf.Sin(phase + 0.6f) + 0.35f * gust)) * Mathf.Deg2Rad;
            // 폭을 양수로 유지해 뒤집힐 때 뒷면이 잘리거나 순간적으로 사라지는 현상을 방지.
            float widthScale = Mathf.Lerp(1f, Mathf.Max(0.12f, Mathf.Abs(Mathf.Cos(phase * 0.63f))), flipAmount);
            Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-axis.y, axis.x);
            Color tint = feather.tint;
            // 등장/퇴장 물결은 연출 시간, 개별 깃털 이동은 별도의 누적 거리로 제어.
            float sweep = Mathf.Clamp01(((rect.xMax - center.x) / Mathf.Max(1f, rect.width)
                + (rect.yMax - center.y) / Mathf.Max(1f, rect.height)) * 0.5f);
            float arrival = sweep * 0.28f;
            float departure = 0.65f + sweep * 0.22f;
            tint.a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(arrival, arrival + 0.12f, progress))
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(departure, departure + 0.12f, progress)));
            if (featherSprite != null)
                DrawSprite(vh, center, angle, length, widthScale, tint.a);
            else DrawFeather(vh, center, axis, side * widthScale, length, tint);
        }
    }

    private void DrawSprite(VertexHelper vh, Vector2 center, float angle, float length, float widthScale, float opacity)
    {
        // 이미지 위쪽(+Y)이 이동 방향을 바라보도록 회전. 원본 비율/색상/알파 유지.
        float rotation = angle - Mathf.PI * 0.5f + imageRotation * Mathf.Deg2Rad;
        Vector2 right = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
        Vector2 up = new Vector2(-right.y, right.x);
        Color tint = imageTint;
        tint.a *= opacity;
        int start = vh.currentVertCount;
        for (int i = 0; i < spriteVertices.Length; i++)
        {
            Vector2 vertex = spriteVertices[i];
            vh.AddVert(center + (right * vertex.x * widthScale + up * vertex.y) * length, tint, spriteUV[i]);
        }
        for (int i = 0; i < spriteTriangles.Length; i += 3)
            vh.AddTriangle(start + spriteTriangles[i], start + spriteTriangles[i + 1], start + spriteTriangles[i + 2]);
    }

    private static void DrawFeather(VertexHelper vh, Vector2 center, Vector2 axis, Vector2 side, float length, Color tint)
    {
        const int segments = 14;
        // 비대칭 깃판과 작은 갈라짐. 끝으로 갈수록 휘어지고 가늘어짐.
        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1f) / segments;
            Vector2 p0 = Spine(center, axis, side, length, t0);
            Vector2 p1 = Spine(center, axis, side, length, t1);
            float w0 = Width(t0) * length;
            float w1 = Width(t1) * length;
            AddQuad(vh, p0 - side * w0 * 0.72f, p1 - side * w1 * 0.72f,
                p1 + side * w1, p0 + side * w0, tint);
            if (i < 2 || i > 12) continue;
            Color barb = new Color(tint.r + 0.025f, tint.g + 0.023f, tint.b + 0.03f, tint.a * 0.65f);
            Vector2 stem = Spine(center, axis, side, length, t0 - 0.07f);
            AddLine(vh, stem, p0 + side * w0 * 0.94f, length * 0.0017f, barb);
            AddLine(vh, stem, p0 - side * w0 * 0.67f, length * 0.0017f, barb);
        }
        Color shaft = new Color(tint.r + 0.055f, tint.g + 0.045f, tint.b + 0.06f, tint.a);
        for (int i = 0; i < 7; i++)
            AddLine(vh, Spine(center, axis, side, length, i / 7f),
                Spine(center, axis, side, length, (i + 1f) / 7f), length * 0.003f * (1f - i / 8f), shaft);
    }

    private static Vector2 Spine(Vector2 center, Vector2 axis, Vector2 side, float length, float t)
        => center + axis * ((t - 0.5f) * length) + side * (t * t * length * 0.08f);

    private static float Width(float t)
    {
        if (t <= 0.10f || t >= 1f) return 0f;
        float profile = Mathf.Pow(Mathf.Sin((t - 0.10f) / 0.9f * Mathf.PI), 0.75f);
        return profile * 0.16f * (1f - t * 0.35f) * (1f - 0.10f * Mathf.Abs(Mathf.Sin(t * 71f)));
    }

    private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 direction = (b - a).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * width;
        AddQuad(vh, a - normal, a + normal, b + normal, b - normal, tint);
    }

    private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, tint, Vector2.zero);
        vh.AddVert(b, tint, Vector2.zero);
        vh.AddVert(c, tint, Vector2.zero);
        vh.AddVert(d, tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
