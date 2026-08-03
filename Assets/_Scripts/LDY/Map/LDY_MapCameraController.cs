using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 맵 진입 연출: 전체 맵을 넓게 보여준 뒤 "현재 위치 + 갈 수 있는 곳"으로 자동 확대.
// 그 이후에는 드래그로 이동, 휠/스크롤로 확대·축소하는 자유 시점을 제공.
// 줌은 maxZoom(기본 1 = 맵 원래 크기)을 못 넘고, 팬도 맵 바운딩 박스 밖으로는 못 나가도록 항상 클램프됨.
// NodeContainer/LineContainer의 anchoredPosition과 localScale을 함께 움직여서 팬/줌을 구현 (동일하게 맞춰야 노드-선이 어긋나지 않음)
public class LDY_MapCameraController : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Header("References")]
    [SerializeField] private LDY_MapManager mapManager;
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private Canvas canvas;

    [Header("줌 범위")]
    [SerializeField] private float minZoom = 0.4f;
    [Tooltip("인트로가 끝난 뒤 사용자가 드래그/스크롤로 조작할 때 적용되는 상한 (맵 원래 크기 = 1)")]
    [SerializeField] private float maxZoom = 1f;
    [Tooltip("인트로 연출(오버뷰→현재 위치 확대) 계산에만 쓰이는 상한. 자연스럽게 보이도록 maxZoom보다 크게 둬도 됨")]
    [SerializeField] private float introMaxZoom = 3f;

    [Header("팬 범위")]
    [Tooltip("맵 바운딩 박스 바깥으로 이 값(px)만큼만 여유를 두고, 그 이상은 드래그해도 못 나가게 막음")]
    [SerializeField] private float panBoundsPadding = 150f;

    [Header("여백 (px, 확대 축소 계산 시 화면 가장자리에 남길 공간)")]
    [SerializeField] private float overviewPadding = 80f;
    [SerializeField] private float focusPadding = 220f;

    [Header("인트로 연출")]
    [Tooltip("꺼두면 오버뷰만 보여주고 자동으로 현재 위치까지 확대하지 않음 (그 뒤 자유 팬/줌은 항상 가능)")]
    [SerializeField] private bool autoZoomToFrontier = true;
    [SerializeField] private float overviewHoldTime = 1.0f;
    [SerializeField] private float zoomInDuration = 1.2f;

    [Header("입력")]
    [SerializeField] private float scrollZoomSpeed = 0.15f;

    private float currentZoom = 1f;
    private Vector2 currentPan = Vector2.zero;
    private Coroutine introRoutine;

    private bool hasBounds;
    private Vector2 boundsMin;
    private Vector2 boundsMax;

    private void Start()
    {
        // 씬 재로드 시 로컬(파괴 예정) MapManager보다 영속 인스턴스를 우선한다 - LDY_MapUIController와 동일한 이유
        if (LDY_MapManager.Instance != null) mapManager = LDY_MapManager.Instance;

        if (mapManager == null)
        {
            Debug.LogError("[LDY_MapCameraController] Map Manager를 찾을 수 없습니다. 인스펙터에 연결했는지 확인하세요.", this);
            return;
        }
        if (nodeContainer == null)
        {
            Debug.LogError("[LDY_MapCameraController] Node Container가 연결되지 않았습니다. 인스펙터에서 연결해주세요.", this);
            return;
        }
        if (mapManager.Nodes == null || mapManager.Nodes.Count == 0)
        {
            Debug.LogWarning("[LDY_MapCameraController] MapManager에 노드가 하나도 없습니다. 별자리 맵 에디터로 노드를 배치했는지 확인하세요.", this);
            return;
        }

        ComputeMapBounds();
        introRoutine = StartCoroutine(IntroRoutine());
    }

    private void ComputeMapBounds()
    {
        List<Vector2> all = AllNodePositions();
        if (all.Count == 0)
        {
            hasBounds = false;
            return;
        }

        Vector2 min = all[0];
        Vector2 max = all[0];
        foreach (Vector2 p in all)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        boundsMin = min - Vector2.one * panBoundsPadding;
        boundsMax = max + Vector2.one * panBoundsPadding;
        hasBounds = true;
    }

    private IEnumerator IntroRoutine()
    {
        List<Vector2> all = AllNodePositions();
        List<Vector2> frontier = FrontierNodePositions();

        (Vector2 overviewPan, float overviewZoom) = ComputeFit(all, overviewPadding, introMaxZoom);
        SetPanZoomDirect(overviewPan, overviewZoom);

        if (!autoZoomToFrontier)
        {
            introRoutine = null;
            yield break;
        }

        yield return new WaitForSeconds(overviewHoldTime);

        (Vector2 focusPan, float focusZoom) = ComputeFit(frontier, focusPadding, introMaxZoom);
        yield return AnimateTo(focusPan, focusZoom, zoomInDuration);

        introRoutine = null;
    }

    private void CancelIntro()
    {
        if (introRoutine == null) return;
        StopCoroutine(introRoutine);
        introRoutine = null;
    }

    private List<Vector2> AllNodePositions()
    {
        List<Vector2> list = new List<Vector2>();
        foreach (LDY_MapNode n in mapManager.Nodes) list.Add(n.position);
        return list;
    }

    private List<Vector2> FrontierNodePositions()
    {
        List<Vector2> list = new List<Vector2>();
        foreach (LDY_MapNode n in mapManager.Nodes)
            if (n.isUnlocked && !n.isCleared) list.Add(n.position);

        return list.Count > 0 ? list : AllNodePositions();
    }

    private Rect GetViewport()
    {
        return viewportRect != null ? viewportRect.rect : new Rect(-960f, -540f, 1920f, 1080f);
    }

    private (Vector2 pan, float zoom) ComputeFit(List<Vector2> positions, float padding, float zoomCap)
    {
        if (positions == null || positions.Count == 0) return (Vector2.zero, Mathf.Clamp(1f, minZoom, zoomCap));

        Vector2 min = positions[0];
        Vector2 max = positions[0];
        foreach (Vector2 p in positions)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = new Vector2(Mathf.Max(max.x - min.x, 1f), Mathf.Max(max.y - min.y, 1f));

        Rect viewport = GetViewport();
        float availableW = Mathf.Max(viewport.width - padding * 2f, 50f);
        float availableH = Mathf.Max(viewport.height - padding * 2f, 50f);

        float zoom = Mathf.Clamp(Mathf.Min(availableW / size.x, availableH / size.y), minZoom, zoomCap);
        Vector2 pan = -center * zoom;

        return (pan, zoom);
    }

    private IEnumerator AnimateTo(Vector2 targetPan, float targetZoom, float duration)
    {
        Vector2 startPan = currentPan;
        float startZoom = currentZoom;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetPanZoomDirect(Vector2.Lerp(startPan, targetPan, k), Mathf.Lerp(startZoom, targetZoom, k));
            yield return null;
        }

        SetPanZoomDirect(targetPan, targetZoom);
    }

    // 사용자 드래그/스크롤에서만 사용 - 팬이 맵 바운딩 박스 밖으로 못 나가게 clamp
    private void SetPanZoom(Vector2 pan, float zoom)
    {
        float clampedZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        currentZoom = clampedZoom;
        currentPan = hasBounds ? ClampPan(pan, clampedZoom) : pan;
        ApplyTransform();
    }

    // 인트로 연출(오버뷰/포커스로 자동 이동)에서만 사용 - ComputeFit이 introMaxZoom으로 이미 클램프한 값이므로
    // 여기서 (더 좁은) maxZoom으로 다시 누르지 않고, 바운딩 박스 clamp도 걸지 않음
    // (걸면 줌 도중 clamp 공식이 바뀌면서 경로가 갑자기 꺾여 보임)
    private void SetPanZoomDirect(Vector2 pan, float zoom)
    {
        currentZoom = zoom;
        currentPan = pan;
        ApplyTransform();
    }

    private Vector2 ClampPan(Vector2 pan, float zoom)
    {
        Rect viewport = GetViewport();
        pan.x = ClampAxis(pan.x, boundsMin.x, boundsMax.x, zoom, viewport.width);
        pan.y = ClampAxis(pan.y, boundsMin.y, boundsMax.y, zoom, viewport.height);
        return pan;
    }

    private float ClampAxis(float pan, float min, float max, float zoom, float viewportSize)
    {
        float contentSize = (max - min) * zoom;

        if (contentSize <= viewportSize)
        {
            // 맵이 화면보다 작으면 더 움직일 곳이 없으니 가운데로 고정
            return -(min + max) * 0.5f * zoom;
        }

        float lower = viewportSize * 0.5f - max * zoom;
        float upper = -viewportSize * 0.5f - min * zoom;
        return Mathf.Clamp(pan, lower, upper);
    }

    private void ApplyTransform()
    {
        if (nodeContainer != null)
        {
            nodeContainer.anchoredPosition = currentPan;
            nodeContainer.localScale = Vector3.one * currentZoom;
        }
        if (lineContainer != null)
        {
            lineContainer.anchoredPosition = currentPan;
            lineContainer.localScale = Vector3.one * currentZoom;
        }
    }

    // 씬 전환(아이리스) 연출이 재생 중이면 맵을 드래그/스크롤로 못 움직이게 막음
    private bool IsInputLocked => LDY_SceneTransition.Instance != null && LDY_SceneTransition.Instance.IsPlaying;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsInputLocked) return;
        CancelIntro();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsInputLocked) return;
        CancelIntro();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        SetPanZoom(currentPan + eventData.delta / Mathf.Max(scaleFactor, 0.01f), currentZoom);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (IsInputLocked) return;
        CancelIntro();
        SetPanZoom(currentPan, currentZoom + eventData.scrollDelta.y * scrollZoomSpeed);
    }
}
