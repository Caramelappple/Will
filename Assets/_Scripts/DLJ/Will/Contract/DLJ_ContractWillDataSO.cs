using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "ContractWillData", menuName = "DLJ/Will/Contract")]
public sealed class DLJ_ContractWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Contract;

    [Tooltip("이펙트 최소 유지 시간. 파티클 재생 시간이 더 길면 끝까지 유지.")]
    [Min(0f)] public float holdTime = 0.3f;

    [Header("환급 금화")]
    public Mesh coinMesh;
    public Material goldMaterial;
    [Min(0.01f)] public float coinDiameter = 0.28f;
    [Min(0.01f)] public float scatterRadius = 0.6f;
    [Min(0.05f)] public float collectionDuration = 0.65f;
    [Min(0f)] public float collectionInterval = 0.12f;
}
