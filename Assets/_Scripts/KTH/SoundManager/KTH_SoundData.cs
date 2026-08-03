using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Audio/SoundData", order = 1)]
public class KTH_SoundData : ScriptableObject
{
    public string id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;
}
