using UnityEngine;

public class KTH_SoundManager : MonoBehaviour
{
    public static KTH_SoundManager Instance { get; private set; }

    [SerializeField] private KTH_SoundLibrarySO library;
    [SerializeField] private KTH_SfxPlayer sfxPlayer;
    [SerializeField] private KTH_BgmPlayer bgmPlayer;
    private void Awake()
    {
        Instance = this;
    }

    public void PlaySfx(string id)
    {
        var data = library.GetSound(id);

        if (data != null)
            sfxPlayer.Play(data);
    }

    public void PlayBgm(string id)
    {

        var data = library.GetSound(id);
        if (data != null)
            bgmPlayer.Play(data);
    }
}