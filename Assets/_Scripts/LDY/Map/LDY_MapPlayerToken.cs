using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LDY_MapPlayerToken : MonoBehaviour
{
    private RectTransform rt;
    private Coroutine moveCoroutine;

    // ★ 이동 중인지 여부를 확인할 수 있는 플래그 추가
    public bool IsMoving => moveCoroutine != null;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        EnsureCorrectParent();
        BringToFront();
    }

    public void EnsureCorrectParent()
    {
        if (transform.parent == null || transform.parent.GetComponent<RectTransform>() == null)
        {
            GameObject nodeContainer = GameObject.Find("NodeContainer");
            if (nodeContainer != null)
            {
                transform.SetParent(nodeContainer.transform, false);
            }
            else
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform, false);
                }
            }
        }
    }

    public void BringToFront()
    {
        EnsureCorrectParent();
        transform.SetAsLastSibling();
    }

    public void SetPosition(Vector2 mapPosition)
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        EnsureCorrectParent();

        if (rt != null)
        {
            rt.anchoredPosition = mapPosition;
        }

        StartCoroutine(Co_BringToFrontEndFrame());
    }

    public void MoveTo(Vector2 targetMapPosition, Action onComplete, float duration = 0.8f)
    {
        List<Vector2> path = new List<Vector2> { targetMapPosition };
        MoveAlongPath(path, onComplete, duration);
    }

    public void MoveAlongPath(List<Vector2> pathPoints, Action onComplete, float totalDuration = 0.8f)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (pathPoints == null || pathPoints.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        moveCoroutine = StartCoroutine(Co_MoveAlongPath(pathPoints, totalDuration, onComplete));
    }

    private IEnumerator Co_MoveAlongPath(List<Vector2> pathPoints, float totalDuration, Action onComplete)
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        EnsureCorrectParent();
        BringToFront();

        float durationPerSegment = totalDuration / pathPoints.Count;

        foreach (Vector2 targetPos in pathPoints)
        {
            Vector2 startPos = rt != null ? rt.anchoredPosition : targetPos;
            float elapsed = 0f;

            while (elapsed < durationPerSegment)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durationPerSegment);
                t = Mathf.SmoothStep(0f, 1f, t);

                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                }

                BringToFront();
                yield return null;
            }

            if (rt != null) rt.anchoredPosition = targetPos;
        }

        BringToFront();
        moveCoroutine = null; // ★ 이동 완료 후 코루틴 변수 초기화
        onComplete?.Invoke();
    }

    private IEnumerator Co_BringToFrontEndFrame()
    {
        yield return new WaitForEndOfFrame();
        BringToFront();
    }

    public Vector2 GetScreenUV()
    {
        if (rt == null) return new Vector2(0.5f, 0.5f);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return new Vector2(0.5f, 0.5f);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector3 localPos = canvasRect.InverseTransformPoint(rt.position);
        Rect rect = canvasRect.rect;

        return new Vector2(
            (localPos.x - rect.xMin) / rect.width,
            (localPos.y - rect.yMin) / rect.height
        );
    }
}