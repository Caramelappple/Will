using UnityEngine;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 손패에서 카드를 고르면 통과. 3번 챕터의 "카드 한 장을 선택해보세요"가 여기다.
    ///
    /// 어느 카드인지는 보지 않는다. 대본이 특정 카드를 가리켰더라도,
    /// 다른 카드를 골라 "카드는 이렇게 고르는 것"을 안 것도 배운 것이다.
    ///
    /// **Step 의 Allowed 에 Card Select 를 켜두어야 한다.** 안 켜면
    /// LSO_TutorialLock 이 카드 클릭을 막아서 이 관문이 영영 안 열린다.
    /// </summary>
    [CreateAssetMenu(fileName = "Gate_CardSelected", menuName = "SO/Tutorial/Gate/카드 고름")]
    public sealed class LSO_GateCardSelected : LSO_TutorialGateSO
    {
        protected override void OnArm()
        {
            KTH_HandCardLayout.CardConfirmed -= Handle;
            KTH_HandCardLayout.CardConfirmed += Handle;
        }

        protected override void OnDisarm()
        {
            KTH_HandCardLayout.CardConfirmed -= Handle;
        }

        private void Handle(KTH_HandCard card)
        {
            Pass();
        }
    }
}
