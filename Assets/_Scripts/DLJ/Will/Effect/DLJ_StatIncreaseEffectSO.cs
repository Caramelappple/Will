using UnityEngine;

[CreateAssetMenu(
    fileName = "DLJ_StatIncreaseEffectSO",
    menuName = "DLJ/Will/Effects/Stat Increase")]
public sealed class DLJ_StatIncreaseEffectSO : ScriptableObject
{
    [Tooltip("유언 효과로 능력치가 증가한 기물 위에 재생할 파티클 프리팹.")]
    public GameObject effectPrefab;
}
