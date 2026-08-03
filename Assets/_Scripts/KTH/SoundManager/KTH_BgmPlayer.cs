using UnityEngine;

public class KTH_BgmPlayer : MonoBehaviour, KTH_IAudioPlayer
{
    [SerializeField] private AudioSource audioSource;

    public void Play(KTH_SoundData data)
    {
        audioSource.clip = data.clip;
        audioSource.volume = data.volume;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void Stop() => audioSource.Stop();
}
