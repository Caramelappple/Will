using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 씬 전환 연출: ① 화면이 안 가려진 상태에서 점점 좁아져 클릭한 지점(별) 주변 구멍 크기까지 채워짐(fillInDuration)
// → ② 그 구멍으로 별이 보이는 상태를 유지(holdDuration) → ③ 구멍이 서서히 완전히 닫힘(duration) → 다음 씬으로 이동.
// 셰이더 없이 순수 C#으로 원형 구멍이 뚫린 텍스처를 직접 그려서 구현
// (LDY_ProceduralSprite와 같은 방식 - 매 프레임이 아니라 초당 updatesPerSecond 만큼만 다시 그려 가볍게 동작)
public class LDY_SceneTransition : MonoBehaviour
{
    public static LDY_SceneTransition Instance { get; private set; }

    /// <summary>
    /// Reload Domain을 끈 에디터에서는 static이 플레이를 멈춰도 살아남는다.
    /// 지난 플레이의 값이 남아 있으면 두 번째 실행부터 엉뚱하게 동작하므로,
    /// 씬이 로드되기 전에 직접 비운다. LDY_RunSeed와 같은 이유다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    // 전환 재생 중에는 맵 카메라(드래그/스크롤) 입력을 막는 데 씀 (LDY_MapCameraController에서 참조)
    public bool IsPlaying => isPlaying;

    [SerializeField] private RawImage overlay;
    [Tooltip("씬 로드 직후 재생할 카운트다운 (선택). 비워두면 카운트다운 없이 바로 오버레이가 사라짐")]
    [SerializeField] private LDY_BattleCountdown countdown;
    [SerializeField] private int textureSize = 256;
    [Tooltip("전체 화면이 안 가려진 상태로 취급할 시작 반경 (화면 대각선보다 커야 함)")]
    [SerializeField] private float openRadius = 1.5f;
    [SerializeField] private float startRadius = 0.16f;
    [SerializeField] private float edgeSoftness = 0.015f;
    [Tooltip("openRadius에서 startRadius(구멍 크기)까지 좁혀지는 데 걸리는 시간")]
    [SerializeField] private float fillInDuration = 0.5f;
    [Tooltip("구멍이 완전히 닫힌(까만) 상태를 씬 로드 전까지 유지하는 시간")]
    [SerializeField] private float holdDuration = 1.2f;
    [Tooltip("startRadius(구멍 크기)에서 0(완전히 닫힘)까지 좁혀지는 데 걸리는 시간")]
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float updatesPerSecond = 24f;

    [Header("타입별 아이리스 색")]
    [SerializeField] private Color battleColor = new Color32(0xE0, 0x30, 0x30, 0xFF);
    [SerializeField] private Color eventColor = new Color32(0xE0, 0xD0, 0x30, 0xFF);
    [SerializeField] private Color shopColor = new Color32(0x30, 0xC0, 0x50, 0xFF);
    [SerializeField] private Color bossColor = new Color32(0x90, 0x30, 0xC0, 0xFF);
    [SerializeField] private Color defaultColor = Color.black;

    private Texture2D texture;
    private Color32[] pixels;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        pixels = new Color32[textureSize * textureSize];

        if (overlay != null)
        {
            overlay.texture = texture;
            overlay.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[LDY_SceneTransition] Overlay(Raw Image)가 연결되어 있지 않습니다. 아이리스 없이 바로 씬 전환됩니다.", this);
        }

