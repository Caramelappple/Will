using _Scripts.LSO.Deck.Data;
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

    [Header("그리드 보드 연동")]
    [Tooltip("이 카드로 실제 전투 그리드에 소환할 동물 카드 데이터. 비워두면 그리드에는 소환되지 않고 기존 연출용 배치만 된다.")]
    public LSO_CardSO animalCard;
}
