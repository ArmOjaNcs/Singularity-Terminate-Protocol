namespace Core.Services
{
    public interface IAudioService
    {
        void Activate();

        void Deactivate();

        void PlaySound(SoundType type);

        void PlayMusic();

        void StopMusic();

        bool GetSoundOn();

        void SetSound(bool value);
    }
}