using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "RageWillData", menuName = "DLJ/Will/Rage")]
public sealed class DLJ_RageWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Rage;

    [Header("System")]
    public int damage;
    public int range;

    [Header("Visual")]
    public float expandTime = 0.25f;
    public float holdTime = 0.3f;
    public float effectHeight = 0.12f;

    public override int DisplayDamage => damage;
    public override int DisplayRange => range;
}
