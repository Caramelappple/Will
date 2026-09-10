using UnityEngine;

// 기물 호버 애니메이션들을 여기 모아 관리한다.
// 호버 감지는 KTH_PiecesHoverEffect가 맡고, 이 클래스는 재생만 한다.
public class KTH_PiecesHoveringAnimation : MonoBehaviour
{


    public void PlayHoverEnter()
    {
        Debug.Log("PlayHoverEnter");
    }

    public void PlayHoverExit()
    {
        Debug.Log("PlayHoverExit");
    }

    private void HoverAnimation()
    {

    }
}
