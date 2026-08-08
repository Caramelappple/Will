using UnityEngine;

public class KTH_Clear : MonoBehaviour
{
    public static KTH_Clear Instance { get; private set; }

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
