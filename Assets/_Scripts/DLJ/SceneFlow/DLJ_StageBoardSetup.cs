using _Scripts.LDY;
using _Scripts.LDY.Stage;
using UnityEngine;

namespace _Scripts.DLJ.SceneFlow
{
    /// <summary>테스트 씬의 생존 아군은 유지하고, 다음 적 배치 전에 아군을 앞줄로 정렬한다.</summary>
    public sealed class DLJ_StageBoardSetup : MonoBehaviour, LDY_IStageSetupStep
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;

        public void Setup(LDY_StageSO stage)
        {
            if (board == null) return;
            foreach (LDY_Animal enemy in board.GetAllByTeam(LDY_Team.Enemy))
            {
                board.Remove(enemy);
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            var allies = board.GetAllByTeam(LDY_Team.Player);
            foreach (LDY_Animal ally in allies) board.Remove(ally);
            for (int i = 0; i < allies.Count; i++)
            {
                LDY_Animal ally = allies[i];
                ally.gameObject.SetActive(true);
                board.Place(ally, new Vector3Int(i % LDY_BoardManager.Size, ally.pos.y, i / LDY_BoardManager.Size));
            }
            if (actionPoints != null) actionPoints.ResetPoints();
        }
    }
}
