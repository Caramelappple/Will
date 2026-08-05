using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지에 지정된 턴당 소환 코스트를 적용한다. 값이 0 이하면 씬에 설정된 기본값을 그대로 둔다.
    /// 씬 배선: CardPlacer를 연결할 것.
    /// </summary>
    public class LDY_SummonCostStageStep : MonoBehaviour, LDY_IStageSetupStep
    {
        [SerializeField] private LDY_CardPlacer cardPlacer;

        public void Setup(LDY_StageSO stage)
        {
            if (stage == null || cardPlacer == null) return;
            if (stage.summonCostPerTurn <= 0) return;

            cardPlacer.SetMaxCost(stage.summonCostPerTurn);
        }
    }
}
