using System.Collections;

namespace _Scripts.LSO.UI.Transition
{
    /// <summary>
    /// 화면을 가렸다 걷는 연출.
    ///
    /// 씬을 불러오는 쪽은 "어떻게" 가려지는지 몰라야 한다.
    /// 검은 페이드든 아이리스든 와이프든 이 계약만 지키면 갈아끼울 수 있다.
    /// </summary>
    public interface LSO_IScreenFader
    {
        /// <summary>지금 화면이 가려져 있는지.</summary>
        bool IsCovered { get; }

        /// <summary>화면을 가린다. 끝날 때까지 yield할 것.</summary>
        IEnumerator Cover();

        /// <summary>화면을 걷는다. 끝날 때까지 yield할 것.</summary>
        IEnumerator Reveal();
    }
}
