using UnityEngine;
public enum SfxID
{
    Jump,
    Hit,
    Explosion,
    Attack,

    // 값은 반드시 뒤에만 추가할 것.
    // 중간에 끼워 넣으면 씬과 SO 에셋에 저장된 기존 값이 다른 소리로 밀린다.
    UIHover,
    UIClick
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