using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

[CreateAssetMenu(fileName = "CurseWillData", menuName = "DLJ/Will/Curse")]
public sealed class DLJ_CurseWillDataSO : DLJ_WillDataSO
{
    public override LSO_WillType WillType => LSO_WillType.Curse;

    [Header("System")]
    public int damage;
    public int range;
    public int duration;

    [Header("Visual")]
    public float expandTime = 0.25f;
    public float effectHeight = 0.12f;

    public override int DisplayDamage => damage;
    public override int DisplayRange => range;
    public override int DisplayDuration => duration;
}
