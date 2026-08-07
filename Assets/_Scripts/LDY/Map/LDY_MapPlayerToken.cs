using System;
using System.Collections;
using UnityEngine;

public class LDY_MapPlayerToken : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 800f; // 맵 좌표 단위/초
    [SerializeField] private float minMoveDuration = 0.3f;
    [SerializeField] private float maxMoveDuration = 1.2f;

    private RectTransform rt;
    private Coroutine moving;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public void SetPosition(Vector2 mapPosition)
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = mapPosition;
    }

    public void MoveTo(Vector2 targetMapPosition, Action onComplete)
    {
        if (rt == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (moving != null) StopCoroutine(moving);
        moving = StartCoroutine(MoveRoutine(targetMapPosition, onComplete));
    }

    private IEnumerator MoveRoutine(Vector2 target, Action onComplete)
    {
        Vector2 start = rt.anchoredPosition;
        float distance = Vector2.Distance(start, target);
        float duration = Mathf.Clamp(distance / Mathf.Max(moveSpeed, 1f), minMoveDuration, maxMoveDuration);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f); // ease-out
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }

        rt.anchoredPosition = target;
        moving = null;
        onComplete?.Invoke();
    }

    public Vector2 GetScreenUV()
    {
        if (rt == null) return new Vector2(0.5f, 0.5f);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return new Vector2(0.5f, 0.5f);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector3 localPos = canvasRect.InverseTransformPoint(rt.position);
        Rect rect = canvasRect.rect;

        Vector2 uv = new Vector2(
            (localPos.x - rect.xMin) / rect.width,
            (localPos.y - rect.yMin) / rect.height);

        return uv;
    }
}