using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Services
{
    public class AudioService : MonoBehaviour, IAudioService
    {
        [Header("Sources")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _musicSource;        

        [Header("Audio Clips")]
        [SerializeField] private List<SoundData> _sounds;

        private bool _isSoundOn;        

        public void Activate()
        {
            _sfxSource.enabled = true;
            _musicSource.enabled = true;

            PlayMusic();
        }

        public void Deactivate()
        {
            _sfxSource.enabled = false;
            _musicSource.enabled = false;
        }

        public bool GetSoundOn()
        {
            return _isSoundOn;
        }

        public void SetSound(bool value)
        {
            _isSoundOn = value;

            //YG2.saves.IsSoundOn = value;
            //YG2.SaveProgress();

            if (_isSoundOn)
            {
                _sfxSource.enabled = true;
                _musicSource.enabled = true;                
                PlayMusic();
            }
            else
            {
                _musicSource.enabled = false;
                _sfxSource.enabled = false;
                StopMusic();
            }
        }

        public void PlaySound(SoundType type)
        {
            if (_isSoundOn)
            {
                AudioClip clip = GetClip(type);

                _sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayMusic()
        {
            if (_isSoundOn)
            {
                AudioClip clip = GetClip(SoundType.BackgroundMusic);

                _musicSource.clip = clip;
                _musicSource.loop = true;
                _musicSource.Play();
            }
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        private AudioClip GetClip(SoundType type)
        {
            var sound = _sounds.Find(s => s.Type == type);

            if (sound.Clip == null)
                Debug.Log($"AudioClip для звука {type} не назначен в инспекторе префаба!");

            return sound.Clip;
        }

        [Serializable]
        public struct SoundData
        {
            public SoundType Type;
            public AudioClip Clip;
        }
    }
}
