using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "ContractWillData", menuName = "DLJ/Will/Contract")]
public sealed class DLJ_ContractWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Contract;

    [Min(0f)] public float holdTime = 0.3f;
}
