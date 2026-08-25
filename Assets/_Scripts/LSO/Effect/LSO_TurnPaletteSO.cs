using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Effect
{
    /// <summary>
    /// 턴에 따른 색과 전환 설정. 순수 데이터만 담으며 스스로 아무것도 실행하지 않는다.
    ///
    /// 촛불마다 색을 따로 넣어두면 열 개를 켜둔 뒤 색을 조정할 때 열 번을 만져야 하고,
    /// 하나를 빠뜨려도 눈으로 찾기 전까지 모른다. 그래서 설정만 여기로 모은다.
    ///
    /// 프로젝트 창에서 우클릭 &gt; Create &gt; LSO &gt; Effect &gt; Turn Palette 로 만든다.
    /// 예외를 두고 싶은 촛불(예: 보스방)이 생기면 에셋을 하나 더 만들어 그것만 끼우면 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "LSO_TurnPalette", menuName = "LSO/Effect/Turn Palette")]
    public class LSO_TurnPaletteSO : ScriptableObject
    {
        // 기획서: "턴 종료 후 적의 턴으로 넘어가면 화면에 분위기를 이루는 촛불의 색이
        //          붉게 바뀐다(기본은 주황&노랑)."
        [Header("팀 색")]
        [Tooltip("플레이어 턴. 기획서의 기본값인 주황·노랑.")]
        [SerializeField] private Color playerColor = new Color(1f, 0.78f, 0.42f);

        [Tooltip("적 턴. 기획서의 '붉게'.")]
        [SerializeField] private Color enemyColor = new Color(0.95f, 0.24f, 0.14f);

        [Header("전환")]
        [Tooltip("색이 넘어가는 데 걸리는 시간(초). 0이면 즉시 바뀐다.")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.6f;

        [SerializeField] private Ease ease = Ease.InOutSine;

        public Color PlayerColor => playerColor;

        public Color EnemyColor => enemyColor;

        public float TransitionDuration => transitionDuration;

        public Ease Ease => ease;

        public Color ColorFor(LDY_Team team)
        {
            return team == LDY_Team.Player ? playerColor : enemyColor;
        }
    }
}
