using _Scripts.LDY;
using _Scripts.LSO.UI.Effect;
using UnityEngine;

namespace _Scripts.LSO.Animal
{
    /// <summary>
    /// 보드 기물의 호버 연출 기본값. 한 벌만 두고 모든 기물이 같은 값을 쓴다.
    ///
    /// 프리팹마다 적어두면 동물이 늘 때마다 반복해야 하고, 값을 고칠 때
    /// 하나를 빠뜨리면 그 기물만 다르게 움직인다. 그래서 밖으로 뺐다.
    ///
    /// 특정 기물만 다르게 하고 싶으면 그 프리팹에 호버 컴포넌트를 직접 붙이면 된다.
    /// LSO_PieceHoverInstaller는 이미 붙어 있는 것은 건드리지 않는다.
    ///
    /// 배치: Assets/Resources/LSO_PieceHoverSettings.asset
    /// 이름과 위치가 정확해야 Resources.Load가 찾는다. 없으면 코드 기본값으로 돈다.
    /// </summary>
    [CreateAssetMenu(
        fileName = ResourcePath,
        menuName = "LSO/기물 호버 설정",
        order = 0)]
    public class LSO_PieceHoverSettingsSO : ScriptableObject
    {
        /// <summary>Resources.Load가 찾을 수 있는 유일한 경로. 파일 이름과 같아야 한다.</summary>
        public const string ResourcePath = "LSO_PieceHoverSettings";

        [Header("붙일지 여부")]
        [Tooltip("끄면 기물에 호버를 아예 붙이지 않는다.\n" +
                 "연출을 통째로 잠시 꺼보고 싶을 때 쓴다.")]
        [SerializeField] private bool enableHover = true;

        [Tooltip("켜면 커서 모양도 함께 바꾼다.\n" +
                 "LSO_CursorManager에 텍스처를 넣어두지 않았다면 꺼둘 것.")]
        [SerializeField] private bool enableCursorChange;

        [Header("팀")]
        [Tooltip("이 팀의 기물만 호버에 반응한다.\n" +
                 "적 기물이 떠오르면 고를 수 있는 것으로 읽히기 때문이다.")]
        [SerializeField] private LDY_Team allowedTeam = LDY_Team.Player;

        [Header("움직임")]
        [SerializeField] private LSO_HoverMoveTuning move = LSO_HoverMoveTuning.Default;

        public bool EnableHover => enableHover;

        public bool EnableCursorChange => enableCursorChange;

        public LDY_Team AllowedTeam => allowedTeam;

        public LSO_HoverMoveTuning Move => move;

        /// <summary>
        /// 에셋이 없을 때 쓸 값. 에셋이 있으면 이 경로는 지나가지 않는다.
        ///
        /// 여기에 값을 적어두는 이유는, 에셋을 안 만들었다고 호버가 통째로
        /// 죽어버리면 "왜 안 되는지"가 아니라 "원래 없는 기능인지"로 보이기 때문이다.
        /// </summary>
        public static LSO_PieceHoverSettingsSO CreateDefault()
        {
            var settings = CreateInstance<LSO_PieceHoverSettingsSO>();

            settings.enableHover = true;
            settings.enableCursorChange = false;
            settings.allowedTeam = LDY_Team.Player;
            settings.move = LSO_HoverMoveTuning.Default;

            return settings;
        }

        private void Reset()
        {
            move = LSO_HoverMoveTuning.Default;
        }
    }
}
