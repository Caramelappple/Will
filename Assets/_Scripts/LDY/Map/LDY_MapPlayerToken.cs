using System;
using System.Collections;
using UnityEngine;

// 맵 위에서 플레이어의 현재 위치를 나타내는 토큰. 노드를 클릭하면 그 노드 좌표로 이동한 뒤 콜백을 호출함.
// NodeContainer 밑에 둬서 팬/줌에 노드들과 똑같이 따라 움직임.
// 씬 전환(아이리스) 연출은 이 토큰이 실제로 도착한 위치를 기준으로 재생되므로,
// 카메라가 움직이는 중에 클릭해도 항상 "플레이어가 서있는 자리"를 기준으로 자연스럽게 나옴
public class LDY_MapPlayerToken : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 800f; // 맵 좌표 단위/초
    [SerializeField] private float minMoveDuration = 0.3f;
    [SerializeField] private float maxMoveDuration = 1.2f;

    private RectTransform rt;
    private Coroutine moving;

    private void Awake()
    {
        rt = (RectTransform)transform;
    }

    public void SetPosition(Vector2 mapPosition)
    {
        if (rt == null) rt = (RectTransform)transform;
        rt.anchoredPosition = mapPosition;
    }

    public void MoveTo(Vector2 targetMapPosition, Action onComplete)
    {
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

    // 씬 전환 아이리스 연출의 중심점으로 쓰기 위한, 이 토큰의 화면상 위치(0~1).
    // 캔버스의 world scale이 CanvasScaler 때문에 실제 픽셀과 단위가 안 맞을 수 있으므로,
    // world 좌표를 그냥 빼서 쓰지 않고 InverseTransformPoint로 "캔버스 로컬 공간"으로 정확히 변환한 뒤
    // 그 로컬 공간과 같은 단위인 canvas.rect(참조 해상도)를 기준으로 0~1 비율을 계산함
    public Vector2 GetScreenUV()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return new Vector2(0.5f, 0.5f);

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Vector3 localPos = canvasRect.InverseTransformPoint(rt.position);
        Rect rect = canvasRect.rect;

        Vector2 uv = new Vector2(
            (localPos.x - rect.xMin) / rect.width,
            (localPos.y - rect.yMin) / rect.height);

        Debug.Log($"[LDY_MapPlayerToken] GetScreenUV: anchoredPosition(local)={rt.anchoredPosition}, " +
            $"worldPos={rt.position}, localPos(canvas space)={localPos}, rect={rect}, uv={uv}");

        return uv;
    }
}
