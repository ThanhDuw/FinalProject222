using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKitCodeInternal 
{
    public class RandomBGMPlayer : MonoBehaviour
    {
        public AudioClip[] clips;
        private AudioSource m_source;

        void Start()
        {
            if (clips.Length == 0)
            {
                Destroy(this);
                return;
            }

            m_source = GetComponent<AudioSource>();
            m_source.clip = clips[Random.Range(0, clips.Length)];
            m_source.volume = AudioVolumeController.MusicVolume;
            m_source.Play();
        }

        private void OnEnable()
        {
            AudioVolumeController.OnMusicVolumeChanged += OnMusicVolumeChanged;
        }

        private void OnDisable()
        {
            AudioVolumeController.OnMusicVolumeChanged -= OnMusicVolumeChanged;
        }

        private void OnMusicVolumeChanged(float volume)
        {
            if (m_source != null)
                m_source.volume = volume;
        }
    }
}

// Để tránh gây khó chịu khi liên tục vào/ra chế độ chơi trong editor trong bộ kit này, chúng ta chọn ngẫu nhiên một track BGM mỗi khi bắt đầu game.