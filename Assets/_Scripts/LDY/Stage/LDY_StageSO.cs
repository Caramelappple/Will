using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지 한 판의 정의. 순수 데이터만 담으며 스스로 아무것도 실행하지 않는다.
    /// 실제 적용은 LDY_IStageSetupStep 구현들이 나눠서 맡는다.
    /// 프로젝트 창에서 우클릭 &gt; Create &gt; LDY &gt; Stage 로 만들고, 씬은 그대로 둔 채 이 에셋만 갈아끼우면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStage", menuName = "LDY/Stage")]
    public class LDY_StageSO : ScriptableObject
    {
        [Header("표시용")]
        public string stageName;

        [TextArea(2, 4)]
        public string description;

        [Header("적 배치")]
        public List<LDY_StageEnemyEntry> enemies = new List<LDY_StageEnemyEntry>();

        [Header("스테이지 규칙")]
        [Tooltip("이 스테이지에서 한 턴에 쓸 수 있는 행동력. 0 이하면 씬에 설정된 기본값을 그대로 쓴다.")]
        public int actionPointsPerTurn;

        [Tooltip("이 스테이지에서 턴마다 회복되는 소환 코스트. 0 이하면 씬에 설정된 기본값을 그대로 쓴다.")]
        public int summonCostPerTurn;
    }
}
