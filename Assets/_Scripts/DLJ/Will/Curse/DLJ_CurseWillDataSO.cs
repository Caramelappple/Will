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
    [Tooltip("바닥 MeshRenderer가 없는 파티클 전용 프리팹의 월드 크기당 배율. 바닥 메시가 있으면 칸 범위에 자동으로 맞춤.")]
    [Min(0f)]
    public float effectScalePerWorldUnit = 0.03111111f;

    [Tooltip("저주 종료 후 남은 파티클이 사라지는 시간")]
    [Min(0f)]
    public float effectFadeOutTime = 0.75f;

    public override int DisplayDamage => damage;
    public override int DisplayRange => range;
    public override int DisplayDuration => duration;
}
