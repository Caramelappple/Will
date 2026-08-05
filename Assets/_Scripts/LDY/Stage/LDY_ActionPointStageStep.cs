using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지에 지정된 턴당 행동력을 적용한다. 값이 0 이하면 씬에 설정된 기본값을 그대로 둔다.
    /// 씬 배선: ActionPointManager를 연결할 것.
    /// </summary>
    public class LDY_ActionPointStageStep : MonoBehaviour, LDY_IStageSetupStep
    {
        [SerializeField] private LDY_ActionPointManager actionPoints;

        public void Setup(LDY_StageSO stage)
        {
            if (stage == null || actionPoints == null) return;
            if (stage.actionPointsPerTurn <= 0) return;

            actionPoints.SetMax(stage.actionPointsPerTurn);
        }
    }
}
