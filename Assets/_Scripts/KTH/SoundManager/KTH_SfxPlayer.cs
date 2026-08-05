using UnityEngine;

public class KTH_SfxPlayer : MonoBehaviour, KTH_ISfxPlayer
{
    [SerializeField]
    private AudioSource audioSource;

    private float sfxVolume = 1f;
    private float masterVolume = 1f;

    public void Play(KTH_SfxData data)
    {
        audioSource.PlayOneShot(data.clip, data.volume * sfxVolume * masterVolume);
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