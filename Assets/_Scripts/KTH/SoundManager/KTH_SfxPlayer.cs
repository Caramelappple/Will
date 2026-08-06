using System.Collections;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class KTH_SfxPlayer : MonoBehaviour, KTH_ISfxPlayer
{
    [SerializeField]private KTH_AudioSourcePool audioSourcePool;

    private float sfxVolume = 1f;
    private float masterVolume = 1f;

    public void Play(KTH_SfxData data)
    {
        AudioSource audioSource = audioSourcePool.Get();

        audioSource.clip = data.clip;
        audioSource.volume = data.volume * sfxVolume * masterVolume;
        audioSource.loop = false;

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

        audioSource.Play();

        StartCoroutine(ReturnRoutine(audioSource));
    }

    private IEnumerator ReturnRoutine(AudioSource audio)
    {
        yield return new WaitWhile(() => audio.isPlaying);
        audioSourcePool.Return(audio);
    }

    public void Stop()
    {
        StopAllCoroutines();
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