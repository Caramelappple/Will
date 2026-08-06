using UnityEngine;

public class KTH_SoundManager : MonoBehaviour
{
    public static KTH_SoundManager Instance { get; private set; }

    [Header("Repository")]
    [SerializeField] private KTH_SoundLibrarySO soundLibrary;

    [Header("Players")]
    [SerializeField] private KTH_SfxPlayer sfxPlayer;
    [SerializeField] private KTH_BgmPlayer bgmPlayer;

    private KTH_ISoundRepository repository;
    private KTH_ISfxPlayer sfx;
    private KTH_IBgmPlayer bgm;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        repository = soundLibrary;

        sfx = sfxPlayer;
        bgm = bgmPlayer;
    }

    #region Play

    public void PlaySfx(SfxID id)
    {
        var data = repository.GetSfx(id);

        if (data == null)
        {
            Debug.LogWarning($"SFX를 찾을 수 없습니다 : {id}");
            return;
        }

        sfx.Play(data);
    }

    public void PlayBgm(BgmID id)
    {
        var data = repository.GetBgm(id);

        if (data == null)
        {
            Debug.LogWarning($"BGM을 찾을 수 없습니다 : {id}");
            return;
        }

        bgm.Play(data);
    }

    public void StopBgm()
    {
        bgm.Stop();
    }

    public void StopSfx()
    {
        sfx.Stop();
    }

    #endregion

    #region Volume

    public void SetMasterVolume(float value)
    {
        sfx.SetMasterVolume(value);
        bgm.SetMasterVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        sfx.SetVolume(value);
    }

    public void SetBgmVolume(float value)
    {
        bgm.SetVolume(value);
    }

    #endregion
}