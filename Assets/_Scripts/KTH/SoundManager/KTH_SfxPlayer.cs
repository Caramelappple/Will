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

        audioSource.pitch = data.pitch;
        audioSource.PlayOneShot(data.clip, data.volume * sfxVolume * masterVolume);
        audioSource.clip = data.clip;

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