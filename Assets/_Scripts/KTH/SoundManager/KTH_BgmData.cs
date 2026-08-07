using UnityEngine;

public enum BgmID
{
    Title,
    Stage1,
    Boss,
    Ending
}
[CreateAssetMenu(fileName = "BgmData", menuName = "Audio/BGM Data")]
public class KTH_BgmData : ScriptableObject
{
    public BgmID id;

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

    [Header("Loop")]
    public bool loop = true;
}