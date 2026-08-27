using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "SacrificeWillData", menuName = "DLJ/Will/Sacrifice")]
public sealed class DLJ_SacrificeWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Sacrifice;

    [Min(0f)] public float holdTime = 0.3f;
}
