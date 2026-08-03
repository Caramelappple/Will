using UnityEngine;

public class KTH_SoundManager : MonoBehaviour
{
    [SerializeField] private KTH_SoundLibrarySO library; // ISoundRepository로 주입받아도 됨
    [SerializeField] private KTH_SfxPlayer sfxPlayer;
    [SerializeField] private KTH_BgmPlayer bgmPlayer;

    public void PlaySfx(string id)
    {
        var data = library.GetSound(id);
        if (data != null) sfxPlayer.Play(data);
    }

    public void PlayBgm(string id)
    {
        var data = library.GetSound(id);
        if (data != null) bgmPlayer.Play(data);
    }
}