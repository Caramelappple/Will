using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class LDY_MapPlayerToken : MonoBehaviour
{
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Canvas tokenCanvas; // ★ 토큰 전용 독립 렌더링 Canvas
    private Coroutine moveCoroutine;

    [Header("Scene Settings")]
    [Tooltip("플레이어 토큰이 존재해야 하는 맵 씬의 이름")]
    [SerializeField] private string mapSceneName = "KTH_StageScene";

    [Header("Sorting Settings")]
    [Tooltip("다른 노드 UI보다 항상 위에 그려지도록 설정할 Sorting Order")]
    [SerializeField] private int sortingOrder = 100; // ★ 노드보다 확연히 높은 값

    public bool IsMoving => moveCoroutine != null;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ★ Canvas 컴포넌트 자동 추가 및 Sorting Order 강제 설정
        tokenCanvas = GetComponent<Canvas>();
        if (tokenCanvas == null) tokenCanvas = gameObject.AddComponent<Canvas>();

        SetupCanvasSorting();
    }

    private void OnEnable()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        SetupCanvasSorting();
        EnsureCorrectParent();
        BringToFront();

        StartCoroutine(Co_EnsureBringToFrontOnEnable());
    }

    /// <summary>
    /// Hierarchy 순서와 관계없이 화면 맨 앞에 그려지도록 Canvas Sorting을 강제합니다.
    /// </summary>
    private void SetupCanvasSorting()
    {
        if (tokenCanvas == null) tokenCanvas = GetComponent<Canvas>();
        if (tokenCanvas != null)
        {
            tokenCanvas.overrideSorting = true;
            tokenCanvas.sortingOrder = sortingOrder; // 100으로 설정하여 노드보다 무조건 앞에 그림
        }
    }

    private bool IsCurrentSceneMap()
    {
        if (string.IsNullOrEmpty(mapSceneName)) return true;
        return SceneManager.GetActiveScene().name.Equals(mapSceneName, StringComparison.OrdinalIgnoreCase);
    }

    public void EnsureCorrectParent()
    {
        if (transform.parent != null && transform.parent.gameObject.activeInHierarchy) return;

        LDY_MapUIController uiController = FindFirstObjectByType<LDY_MapUIController>();
        if (uiController != null)
        {
            Transform containerTransform = uiController.transform.Find("NodeContainer");
            if (containerTransform != null)
            {
                transform.SetParent(containerTransform, false);
                return;
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            if (root.name == "MapCanvas" || root.name == "NodeContainer")
            {
                Transform foundNodeContainer = root.name == "NodeContainer" ? root.transform : root.transform.Find("NodeContainer");
                if (foundNodeContainer != null)
                {
                    transform.SetParent(foundNodeContainer, false);
                    return;
                }
            }
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && canvas.gameObject.scene == activeScene)
        {
            transform.SetParent(canvas.transform, false);
        }
    }

    public void BringToFront()
    {
        EnsureCorrectParent();
        SetupCanvasSorting();
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

        BringToFront();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(Co_BringToFrontEndFrame());
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
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
            BringToFront();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            onComplete?.Invoke();
            return;
        }

        if (!gameObject.activeInHierarchy)
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            if (rt != null && pathPoints.Count > 0)
            {
                rt.anchoredPosition = pathPoints[pathPoints.Count - 1];
            }
            BringToFront();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
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

        if (canvasGroup != null) canvasGroup.alpha = 1f;

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
        moveCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator Co_BringToFrontEndFrame()
    {
        yield return new WaitForEndOfFrame();
        BringToFront();

        yield return null;
        BringToFront();

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private IEnumerator Co_EnsureBringToFrontOnEnable()
    {
        yield return new WaitForEndOfFrame();
        BringToFront();
        yield return null;
        BringToFront();

        if (canvasGroup != null && moveCoroutine == null)
        {
            canvasGroup.alpha = 1f;
        }
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