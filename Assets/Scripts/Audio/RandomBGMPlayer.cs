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

//To make the constant entering/exiting play mode in editor less annoying in this kit, we pick one of 3 random BGM
//track at random every game start