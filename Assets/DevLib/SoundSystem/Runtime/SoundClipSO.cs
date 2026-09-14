using UnityEngine;

namespace DevLib.SoundSystem.Runtime
{
    public enum AudioType
    {
        Sfx, Music
    }
    
    [CreateAssetMenu(fileName = "Clip data", menuName = "Lib/Sound/Clip data", order = 0)]
    public class SoundClipSO : ScriptableObject
    {
        public AudioType audioType;
        public AudioClip clip;
        public bool isLoop;
        public bool randomizePitch;

        [Range(0f, 1f)] public float randomPitchModifier = 0.1f;
        [Range(0f, 1f)] public float volume  = 1f;
        [Range(0f, 1f)] public float pitch = 1f;

        public float startTime;
        public float endTime;

    }
}