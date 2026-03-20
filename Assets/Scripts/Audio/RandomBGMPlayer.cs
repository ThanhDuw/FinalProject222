using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKitCodeInternal 
{
    public class RandomBGMPlayer : MonoBehaviour
    {
        public AudioClip[] clips;

        // Start is called before the first frame update
void Start()
        {
            if (clips.Length == 0)
            {
                Destroy(this);
                return;
            }

            var source = GetComponent<AudioSource>();
            source.clip = clips[Random.Range(0, clips.Length)];

            // Apply persisted music volume if available
            float savedVolume = UnityEngine.PlayerPrefs.GetFloat("volume_music", 1f);
            source.volume = savedVolume;

            source.Play();
        }
    }
}

//To make the constant entering/exiting play mode in editor less annoying in this kit, we pick one of 3 random BGM
//track at random every game start