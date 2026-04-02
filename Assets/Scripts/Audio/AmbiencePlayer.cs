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


        private float m_masterVolume = 1f;
        private float m_farRatio     = 0f;
        private float m_closeRatio   = 1f;

        void Awake()
        {
            s_Instance = this;
        }

        void Start()
        {
            // Tự khởi tạo từ cài đặt đã lưu để đảm bảo âm lượng chính xác ngay cả khi chưa mở bảng Options.
            m_masterVolume = AudioVolumeController.MusicVolume;
            ApplyVolumes();
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
            SetMasterVolume(volume);
        }

        /// <summary>
        /// Được gọi bởi AudioVolumeController để thiết lập tổng âm lượng môi trường.
        /// Giữ nguyên tỷ lệ Far/Close được đặt bởi độ thu phóng của camera.
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