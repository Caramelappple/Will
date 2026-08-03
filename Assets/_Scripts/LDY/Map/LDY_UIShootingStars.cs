using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 배경 별가루 위로 이따금 유성이 대각선으로 스쳐 지나가는 연출. LDY_UIStarfield와 같은 Canvas 밑에 붙이면 됨
// (같은 오브젝트에 같이 붙여도 되고, 별도 오브젝트에 붙여도 됨)
public class LDY_UIShootingStars : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private Color color = Color.white;

    [Header("등장 빈도 (초, 이 범위에서 랜덤 대기 후 하나씩 생성)")]
    [SerializeField] private Vector2 intervalRange = new Vector2(3f, 8f);

    [Header("속도 / 길이 / 두께")]
    [SerializeField] private Vector2 speedRange = new Vector2(1200f, 1800f);
    [SerializeField] private Vector2 lengthRange = new Vector2(150f, 320f);
    [SerializeField] private float thickness = 4f;

    [Header("떨어지는 각도 (도, 0=오른쪽, -90=아래쪽)")]
    [SerializeField] private Vector2 angleRangeDeg = new Vector2(-60f, -30f);

    private void Start()
    {
        if (container == null) container = (RectTransform)transform;

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        Rect rect = container.rect;
        float angle = Random.Range(angleRangeDeg.x, angleRangeDeg.y);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        // 위쪽 또는 왼쪽 가장자리 바깥에서 랜덤 시작 (떨어지는 방향의 반대편)
        Vector2 start = Random.value < 0.5f
            ? new Vector2(Random.Range(rect.xMin, rect.xMax), rect.yMax + 60f)
            : new Vector2(rect.xMin - 60f, Random.Range(rect.center.y, rect.yMax));

        float length = Random.Range(lengthRange.x, lengthRange.y);
        float speed = Random.Range(speedRange.x, speedRange.y);

        GameObject go = new GameObject("ShootingStar", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(length, thickness);
        rt.anchoredPosition = start;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        Image img = go.GetComponent<Image>();
        img.sprite = LDY_ProceduralSprite.MeteorTrail;
        img.color = color;
        img.raycastTarget = false;

        StartCoroutine(MoveAndFade(go, rt, img, start, dir, speed, length));
    }

    private IEnumerator MoveAndFade(GameObject go, RectTransform rt, Image img, Vector2 start, Vector2 dir, float speed, float length)
    {
        Rect rect = container.rect;
        float travelLimit = Mathf.Max(rect.width, rect.height) + length + 200f;
        float traveled = 0f;

        const float fadeIn = 0.1f;
        float t = 0f;

        while (traveled < travelLimit)
        {
            float dt = Time.unscaledDeltaTime;
            traveled += speed * dt;
            rt.anchoredPosition = start + dir * traveled;

            t += dt;
            Color c = img.color;
            c.a = t < fadeIn ? t / fadeIn : 1f;
            img.color = c;

            yield return null;
        }

        Destroy(go);
    }
}
