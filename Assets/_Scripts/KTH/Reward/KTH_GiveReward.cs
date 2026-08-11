using System.Collections.Generic;
using UnityEngine;
using _Scripts.LSO;

public class KTH_GiveReward : MonoBehaviour
{
    [Header("현재 스테이지 정보")]
    [SerializeField] private int currentChapter = 1;
    [SerializeField] private int currentStage = 1;

    public KTH_UnlockState GiveStageReward(int chapter, int stage)
    {
        this.currentChapter = chapter;
        this.currentStage = stage;
        return GiveStageReward();
    }

    public KTH_UnlockState GiveStageReward()
    {
        Debug.Log($"[KTH_GiveReward] 보상 지급 요청됨! (Chapter: {currentChapter}, Stage: {currentStage})");

        if (KTH_Reward.Instance == null)
        {
            Debug.LogError("[KTH_GiveReward] KTH_Reward 인스턴스가 씬에 존재하지 않습니다!");
            return null;
        }

        return KTH_Reward.Instance.UnlockByStage(currentChapter, currentStage);
    }
}