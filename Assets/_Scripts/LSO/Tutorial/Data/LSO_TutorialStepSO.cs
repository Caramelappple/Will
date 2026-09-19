using _Scripts.LSO.Tutorial.Gate;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Data
{
    /// <summary>
    /// 튜토리얼 한 걸음.
    ///
    /// 대본의 한 줄이 아니라 **한 장면**이다. 카메라가 어디를 보고, 무슨 말을 하고,
    /// 어느 칸을 물들이고, 무엇을 기다릴지가 한 덩어리로 들어간다.
    ///
    /// 코드가 아니라 에셋으로 두는 이유는, 기획이 바뀔 때 스크립트를 안 건드리기 위해서다.
    /// 걸음을 하나 더 끼우는 것이 에셋 하나 만들고 목록에 넣는 일이 된다.
    /// </summary>
    [CreateAssetMenu(fileName = "TutStep", menuName = "SO/Tutorial/Step")]
    public sealed class LSO_TutorialStepSO : ScriptableObject
    {
        [Header("카메라")]
        [Tooltip("LSO_CameraDirector 에 등록된 샷 이름. 비우면 카메라를 안 움직인다.")]
        public string shotId;

        [Tooltip("켜면 카메라가 다 움직인 뒤에 안내문을 띄운다.\n" +
                 "\n" +
                 "끄면 움직이면서 같이 읽힌다. 짧은 이동이면 끄는 편이 덜 늘어진다.")]
        public bool waitForCamera = true;

        [Header("안내문")]
        [Tooltip("위에서부터 차례로 뜬다. 마지막 줄은 관문이 열릴 때까지 남는다.")]
        [TextArea(2, 5)] public string[] lines;

        [Tooltip("한 줄이 머무는 시간(초). 마지막 줄에는 쓰이지 않는다.")]
        [Min(0f)] public float lineHold = 2.5f;

        [Header("가이드")]
        [Tooltip("칸을 물들일지. 여기 담긴 칸이 **놓거나 갈 수 있는 유일한 칸**이 된다.\n" +
                 "\n" +
                 "표시와 허용을 한 값에서 뽑는다. 따로 적으면 언젠가 어긋나고,\n" +
                 "그때 플레이어는 노란 칸을 눌렀는데 아무 일도 안 일어나는 화면을 본다.")]
        public LSO_TutorialGuideKind guide;

        public Vector3Int[] guideTiles;

        [Header("조작 허용")]
        [Tooltip("이 걸음 동안 할 수 있는 것. 여기 없는 것은 막힌다.")]
        public LSO_TutorialAction allowed = LSO_TutorialAction.None;

        [Header("관문")]
        [Tooltip("언제 다음 걸음으로 넘어갈지. 비우면 마지막 줄을 읽고 바로 넘어간다.")]
        public LSO_TutorialGateSO gate;

        [Tooltip("관문이 이 시간 안에 안 열리면 경고를 남기고 그냥 넘어간다(초).\n" +
                 "\n" +
                 "0이면 영영 기다린다. **되도록 쓰지 말 것** — 멈춰 선 튜토리얼은\n" +
                 "왜 안 넘어가는지 알 방법이 없다.")]
        [Min(0f)] public float gateTimeout = 30f;

        /// <summary>목록에서 알아보기 쉽게. 대본 한 줄을 그대로 보여준다.</summary>
        public string Preview =>
            lines != null && lines.Length > 0 ? lines[0] : "(빈 걸음)";
    }
}
