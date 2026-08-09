using System.Collections.Generic;
using UnityEngine;

public class KTH_AudioSourcePool : MonoBehaviour
{
    [SerializeField] private AudioSource prefab;
    [SerializeField] private int initialSize = 5;

    private readonly Queue<AudioSource> pool = new Queue<AudioSource>();

    private void Awake()
    {
        // MonoBehaviour에서는 생성자(new) 대신 Awake/Start에서 초기화해야 합니다.
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        AudioSource instance;

        if (prefab != null)
        {
            instance = Instantiate(prefab, transform);
        }
        else
        {
            // 프리팹이 없을 경우 새로 빈 오브젝트 생성
            GameObject go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform);
            instance = go.AddComponent<AudioSource>();
        }

        instance.gameObject.SetActive(false);
        pool.Enqueue(instance);
        return instance;
    }

    public AudioSource Get()
    {
        AudioSource instance;

        if (pool.Count > 0)
        {
            instance = pool.Dequeue();
        }
        else
        {
            instance = CreateNewAudioSource();
        }

        if (instance != null)
        {
            instance.gameObject.SetActive(true);
        }

        return instance;
    }

    public void Return(AudioSource instance)
    {
        if (instance == null) return;

        instance.Stop();
        instance.clip = null;
        instance.gameObject.SetActive(false);

        // 중복 반환 방지
        if (!pool.Contains(instance))
        {
            pool.Enqueue(instance);
        }
    }
}