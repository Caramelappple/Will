using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지를 시작하기 전에 보드에 남아 있던 기물을 모두 치운다.
    /// 같은 씬에서 스테이지를 갈아끼울 때 이전 판의 기물이 남지 않게 하는 것이 유일한 책임이다.
    /// 씬 배선: BoardManager를 연결하고, StageDirector와 같은 오브젝트에 붙일 것(스텝 중 가장 위에).
    /// </summary>
    public class LDY_BoardClearStep : MonoBehaviour, LDY_IStageSetupStep
    {
        [SerializeField] private LDY_BoardManager board;

        public void Setup(LDY_StageSO stage)
        {
            if (board == null)
            {
                Debug.LogError($"{name}: BoardManager가 연결되어 있지 않습니다.", this);
                return;
            }

            board.ClearAll();
        }
    }
}
