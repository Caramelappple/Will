using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace DevLib.SoundSystem.Runtime
{
    /// <summary>
    /// 소리 하나를 재생하는 스피커. 풀에서 빌려 쓰고 돌려준다.
    ///
    /// ── 풀에 들어가면 조심할 것 ───────────────────────────────
    /// 재생이 끝나기를 기다리는 타이머가 비동기로 돈다. 그런데 풀에 들어간 뒤
    /// 곧바로 다른 소리로 다시 빌려 나가면, 앞 소리의 타이머가 뒤늦게 깨어나
    /// **지금 나고 있는 소리를 끊는다.**
    ///
    /// 재생할 때마다 번호(_playToken)를 하나 올리고, 타이머는 깨어난 뒤
    /// 그 번호가 그대로인지 본다. 다르면 자기 차례가 지난 것이므로 조용히 물러난다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;

        private AudioSource _audioSource;

        /// <summary>몇 번째 재생인지. 지나간 타이머를 걸러내는 데만 쓴다.</summary>
        private int _playToken;

        public event Action<SoundPlayer> OnSoundFinished;

        /// <summary>지금 소리를 내고 있는지. 풀이 상태를 볼 때 쓴다.</summary>
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        private void Awake()
        {
            EnsureSource();
        }

        private void EnsureSource()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        }

        public void PlaySound(SoundClipSO clipData)
        {
            EnsureSource();

            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"{name}: 클립이 비어 있어 재생할 것이 없습니다.", this);

                // 빌려준 쪽이 영영 기다리지 않게 곧바로 끝났다고 알린다.
                OnSoundFinished?.Invoke(this);
                return;
            }

            // 앞 재생의 타이머를 무효로 만든다.
            int token = ++_playToken;

            if (clipData.audioType == AudioType.Sfx)
            {
                _audioSource.outputAudioMixerGroup = sfxGroup;
            }
            else if (clipData.audioType == AudioType.Music)
            {
                _audioSource.outputAudioMixerGroup = musicGroup;
            }

            _audioSource.volume = clipData.volume;
            _audioSource.pitch = clipData.pitch;

            if (clipData.randomizePitch)
            {
                _audioSource.pitch += Random.Range(-clipData.randomPitchModifier, clipData.randomPitchModifier);
            }

            _audioSource.clip = clipData.clip;
            _audioSource.loop = clipData.isLoop;

            float startTime = clipData.startTime;
            float endTime = clipData.endTime;

            _audioSource.timeSamples = Mathf.RoundToInt(startTime * clipData.clip.frequency);
            _audioSource.Play();

            if (!clipData.isLoop)
            {
                _ = DisableSoundTimer(ResolveDuration(clipData, startTime, endTime), token);
            }
        }

        /// <summary>
        /// 몇 초 뒤에 멈출지.
        ///
        /// startTime 부터 endTime 까지가 재생 구간이고, 피치를 올리면 그만큼 빨리 끝난다.
        ///
        /// endTime 이 안 잡혀 있으면(0이거나 startTime 보다 앞이면) 클립 끝까지로 본다.
        /// 새로 만든 Clip data 는 endTime 이 0이라, 그대로 두면 구간이 음수가 되어
        /// 소리가 나자마자 꺼진다.
        /// </summary>
        private float ResolveDuration(SoundClipSO clipData, float startTime, float endTime)
        {
            float stop = endTime > startTime ? endTime : clipData.clip.length;

            float pitch = Mathf.Max(0.01f, Mathf.Abs(_audioSource.pitch));

            return Mathf.Max(0f, (stop - startTime) / pitch);
        }

        /// <summary>
        /// 재생이 끝날 때까지 기다렸다 멈춘다.
        ///
        /// 깨어난 뒤 자기 차례가 맞는지 반드시 확인한다. 풀에서 재사용되면
        /// 이 타이머가 남의 소리를 끄게 된다.
        /// </summary>
        private async Task DisableSoundTimer(float time, int token)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(time);
            }
            catch (OperationCanceledException)
            {
                // 플레이 모드를 나가거나 오브젝트가 사라진 것. 할 일이 없다.
                return;
            }

            // 유니티 오브젝트는 파괴돼도 참조가 남는다. == null 로 물어봐야 한다.
            if (this == null) return;

            // 그 사이 다른 소리로 다시 빌려 나갔다. 내 차례는 지났다.
            if (token != _playToken) return;

            if (_audioSource != null) _audioSource.Stop();

            OnSoundFinished?.Invoke(this);
        }

        public void ForceStopSound()
        {
            // 돌고 있는 타이머를 무효로 만든다. 안 그러면 다음에 빌려 쓸 때 끊긴다.
            _playToken++;

            if (_audioSource != null) _audioSource.Stop();
        }

        /// <summary>
        /// 풀에 돌려주기 전에 깨끗이 비운다.
        ///
        /// 클립 참조를 놓지 않으면 다 쓴 오디오가 메모리에 붙어 있고,
        /// 구독을 놓지 않으면 지난 주인이 다음 소리의 완료 신호를 받는다.
        /// </summary>
        public void ResetForPool()
        {
            ForceStopSound();

            OnSoundFinished = null;

            if (_audioSource == null) return;

            _audioSource.clip = null;
            _audioSource.outputAudioMixerGroup = null;
            _audioSource.loop = false;
            _audioSource.pitch = 1f;
            _audioSource.volume = 1f;

            // timeSamples 는 되돌리지 않는다.
            //
            // 클립을 비운 뒤라 건드릴 대상이 없어서 유니티가 경고를 낸다.
            // 되돌릴 이유도 없다 — PlaySound 가 재생할 때마다 클립을 꽂고
            // 곧바로 이 값을 다시 잡는다.
        }
    }
}
