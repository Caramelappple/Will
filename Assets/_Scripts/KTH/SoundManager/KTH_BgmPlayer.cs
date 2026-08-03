using UnityEngine;

public class KTH_BgmPlayer : MonoBehaviour, KTH_IAudioPlayer
{
    [SerializeField] private AudioSource audioSource;

    private float bgmVolume = 1f;
    private float masterVolume = 1f;

    public void Play(KTH_SoundData data)
    {
        audioSource.clip = data.clip;
        audioSource.loop = true;
        audioSource.volume = data.volume * bgmVolume * masterVolume;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        audioSource.volume = bgmVolume * masterVolume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        audioSource.volume = bgmVolume * masterVolume;
    }
}