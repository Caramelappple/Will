using System;
using System.Collections.Generic;
using DevLib.SoundSystem.Runtime;
using UnityEngine;
using UnityEngine.Pool;

namespace DevLib.ServiceLocator
{
    [Serializable]
    public struct NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    /// <summary>
    /// 소리를 재생하는 곳. 스피커(SoundPlayer)를 풀에서 빌려 쓴다.
    ///
    /// ── 왜 풀을 쓰나 ──────────────────────────────────────────
    /// 예전에는 소리 하나에 GameObject 를 하나 만들고 끝나면 파괴했다.
    /// 전투 중에는 타격음·특성음이 초당 여러 번 나므로 그때마다 Instantiate 와
    /// Destroy 가 돌고, 쌓인 쓰레기를 GC 가 치우면서 프레임이 튄다.
    ///
    /// 풀은 다 쓴 스피커를 끄고 쥐고 있다가 다음 소리에 다시 내준다.
    /// 만드는 일은 처음 몇 번뿐이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// BGM 은 풀에서 빌리지 않는다. 하나가 계속 물고 있어야 하는 것이라
    /// 빌리고 돌려주는 규칙에 맞지 않는다.
    /// </summary>
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("연결")]
        [Tooltip("스피커 하나짜리 프리팹. SoundPlayer 가 붙어 있어야 한다.")]
        [SerializeField] private GameObject soundPlayerPrefab;

        [Header("풀")]
        [Tooltip("미리 만들어둘 개수. 첫 전투에서 만드느라 끊기는 것을 막는다.")]
        [SerializeField, Min(0)] private int prewarm = 8;

        [Tooltip("놀고 있는 스피커를 몇 개까지 쥐고 있을지.\n" +
                 "이보다 많이 돌아오면 남는 것은 파괴한다. 한꺼번에 소리가 몰린 뒤\n" +
                 "빈 스피커를 계속 들고 있지 않게 한다.")]
        [SerializeField, Min(1)] private int maxRetained = 32;

        [Header("진단")]
        [Tooltip("켜면 빌리고 돌려줄 때마다 콘솔에 찍는다. 풀이 새는지 볼 때만 켤 것.")]
        [SerializeField] private bool logPool;

        private ObjectPool<SoundPlayer> _pool;

        /// <summary>채널을 쥐고 있는 스피커. 같은 채널로 다시 내면 앞 소리를 끊는다.</summary>
        private readonly Dictionary<int, SoundPlayer> _playerDict = new();

        /// <summary>지금 나가 있는 스피커. 서비스가 사라질 때 남김없이 걷으려고 센다.</summary>
        private readonly HashSet<SoundPlayer> _live = new();

        private SoundPlayer _bgmPlayer;

        private void Awake()
        {
            if (soundPlayerPrefab == null)
            {
                Debug.LogError($"{name}: Sound Player Prefab 이 비어 있어 소리를 낼 수 없습니다.", this);
                return;
            }

            ServiceLocator.Register<IAudioService>(this);

            _pool = new ObjectPool<SoundPlayer>(
                CreatePlayer,
                OnTake,
                OnReturn,
                OnDiscard,
                collectionCheck: true,
                defaultCapacity: Mathf.Max(1, prewarm),
                maxSize: maxRetained);

            Prewarm();

            // BGM 은 하나를 계속 쥐고 있는다. 풀의 규칙과 맞지 않아 따로 만든다.
            GameObject bgmObject = Instantiate(soundPlayerPrefab, transform);
            bgmObject.name = "BgmPlayer";
            _bgmPlayer = bgmObject.GetComponent<SoundPlayer>();

            if (_bgmPlayer == null)
                Debug.LogError($"{name}: 프리팹에 SoundPlayer 가 없습니다.", soundPlayerPrefab);
        }

        private void OnDestroy()
        {
            // 나가 있던 스피커부터 걷는다. 안 걷으면 풀이 비워져도 소리가 계속 난다.
            foreach (SoundPlayer player in new List<SoundPlayer>(_live))
            {
                if (player != null) player.ResetForPool();
            }

            _live.Clear();
            _playerDict.Clear();

            _pool?.Clear();

            ServiceLocator.Register<IAudioService>(new NullAudioService());
        }

        // =========================================================
        // 풀
        // =========================================================

        private void Prewarm()
        {
            if (prewarm <= 0) return;

            var buffer = new List<SoundPlayer>(prewarm);

            for (int i = 0; i < prewarm; i++)
                buffer.Add(_pool.Get());

            foreach (SoundPlayer player in buffer)
                _pool.Release(player);

            Log($"미리 {prewarm}개 만들어뒀습니다.");
        }

        private SoundPlayer CreatePlayer()
        {
            GameObject instance = Instantiate(soundPlayerPrefab, transform);
            instance.name = "SoundPlayer";

            SoundPlayer player = instance.GetComponent<SoundPlayer>();

            if (player == null)
            {
                Debug.LogError($"{name}: 프리팹에 SoundPlayer 가 없습니다.", soundPlayerPrefab);
                Destroy(instance);
            }

            return player;
        }

        private void OnTake(SoundPlayer player)
        {
            if (player == null) return;

            player.gameObject.SetActive(true);

            _live.Add(player);

            Log($"빌림 — 나가 있는 것 {_live.Count}개");
        }

        private void OnReturn(SoundPlayer player)
        {
            if (player == null) return;

            // 끄기 전에 비운다. 꺼진 뒤에는 코루틴도 타이머도 손댈 수 없다.
            player.ResetForPool();

            player.gameObject.SetActive(false);

            _live.Remove(player);

            Log($"돌려받음 — 나가 있는 것 {_live.Count}개");
        }

        private void OnDiscard(SoundPlayer player)
        {
            if (player == null) return;

            _live.Remove(player);

            Destroy(player.gameObject);
        }

        // =========================================================
        // 재생
        // =========================================================

        public void PlaySfx(SoundClipSO clipData, int channel = 0)
        {
            if (_pool == null || clipData == null) return;

            SoundPlayer player = _pool.Get();

            if (player == null) return;

            // 채널을 쓰면 앞 소리를 먼저 끊는다.
            // 새로 빌린 뒤에 끊는 이유는, 먼저 끊으면 그 스피커가 곧바로 풀로 돌아와
            // 방금 빌린 것과 같은 것이 될 수 있기 때문이다.
            if (channel > 0)
            {
                if (_playerDict.TryGetValue(channel, out SoundPlayer old) &&
                    old != null && old != player)
                {
                    ReturnToPool(old);
                }

                _playerDict[channel] = player;
            }

            player.OnSoundFinished += HandleSoundFinish;

            player.PlaySound(clipData);
        }

        public void StopSfx(int channel)
        {
            if (!_playerDict.TryGetValue(channel, out SoundPlayer player)) return;

            _playerDict.Remove(channel);

            ReturnToPool(player);
        }

        private void HandleSoundFinish(SoundPlayer player)
        {
            ReturnToPool(player);
        }

        /// <summary>
        /// 풀로 돌려보낸다. 두 번 돌려보내지 않게 나가 있는 목록으로 거른다.
        ///
        /// 중복 반납은 같은 스피커가 두 곳에서 동시에 쓰이는 모양으로 드러나서
        /// 원인을 찾기가 대단히 어렵다.
        /// </summary>
        private void ReturnToPool(SoundPlayer player)
        {
            if (player == null) return;
            if (!_live.Contains(player)) return;

            player.OnSoundFinished -= HandleSoundFinish;

            // 채널이 이 스피커를 물고 있으면 놓아준다.
            // 돌면서 지우지 않는다 — 열쇠를 먼저 찾아두고 빠져나온 뒤에 지운다.
            int held = 0;

            foreach (KeyValuePair<int, SoundPlayer> pair in _playerDict)
            {
                if (pair.Value != player) continue;

                held = pair.Key;
                break;
            }

            if (held != 0) _playerDict.Remove(held);

            _pool.Release(player);
        }

        // =========================================================
        // BGM
        // =========================================================

        public void PlayBgm(SoundClipSO bgmSound)
        {
            if (_bgmPlayer == null) return;

            _bgmPlayer.ForceStopSound();
            _bgmPlayer.PlaySound(bgmSound);
        }

        public void StopBgm()
        {
            if (_bgmPlayer == null) return;

            _bgmPlayer.ForceStopSound();
        }

        private void Log(string message)
        {
            if (logPool) Debug.Log($"[{name}] {message}", this);
        }
    }
}
