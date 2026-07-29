//이거 사용
using UnityEngine;

[CreateAssetMenu(fileName = "DamageableResourceSO", menuName = "SO/DamageableResourceSO")]
public class DamageableResourceSO : ScriptableObject
{
    [field: SerializeField] public int MaxValue { get; private set; }
    [field: SerializeField] public int MinValue { get; private set; }
    [field: SerializeField] public int StartValue { get; private set; }
}
