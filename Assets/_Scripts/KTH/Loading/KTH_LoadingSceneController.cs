using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class KTH_LoadingSceneController : MonoBehaviour
{
    private static string nextSceneName;

    /// <summary>
    /// Reload Domain을 끈 에디터에서는 static이 플레이를 멈춰도 살아남는다.
    /// 지난 플레이의 값이 남아 있으면 두 번째 실행부터 엉뚱하게 동작하므로,
    /// 씬이 로드되기 전에 직접 비운다. LDY_RunSeed와 같은 이유다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        nextSceneName = null;
    }

    [SerializeField] private Image progressBar;

    public static void LoadScene(string sceneName)
    {
        nextSceneName = sceneName;
        SceneManager.LoadScene("KTH_LoadingScene");
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[Loading] 다음 씬 이름이 없습니다.");
            return;
        }

        if (progressBar != null)
            progressBar.fillAmount = 0f;

        StartCoroutine(LoadSceneProcess());
    }

    private IEnumerator LoadSceneProcess()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            yield return null;

            if (operation.progress < 0.9f)
            {
                progressBar.fillAmount = operation.progress;
            }
            else
            {
                timer += Time.unscaledDeltaTime;

                progressBar.fillAmount = Mathf.Lerp(0.9f, 1f, timer);

                if (progressBar.fillAmount >= 1f)
                    operation.allowSceneActivation = true;
            }
        }
    }
}