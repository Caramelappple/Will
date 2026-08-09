using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class KTH_AudioSourcePool : MonoBehaviour
{
    [SerializeField]private AudioSource audioSourcePrefabs;
    [SerializeField]private int initialPoolSize = 10;

    private readonly Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();

    private void Awake()
    {
        if (audioSourcePrefabs == null)
        {
            Debug.LogError($"{name}: Audio Source Prefabs가 연결되지 않았습니다.", this);
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
            Create();

    }

    private AudioSource Create()
    {
        if (audioSourcePrefabs == null) return null;

        AudioSource audio = Instantiate(audioSourcePrefabs, transform);
        audio.gameObject.SetActive(false);

        audioSourcePool.Enqueue(audio);

        return audio;
    }

    public AudioSource Get()
    {
        if (audioSourcePool.Count == 0)
            Create();

        // 프리팹이 없으면 Create가 아무것도 넣지 못한다. 그대로 Dequeue하면 예외가 난다.
        if (audioSourcePool.Count == 0) return null;

        AudioSource audio = audioSourcePool.Dequeue();
        audio.gameObject.SetActive(true);

        return audio;
    }

    public void Return(AudioSource source)
    {
        if (source == null) return;

        source.Stop();

        source.clip = null;
        source.volume = 1f;
        source.pitch = 1f;
        source.loop = false;

        source.gameObject.SetActive(false);

        audioSourcePool.Enqueue(source);
    }
}
