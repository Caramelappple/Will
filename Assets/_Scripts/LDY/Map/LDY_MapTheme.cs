using UnityEngine;
using TMPro;

// 맵 전체가 참조하는 단일 팔레트/폰트 소스 (3톤 컬러만 사용)
[CreateAssetMenu(fileName = "LDY_MapTheme", menuName = "LDY/Map/Map Theme")]
public class LDY_MapTheme : ScriptableObject
{
    [Header("베이스 - 밤하늘")]
    public Color navyBackground = Color.white;
    public Color navyPanel = new Color32(0x22, 0x1D, 0x40, 0xFF);
    public Color navyLocked = new Color32(0x23, 0x22, 0x2B, 0xFF);

    [Header("포인트 - 골드/앰버 (별빛, 강조 라인, 선택된 노드, 클리어)")]
    public Color gold = new Color32(0xD9, 0xAA, 0x4C, 0xFF);
    public Color goldDim = new Color32(0x8A, 0x6C, 0x35, 0xFF);

    [Header("보조 - 로즈/코랄 (채도 낮은 점성술 강조)")]
    public Color rose = new Color32(0xB8, 0x7B, 0x7E, 0xFF);
    public Color roseDim = new Color32(0x7A, 0x55, 0x58, 0xFF);

    [Header("텍스트")]
    public Color textOnDark = new Color32(0xE9, 0xE2, 0xD6, 0xFF);
    public Color textOnGold = new Color32(0x23, 0x1C, 0x10, 0xFF);
    public Color textLocked = new Color32(0x6E, 0x6C, 0x78, 0xFF);

    [Header("노드 투명도")]
    [Tooltip("갈 수 있는 곳(진행 가능/클리어)은 항상 1(불투명). 잠긴 곳만 이 값 적용")]
    [Range(0f, 1f)] public float lockedOpacity = 0.5f;

    [Header("폰트 (총 2종만 사용)")]
    [Tooltip("세리프 - 타이틀/헤더 전용")]
    public TMP_FontAsset headerFont;
    [Tooltip("고딕 - 본문/버튼 전용")]
    public TMP_FontAsset bodyFont;

    [Header("경로 라인 (얇은 별자리 선 + 은은한 글로우)")]
    public Color lineColor = Color.white;
    [Range(1f, 6f)] public float lineThickness = 2f;
    [Range(0f, 1f)] public float lineAlpha = 0.85f;
    [Range(0f, 1f)] public float lineGlowAlpha = 0.35f;
    [Range(1f, 12f)] public float lineGlowWidthMultiplier = 6f;

    [Header("배경 별가루 파티클")]
    public Color starColorA = new Color32(0xD9, 0xAA, 0x4C, 0xFF);
    public Color starColorB = new Color32(0xE9, 0xE2, 0xD6, 0xFF);
    [Range(10, 150)] public int maxStars = 60;
    [Tooltip("별이 아주 느리게 흘러가는 정도 (units/sec)")]
    [Range(0f, 15f)] public float starDriftSpeed = 3f;

    [Header("노드 글로우 (클리어 / 진행가능 노드 pulse)")]
    [Range(0.2f, 4f)] public float glowPulseSpeed = 1.4f;
    [Range(0f, 1f)] public float glowMinAlpha = 0.25f;
    [Range(0f, 1f)] public float glowMaxAlpha = 0.75f;
    [Tooltip("1보다 크게 올리면 URP Bloom과 함께 실제로 빛이 번짐 (Screen Space - Camera + Bloom 설정 필요). Overlay Canvas에서는 효과 없음")]
    [Range(1f, 4f)] public float glowHdrIntensity = 1f;

    [Header("타입별 글로우 색 (아이콘 색과 어울리도록 타입마다 다르게)")]
    public Color startGlow = new Color32(0xEB, 0xEB, 0xE6, 0xFF);
    public Color battleGlow = new Color32(0xE0, 0x52, 0x52, 0xFF);
    public Color eventGlow = new Color32(0xE0, 0xA0, 0x52, 0xFF);
    public Color shopGlow = new Color32(0x52, 0xC9, 0x7A, 0xFF);
    public Color bossGlow = new Color32(0xB0, 0x52, 0xE0, 0xFF);

    [Header("플레이어가 서있는 노드 글로우 (타입 색보다 우선)")]
    public Color playerGlow = new Color32(0x5A, 0xC8, 0xE8, 0xFF);

    public Color GetTypeGlowColor(LDY_NodeType type)
    {
        switch (type)
        {
            case LDY_NodeType.Start: return startGlow;
            case LDY_NodeType.Battle: return battleGlow;
            case LDY_NodeType.Event: return eventGlow;
            case LDY_NodeType.Shop: return shopGlow;
            case LDY_NodeType.Boss: return bossGlow;
            default: return gold;
        }
    }
}
