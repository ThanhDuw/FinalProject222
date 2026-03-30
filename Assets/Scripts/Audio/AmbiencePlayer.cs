using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKitCodeInternal 
{
    public class AmbiencePlayer : MonoBehaviour
    {
        static AmbiencePlayer s_Instance;
    
        public AudioSource FarAudioSource;
        public AudioSource CloseAudioSource;

        // ── Master volume (controlled by AudioVolumeController) ──────────────
        private float m_masterVolume = 1f;
        private float m_farRatio     = 0f;
        private float m_closeRatio   = 1f;

        void Awake()
        {
            s_Instance = this;
        }

        /// <summary>
        /// Called by AudioVolumeController to set the overall ambience volume.
        /// Preserves the current Far/Close ratio set by camera zoom.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public static void UpdateVolume(float zoomRatio)
        {
            s_Instance.m_farRatio   = zoomRatio;
            s_Instance.m_closeRatio = 1.0f - zoomRatio;
            s_Instance.ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            if (FarAudioSource   != null) FarAudioSource.volume   = m_farRatio   * m_masterVolume;
            if (CloseAudioSource != null) CloseAudioSource.volume = m_closeRatio * m_masterVolume;
        }
    }
}