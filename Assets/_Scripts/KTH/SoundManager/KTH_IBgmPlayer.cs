public interface KTH_IBgmPlayer
{
    void Play(KTH_BgmData data);

    void Stop();

    void SetVolume(float volume);

    void SetMasterVolume(float volume);
}