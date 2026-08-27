using UnityEngine;

public class KTH_ChapterClearManager : MonoBehaviour
{
    public static KTH_ChapterClearManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 보스 클리어 처리.
    ///
    /// 다음 챕터 진행(챕터++, 노드 재생성)은 LDY_MapManager.CompleteNode()가
    /// Boss 타입을 처리할 때 이미 알아서 한다. 여기서는 그 결과로 어떤 씬으로
    /// 갈지(보상 대기 → 클리어 연출 씬 or 맵)만 LDY_MapManager에 그대로 위임한다.
    ///
    /// 예전에는 다음 챕터 존재 여부를 직접 확인해서 있으면 클리어 연출 씬을
    /// 건너뛰고 곧장 맵으로 갔지만, 그 경로는 보상 UI의 OnRewardResolved를
    /// 기다리지 않고 씬을 넘겨버려 보상 선택 중에 화면이 끊기는 문제가 있었다.
    /// CompleteActiveNodeAndReturnToMap()으로 일원화하면 그 대기를 항상 보장한다.
    /// </summary>
    public void HandleBossClear()
    {
        LDY_MapManager mapManager = LDY_MapManager.Instance;

        if (mapManager == null)
        {
            Debug.LogError(
                "[KTH_ChapterClearManager] LDY_MapManager.Instance를 찾을 수 없습니다."
            );

            return;
        }

        mapManager.CompleteActiveNodeAndReturnToMap();
    }
}