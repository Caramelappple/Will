using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))] // ★ 위치 잡힐 때까지 잔상 방지용
public class LDY_MapPlayerToken : MonoBehaviour
{
    private RectTransform rt;
    private CanvasGroup canvasGroup;
    private Coroutine moveCoroutine;

    [Header("Scene Settings")]
    [Tooltip("플레이어 토큰이 존재해야 하는 맵 씬의 이름")]
    [SerializeField] private string mapSceneName = "KTH_StageScene";

    public bool IsMoving => moveCoroutine != null;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // ★ 켜지는 순간에는 숨김 (위치가 정확히 맞춰진 후 보여줌)
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        EnsureCorrectParent();
        BringToFront();
    }

    private bool IsCurrentSceneMap()
    {
        if (string.IsNullOrEmpty(mapSceneName)) return true;
        return SceneManager.GetActiveScene().name.Equals(mapSceneName, StringComparison.OrdinalIgnoreCase);
    }

    public void EnsureCorrectParent()
    {
        // 부모가 이미 올바르게 잡혀있다면 패스
        if (transform.parent != null && transform.parent.gameObject.activeInHierarchy) return;

        // 1. 현재 활성화된 씬의 LDY_MapUIController를 찾아 안전하게 nodeContainer 참조
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

        // 2. Fallback: 현재 activeScene에 속한 NodeContainer만 탐색 (DontDestroyOnLoad 구역 감지 방지)
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

        // 3. 최후의 수단: 씬의 최상위 Canvas 사용
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && canvas.gameObject.scene == activeScene)
        {
            transform.SetParent(canvas.transform, false);
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

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(Co_BringToFrontEndFrame());
        }
        else
        {
            BringToFront();
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

        // ★ 프레임 이동 및 위치 계산이 끝난 뒤에 깔끔하게 노출
        if (canvasGroup != null) canvasGroup.alpha = 1f;
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