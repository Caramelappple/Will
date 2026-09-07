using _Scripts.LSO.Stage;
using TMPro;
using UnityEngine;

public class KTH_StageChecker : MonoBehaviour
{
    [Header("Stage Text")]
    [SerializeField] private TMP_Text stageText;

    private void Start()
    {
        if (stageText == null)
        {
            Debug.LogError("[KTH_StageChecker] StageText가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        if (!LSO_StageProgression.HasInstance)
        {
            enabled = false;
            return;
        }

        LSO_StageProgression.Instance.Advanced += HandleAdvanced;

        UpdateStageText();
    }

    private void OnDestroy()
    {
        if (LSO_StageProgression.HasInstance)
            LSO_StageProgression.Instance.Advanced -= HandleAdvanced;
    }

    // 진행이 한 칸 넘어갈 때마다 다시 그린다. 인자는 쓰지 않는다.
    private void HandleAdvanced(_Scripts.LDY.Stage.LDY_StageSO _) => UpdateStageText();

    private void UpdateStageText()
    {
        if (!LSO_StageProgression.HasInstance) return;

        LSO_StageProgression p = LSO_StageProgression.Instance;

        stageText.text = $"{p.ChapterNumber}-{p.StageNumber}";
    }
}