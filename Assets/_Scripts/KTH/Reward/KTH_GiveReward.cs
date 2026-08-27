using System.Collections.Generic;
using _Scripts.LSO.Reward;
using UnityEngine;

public class KTH_GiveReward : MonoBehaviour
{
    [Header("현재 스테이지 정보")]
    [SerializeField] private int currentChapter = 1;
    [SerializeField] private int currentStage = 1;

    public List<LSO_RewardOption> GiveStageReward(int chapter, int stage)
    {
        currentChapter = chapter;
        currentStage = stage;

        return GiveStageReward();
    }

    public List<LSO_RewardOption> GiveStageReward()
    {
        Debug.Log($"[KTH_GiveReward] 보상 선택 시작! (Chapter: {currentChapter}, Stage: {currentStage})");

        if (KTH_Reward.Instance == null)
        {
            Debug.LogError("[KTH_GiveReward] KTH_Reward 인스턴스가 없습니다!");
            return null;
        }

        // 1. 보상 후보 생성
        List<LSO_RewardOption> options = KTH_Reward.Instance.GenerateRewardOptions(currentChapter, currentStage);

        // 2. UI 인스턴스 가져오기 (비활성화 상태여도 Instance 프로퍼티가 찾아서 반환함)
        KTH_RewardChoiceUI rewardUI = KTH_RewardChoiceUI.Instance;

        if (rewardUI != null)
        {
            rewardUI.ShowRewards(options);
        }
        else
        {
            Debug.LogError("[KTH_GiveReward] 현재 씬 Hierarchy에서 KTH_RewardChoiceUI 오브젝트를 찾을 수 없습니다!");
        }

        return options;
    }
}