        Debug.Log("[LDY_SceneTransition] Instance 등록 완료.", this);
    }

    // screenUV: 구멍의 중심이 될 화면상 위치 (0~1, 좌하단 기준). nodeType에 따라 아이리스 색이 달라짐.
    // 씬 로드 후 곧바로 오버레이를 내려서 화면(보드)을 먼저 보여주고, 그 위에 카운트다운(있으면)을 재생함 -
    // 그래서 전투/보스 씬에 아무것도 안 만들어도 화면이 보인 채로 "3,2,1,START!"가 자동으로 뜸
    public void PlayIrisCloseThenLoad(Vector2 screenUV, string sceneName, LDY_NodeType nodeType)
    {
        if (isPlaying) return;
        StartCoroutine(PlayRoutine(screenUV, GetColorForType(nodeType), () =>
        {
            SceneManager.LoadScene(sceneName);
            HideOverlay();
            if (countdown != null) countdown.Play(null);
        }));
    }

    // 씬을 넘기지 않고(상점/이벤트 팝업처럼) 구멍이 다 닫힌 뒤 임의의 동작만 실행하고 싶을 때 - 카운트다운 없이 바로 사라짐
    public void PlayIrisCloseThen(Vector2 screenUV, LDY_NodeType nodeType, Action onClosed)
    {
        if (isPlaying) return;
        StartCoroutine(PlayRoutine(screenUV, GetColorForType(nodeType), () =>
        {
            onClosed?.Invoke();
            HideOverlay();
        }));
    }

    private void HideOverlay()
    {
        if (overlay != null) overlay.gameObject.SetActive(false);
    }

    private Color GetColorForType(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Battle: return battleColor;
            case LDY_NodeType.Event: return eventColor;
            case LDY_NodeType.Shop: return shopColor;
            case LDY_NodeType.Boss: return bossColor;
            default: return defaultColor;
        }
    }

    private IEnumerator PlayRoutine(Vector2 screenUV, Color color, Action onClosed)
    {
        isPlaying = true;

        if (overlay == null)
        {
            Debug.LogWarning("[LDY_SceneTransition] overlay가 null이라 아이리스 없이 바로 진행합니다.", this);
            onClosed?.Invoke();
            isPlaying = false;
            yield break;
        }
        overlay.gameObject.SetActive(true);
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1f;

        // 1) 화면이 안 가려진 상태에서 시작해, 점점 좁아져서 별 주변 구멍 크기까지 채워짐
        Paint(screenUV, openRadius, aspect, color);
        yield return AnimateRadius(screenUV, openRadius, startRadius, fillInDuration, aspect, color);

        // 2) 구멍으로 별이 보이는 상태를 holdDuration만큼 유지
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        // 3) 그 다음에 구멍을 duration 동안 서서히 완전히 닫음
        yield return AnimateRadius(screenUV, startRadius, 0f, duration, aspect, color);

        // 4) 완전히 닫힌 뒤 요청받은 동작 실행 (씬 로드+카운트다운이든, 팝업 열기든).
        // 오버레이를 언제 끌지는 onClosed 쪽(PlayIrisCloseThenLoad/PlayIrisCloseThen)이 알아서 처리함 -
        // 카운트다운이 있으면 그게 끝날 때까지 화면이 계속 가려져 있어야 하므로 여기서 무조건 끄면 안 됨
        onClosed?.Invoke();
        isPlaying = false;
    }

    private IEnumerator AnimateRadius(Vector2 center, float fromRadius, float toRadius, float animDuration, float aspect, Color color)
    {
        if (animDuration <= 0f)
        {
            Paint(center, toRadius, aspect, color);
            yield break;
        }

        float t = 0f;
        float updateInterval = 1f / Mathf.Max(updatesPerSecond, 1f);
        float sinceLastUpdate = 0f;

        while (t < animDuration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            sinceLastUpdate += dt;

            if (sinceLastUpdate >= updateInterval)
            {
                sinceLastUpdate = 0f;
                float radius = Mathf.Lerp(fromRadius, toRadius, Mathf.Clamp01(t / animDuration));
                Paint(center, radius, aspect, color);
            }

            yield return null;
        }

        Paint(center, toRadius, aspect, color);
    }

    private void Paint(Vector2 center, float radius, float aspect, Color color)
    {
        Color32 baseColor = color;

        for (int y = 0; y < textureSize; y++)
        {
            float v = y / (float)(textureSize - 1);
            float dy = v - center.y;

            for (int x = 0; x < textureSize; x++)
            {
                float u = x / (float)(textureSize - 1);
                float dx = (u - center.x) * aspect;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01((dist - radius) / Mathf.Max(edgeSoftness, 0.0001f));
                pixels[y * textureSize + x] = new Color32(baseColor.r, baseColor.g, baseColor.b, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }
}
