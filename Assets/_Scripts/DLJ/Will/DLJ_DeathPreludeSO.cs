using UnityEngine;

[CreateAssetMenu(fileName = "DLJ_DeathPrelude", menuName = "DLJ/Will/Effects/Death Prelude")]
public sealed class DLJ_DeathPreludeSO : ScriptableObject
{
    [Tooltip("사망 직후 바닥에 드러날 유언 문양.")]
    public Sprite sigilSprite;
    public Color sigilColor = Color.white;
    [Min(0f)] public float silenceDuration = 0.18f;
    [Min(0f)] public float revealDuration = 0.25f;
    [Min(0f)] public float holdDuration = 0.3f;
    [Min(0f)] public float fadeDuration = 0.35f;
}
