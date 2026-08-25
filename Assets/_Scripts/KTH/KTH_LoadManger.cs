using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KTH_LoadManger : MonoBehaviour
{
    [SerializeField] private Button endSceneButton;
    [SerializeField]private string sceneName;

    private void OnEnable()
    {
        endSceneButton?.onClick.AddListener(OnEndSceneButtonClicked);
    }

    private void OnDisable()
    {
        endSceneButton?.onClick.RemoveListener(OnEndSceneButtonClicked);
    }

    private void OnEndSceneButtonClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
}
