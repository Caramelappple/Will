using UnityEngine;

public class KTH_Clear : MonoBehaviour
{
    public static KTH_Clear Instance { get; private set; }

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

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnClearButtonClicked()
    {
        if (LDY_MapManager.Instance == null)
        {
            Debug.LogWarning("[KTH_TestClearButton] LDY_MapManager.Instance가 없습니다. (단독 테스트 씬 진입 상태일 수 있음)");

            // 싱글톤이 없는 단독 씬 테스트용 예외 처리 (필요시 사용)
            // UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
            return;
        }

        LDY_MapManager.Instance.CompleteActiveNodeAndReturnToMap();
    }
}
