using UnityEngine;
using UnityEngine.Audio;

namespace hunt
{
    public class AudioHelper : MonoBehaviourSingleton<AudioHelper>
    {
        [Header("AUDIO MIXER")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        protected override bool DontDestroy => base.DontDestroy;
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        public void PlaySfx()
        {

        }

        public void PlayBgm()
        {

        }


    }
}
