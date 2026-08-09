using System.Collections;
using UnityEngine;

public class KTH_SfxPlayer : MonoBehaviour, KTH_ISfxPlayer
{
    [SerializeField] private KTH_AudioSourcePool audioSourcePool;

    private float sfxVolume = 1f;
    private float masterVolume = 1f;

    private void Awake()
    {
        if (audioSourcePool == null)
            audioSourcePool = GetComponentInChildren<KTH_AudioSourcePool>();
    }

    public void Play(KTH_SfxData data)
    {
        if (data == null || data.clip == null) return;

        if (audioSourcePool == null)
        {
            Debug.LogError($"{name}: Audio Source Pool이 없어 '{data.name}'을 재생하지 못했습니다.", this);
            return;
        }

        AudioSource audioSource = audioSourcePool.Get();
        if (audioSource == null) return;

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

        // 안전하게 클립 길이만큼 대기 후 반환하는 방식으로 변경
        StartCoroutine(ReturnRoutine(audioSource, data.clip.length));
    }

    private IEnumerator ReturnRoutine(AudioSource audio, float duration)
    {
        // clip 길이 + 여유시간 후 안전 반환 (무한 루프 방지)
        yield return new WaitForSeconds(duration + 0.1f);

        if (audio != null && audioSourcePool != null)
        {
            audioSourcePool.Return(audio);
        }
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