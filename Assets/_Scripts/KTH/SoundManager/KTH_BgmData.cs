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

    [Range(0f, 1f)]
    public float pitch = 1f;

    public bool loop = true;
}