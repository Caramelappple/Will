using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    public class NullAudioService : IAudioService
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void SetDefaultService()
        {
            ServiceLocator.Register<IAudioService>(new NullAudioService());
        }
        public void PlaySfx(SoundClipSO clipData, int channel = 0) { }
        public void StopSfx(int channel) { }
        public void PlayBgm(SoundClipSO bgmSound) { }
        public void StopBgm() { }
    }
}