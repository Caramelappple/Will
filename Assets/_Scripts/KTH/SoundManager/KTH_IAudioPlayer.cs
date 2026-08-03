public interface KTH_IAudioPlayer
{
    void Play(KTH_SoundData data);
    void Stop();

    void SetVolume(float volume);
    void SetMasterVolume(float volume);
}