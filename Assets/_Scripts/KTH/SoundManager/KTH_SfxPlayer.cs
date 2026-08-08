using System.Collections;
using UnityEngine;

public class KTH_SfxPlayer : MonoBehaviour, KTH_ISfxPlayer
{
    [SerializeField]private KTH_AudioSourcePool audioSourcePool;

    private float sfxVolume = 1f;
    private float masterVolume = 1f;

    private void Awake()
    {
        // 인스펙터 연결이 빠지면 Play에서 NullReference가 난다.
        // 소리가 필요한 순간이 아니라 시작할 때 알려줘야 원인을 바로 찾는다.
        if (audioSourcePool == null)
            Debug.LogError($"{name}: Audio Source Pool이 연결되지 않았습니다.", this);
    }

    public void Play(KTH_SfxData data)
    {
        if (data == null) return;

        // 사운드 매니저가 DontDestroyOnLoad로 남는데 풀이 씬에 남아 있으면
        // 씬을 넘긴 뒤 파괴된 참조가 된다. == null 은 그 경우도 잡는다.
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