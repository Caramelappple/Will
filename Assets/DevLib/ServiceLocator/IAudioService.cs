using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace DevLib.ServiceLocator
{
    public interface IAudioService
    {
        void PlaySfx(SoundClipSO clipData, int channel = 0);
        void StopSfx(int channel);
        
        void PlayBgm(SoundClipSO bgmSound);
        void StopBgm();
    }
}