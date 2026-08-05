public interface KTH_ISfxPlayer
{
    void Play(KTH_SfxData data);

    void Stop();

    void SetVolume(float volume);

    void SetMasterVolume(float volume);
}