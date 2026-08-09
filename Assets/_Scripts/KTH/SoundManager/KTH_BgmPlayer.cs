using UnityEngine;

public class KTH_BgmPlayer : MonoBehaviour, KTH_IBgmPlayer
{
    [SerializeField] private AudioSource audioSource;

    private float bgmVolume = 1f;
    private float masterVolume = 1f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Play(KTH_BgmData data)
    {
        if (data == null || data.clip == null)
        {
            Debug.LogWarning("[KTH_BgmPlayer] BGM Data 또는 Clip이 비어있습니다.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[KTH_BgmPlayer] AudioSource가 할당되지 않았습니다.");
            return;
        }

        audioSource.clip = data.clip;
        audioSource.loop = data.loop;

        if (data.randomPitch)
        {
            float min = Mathf.Min(data.minPitch, data.maxPitch);
            float max = Mathf.Max(data.minPitch, data.maxPitch);
            audioSource.pitch = Random.Range(min, max);
        }
        else
        {
            audioSource.pitch = data.pitch;
        }

        audioSource.volume = data.volume * bgmVolume * masterVolume;
        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.volume = bgmVolume * masterVolume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.volume = bgmVolume * masterVolume;
    }
}