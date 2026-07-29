using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class KTH_CardData : ScriptableObject
{
    public string cardId;
    public string cardName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;          // 손패 카드에 쓰이는 2D 아이콘
    public int cost;

    [Header("배치 시 소환될 3D 기물")]
    public GameObject unitModelPrefab;   // 실제 필드에 나오는 3D 모델 프리팹
}
