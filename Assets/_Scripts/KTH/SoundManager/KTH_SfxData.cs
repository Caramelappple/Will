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
}