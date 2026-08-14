using System.Collections.Generic;
using UnityEngine;

public class KTH_GiveReward : MonoBehaviour
{
    [Header("현재 스테이지 정보")]
    [SerializeField] private int currentChapter = 1;
    [SerializeField] private int currentStage = 1;

    public List<KTH_RewardOption> GiveStageReward(int chapter, int stage)
    {
        currentChapter = chapter;
        currentStage = stage;

        return GiveStageReward();
    }

    public List<KTH_RewardOption> GiveStageReward()
    {
        Debug.Log($"[KTH_GiveReward] 보상 선택 시작! (Chapter: {currentChapter}, Stage: {currentStage})");

        if (KTH_Reward.Instance == null)
        {
            Debug.LogError("[KTH_GiveReward] KTH_Reward 인스턴스가 없습니다!");
            return null;
        }

        // 1. 보상 후보 생성
        List<KTH_RewardOption> options = KTH_Reward.Instance.GenerateRewardOptions(currentChapter, currentStage);

        // 2. UI 스크립트에 생성된 보상 목록을 넘겨 직접 띄우기
        if (KTH_RewardChoiceUI.Instance != null)
        {
            KTH_RewardChoiceUI.Instance.ShowRewards(options);
        }
        else
        {
            Debug.LogError("[KTH_GiveReward] KTH_RewardChoiceUI.Instance를 찾을 수 없습니다!");
        }

        return options;
    }
}