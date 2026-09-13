using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 커서가 올라갈 때(그리고 원하면 벗어날 때) 효과음을 낸다.
    ///
    /// 소리를 어떻게 재생하는지는 IAudioService가 안다.
    /// 여기서는 "언제" 낼지만 정하므로, 사운드 구현이 바뀌어도 이 파일은 그대로다.
    ///
    /// 사운드가 없어도 조용히 넘어간다. 서비스가 등록되지 않은 씬에서는
    /// NullAudioService가 대신 받으므로 예외도 나지 않는다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverSoundEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Tooltip("커서가 올라갈 때 재생할 효과음. 비워두면 아무 소리도 안 난다.")]
        [SerializeField] private SoundClipSO enterSfx;

        [Tooltip("커서가 벗어날 때도 소리를 낼지. 보통은 꺼두는 편이 덜 시끄럽다.")]
        [SerializeField] private bool playOnExit;

        [SerializeField] private SoundClipSO exitSfx;

        [Tooltip("0이면 겹쳐서 난다. 1 이상이면 그 채널의 앞선 소리를 끊고 낸다.")]
        [SerializeField, Min(0)] private int channel;

        [Tooltip("이 간격 안에는 다시 재생하지 않는다.\n" +
                 "버튼 경계에서 커서가 떨릴 때 소리가 드르륵 이어지는 걸 막는다.")]
        [SerializeField, Min(0f)] private float cooldown = 0.05f;

        private float _lastPlayTime = float.NegativeInfinity;

        public void OnHoverEnter()
        {
            Play(enterSfx);
        }

        public void OnHoverExit()
        {
            if (!playOnExit) return;

            Play(exitSfx);
        }

        private void Play(SoundClipSO clip)
        {
            if (clip == null) return;
            if (Time.unscaledTime - _lastPlayTime < cooldown) return;

            _lastPlayTime = Time.unscaledTime;

            ServiceLocator.Get<IAudioService>()?.PlaySfx(clip, channel);
        }
    }
}
