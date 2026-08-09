using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 클릭했을 때 효과음을 낸다.
    ///
    /// 소리를 어떻게 재생하는지는 KTH_SoundManager가 안다.
    /// 여기서는 "언제" 낼지만 정하므로, 사운드 구현이 바뀌어도 이 파일은 그대로다.
    ///
    /// 사운드 매니저가 씬에 없으면 조용히 넘어간다. 연출이 없다고 UI가 멈추면 안 된다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public class LSO_ClickSoundEffect : MonoBehaviour, LSO_IClickEffect
    {
        [Tooltip("클릭했을 때 재생할 효과음.")]
        [SerializeField] private SfxID clickSfx = SfxID.UIClick;

        public void OnClick()
        {
            // 버튼이 비활성이면 LSO_ButtonClickHandler가 애초에 부르지 않는다.
            KTH_SoundManager manager = KTH_SoundManager.Instance;
            if (manager == null) return;

            manager.PlaySfx(clickSfx);
        }
    }
}
