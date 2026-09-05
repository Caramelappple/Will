using _Scripts.LSO.Stage;
using UnityEngine;
using UnityEngine.UI;

// 테스트 씬에 놓는 버튼용 스크립트.
// 버튼 하나가 "클리어 처리 + 맵 씬 복귀" 책임만 가지도록 분리했다(SRP).
// LSO_StageFlow는 DontDestroyOnLoad 싱글톤이라 테스트 씬 시점엔 이미 존재하지만,
// 씬이 다르기 때문에 인스펙터로 직접 드래그해 연결할 수 없어 런타임에 Instance로 접근한다.
[RequireComponent(typeof(Button))]
public class KTH_TestClearButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClearButtonClicked);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClearButtonClicked);
    }

    private void OnClearButtonClicked()
    {
        if (!LSO_StageFlow.HasInstance)
        {
            Debug.LogWarning("[KTH_TestClearButton] LSO_StageFlow가 없습니다.", this);
            return;
        }

        LSO_StageFlow.Instance.ClearStage();
    }
}