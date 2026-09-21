using System.Collections.Generic;
using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEngine;

namespace _Scripts.LSO.Sound
{
    /// <summary>게임의 재생 요청을 기존 IAudioService에 전달한다. 클립 설정은 SO에서 편집한다.</summary>
    public static class LSO_GameAudio
    {
        public const int BullMovementChannel = 103;
        private static readonly Dictionary<LSO_SoundCue, SoundClipSO> Clips = new();
        private static readonly Dictionary<LSO_SoundCue, float> LastPlayed = new();
        private static SoundClipSO _music;
        private static IAudioService _musicService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Clips.Clear();
            LastPlayed.Clear();
            _music = null;
            _musicService = null;
        }

        public static SoundClipSO Clip(LSO_SoundCue cue)
        {
            if (Clips.TryGetValue(cue, out SoundClipSO clip)) return clip;
            clip = Resources.Load<SoundClipSO>("WillAudio/Clips/" + cue);
            Clips[cue] = clip;
            if (clip == null || clip.clip == null)
                Debug.LogError($"[Will Audio] {cue} 음원 연결이 없습니다.");
            return clip;
        }

        public static void Play(LSO_SoundCue cue, int channel = 0, float interval = 0.08f)
        {
            if (!Application.isPlaying || !LSO_AudioRuntime.Ensure()) return;
            // 같은 프레임의 다중 기물·연쇄 효과가 수십 겹으로 쌓이지 않게 제한한다.
            if (LastPlayed.TryGetValue(cue, out float last) && Time.unscaledTime - last < interval) return;
            SoundClipSO clip = Clip(cue);
            if (clip == null || clip.clip == null) return;
            LastPlayed[cue] = Time.unscaledTime;
            if (channel == 0 && cue == LSO_SoundCue.TurnChange) channel = 101;
            ServiceLocator.Get<IAudioService>()?.PlaySfx(clip, channel);
        }

        public static void Music(LSO_SoundCue cue)
        {
            if (!Application.isPlaying || !LSO_AudioRuntime.Ensure()) return;
            SoundClipSO clip = Clip(cue);
            IAudioService service = ServiceLocator.Get<IAudioService>();
            if (clip == null || clip.clip == null || service == null || service is NullAudioService) return;
            if (_music == clip && ReferenceEquals(_musicService, service)) return;
            _music = clip;
            _musicService = service;
            service.PlayBgm(clip);
        }

        public static void StopMusic()
        {
            if (Application.isPlaying)
                ServiceLocator.Get<IAudioService>()?.StopBgm();
            _music = null;
            _musicService = null;
        }

        public static void Stop(int channel)
        {
            if (Application.isPlaying)
                ServiceLocator.Get<IAudioService>()?.StopSfx(channel);
        }
    }
}
