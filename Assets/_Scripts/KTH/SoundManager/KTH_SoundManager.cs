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
        // 1. 싱글톤 중복 검사 및 즉시 파괴
        if (Instance != null && Instance != this)
        {
            // 중복 생성된 사운드 매니저는 즉시 파괴 후 실행 중단
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2. 컴포넌트 자동 탐색 (인스펙터 누락 방지)
        if (sfxPlayer == null) sfxPlayer = GetComponentInChildren<KTH_SfxPlayer>();
        if (bgmPlayer == null) bgmPlayer = GetComponentInChildren<KTH_BgmPlayer>();

        repository = soundLibrary;
        sfx = sfxPlayer;
        bgm = bgmPlayer;
    }

    #region Play

    public void PlaySfx(SfxID id)
    {
        if (sfx == null)
        {
            Debug.LogWarning("[KTH_SoundManager] sfxPlayer가 연결되어 있지 않습니다.");
            return;
        }

        if (repository == null)
        {
            Debug.LogWarning("[KTH_SoundManager] soundLibrary(Repository)가 연결되어 있지 않습니다.");
            return;
        }

        var data = repository.GetSfx(id);
        if (data == null)
        {
            Debug.LogWarning($"[KTH_SoundManager] SFX를 찾을 수 없습니다 : {id}");
            return;
        }

        sfx.Play(data);
    }

    public void PlayBgm(BgmID id)
    {
        if (bgm == null)
        {
            Debug.LogWarning("[KTH_SoundManager] bgmPlayer가 연결되어 있지 않습니다.");
            return;
        }

        if (repository == null)
        {
            Debug.LogWarning("[KTH_SoundManager] soundLibrary(Repository)가 연결되어 있지 않습니다.");
            return;
        }

        var data = repository.GetBgm(id);
        if (data == null)
        {
            Debug.LogWarning($"[KTH_SoundManager] BGM을 찾을 수 없습니다 : {id}");
            return;
        }

        bgm.Play(data);
    }

    public void StopBgm()
    {
        bgm?.Stop();
    }

    public void StopSfx()
    {
        sfx?.Stop();
    }

    #endregion

    #region Volume

    public void SetMasterVolume(float value)
    {
        // 자기 자신을 다시 호출하는 일이 없도록 순수 컴포넌트에만 전달
        if (sfx != null && sfx != (object)this) sfx.SetMasterVolume(value);
        if (bgm != null && bgm != (object)this) bgm.SetMasterVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        if (sfx != null && sfx != (object)this) sfx.SetVolume(value);
    }

    public void SetBgmVolume(float value)
    {
        if (bgm != null && bgm != (object)this) bgm.SetVolume(value);
    }

    #endregion
}