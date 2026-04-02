using System;
using System.Collections.Generic;
using CreatorKitCode;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CreatorKitCode 
{
    /// <summary>
    /// Lớp xử lý tất cả các hiệu ứng âm thanh (SFX). Thông qua các hàm của lớp này, bạn có thể phát SFX của một loại cụ thể tại một vị trí cụ thể.
    /// Nó sử dụng pooling để tạo trước tất cả các AudioSource và tái sử dụng chúng nhằm mục đích tối ưu hiệu suất.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {

        public enum Use
        {
            Player,
            Enemies,
            WorldSound,
            Sound2D
        }

        /// <summary>
        /// Lưu trữ tất cả dữ liệu được sử dụng để phát âm thanh. Cao độ (pitch) sẽ được chọn ngẫu nhiên giữa PitchMin và PitchMax.
        /// </summary>
        public class PlayData
        {
            public AudioClip Clip;
            public Vector3 Position = Vector3.zero;

            public float PitchMin = 1.0f;
            public float PitchMax = 1.0f;

            public float Volume = 1.0f;
        }
    
        public static SFXManager Instance { get; private set; }

        public AudioListener Listener;
        public Transform ListenerTarget;

        [Header("Defaults")]
        public AudioClip[] DefaultSwingSound;
        public AudioClip[] DefaultHitSound;
        public AudioClip DefaultItemUsedSound;
        public AudioClip DefaultItemEquipedSound;
        public AudioClip DefaultPickupSound;
        public AudioClip ButtonClickSound;
    
        public static AudioClip ItemUsedSound => Instance.DefaultItemUsedSound;
        public static AudioClip ItemEquippedSound => Instance.DefaultItemEquipedSound;
        public static AudioClip PickupSound => Instance.DefaultPickupSound;
    
        [SerializeField]
        AudioSource[] m_Prefabs;
        [SerializeField]
        int[] m_PoolAmount;
    
        Queue<AudioSource>[] m_Instances;

        void Awake()
        {
            Instance = this;
            m_Instances = new Queue<AudioSource>[m_Prefabs.Length];
            for (int i = 0; i < m_Prefabs.Length; ++i)
            {
                m_Instances[i] = new Queue<AudioSource>();

                for (int k = 0; k < m_PoolAmount[i]; ++k)
                {
                    var audioSource = Instantiate(m_Prefabs[i]);

                    m_Instances[i].Enqueue(audioSource);
                }
            }
        }

        void Reset()
        {
            m_Prefabs = new AudioSource[Enum.GetValues(typeof(Use)).Length];
            m_PoolAmount = new int[m_Prefabs.Length];
        }

        void Update()
        {
            Listener.transform.position = ListenerTarget.transform.position;
        }

        /// <summary>
        /// Lấy một AudioSource của loại cụ thể. Bạn sẽ hiếm khi gọi hàm này trực tiếp mà thay vào đó nên sử dụng PlaySound.
        /// </summary>
        /// <param name="useType">Loại âm thanh (tương ứng với một mixer cụ thể)</param>
        /// <returns>AudioSource ở đầu hàng đợi pool hiện tại cho loại âm thanh đã cho</returns>
        public static AudioSource GetSource(Use useType)
        {
            var s = Instance.m_Instances[(int)useType].Dequeue();
            Instance.m_Instances[(int)useType].Enqueue(s);

            return s;
        }

        /// <summary>
        /// Phát âm thanh của một loại tùy chỉnh bằng thông tin trong PlayData. Hàm này sẽ tự động lấy
        /// một AudioSource của loại tương ứng.
        /// </summary>
        /// <param name="useType">Loại âm thanh (tương ứng với một mixer cụ thể)</param>
        /// <param name="data">PlayData chứa tất cả dữ liệu của âm thanh cần phát (clip, âm lượng, vị trí v.v.)</param>
public static void PlaySound(Use useType, PlayData data)
        {
            var source = GetSource(useType);

            source.clip = data.Clip;
            source.gameObject.transform.position = data.Position;
            source.pitch = Random.Range(data.PitchMin, data.PitchMax);
            // Tính toán âm lượng dựa trên SFX toàn cục từ AudioVolumeController
            source.volume = data.Volume * AudioVolumeController.SFXVolume;
        
            source.Play();
        }

        public static AudioClip GetDefaultSwingSound()
        {
            var clipArray = Instance.DefaultSwingSound;

            return clipArray[Random.Range(0, clipArray.Length)];
        }
    
        public static AudioClip GetDefaultHit()
        {
            var clipArray = Instance.DefaultHitSound;

            return clipArray[Random.Range(0, clipArray.Length)];
        }

        /// <summary>
        /// Phát hiệu ứng âm thanh click nút cho giao diện người dùng (âm thanh 2D, tuân theo cài đặt âm lượng SFX).
        /// </summary>
        public static void PlayButtonClick()
        {
            if (Instance == null || Instance.ButtonClickSound == null) return;
            PlaySound(Use.Sound2D, new PlayData
            {
                Clip = Instance.ButtonClickSound,
                Volume = 1.0f
            });
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SFXManager))]
public class SFXManagerEditor : Editor
{
    SerializedProperty m_PrefabsArrayProp;
    SerializedProperty m_PoolAmountProp;
    SerializedProperty m_ListenerProp;
    SerializedProperty m_ListenerTargetProp;
    SerializedProperty m_DefaultSwingSoundProp;
    SerializedProperty m_DefaultHitSoundProp;
    SerializedProperty m_DefaultItemUsedSound;
    SerializedProperty m_DefaultItemEquippedSound;
    SerializedProperty m_DefaultPickupSoundProp;
    SerializedProperty m_ButtonClickSoundProp;
    
