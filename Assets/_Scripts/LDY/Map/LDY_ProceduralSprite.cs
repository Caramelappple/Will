using UnityEngine;

// 별 문양 / 얇은 테두리를 위한 최소 스프라이트를 코드로 생성해 외부 아트 의존을 없앰
// (흰색+알파로만 굽고 실제 색은 Image.color로 입힘 → 인스턴스당 텍스처 생성 없이 재사용)
public static class LDY_ProceduralSprite
{
    private static Sprite sparkleSprite;
    private static Sprite thinFrameSprite;
    private static Sprite softGlowSprite;
    private static Sprite ringSprite;
    private static Sprite softLineSprite;
    private static Sprite meteorTrailSprite;

    public static Sprite Sparkle => sparkleSprite != null ? sparkleSprite : (sparkleSprite = BuildSparkle());
    public static Sprite ThinFrame => thinFrameSprite != null ? thinFrameSprite : (thinFrameSprite = BuildFrame());
    public static Sprite SoftGlow => softGlowSprite != null ? softGlowSprite : (softGlowSprite = BuildSoftGlow());
    public static Sprite Ring => ringSprite != null ? ringSprite : (ringSprite = BuildRing());
    public static Sprite SoftLine => softLineSprite != null ? softLineSprite : (softLineSprite = BuildSoftLine());
    public static Sprite MeteorTrail => meteorTrailSprite != null ? meteorTrailSprite : (meteorTrailSprite = BuildMeteorTrail());

    private static Sprite BuildSparkle()
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - center.x) / center.x;
                float dy = Mathf.Abs(y + 0.5f - center.y) / center.y;

                float arm = Mathf.Max(1f - dx * 7f, 1f - dy * 7f);
                float core = 1f - Mathf.Clamp01((dx + dy) * 1.4f);
                float alpha = Mathf.Clamp01(Mathf.Max(arm, core * 0.7f));

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite BuildSoftGlow()
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float t = Mathf.Clamp01(1f - dist);

                // 밝은 중심(좁고 강하게) + 은은하게 퍼지는 꼬리(넓고 약하게)를 섞어서
                // "꽉 찬 원판"이 아니라 진짜 빛 번짐처럼 보이게 함
                float core = Mathf.Pow(t, 4.5f);
                float tail = Mathf.Pow(t, 1.4f) * 0.35f;
                float alpha = Mathf.Clamp01(Mathf.Max(core, tail));

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite BuildSoftLine()
    {
        const int width = 8;
        const int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        float center = height * 0.5f;
        for (int y = 0; y < height; y++)
        {
            float dist = Mathf.Abs(y + 0.5f - center) / center;
            float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 2.2f);
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Sprite BuildMeteorTrail()
    {
        const int width = 128;
        const int height = 16;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        float centerY = height * 0.5f;
        for (int x = 0; x < width; x++)
        {
            // 왼쪽(꼬리)은 투명, 오른쪽(머리)으로 갈수록 진해짐
            float lengthT = x / (float)(width - 1);
            float headAlpha = Mathf.Pow(lengthT, 1.6f);

            for (int y = 0; y < height; y++)
            {
                float dy = Mathf.Abs(y + 0.5f - centerY) / centerY;
                float widthFalloff = Mathf.Pow(Mathf.Clamp01(1f - dy), 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, headAlpha * widthFalloff));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Sprite BuildRing()
    {
        const int size = 64;
        const float outerR = 0.5f;
        const float innerR = 0.36f;
        const float mid = (outerR + innerR) * 0.5f;
        const float halfWidth = (outerR - innerR) * 0.5f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float alpha = Mathf.Clamp01(1f - Mathf.Abs(dist - mid) / halfWidth);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite BuildFrame()
    {
        const int size = 64;
        const int border = 3;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, onBorder ? 1f : 0f));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }
}
