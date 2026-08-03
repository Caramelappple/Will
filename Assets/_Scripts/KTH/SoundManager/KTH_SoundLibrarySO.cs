using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/SoundLibrary", order = 2)]
public class KTH_SoundLibrarySO : ScriptableObject,KTH_ISoundRepository
{
    [SerializeField] private List<KTH_SoundData> sounds;
    private Dictionary<string, KTH_SoundData> _lookup;

    private void OnEnable()
    {
        _lookup = sounds.ToDictionary(s => s.id, s => s);
    }

    public KTH_SoundData GetSound(string id)
    {
        _lookup.TryGetValue(id, out var data);
        return data;
    }
}
