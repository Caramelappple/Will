using UnityEngine;

public class KTH_SfxPlayer : MonoBehaviour, KTH_IAudioPlayer
{
    [SerializeField] private AudioSource audioSource;

    private float sfxVolume = 1f;
    private float masterVolume = 1f;

    public void Play(KTH_SoundData data)
    {
        float finalVolume = data.volume * sfxVolume * masterVolume;
        audioSource.PlayOneShot(data.clip, finalVolume);
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }
}