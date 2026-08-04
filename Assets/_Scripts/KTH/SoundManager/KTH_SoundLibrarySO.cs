using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/SoundLibrary")]
public class KTH_SoundLibrarySO : ScriptableObject, KTH_ISoundRepository
{
    [Header("SFX")]
    [SerializeField]
    private List<KTH_SfxData> sfxList;

    [Header("BGM")]
    [SerializeField]
    private List<KTH_BgmData> bgmList;

    private Dictionary<SfxID, KTH_SfxData> sfxLookup;
    private Dictionary<BgmID, KTH_BgmData> bgmLookup;

    private void OnEnable()
    {
        BuildSfxDictionary();
        BuildBgmDictionary();
    }

    private void BuildSfxDictionary()
    {
        sfxLookup = new Dictionary<SfxID, KTH_SfxData>();

        foreach (var sound in sfxList)
        {
            if (sound == null)
                continue;

            if (sfxLookup.ContainsKey(sound.id))
            {
                Debug.LogError($"중복된 SFX ID : {sound.id}", sound);
                continue;
            }

            sfxLookup.Add(sound.id, sound);
        }
    }

    private void BuildBgmDictionary()
    {
        bgmLookup = new Dictionary<BgmID, KTH_BgmData>();

        foreach (var sound in bgmList)
        {
            if (sound == null)
                continue;

            if (bgmLookup.ContainsKey(sound.id))
            {
                Debug.LogError($"중복된 BGM ID : {sound.id}", sound);
                continue;
            }

            bgmLookup.Add(sound.id, sound);
        }
    }

    public KTH_SfxData GetSfx(SfxID id)
    {
        sfxLookup.TryGetValue(id, out var sound);
        return sound;
    }

    public KTH_BgmData GetBgm(BgmID id)
    {
        bgmLookup.TryGetValue(id, out var sound);
        return sound;
    }
}