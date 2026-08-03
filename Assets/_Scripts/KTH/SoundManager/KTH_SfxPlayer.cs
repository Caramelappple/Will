using UnityEngine;

public class KTH_SfxPlayer : MonoBehaviour,KTH_IAudioPlayer
{
    [SerializeField] private AudioSource audioSource;

    public void Play(KTH_SoundData data)
    {
        audioSource.PlayOneShot(data.clip, data.volume);
    }

    public void Stop() => audioSource.Stop();
}
