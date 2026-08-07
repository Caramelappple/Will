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
        for (int i = 0; i < initialPoolSize; i++) 
            Create();
        
    }

    private AudioSource Create()
    {
        AudioSource audio = Instantiate(audioSourcePrefabs, transform);
        audio.gameObject.SetActive(false);

        audioSourcePool.Enqueue(audio);

        return audio;
    }

    public AudioSource Get()
    {
        if (audioSourcePool.Count == 0)
            Create();
        
        AudioSource audio = audioSourcePool.Dequeue();
        audio.gameObject.SetActive(true);
        
        return audio;
    }

    public void Return(AudioSource source)
    {
        source.Stop();

        source.clip = null;
        source.volume = 1f;
        source.pitch = 1f;
        source.loop = false;

        source.gameObject.SetActive(false);

        audioSourcePool.Enqueue(source);
    }
}
