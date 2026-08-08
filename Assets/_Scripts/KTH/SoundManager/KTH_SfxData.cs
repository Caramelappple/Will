using UnityEngine;
public enum SfxID
{
    Jump,
    Hit,
    Explosion,
    Attack
}   

[CreateAssetMenu(fileName = "SfxData", menuName = "Audio/SFX Data")]
public class KTH_SfxData : ScriptableObject
{
    public SfxID id;

    public AudioClip clip;

    [Range(0f, 1f)] 
    public float volume = 1f;

    [Header("Pitch")]
    public bool randomPitch = false;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Header("Random Pitch")]
    [Min(0.1f)]
    public float minPitch = 0.95f;

    [Min(0.1f)]
    public float maxPitch = 1.05f;
}