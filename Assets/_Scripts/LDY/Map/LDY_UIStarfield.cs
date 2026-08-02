using UnityEngine;
using UnityEngine.UI;

// 배경에 붙이는 별가루 애니메이션 - 순수 UI Image로 구현해서 Screen Space Overlay Canvas에서도
// 항상 보임 (ParticleSystem 방식은 카메라/렌더모드 설정이 맞아야만 보이는 문제가 있어 대체함)
// 아주 느린 드리프트(패럴랙스 느낌)와 Sin 기반 밝기 트윙클을 매 프레임 직접 계산 - 가볍게 동작
public class LDY_UIStarfield : MonoBehaviour
{
    [SerializeField] private LDY_MapTheme theme;
    [SerializeField] private RectTransform container;

    [Header("개수 / 크기 (Theme이 있으면 개수는 Theme.maxStars 우선)")]
    [SerializeField] private int starCount = 60;
    [SerializeField] private Vector2 sizeRange = new Vector2(2f, 5f);

    [Header("드리프트 / 트윙클")]
    [SerializeField] private float driftSpeed = 3f;
    [SerializeField] private Vector2 twinkleSpeedRange = new Vector2(0.5f, 1.5f);
    [SerializeField] private Vector2 alphaRange = new Vector2(0.15f, 0.9f);

    private RectTransform[] stars;
    private Image[] images;
    private Vector2[] velocity;
    private float[] phase;
    private float[] speed;

    private void Start()
    {
        if (container == null) container = (RectTransform)transform;

        int count = theme != null ? theme.maxStars : starCount;
        float drift = theme != null ? theme.starDriftSpeed : driftSpeed;

        stars = new RectTransform[count];
        images = new Image[count];
        velocity = new Vector2[count];
        phase = new float[count];
        speed = new float[count];

        Rect rect = container.rect;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Star", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(container, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float size = Random.Range(sizeRange.x, sizeRange.y);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax));

            Image img = go.GetComponent<Image>();
            img.sprite = LDY_ProceduralSprite.Sparkle;
            img.raycastTarget = false;
            img.color = theme != null ? Color.Lerp(theme.starColorA, theme.starColorB, Random.value) : Color.white;

            float driftAngle = Random.Range(0f, Mathf.PI * 2f);
            float driftMag = drift * Random.Range(0.3f, 1f);

            stars[i] = rt;
            images[i] = img;
            velocity[i] = new Vector2(Mathf.Cos(driftAngle), Mathf.Sin(driftAngle)) * driftMag;
            phase[i] = Random.Range(0f, Mathf.PI * 2f);
            speed[i] = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y);
        }
    }

    private void Update()
    {
        if (stars == null) return;

        Rect rect = container.rect;
        float t = Time.time;

        for (int i = 0; i < stars.Length; i++)
        {
            RectTransform rt = stars[i];
            Vector2 pos = rt.anchoredPosition + velocity[i] * Time.deltaTime;

            // 화면 가장자리를 넘어가면 반대편에서 다시 등장 (끊김 없는 흐름)
            if (pos.x < rect.xMin) pos.x = rect.xMax;
            else if (pos.x > rect.xMax) pos.x = rect.xMin;
            if (pos.y < rect.yMin) pos.y = rect.yMax;
            else if (pos.y > rect.yMax) pos.y = rect.yMin;

            rt.anchoredPosition = pos;

            float wave = (Mathf.Sin(t * speed[i] + phase[i]) + 1f) * 0.5f;
            Color c = images[i].color;
            c.a = Mathf.Lerp(alphaRange.x, alphaRange.y, wave);
            images[i].color = c;
        }
    }
}
