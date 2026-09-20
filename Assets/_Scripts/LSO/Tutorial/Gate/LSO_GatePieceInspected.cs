using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 기물 정보창이 열리면 통과. 2번 챕터의 "해당 기물을 더블 클릭 해보세요"가 여기다.
    ///
    /// **이미 있는 신호를 쓰는 관문의 본보기다.** 구독을 걸고 Disarm 에서 푼다 —
    /// 안 풀면 다음 걸음에서 유령 신호가 날아와 엉뚱한 곳이 통과한다.
    /// </summary>
    [CreateAssetMenu(fileName = "Gate_PieceInspected", menuName = "SO/Tutorial/Gate/기물 정보창 열림")]
    public sealed class LSO_GatePieceInspected : LSO_TutorialGateSO
    {
        protected override void OnArm()
        {
            DLJ_InfoPanelEvents.PieceDoubleClicked -= Handle;
            DLJ_InfoPanelEvents.PieceDoubleClicked += Handle;
        }

        protected override void OnDisarm()
        {
            DLJ_InfoPanelEvents.PieceDoubleClicked -= Handle;
        }

        private void Handle(LDY_Animal unit)
        {
            // 어느 기물인지는 보지 않는다. 대본이 "저 기물을 눌러라"라고 했어도,
            // 다른 기물을 눌러 정보창이 어떻게 생겼는지 안 것도 배운 것이다.
            Pass();
        }
    }
}
