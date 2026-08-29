using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "SuccessionWillData", menuName = "DLJ/Will/Succession")]
public sealed class DLJ_SuccessionWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Succession;

    [Min(0f)] public float moveDuration = 1f;
    public DLJ_SuccessionEffectSO successionEffect;
}
