using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class KTH_LoadingSceneController : MonoBehaviour
{
    private static string nextSceneName;

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