using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 클릭했을 때 효과음을 낸다.
    ///
    /// 소리를 어떻게 재생하는지는 IAudioService가 안다.
    /// 여기서는 "언제" 낼지만 정하므로, 사운드 구현이 바뀌어도 이 파일은 그대로다.
    ///
    /// 사운드가 없어도 조용히 넘어간다. 서비스가 등록되지 않은 씬에서는
    /// NullAudioService가 대신 받으므로 예외도 나지 않는다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public class LSO_ClickSoundEffect : MonoBehaviour, LSO_IClickEffect
    {
        [Tooltip("클릭했을 때 재생할 효과음. 비워두면 아무 소리도 안 난다.")]
        [SerializeField] private SoundClipSO clickSfx;

        [Tooltip("0이면 겹쳐서 난다. 1 이상이면 그 채널의 앞선 소리를 끊고 낸다.\n" +
                 "빠르게 연타할 때 소리가 뭉치는 것을 막고 싶으면 채널을 준다.")]
        [SerializeField, Min(0)] private int channel;

        public void OnClick()
        {
            // 버튼이 비활성이면 LSO_ButtonClickHandler가 애초에 부르지 않는다.
            if (clickSfx == null) return;

            ServiceLocator.Get<IAudioService>()?.PlaySfx(clickSfx, channel);
        }
    }
}