    void OnEnable()
    {
        m_PrefabsArrayProp = serializedObject.FindProperty("m_Prefabs");
        m_PoolAmountProp = serializedObject.FindProperty("m_PoolAmount");

        m_ListenerProp = serializedObject.FindProperty(nameof(SFXManager.Listener));
        m_ListenerTargetProp = serializedObject.FindProperty(nameof(SFXManager.ListenerTarget));

        m_DefaultSwingSoundProp = serializedObject.FindProperty(nameof(SFXManager.DefaultSwingSound));
        m_DefaultHitSoundProp = serializedObject.FindProperty(nameof(SFXManager.DefaultHitSound));
        m_DefaultItemUsedSound = serializedObject.FindProperty(nameof(SFXManager.DefaultItemUsedSound));
        m_DefaultItemEquippedSound = serializedObject.FindProperty(nameof(SFXManager.DefaultItemEquipedSound));
        m_DefaultPickupSoundProp = serializedObject.FindProperty(nameof(SFXManager.DefaultPickupSound));
        m_ButtonClickSoundProp = serializedObject.FindProperty(nameof(SFXManager.ButtonClickSound));
        
        int useSize = Enum.GetValues(typeof(SFXManager.Use)).Length;
        if (m_PrefabsArrayProp.arraySize != useSize)
            m_PrefabsArrayProp.arraySize = useSize;
        if (m_PoolAmountProp.arraySize != useSize)
            m_PoolAmountProp.arraySize = useSize;

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.LabelField("Listener Info");
        EditorGUILayout.PropertyField(m_ListenerProp);
        EditorGUILayout.PropertyField(m_ListenerTargetProp);

        EditorGUILayout.PropertyField(m_DefaultSwingSoundProp, true);
        EditorGUILayout.PropertyField(m_DefaultHitSoundProp, true);
        EditorGUILayout.PropertyField(m_DefaultItemUsedSound);
        EditorGUILayout.PropertyField(m_DefaultItemEquippedSound);
        EditorGUILayout.PropertyField(m_DefaultPickupSoundProp);
        EditorGUILayout.PropertyField(m_ButtonClickSoundProp);
        
        EditorGUILayout.LabelField("Prefab Per Use");

        float saveWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 128.0f;
        for (int i = 0; i < m_PrefabsArrayProp.arraySize; ++i)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_PrefabsArrayProp.GetArrayElementAtIndex(i), new GUIContent(((SFXManager.Use)i).ToString()));
            EditorGUILayout.PropertyField(m_PoolAmountProp.GetArrayElementAtIndex(i), new GUIContent("Pool Size"));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUIUtility.labelWidth = saveWidth;
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
