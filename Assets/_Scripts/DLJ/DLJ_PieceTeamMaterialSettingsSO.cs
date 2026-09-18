using _Scripts.LDY;
using UnityEngine;

[CreateAssetMenu(menuName = "DLJ/Piece Team Materials", fileName = "DLJ_PieceTeamMaterials")]
public sealed class DLJ_PieceTeamMaterialSettingsSO : ScriptableObject
{
    public const string ResourcePath = "DLJ/DLJ_PieceTeamMaterials";

    [Tooltip("아군 기물에 사용할 머티리얼. 비우면 원래 머티리얼을 사용")]
    [SerializeField] private Material playerMaterial;
    [Tooltip("적 기물에 사용할 머티리얼. 비우면 원래 머티리얼을 사용")]
    [SerializeField] private Material enemyMaterial;

    public Material GetMaterial(LDY_Team team)
    {
        return team switch
        {
            LDY_Team.Player => playerMaterial,
            LDY_Team.Enemy => enemyMaterial,
            _ => null
        };
    }
}
