using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Stage
{
    /// <summary>
    /// 스테이지 한 판의 정의. 순수 데이터만 담으며 스스로 아무것도 실행하지 않는다.
    /// 실제 적용은 LDY_IStageSetupStep 구현들이 나눠서 맡는다.
    /// 프로젝트 창에서 우클릭 &gt; Create &gt; LDY &gt; Stage 로 만든다.
    ///
    /// ── 덜어낸 것 ─────────────────────────────────────────────
    /// 이동할 씬     한 화면에서 이어가게 되면서 필요 없어졌다.
    /// 스테이지 규칙  행동력·소환 코스트를 스테이지마다 덮어쓰던 값이다.
    ///               지금은 씬에 설정된 기본값 하나로 돈다.
    ///
    /// 규칙을 되살릴 때는 값을 여기 다시 넣고, 그것을 읽는 스텝을 만들어
    /// LDY_StageDirector와 같은 오브젝트에 붙이면 된다.
    /// ─────────────────────────────────────────────────────────
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
    }
}
