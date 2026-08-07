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

        if (LDY_MapManager.Instance == null)
        {
            enabled = false;
            return;
        }

        LDY_MapManager.Instance.onStageChanged.AddListener(UpdateStageText);

        UpdateStageText();
    }

    private void OnDestroy()
    {
        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onStageChanged.RemoveListener(UpdateStageText);
    }

    private void UpdateStageText()
    {
        stageText.text =
            $"{LDY_MapManager.Instance.CurrentChapter}-{LDY_MapManager.Instance.CurrentStage}";
    }
}